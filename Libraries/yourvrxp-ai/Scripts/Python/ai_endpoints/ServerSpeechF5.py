import os
import io
import json
import hashlib
from flask import Flask, request, send_file, jsonify
import soundfile as sf
from pydub import AudioSegment
from pathlib import Path
from transformers import pipeline
import torch
import torchaudio

# If you have your TTS code in a separate file, you can import:
#   from f5_tts.infer.utils_infer import (
#       load_vocoder,
#       load_model,
#       preprocess_ref_audio_text,
#       infer_process,
#       remove_silence_for_generated_wav
#   )
#
# or you can directly copy/paste those functions here from your `infer_gradio.py` code.

from f5_tts.model import DiT, UNetT
from f5_tts.infer.utils_infer import (
    load_vocoder,
    load_model,
    preprocess_ref_audio_text,
    infer_process,
    remove_silence_for_generated_wav,
    save_spectrogram,
)

import tempfile

device = (
    "cuda"
    if torch.cuda.is_available()
    else "mps" if torch.backends.mps.is_available() else "cpu"
)

app = Flask(__name__)
app.config['wav_voices'] = '/home/F5-TTS/wav_voices'

output_dir = "Output"
# Check if the directory exists
if not os.path.exists(output_dir):
    # If it doesn't exist, create it
    os.makedirs(output_dir)

vocoder = load_vocoder()

default_speed = 1.0
default_nfe_step = 24  # 16, 24, 32

# For example, load F5TTS (replace with E2TTS or a custom model if needed)
def load_f5tts():
    # Adjust the path(s) as needed. Example from your original code:
    # "hf://SWivid/F5-TTS/F5TTS_Base/model_1200000.safetensors"
    from cached_path import cached_path
    ckpt_path = str(cached_path("hf://SWivid/F5-TTS/F5TTS_Base/model_1200000.safetensors"))
    model_cfg = dict(dim=1024, depth=22, heads=16, ff_mult=2, text_dim=512, conv_layers=4)
    return load_model(DiT, model_cfg, ckpt_path).to(device)

def load_e2tts():
    # Adjust the path(s) as needed. Example from your original code:
    # "hf://SWivid/F5-TTS/F5TTS_Base/model_1200000.safetensors"
    from cached_path import cached_path
    ckpt_path = str(cached_path("hf://SWivid/E2-TTS/E2TTS_Base/model_1200000.safetensors"))
    model_cfg = dict(dim=1024, depth=24, heads=16, ff_mult=4)
    return load_model(UNetT, model_cfg, ckpt_path).to(device)

# F5TTS_ema_model = load_f5tts()
F5TTS_ema_model = load_e2tts()

def get_unique_id(username, length=9):
    hash_object = hashlib.sha256(username.encode())
    hash_int = int(hash_object.hexdigest(), 16)
    unique_id = hash_int % (10 ** length)
    return unique_id
    
def count_words(text):
    words = text.split()
    return len(words)

def split_text_by_dot_and_comma(text, word_limit=250):
    # Check if the number of words exceeds the limit
    if len(text) > word_limit:
        # Split the text by the dot character
        sentences = re.split(r'[.,]', text)
        # Remove any leading or trailing whitespace from each sentence
        sentences = [sentence.strip() for sentence in sentences if sentence.strip()]
        return sentences
    else:
        return text

def ends_with_dot(value):
    return value.endswith('.')
        
def split_text_by_dot_and_comma(text, word_limit=250):
    exceptions = {"Mr.", "Ms.", "Dr.", "Mrs.", "Jr.", "Sr.", "St."}

    # Check if the number of words exceeds the limit
    if len(text) > word_limit:
        sentences = []
        current_sentence = []
        
        i = 0
        while i < len(text):
            if text[i] == '.':
                # Check if the preceding characters form an exception
                if any(text[max(0, i - len(exc) + 1):i + 1] == exc for exc in exceptions):
                    current_sentence.append(text[i])
                else:
                    # Append the current sentence to sentences list
                    if current_sentence:
                        shouldAddDot = True
                        if len(text) > i+1:
                            if text[i+1] == '"' or text[i+1] == '\'':
                                shouldAddDot = False
                        if shouldAddDot:
                            current_sentence.append('.')
                        sentences.append(''.join(current_sentence).strip())
                        current_sentence = []
            elif text[i] == ',':
                # Append the current sentence to sentences list
                if current_sentence:
                    current_sentence.append(',')
                    sentences.append(''.join(current_sentence).strip())
                    current_sentence = []
            else:
                current_sentence.append(text[i])
            i += 1
        
        # Append the last sentence if any
        if current_sentence:
            sentences.append(''.join(current_sentence).strip())
        
        return sentences
    else:
        return text

def generate_tts(
    ref_audio_path: str,
    ref_text: str,
    gen_text: str,
    remove_silence: bool = False,
    cross_fade_duration: float = 0.15,
    nfe_step: int = 32,
    speed: float = 1.0,
):
    _, ref_text_out = preprocess_ref_audio_text(ref_audio_path, ref_text, show_info=print)

    # 2) TTS Inference
    final_wave, final_sample_rate, _ = infer_process(
        ref_audio_path,
        ref_text_out,
        gen_text,
        F5TTS_ema_model,
        vocoder,
        cross_fade_duration=cross_fade_duration,
        nfe_step=nfe_step,
        speed=speed,
        show_info=print,
    )

    if remove_silence:
        with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as tmp:
            sf.write(tmp.name, final_wave, final_sample_rate)
            remove_silence_for_generated_wav(tmp.name)
            wave_t, sr_t = torchaudio.load(tmp.name)
        final_wave = wave_t.squeeze().cpu().numpy()
        final_sample_rate = sr_t

    return final_wave, final_sample_rate

def infer(output_file, ref_audio_orig, ref_text, gen_text, remove_silence, cross_fade_duration, speed):

    if not ref_text.strip():
        print("No reference text provided, transcribing reference audio...")
        pipe = pipeline(
            "automatic-speech-recognition",
            model="openai/whisper-large-v3-turbo",
            torch_dtype=torch.float16,
            device=device,
        )
        ref_text = pipe(
            ref_audio_orig,
            chunk_length_s=30,
            batch_size=128,
            generate_kwargs={"task": "transcribe"},
            return_timestamps=False,
        )["text"].strip()
        print("Finished transcription")
    else:
        print("Using custom reference text...")

    # Add the functionality to ensure it ends with ". "
    if not ref_text.endswith(". ") and not ref_text.endswith("。"):
        if ref_text.endswith("."):
            ref_text += " "
        else:
            ref_text += ". "
            
    # Run the TTS
    final_wave, final_sample_rate = generate_tts(
        ref_audio_path=ref_audio_orig,
        ref_text=ref_text,
        gen_text=gen_text,
        remove_silence=remove_silence,
        cross_fade_duration=cross_fade_duration,
        nfe_step=default_nfe_step,
        speed=speed,
    )
    
    with open(output_file, "wb") as f:
        sf.write(f.name, final_wave, final_sample_rate)            

def synthesize_text(project, username, voice, speech, emotion, language, speed, remove_silence, cross_fade_duration):
    # Speech synthesis
    id_project = str(get_unique_id(project))
    path_to_voice = app.config['wav_voices'] + "/" + language + "/" + username + "/" + id_project + "/" + voice
    path_to_voice_ogg = path_to_voice + ".ogg"
    path_to_voice_wav = path_to_voice + ".wav"
    wav = None
    if os.path.exists(path_to_voice_wav) is False:
        sound_data = AudioSegment.from_ogg(path_to_voice_ogg)
        sound_data.export(path_to_voice_wav, format="wav")
    
    tmp_name_wav_file = "temp"+str(get_unique_id(speech))+".wav"
    temp_wav_file = Path(output_dir)/tmp_name_wav_file
    if (len(emotion) > 0):
        print ("Emotions = " + emotion)
        infer(temp_wav_file, path_to_voice_wav, "", speech, remove_silence, cross_fade_duration, speed)
    else:
        infer(temp_wav_file, path_to_voice_wav, "", speech, remove_silence, cross_fade_duration, speed)

    return temp_wav_file

@app.route("/ai/speech/voice", methods=["POST"])
def upload_speech_voice():
    project = request.form.get("project", "")
    username = request.form.get("username", "")
    voicename = request.form.get("voice", "")
    language = request.form.get("language", "")
    voicedata = request.files.get("file")

    print(f"++++language: {language}")

    id_project = str(get_unique_id(project))
    final_path = language + "/" + username + "/" + id_project

    # If the user does not select a file, the browser submits an empty file without a filename.
    if voicedata.filename == '':
        flash('No selected file')
        return jsonify({"success": False})
        
    if voicedata:
        user_directory = os.path.join(app.config['wav_voices'], final_path)
        if not os.path.exists(user_directory):
            os.makedirs(user_directory)
        
        filename = voicename + ".ogg"
        voicedata.save(os.path.join(user_directory, filename))
        return jsonify({"success": True})
        
    return jsonify({"success": False})

@app.route("/ai/speech", methods=["POST"])
def speech_generation() -> bytes:
        args = request.args
        prompt = request.json
        project = prompt["project"]
        username = prompt["username"]
        voice = prompt["voice"]
        speech = prompt["speech"]
        language = prompt["language"]
        emotion = prompt["emotion"]
        speed = prompt["speed"]
    
        max_length_paragraph = 1250
        silence_filename = "silence.wav"

        # Check to split text
        result = split_text_by_dot_and_comma(speech, max_length_paragraph)

        if isinstance(result, list):
            # Create the list of paragraphs below 250 words each entry
            list_paragraphs = []
            single_paragraph = ""
            for idx, sentence in enumerate(result):
                next_paragraph = single_paragraph + " " + sentence
                if len(next_paragraph) > max_length_paragraph: 
                    if len(single_paragraph) == 0:
                        list_paragraphs.append(sentence)  
                        single_paragraph = ""
                    else:
                        list_paragraphs.append(single_paragraph)  
                        single_paragraph = sentence
                    # print("++++")
                    # print(f"++++Added paragraph: {single_paragraph}")
                else:
                    single_paragraph = next_paragraph
            list_paragraphs.append(single_paragraph)      
            
            # Synthesize each of paragraphs below 250 words
            list_wavs_files = []
            for index, item_paragraph in enumerate(list_paragraphs):            
                if len(item_paragraph) > 1:
                    # print("*****")
                    # print(f"*****Synthesize paragraph: {item_paragraph}")
                    if ends_with_dot(item_paragraph) and (index + 1) < len(list_paragraphs):
                        list_wavs_files.append(silence_filename)
                        # print(f"*****Silence added for paragraph: {item_paragraph}")                        
                    list_wavs_files.append(synthesize_text(project, username, voice, item_paragraph, emotion, language, default_speed, False, 0.15))
                    
            
            # Merge the result into a single file
            output_final_wav = 'out_merger.wav'
            input_streams = [ffmpeg.input(file) for file in list_wavs_files]
            # Concatenate the input streams
            concatenated = ffmpeg.concat(*input_streams, v=0, a=1).output(output_final_wav)
            # Run the ffmpeg command
            concatenated.run()
            
            # Remove all the files generated
            for wav_file in list_wavs_files:
                if wav_file != silence_filename:
                    os.remove(wav_file)    
                
            dataaudio = AudioSegment.from_wav(output_final_wav).export(format="ogg")
            os.remove(output_final_wav)
            return dataaudio

        else:
            temp_wav_file = synthesize_text(project, username, voice, speech, emotion, language, default_speed, False, 0.15)
            dataaudio = AudioSegment.from_wav(temp_wav_file).export(format="ogg")
            os.remove(temp_wav_file)
            return dataaudio

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=6000, threaded=False)

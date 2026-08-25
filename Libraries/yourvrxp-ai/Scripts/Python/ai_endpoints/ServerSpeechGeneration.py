from flask import Flask, request, jsonify
import base64
import requests
import json
import hashlib
import torch
from TTS.api import TTS
from pydub import AudioSegment
import os
import ffmpeg
import re

app = Flask(__name__)
app.config['wav_voices'] = '/home/wav_voices/'
deviceTTS = "cuda" if torch.cuda.is_available() else "cpu"
print ("TTS MODE["+deviceTTS+"]")
print(TTS().list_models())
tts = TTS("tts_models/multilingual/multi-dataset/xtts_v2").to(deviceTTS)

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

def synthesize_text(project, username, voice, speech, emotion, language, speed):
    # Speech synthesis
    id_project = str(get_unique_id(project))
    path_to_voice = app.config['wav_voices'] + "/" + language + "/" + username + "/" + id_project + "/" + voice
    path_to_voice_ogg = path_to_voice + ".ogg"
    path_to_voice_wav = path_to_voice + ".wav"
    wav = None
    if os.path.exists(path_to_voice_wav) is False:
        sound_data = AudioSegment.from_ogg(path_to_voice_ogg)
        sound_data.export(path_to_voice_wav, format="wav")
    
    temp_wav_file = "temp"+str(get_unique_id(speech))+".wav"
    if (len(emotion) > 0):
        print ("Emotions = " + emotion)
        tts.tts_to_file(text=speech, speaker_wav=[path_to_voice_wav], language=language, emotion=emotion, speed=speed, file_path=temp_wav_file)
    else:
        tts.tts_to_file(text=speech, speaker_wav=[path_to_voice_wav], language=language, speed=speed, file_path=temp_wav_file)

    return temp_wav_file
    
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
    
        max_length_paragraph = 250
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
                    list_wavs_files.append(synthesize_text(project, username, voice, item_paragraph, emotion, language, speed))
            
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
            temp_wav_file = synthesize_text(project, username, voice, speech, emotion, language, speed)
            dataaudio = AudioSegment.from_wav(temp_wav_file).export(format="ogg")
            os.remove(temp_wav_file)
            return dataaudio

@app.route("/ai/speech/voice", methods=["POST"])
def upload_speech_voice():
        project = request.form.get("project")
        username = request.form.get("username")
        voicename = request.form.get("voice")
        language = request.form.get("language")
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

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=6000, threaded=False)
from flask import Flask, request, jsonify
import base64
import requests
import json
import hashlib
import torch
import torchaudio
from pydub import AudioSegment
import os
from audiocraft.models import AudioGen
from audiocraft.models import MusicGen
from audiocraft.data.audio import audio_write

app = Flask(__name__)
deviceTTS = "cuda" if torch.cuda.is_available() else "cpu"
model_fx = AudioGen.get_pretrained('facebook/audiogen-medium', device='cuda')
model_music = MusicGen.get_pretrained('facebook/musicgen-melody', device='cuda')
print ("CUDA MODE["+deviceTTS+"]")

def get_unique_id(username, length=9):
    hash_object = hashlib.sha256(username.encode())
    hash_int = int(hash_object.hexdigest(), 16)
    unique_id = hash_int % (10 ** length)
    return unique_id
        
@app.route("/ai/audio", methods=["POST"])
def audio_generation() -> bytes:
        args = request.args
        prompt = request.json
        description = prompt["description"]
        duration = int(prompt["duration"])

        print ("FX Description: " + description)

        # Speech synthesis
        model_fx.set_generation_params(duration=duration)
        descriptions = [description]
        wav = model_fx.generate(descriptions)
        for idx, one_wav in enumerate(wav):
            temp_wav_file = "fx"+str(get_unique_id(description))
            # Will save under {idx}.wav, with loudness normalization at -14 db LUFS.
            audio_write(f'{temp_wav_file}', one_wav.cpu(), model_fx.sample_rate, strategy="loudness", loudness_compressor=True)
        
            dataaudio = AudioSegment.from_wav(temp_wav_file+".wav").export(format="ogg")
            os.remove(temp_wav_file+".wav")
            return dataaudio

@app.route("/ai/music", methods=["POST"])
def music_generation() -> bytes:
        args = request.args
        prompt = request.json
        description = prompt["description"]
        duration = int(prompt["duration"])

        print ("Music Description: " + description)

        # Speech synthesis
        model_music.set_generation_params(duration=duration)
        descriptions = [description]
        wav = model_music.generate(descriptions)
        for idx, one_wav in enumerate(wav):
            temp_wav_file = "music"+str(get_unique_id(description))
            # Will save under {idx}.wav, with loudness normalization at -14 db LUFS.
            audio_write(f'{temp_wav_file}', one_wav.cpu(), model_music.sample_rate, strategy="loudness", loudness_compressor=True)
        
            dataaudio = AudioSegment.from_wav(temp_wav_file+".wav").export(format="ogg")
            os.remove(temp_wav_file+".wav")
            return dataaudio
    
if __name__ == '__main__':
    app.run(host='0.0.0.0', port=7000, threaded=False)
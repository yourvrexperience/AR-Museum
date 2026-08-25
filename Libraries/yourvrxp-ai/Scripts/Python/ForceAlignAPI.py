from flask import Flask, request, jsonify
import os
import traceback
import whisperx
import librosa
import numpy as np
import webrtcvad
import wave

app = Flask(__name__)

# Configure environment variables (optional, e.g., for cache directories)
os.environ["XDG_CACHE_HOME"] = "/tmp/.cache"

device_whisperx = "cpu" 
batch_size_whisperx = 8 # reduce if low on GPU mem
compute_type_whisperx = "int8" # change to "int8" if low on GPU mem (may reduce accuracy)

# model_whisperx = whisperx.load_model("large-v2", device_whisperx, compute_type=compute_type_whisperx)
        
def read_wave(path):
    """ Read a WAV file and return the PCM data and sample rate """
    with wave.open(path, "rb") as wf:
        num_channels = wf.getnchannels()  # Should be 1 for VAD
        sample_width = wf.getsampwidth()
        sample_rate = wf.getframerate()
        frames = wf.readframes(wf.getnframes())

        # WebRTC VAD only works with mono (1 channel) audio at 8kHz, 16kHz, 32kHz, or 48kHz.
        assert sample_width == 2, "Audio must be 16-bit PCM"
        assert sample_rate in [8000, 16000, 32000, 48000], "Unsupported sample rate"

        return frames, sample_rate

def get_audio_boundaries_vad(audio_path, mode=3, frame_duration=30):
    """
    Use WebRTC VAD to detect speech start and end times.
    - `mode`: 0 (most sensitive) to 3 (most aggressive noise filtering)
    - `frame_duration`: Frame size in milliseconds (10, 20, or 30)
    """
    vad = webrtcvad.Vad(mode)  # Create a VAD instance with the given mode

    frames, sample_rate = read_wave(audio_path)

    frame_size = int(sample_rate * frame_duration / 1000) * 2  # Convert ms to bytes
    num_frames = len(frames) // frame_size

    speech_start, speech_end = None, None
    for i in range(num_frames):
        start = i * frame_size
        end = start + frame_size
        is_speech = vad.is_speech(frames[start:end], sample_rate)

        if is_speech:
            if speech_start is None:
                speech_start = start / (sample_rate * 2)  # Convert bytes to seconds
            speech_end = end / (sample_rate * 2)

    return speech_start, speech_end
    
@app.route("/align", methods=["POST"])
def align_text_audio():
    try:
        # Check for required inputs in the request
        if "audio" not in request.files or "transcript" not in request.form:
            return jsonify({"error": "Missing required fields: audio and transcript"}), 400

        # Get the audio file and transcript
        audio_file = request.files["audio"]
        transcript = request.form["transcript"]
        language = request.form["language"]

        # Save the audio file temporarily
        audio_path = f"/tmp/{audio_file.filename}"
        audio_file.save(audio_path)        
        # print("filename=" + audio_file.filename)

        # Load Audio and Transcribe
        audio = whisperx.load_audio(audio_path)
        # print("LOADED AUDIO")
        
        # Get the time range where sound occurs
        start_time_audio, end_time_audio = get_audio_boundaries_vad(audio_path)
        # print("START=" + str(start_time_audio) + "::END=" + str(end_time_audio))

        # Construct the output JSON
        final_json = [{
            "text": transcript,
            "start": start_time_audio,
            "end": end_time_audio
        }]        
        # print(final_json)

        # Find phonemes
        model_a, metadata = whisperx.load_align_model(language_code=language, device=device_whisperx)
        result = whisperx.align(final_json, model_a, metadata, audio, device_whisperx, return_char_alignments=False)
        datawords = result["segments"]
        # print(result["segments"])

        # Convert alignment results to JSON format
        word_times = [
            {
                "word": word["word"],
                "starttime": f"{word.get('start', 0)}s",
                "endtime": f"{word.get('end', 0)}s"
            }
            for segment in datawords
            for word in segment["words"]
        ]
            
        # Cleanup temporary audio file
        if os.path.exists(audio_path):
            os.remove(audio_path)

        # Return the alignment results
        return jsonify({"words": word_times}), 200

    except Exception as e:
        # Handle and log errors
        error_traceback = traceback.format_exc()
        print(error_traceback)
        return jsonify({"error": str(e), "traceback": error_traceback}), 500

if __name__ == "__main__":
    # Run the Flask app
    app.run(host="0.0.0.0", port=6000)

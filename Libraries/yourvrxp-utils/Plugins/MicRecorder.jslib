mergeInto(LibraryManager.library, {
  startOggVorbisRecording: function(gameObjectNamePtr, outputMethodPtr) {
    const gameObjectName = UTF8ToString(gameObjectNamePtr);
    const outputMethod = UTF8ToString(outputMethodPtr);

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      console.error("getUserMedia not supported");
      return;
    }

    navigator.mediaDevices.getUserMedia({ audio: true }).then(stream => {
      const audioContext = new AudioContext();
      const source = audioContext.createMediaStreamSource(stream);
      const processor = audioContext.createScriptProcessor(4096, 1, 1);

      const sampleRate = audioContext.sampleRate;
      const bufferData = [];

      console.log("Recording sample rate:", sampleRate);

      processor.onaudioprocess = function(e) {
        const input = e.inputBuffer.getChannelData(0);
        bufferData.push(new Float32Array(input));
      };

      source.connect(processor);
      processor.connect(audioContext.destination);

      // Store handles
      window.__recording = {
        stream,
        processor,
        audioContext,
        bufferData,
        sampleRate,
        gameObjectName,
		outputMethod
      };
    });
  },

  stopOggVorbisRecording: function() {
    const { stream, processor, audioContext, bufferData, sampleRate, gameObjectName, outputMethod } = window.__recording;

    processor.disconnect();
    stream.getTracks().forEach(t => t.stop());
    audioContext.close();

    const merged = new Float32Array(bufferData.reduce((acc, b) => acc + b.length, 0));
    let offset = 0;
    for (const chunk of bufferData) {
      merged.set(chunk, offset);
      offset += chunk.length;
    }

	// Convert Float32Array to byte array (4 bytes per float)
    const byteArray = new Uint8Array(merged.buffer);

    // Convert to base64 string
    const binaryStr = Array.from(byteArray).map(b => String.fromCharCode(b)).join('');
    const base64 = btoa(binaryStr);

    // Send to Unity
    SendMessage(gameObjectName, outputMethod, sampleRate + "|" + base64);
  }
});

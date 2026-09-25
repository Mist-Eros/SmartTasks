window.audioRecorder = {
    mediaRecorder: null,
    chunks: [],
    stream: null,

    start: async function() {
        this.chunks = [];
        this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        this.mediaRecorder = new MediaRecorder(this.stream);
        this.mediaRecorder.ondataavailable = (e) => {
            if (e.data.size > 0) this.chunks.push(e.data);
        };
        this.mediaRecorder.start();
    },

    stop: function() {
        return new Promise((resolve) => {
            this.mediaRecorder.onstop = async () => {
                const webmBlob = new Blob(this.chunks, { type: 'audio/webm' });
                const arrayBuffer = await webmBlob.arrayBuffer();
                
                // Decode to raw PCM
                const audioContext = new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 16000 });
                const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);
                const channelData = audioBuffer.getChannelData(0);
                
                // Encode to WAV
                const wavBuffer = this.encodeWav(channelData, 16000);
                const bytes = new Uint8Array(wavBuffer);
                
                this.stream.getTracks().forEach(t => t.stop());
                resolve(Array.from(bytes));
            };
            this.mediaRecorder.stop();
        });
    },

    encodeWav: function(samples, sampleRate) {
        const buffer = new ArrayBuffer(44 + samples.length * 2);
        const view = new DataView(buffer);

        const writeString = (offset, string) => {
            for (let i = 0; i < string.length; i++)
                view.setUint8(offset + i, string.charCodeAt(i));
        };

        writeString(0, 'RIFF');
        view.setUint32(4, 36 + samples.length * 2, true);
        writeString(8, 'WAVE');
        writeString(12, 'fmt ');
        view.setUint32(16, 16, true);
        view.setUint16(20, 1, true);
        view.setUint16(22, 1, true);
        view.setUint32(24, sampleRate, true);
        view.setUint32(28, sampleRate * 2, true);
        view.setUint16(32, 2, true);
        view.setUint16(34, 16, true);
        writeString(36, 'data');
        view.setUint32(40, samples.length * 2, true);

        let offset = 44;
        for (let i = 0; i < samples.length; i++) {
            const s = Math.max(-1, Math.min(1, samples[i]));
            view.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7FFF, true);
            offset += 2;
        }

        return buffer;
    }
};

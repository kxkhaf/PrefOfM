import { useState, useRef, useEffect } from 'react';
import { musicApi } from '@/api/axios';
import './VoiceRecorderButton.css';

const VoiceRecorderButton = ({ onAudioProcessed }) => {
    const [isRecording, setIsRecording] = useState(false);
    const [countdown, setCountdown] = useState(5 - 1);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(false);
    
    const audioContextRef = useRef(null);
    const processorRef = useRef(null);
    const samplesRef = useRef([]);
    const resetTimerRef = useRef(null);
    const errorTimeoutRef = useRef(null);

    useEffect(() => {
        if (error) {
            if (errorTimeoutRef.current) {
                clearTimeout(errorTimeoutRef.current);
            }

            errorTimeoutRef.current = setTimeout(() => {
                const errorElement = document.querySelector('.error-message');
                if (errorElement) {
                    errorElement.classList.add('fade-out');
                    setTimeout(() => setError(null), 500);
                }
            }, 2500);
        }

        return () => {
            if (errorTimeoutRef.current) {
                clearTimeout(errorTimeoutRef.current);
            }
        };
    }, [error]);

    // Очистка при размонтировании
    useEffect(() => {
        return () => {
            stopAll();
            if (resetTimerRef.current) {
                clearTimeout(resetTimerRef.current);
            }
        };
    }, []);

    const stopAll = () => {
        if (processorRef.current) {
            processorRef.current.disconnect();
            processorRef.current = null;
        }
        if (audioContextRef.current) {
            audioContextRef.current.close();
            audioContextRef.current = null;
        }
    };

    const startRecording = async () => {
        try {
            setError(null);
            setLoading(false);
            samplesRef.current = [];

            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const source = audioContext.createMediaStreamSource(stream);
            const processor = audioContext.createScriptProcessor(4096, 1, 1);

            processor.onaudioprocess = (e) => {
                const input = e.inputBuffer.getChannelData(0);
                samplesRef.current.push(new Float32Array(input));
            };

            source.connect(processor);
            processor.connect(audioContext.destination);

            audioContextRef.current = audioContext;
            processorRef.current = processor;
            setIsRecording(true);
            startCountdown();
        } catch (err) {
            setError('Microphone access denied');
            console.error('Recording failed:', err);
        }
    };

    const stopRecording = () => {
        if (processorRef.current) {
            stopAll();
            setIsRecording(false);
            setCountdown(5 - 1);
            const samples = mergeSamples(samplesRef.current);
            const wavBlob = createWavBlob(samples, 44100);
            sendAudioToServer(wavBlob);
            resetTimerRef.current = setTimeout(() => {
                setLoading(false);
            }, 3000);
        }
    };

    const resetRecording = () => {};

    const startCountdown = () => {
        let timer = 5 - 2;
        const interval = setInterval(() => {
            setCountdown(timer);
            if (timer < 0) {
                clearInterval(interval);
                stopRecording();
            }
            timer--;
            }, 1000);
    };

    const mergeSamples = (arrays) => {
        const length = arrays.reduce((acc, a) => acc + a.length, 0);
        const result = new Float32Array(length);
        let offset = 0;
        arrays.forEach(a => {
            result.set(a, offset);
            offset += a.length;
        });
        return result;
    };

    const createWavBlob = (samples, sampleRate) => {
        const buffer = new ArrayBuffer(44 + samples.length * 2);
        const view = new DataView(buffer);

        const writeString = (offset, str) =>
            [...str].forEach((char, i) => view.setUint8(offset + i, char.charCodeAt(0)));

        writeString(0, 'RIFF');
        view.setUint32(4, 36 + samples.length * 2, true);
        writeString(8, 'WAVE');
        writeString(12, 'fmt ');
        view.setUint32(16, 16, true);
        view.setUint16(20, 1, true); // PCM
        view.setUint16(22, 1, true); // Mono
        view.setUint32(24, sampleRate, true);
        view.setUint32(28, sampleRate * 2, true);
        view.setUint16(32, 2, true);
        view.setUint16(34, 16, true);
        writeString(36, 'data');
        view.setUint32(40, samples.length * 2, true);

        samples.forEach((s, i) => {
            const val = Math.max(-1, Math.min(1, s));
            view.setInt16(44 + i * 2, val < 0 ? val * 0x8000 : val * 0x7FFF, true);
        });

        return new Blob([view], { type: 'audio/wav' });
    };

    const sendAudioToServer = async (audioBlob) => {
        try {
            setLoading(true);
            const formData = new FormData();
            formData.append('file', audioBlob, 'recording.wav');

            const response = await musicApi.post('/api/song-by-emotion', formData, {
                withCredentials: true,
                headers: { 'Content-Type': 'multipart/form-data' },
                timeout: 5000
            });

            // Вызываем колбэк с данными ответа
            if (onAudioProcessed) {
                onAudioProcessed(response.data);
            }
        } catch (err) {
            if (err.code === 'ECONNABORTED') {
                setError('Request timeout. Try again');
            } else {
                setError(err.response?.data?.message || 'Analysis failed');
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="voice-recorder-container">
            <button
                className={`voice-recorder ${isRecording ? 'recording' : ''} ${loading ? 'loading' : ''}`}
                onClick={isRecording ? resetRecording : startRecording}
                disabled={loading}
            >
                {loading ? 'Analyzing...' : isRecording ? `Recording... ${countdown + 1}s` : 'Detect Mood'}
            </button>
            {error && <div className="error-message">{error}</div>}
        </div>
    );
};

export default VoiceRecorderButton;
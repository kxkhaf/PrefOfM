// hooks/useYouTubePlayer.js
import { useRef } from 'react';

export const useYouTubePlayer = () => {
    const videoRef = useRef(null);

    const extractYoutubeId = (url) => {
        const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|&v=)([^#&?]*).*/;
        const match = url?.match(regExp);
        return (match && match[2].length === 11) ? match[2] : null;
    };

    const loadVideo = (url) => {
        if (!url || !videoRef.current) return;
        const videoId = extractYoutubeId(url);
        if (videoId) {
            videoRef.current.src = `https://www.youtube.com/embed/${videoId}?enablejsapi=1&autoplay=1`;
        }
    };

    const resetPlayer = () => {
        if (videoRef.current) {
            videoRef.current.src = '';
        }
    };

    return { videoRef, loadVideo, resetPlayer };
};
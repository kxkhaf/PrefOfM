import { useState, useEffect, useRef, useCallback } from 'react';

const MusicPlayer = ({
                         song,
                         onClose,
                         onMove,
                         videoRef,
                         songs = [],
                         currentIndex,
                         setCurrentIndex,
                         setCurrentSong
                     }) => {
    const [isPlaying, setIsPlaying] = useState(false);
    const [progress, setProgress] = useState(0);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const [player, setPlayer] = useState(null);
    const [videoError, setVideoError] = useState(false);
    const [isBuffering, setIsBuffering] = useState(false);
    const [autoSkipEnabled, setAutoSkipEnabled] = useState(true);

    const progressBarRef = useRef(null);
    const [hoverProgress, setHoverProgress] = useState(null);
    const animationFrameRef = useRef(null);
    const lastUpdateTimeRef = useRef(0);
    const isChangingTrack = useRef(false);
    const errorTimeoutRef = useRef(null);
    const lastErrorTime = useRef(0);

    const setErrorTimeout = () => {
        lastErrorTime.current = lastUpdateTimeRef.current;
        setTimeout(() =>{
            if (videoError === false && (lastUpdateTimeRef.current - lastErrorTime.current) > 1000) {
                setVideoError(true);
                
            }
        }, 2000)
    }
    
    const setVideoErrorFalse = () => {
        lastErrorTime.current = lastUpdateTimeRef.current + 3000;
        setVideoError(false);
    }
    
    const extractYoutubeId = (url) => {
        const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|&v=)([^#&?]*).*/;
        const match = url.match(regExp);
        return (match && match[2].length === 11) ? match[2] : null;
    };

    const formatTime = (seconds) => {
        if (isNaN(seconds)) return '0:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
    };

    const changeTrack = useCallback((direction) => {
        if (isChangingTrack.current) return;
        isChangingTrack.current = true;

        try {
            
            let newIndex = direction === 'next' ? currentIndex + 1 : currentIndex - 1;
            if (newIndex < 0 || newIndex >= songs.length) {
                if (direction === 'next' && player) player.stopVideo();
                return;
            }
            const songData = songs[newIndex];
            const songId = songs.find(s => s.index === newIndex).id;
            

            // Сброс состояния плеера
            setProgress(0);
            setCurrentTime(0);
            setIsPlaying(false);
            setVideoErrorFalse();
            setIsBuffering(true);

            // Обновление состояния
            setCurrentIndex(newIndex);
            setCurrentSong(songData);
            onMove(newIndex, songId);

            // Загрузка нового видео
            if (songData?.realUrl) {
                const videoId = extractYoutubeId(songData.realUrl);
                if (videoId && player?.loadVideoById) {
                    player.loadVideoById(videoId);
                }
            }
        } catch (error) {
            console.error('Track change error:', error);
            setErrorTimeout(true)
        } finally {
            setTimeout(() => {
                isChangingTrack.current = false;
            }, 300);
        }
    }, [currentIndex, songs, player, onMove, setCurrentIndex, setCurrentSong]);

    const handleNext = useCallback(() => {
        changeTrack('next');
    }, [changeTrack]);

    const handlePrevious = useCallback(() => {
        changeTrack('prev');
    }, [changeTrack]);

    const handleSkipUnavailable = useCallback(() => {
        if (videoError) {
            handleNext();
        }
    }, [videoError, handleNext]);

    useEffect(() => {
        if (videoError && autoSkipEnabled) {
            errorTimeoutRef.current = setTimeout(() => {
                handleSkipUnavailable();
            }, 1500);
        }

        return () => {
            if (errorTimeoutRef.current) {
                clearTimeout(errorTimeoutRef.current);
            }
        };
    }, [videoError, autoSkipEnabled, handleSkipUnavailable]);

    useEffect(() => {
        const initializePlayer = () => {
            if (!window.YT || !videoRef.current.src) return;

            const ytPlayer = new window.YT.Player(videoRef.current, {
                events: {
                    'onReady': onPlayerReady,
                    'onStateChange': onPlayerStateChange,
                    'onError': onPlayerError
                }
            });

            setPlayer(ytPlayer);
        };

        if (!window.YT) {
            const tag = document.createElement('script');
            tag.src = "https://www.youtube.com/iframe_api";
            const firstScriptTag = document.getElementsByTagName('script')[0];
            firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

            window.onYouTubeIframeAPIReady = initializePlayer;
        } else {
            initializePlayer();
        }

        return () => {
            if (animationFrameRef.current) {
                cancelAnimationFrame(animationFrameRef.current);
            }
            if (player) {
                player.destroy();
            }
            if (errorTimeoutRef.current) {
                clearTimeout(errorTimeoutRef.current);
            }
        };
    }, [videoRef]);

    useEffect(() => {
        const updateProgress = () => {
            const now = Date.now();
            const updateInterval = 200;

            if (now - lastUpdateTimeRef.current >= updateInterval && player) {
                try {
                    const time = player.getCurrentTime();
                    const dur = player.getDuration();

                    if (typeof time === 'number' && typeof dur === 'number' && dur > 0) {
                        setCurrentTime(time);
                        setDuration(dur);
                        setProgress((time / dur) * 100);

                        if (dur - time < 3 && isPlaying && !isChangingTrack.current) {
                            changeTrack('next');
                        }
                    }
                } catch (e) {
                    window.YT = null;
                    console.error("YouTube API error:", e);
                }
                lastUpdateTimeRef.current = now;
            }
            animationFrameRef.current = requestAnimationFrame(updateProgress);
        };

        animationFrameRef.current = requestAnimationFrame(updateProgress);

        return () => {
            if (animationFrameRef.current) {
                cancelAnimationFrame(animationFrameRef.current);
            }
        };
    }, [player, isPlaying, changeTrack]);

    const onPlayerReady = (event) => {
        try {
            setDuration(event.target.getDuration());
            event.target.playVideo();
            setIsPlaying(true);
            setVideoErrorFalse();
        } catch (e) {
            setErrorTimeout(true)
            
        }
    };

    useEffect(() => {
        const checkPlayerState = () => {
            if (!player) return;

            try {
                const playerState = player.getPlayerState();
                setIsPlaying(playerState === 1);
            } catch (e) {
                console.error("Error checking player state:", e);
            }
        };

        const interval = setInterval(checkPlayerState, 500);

        return () => {
            clearInterval(interval);
        };
    }, [player]);

    const onPlayerStateChange = (event) => {
        try {
            switch (event.data) {
                case 1: // PLAYING
                    setIsPlaying(true);
                    setIsBuffering(false);
                    setVideoErrorFalse();
                    break;
                case 2: // PAUSED
                    setIsPlaying(false);
                    break;
                case 3: // BUFFERING
                    setIsBuffering(true);
                    break;
                case 0: // ENDED
                    break;
                case -1: // ERROR
                    setErrorTimeout(true)
                    setIsPlaying(false);
                    break;
            }
        } catch (e) {
            window.YT = null;
            console.error("Player state error:", e);
        }
    };

    const onPlayerError = (event) => {
        console.error('YouTube Player Error:', event.data);
        setErrorTimeout(true)
        setIsPlaying(false);
    };

    const togglePlay = useCallback(() => {
        if (!player || videoError) return;
        try {
            if (isPlaying) {
                player.pauseVideo();
            } else {
                player.playVideo();
            }
            setIsPlaying(!isPlaying);
        } catch (e) {
            setErrorTimeout(true)
        }
    }, [player, isPlaying, videoError]);

    const handleProgressHover = useCallback((e) => {
        if (!progressBarRef.current) return;
        const rect = progressBarRef.current.getBoundingClientRect();
        const clickPosition = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
        const percentage = (clickPosition / rect.width) * 100;
        setHoverProgress(percentage);
    }, []);

    const handleProgressClick = useCallback((e) => {
        if (!player || videoError || !progressBarRef.current) return;

        try {
            const rect = progressBarRef.current.getBoundingClientRect();
            const clickPosition = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
            const percentage = (clickPosition / rect.width) * 100;
            const seekTime = (percentage / 100) * duration;

            player.seekTo(seekTime, true);
            setCurrentTime(seekTime);
            setProgress(percentage);
            setHoverProgress(null);
        } catch (e) {
            window.YT = null;
            console.error("Seek error:", e);
        }
    }, [player, duration, videoError]);

    const handleClose = useCallback(() => {
        if (player && typeof player.stopVideo === 'function') {
            player.stopVideo();
        }
        if (videoRef.current) {
            videoRef.current.src = '';
        }
        onClose();
    }, [onClose, player]);

    if (!song) return null;

    return (
        <div className="music-player">
            <div className="music-player-header">
                <h3 className="music-player-title">{song.title}</h3>
                {videoError && (
                    <span className="video-error-badge">Not Available</span>
                )}
                <button className="music-player-close" onClick={handleClose}>
                    <div className="close-element">×</div>
                </button>
            </div>

            <div className="music-player-content">
                <div className="music-player-info">
                    <div>{song.artist}</div>
                    <div
                        className="progress-bar"
                        ref={progressBarRef}
                        onClick={handleProgressClick}
                        onMouseMove={handleProgressHover}
                        onMouseLeave={() => setHoverProgress(null)}
                    >
                        <div className="progress" style={{width: `${progress}%`}}/>
                        {hoverProgress !== null && (
                            <div
                                className="progress-hover"
                                style={{
                                    width: `${hoverProgress}%`,
                                    opacity: 0.7
                                }}
                            />
                        )}
                        <div className="progress-thumb" style={{left: `${progress}%`}}/>
                    </div>

                    <div className="time-display">
                        <span>{formatTime(currentTime)}</span>
                        <span>{formatTime(duration)}</span>
                    </div>
                </div>
            </div>

            <div className="music-player-controls">
                <button
                    className="music-player-button"
                    onClick={handlePrevious}
                    disabled={currentIndex <= 0}
                >
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M6 12l12-7v14l-12-7z" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                </button>

                <button
                    className="music-player-button play"
                    onClick={togglePlay}
                    disabled={videoError}
                >
                    {isPlaying ? (
                        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <rect x="6" y="4" width="4" height="16" rx="1" strokeLinecap="round"/>
                            <rect x="14" y="4" width="4" height="16" rx="1" strokeLinecap="round"/>
                        </svg>
                    ) : (
                        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <path d="M8 5v14l11-7z" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                    )}
                </button>

                <button
                    className="music-player-button"
                    onClick={handleNext}
                    disabled={currentIndex >= songs.length - 1}
                >
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M18 12L6 5v14l12-7z" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                </button>
            </div>

            {isBuffering && (
                <div className="buffering-indicator">
                    <div className="buffering-spinner"></div>
                </div>
            )}

            {videoError && (
                <div className="error-actions">
                    <label className="auto-skip-label">
                        <input
                            type="checkbox"
                            checked={autoSkipEnabled}
                            onChange={() => setAutoSkipEnabled(!autoSkipEnabled)}
                        />
                        Auto-skip
                    </label>
                    <button className="skip-button" onClick={handleSkipUnavailable}>
                        Skip
                    </button>
                </div>
            )}
        </div>
    );
};

export default MusicPlayer;
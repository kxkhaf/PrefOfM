import {useState, useEffect, useRef} from 'react';
import {useSongs} from '@/hooks/useSongs';
import {useSearch} from '@/contexts/SearchContext';
import {useYouTubePlayer} from '@/hooks/useYouTubePlayer';
import VoiceRecorderButton from '@/components/VoiceRecorderButton/VoiceRecorderButton';
import MusicContainer from "@/components/MusicContainer/MusicContainer";
import MusicPlayer from '@/components/MusicPlayer/MusicPlayer';
import Tabs from '@/components/Tabs/Tabs';
import Playlists from '@/components/Playlists/Playlists';
import './HomePage.css';
import {useTab} from "@/contexts/TabsContext.jsx";
import {musicApi} from "../../api/axios.js";
import {useEmotion} from "../../contexts/EmotionContext.jsx";
import {useFastStart} from "../../contexts/FastStartContext.jsx";

const HomePage = () => {

    const {activeTab, setActiveTab} = useTab();
    const [currentSong, setCurrentSong] = useState(null);
    const [currentIndex, setCurrentIndex] = useState(0);
    const [showPlayer, setShowPlayer] = useState(false);
    const [showPlaylists, setShowPlaylists] = useState(false);
    const [selectedPlaylist, setSelectedPlaylist] = useState(null);
    const [lastSongs, setLastSongs] = useState([]);
    const [playerSongs, setPlayerSongs] = useState([]);
    const [lastPlaylistSongUpdateVideo, setLastPlaylistSongUpdateVideo] = useState(null);
    const [lastActiveTab, setLastActiveTab] = useState(null);
    const [emotionSongs, setEmotionSongs] = useState([]);
    const needUpdateSongs = useRef(false);
    const emotionChangerIsUsing = useRef(false);

    const skipEmotionEffect = useRef(false);
    const {isFastStart, setIsFastStart} = useFastStart();
    const {videoRef, loadVideo} = useYouTubePlayer();
    const {selectedEmotion, setSelectedEmotion} = useEmotion();
    const {searchQuery, setSearchQuery, isSearching, searchResults, setIsSearching} = useSearch();
    const {songs, loading, loadMore, hasMore} = useSongs(
        activeTab,
        searchQuery,
        isSearching,
        showPlaylists ? null : selectedPlaylist?.id,
        emotionSongs,
        needUpdateSongs
    );

    const displaySongs = songs?.length === 0 ? searchResults : songs;

    const handleTabChange = (tab) => {
        if (tab !== 'main') {
            setSearchQuery('');
            setIsSearching(false);
        }
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
        setLastActiveTab(tab);
        setActiveTab(tab);
        setShowPlaylists(tab === 'playlists');
        setSelectedPlaylist(null);

        if (showPlayer) {
            handleClosePlayer();
        }
    };


    useEffect(() => {
        if (activeTab !== lastActiveTab && lastActiveTab !== undefined && lastActiveTab !== null) {
            handleTabChange(activeTab);
        }
    })

    const handleBackToPlaylists = () => {
        setSelectedPlaylist(null);
        setShowPlaylists(true);
        if (showPlayer) {
            setShowPlayer(false);
            if (videoRef.current) {
                videoRef.current.src = '';
            }
        }
    };

    useEffect(() => {
        if (activeTab === null) {
            setTimeout(() => {
                handleTabChange('main');
            }, 100)
        }
    }, [activeTab, setActiveTab]);


    //const handleSongClick = (song, event) => {
    /*    const handleSongClick = (song) => {
            //const newIndex = parseInt(event.currentTarget.dataset.index);
    
            if (Object.keys(song).length === 0) {
                return;
            }
            setCurrentIndex(song.index);
            setCurrentSong(song);
            setShowPlayer(true);
            loadVideo(song.realUrl);
            setLastSongs(displaySongs);
        };*/

    const handleSongClick = (song) => {
        if (Object.keys(song).length === 0) return;
        const songsForPlayer = songs.map((s, idx) => ({...s, index: idx}));
        musicApi.post('/api/history/add', {
            songId: song.id,
            context: ""
        });
        setPlayerSongs(songsForPlayer);
        setCurrentIndex(song.index);
        setCurrentSong(song);
        setShowPlayer(true);
        loadVideo(song.realUrl);
    };

    const handleSongRemoved = (removedSongId) => {
        setLastSongs(prevSongs => {
            const updatedSongs = prevSongs
                .filter(song => song.id !== removedSongId)
                .map((song, index) => ({...song, index}));

            if (currentSong?.id === removedSongId) {
                if (updatedSongs.length > 0) {
                    const newSong = updatedSongs[0];
                    setCurrentSong(newSong);
                    setCurrentIndex(0);
                    loadVideo(newSong.realUrl);
                } else {
                    handleClosePlayer();
                }
            }

            return updatedSongs;
        });

        if (!selectedPlaylist) {
            // Здесь можно сделать refetch данных или обновить songs
        }
    };

    const handleClosePlayer = () => {
        if (videoRef.current) {
            videoRef.current.src = '';
        }
        setShowPlayer(false);
        setCurrentSong(null);
        setCurrentIndex(0);
    };

    const handleSongChange = (newIndex, songId) => {
        if (newIndex < 0 || newIndex >= displaySongs.length) return;
        const newSong = displaySongs.find(song => song.id === songId);
        if (!newSong) return;
        musicApi.post('/api/history/add', {
            songId: songId,
            context: ""
        });
        setCurrentIndex(newIndex);
        setCurrentSong(newSong);
        scrollToSong(newIndex);
    };

    const scrollToSong = (index) => {
        const songElement = document.querySelector(`.song-card[data-index="${index}"]`);
        if (songElement) {
            songElement.scrollIntoView({behavior: 'smooth', block: 'center'});
        }
    };


    useEffect(() => {
        setTimeout(() => {
            if (isFastStart && !loading && songs.length > 0) {
                const randomIndex = Math.floor(Math.random() * Math.min(50, songs.length));
                const randomSong = songs[randomIndex];

                if (randomSong) {
                    const songsWithIndexes = songs.map((s, idx) => ({...s, index: idx}));
                    setPlayerSongs(songsWithIndexes);
                    setCurrentIndex(randomIndex);
                    setCurrentSong(randomSong);
                    setShowPlayer(true);
                    loadVideo(randomSong.realUrl);
                }

                setIsFastStart(false);
            }
        }, 150)
    }, [songs, loading, isFastStart]);

    useEffect(() => {
        if (displaySongs && displaySongs.length > 0) {
            const songsWithIndexes = displaySongs.map((song, index) => ({
                ...song,
                index
            }));
            setLastSongs(songsWithIndexes);

            /*            if (currentSong) {
                            const updatedSong = songsWithIndexes.find(s => s.id === currentSong.id);
                            if (updatedSong) {
                                setCurrentSong(updatedSong);
                                setCurrentIndex(updatedSong.index);
                            } else {
                                handleClosePlayer();
                            }
                        }*/
        }
    }, [displaySongs]);


    useEffect(() => {
        if (isSearching && searchResults.length > 0) {
            const songsWithIndexes = searchResults.map((song, index) => ({
                ...song,
                index
            }));
            setLastSongs(songsWithIndexes);
        }
    }, [isSearching, searchResults]);


    useEffect(() => {
        let currentVideo = currentSong?.realUrl;
        if (currentSong?.realUrl && showPlayer && activeTab === 'playlists' && lastPlaylistSongUpdateVideo !== currentVideo) {
            setLastPlaylistSongUpdateVideo(currentVideo);
            loadVideo(currentSong.realUrl);
        }

        if (activeTab !== 'playlists') {
            setLastPlaylistSongUpdateVideo(null);
        }
    }, [currentSong, showPlayer, loadVideo]);

    const handlePlaylistSelect = (playlist) => {
        setSelectedPlaylist(playlist);
        setShowPlaylists(false);

        if (playlist.songs?.length > 0) {
            const songsWithIndexes = playlist.songs.map((song, index) => ({
                ...song,
                index
            }));
            setPlayerSongs(songsWithIndexes);

            const firstSong = songsWithIndexes[0];
            setCurrentSong(firstSong);
            setCurrentIndex(0);
            setShowPlayer(true);
            loadVideo(firstSong.realUrl);
        } else {
            handleTabChange('main')
        }
    };

    const mappingRules = {
        'Happy': 'Happy',
        'Sad': 'Melancholic',
        'Neutral': 'Relaxing',
        'Angry': 'Energetic',
        'Other': 'Emotion',
    };

    function emotionMapper(emotion) {
        let output = emotion;
        for (const [key, value] of Object.entries(mappingRules)) {
            output = output.replace(new RegExp(value, 'gi'), key);
        }
        return output;
    }

    function emotionRevertMapper(emotion) {
        let output = emotion;
        for (const [key, value] of Object.entries(mappingRules)) {
            output = output.replace(new RegExp(key, 'gi'), value);
        }
        return output;
    }

    useEffect(() => {
        const fetchSongsByEmotion = async () => {
            if (selectedEmotion && !skipEmotionEffect.current) {
                try {
                    if (emotionChangerIsUsing.current) return;
                    const response = await musicApi.get(
                        `/api/songs/emotion/${emotionMapper(selectedEmotion)}`,
                        {
                            withCredentials: true,
                            headers: {'Content-Type': 'multipart/form-data'},
                            timeout: 5000
                        }
                    );
                    handleAudioProcessed(response.data);
                } catch (error) {
                    console.error('Ошибка при загрузке песен:', error);
                }
            }
        };

        fetchSongsByEmotion();
    }, [selectedEmotion]);
    
    const handleAudioProcessed = (responseData) => {
        if (activeTab !== 'main') {
            handleTabChange('main');
        }
        emotionChangerIsUsing.current = true;
        setTimeout(() => {
            skipEmotionEffect.current = true;
            setSelectedEmotion(emotionRevertMapper(responseData[0].emotion));
            needUpdateSongs.current = true;
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });

            if (responseData?.length > 0) {
                const songsWithIndexes = responseData.map((song, index) => ({
                    ...song,
                    index,
                }));

                const firstSong = songsWithIndexes[0];
                setCurrentSong(firstSong);
                setCurrentIndex(0);
                setShowPlayer(true);
                loadVideo(firstSong.realUrl);
                setEmotionSongs(songsWithIndexes);
                setPlayerSongs(songsWithIndexes);
                console.log(songsWithIndexes);
            }

            // Возвращаем флаг в исходное состояние после всех обновлений
            setTimeout(() => {
                skipEmotionEffect.current = false;
            }, 0);
        }, 200);
        setTimeout(() => {
            emotionChangerIsUsing.current = false;
        }, 500);
    };

    /*const handleAudioProcessed = (responseData) => {
        setActiveTab('emotion'); // Установите конкретную вкладку вместо пробела
        const songsWithIndexes = responseData?.map((song, index) => ({
            ...song,
            index
        })) || [];

        setEmotionSongs(songsWithIndexes);
        needUpdateSongs.current = true;

        // Форсируем обновление
        setPlayerSongs([]);
        setPlayerSongs(songsWithIndexes);

        if (songsWithIndexes.length > 0) {
            const firstSong = songsWithIndexes[0];
            setCurrentSong(firstSong);
            setCurrentIndex(0);
            setShowPlayer(true);
            loadVideo(firstSong.realUrl);
        }
    };*/

    return (
        <div className="home-page">
            <div className="voice-tab-container">
                <div className="voice-recorder-block-helper">
                    <Tabs
                        activeTab={activeTab}
                        onTabChange={handleTabChange}
                        playlistName={selectedPlaylist?.name}
                    />
                    <div className="voice-recorder-helper">
                        <VoiceRecorderButton onAudioProcessed={handleAudioProcessed}/>
                    </div>
                </div>

                {showPlaylists ? (
                    <Playlists
                        onPlaylistSelect={handlePlaylistSelect}
                        onBack={handleBackToPlaylists}
                    />
                ) : (
                    <MusicContainer
                        songs={lastSongs}
                        loadMoreSongs={loadMore}
                        onSongClick={handleSongClick}
                        isLoadingMore={loading}
                        hasMore={hasMore}
                        selectedSongId={currentSong?.id}
                        isPlaylistView={activeTab === 'playlists' && selectedPlaylist || activeTab === 'favourite'}
                        currentPlaylistId={selectedPlaylist ? selectedPlaylist.id : null}
                        onSongRemoved={handleSongRemoved}
                    />
                )}
            </div>

            {!showPlaylists && (
                <>
                    <div className="video-container-helper">
                        <div className="video-container">
                            <iframe
                                ref={videoRef}
                                className="frame-youtube"
                                title="YouTube player"
                                frameBorder="0"
                                style={{
                                    backgroundImage: 'url("PrefOfM.png")',
                                    backgroundSize: 'contain',
                                    backgroundRepeat: 'no-repeat',
                                    backgroundPosition: 'center'
                                }}
                                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                                allowFullScreen
                            />
                        </div>
                    </div>
                    {showPlayer && (
                        <MusicPlayer
                            song={currentSong}
                            onClose={handleClosePlayer}
                            onMove={handleSongChange}
                            videoRef={videoRef}
                            songs={playerSongs}
                            currentIndex={currentIndex}
                            setCurrentIndex={setCurrentIndex}
                            setCurrentSong={setCurrentSong}
                            isSearching={isSearching}
                        />
                    )}
                </>
            )}
        </div>
    );
};

export default HomePage;


import { useState, useRef, useEffect, useCallback } from 'react';
import LoadingSpinner from "../LoadingSpinner.jsx";
import { musicApi } from '@/api/axios';
import './MusicContainer.css';

const DotsIcon = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
        <circle cx="12" cy="12" r="1.5"/>
        <circle cx="6" cy="12" r="1.5"/>
        <circle cx="18" cy="12" r="1.5"/>
    </svg>
);

const HeartIcon = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
    </svg>
);

const PlaylistIcon = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/>
    </svg>
);

const TrashIcon = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
    </svg>
);

const MusicContainer = ({
                            songs = [],
                            loadMoreSongs,
                            onSongClick,
                            isLoadingMore = false,
                            hasMore = true,
                            selectedSongId = null,
                            isPlaylistView = false,
                            currentPlaylistId = null,
                            onSongRemoved
                        }) => {
    const [hoveredSong, setHoveredSong] = useState(null);
    const [activeMenu, setActiveMenu] = useState(null);
    const [playlists, setPlaylists] = useState([]);
    const [isLoadingPlaylists, setIsLoadingPlaylists] = useState(false);
    const loaderRef = useRef();
    const containerRef = useRef();
    const menuRefs = useRef({});

    const uniqueSongs = songs.reduce((acc, current) => {
        const exists = acc.some(song => song.id === current.id);
        if (!exists) acc.push(current);
        return acc;
    }, []);

    // Загрузка плейлистов пользователя
    const fetchUserPlaylists = useCallback(async () => {
        try {
            setIsLoadingPlaylists(true);
            const response = await musicApi.get('/api/playlists/basic-info');
            setPlaylists(response.data);
        } catch (error) {
            console.error('Error fetching playlists:', error);
        } finally {
            setIsLoadingPlaylists(false);
        }
    }, []);

    const handleIntersection = useCallback((entries) => {
        const [entry] = entries;
        if (entry.isIntersecting && !isLoadingMore && hasMore) {
            loadMoreSongs();
        }
    }, [isLoadingMore, hasMore, loadMoreSongs]);

    useEffect(() => {
        const observer = new IntersectionObserver(handleIntersection, {
            root: null,
            rootMargin: '300px',
            threshold: 1
        });

        if (loaderRef.current) observer.observe(loaderRef.current);

        return () => {
            if (loaderRef.current) observer.unobserve(loaderRef.current);
        };
    }, [handleIntersection]);

    useEffect(() => {
        const handleClickOutside = (e) => {
            if (activeMenu && !menuRefs.current[activeMenu]?.contains(e.target)) {
                setActiveMenu(null);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, [activeMenu]);

    const toggleMenu = (songId, e) => {
        e.stopPropagation();
        setActiveMenu(activeMenu === songId ? null : songId);
    };

    const handleAddToFavourites = async (songId, e) => {
        e.stopPropagation();
        try {
            await musicApi.post(`/api/playlists/add-favourite-song/${songId}`);
            setActiveMenu(null);
        } catch (error) {
            console.error('Error adding to favourites:', error);
        }
    };

    const handleAddToPlaylist = async (playlistId, songId) => {
        try {
            await musicApi.post(`/api/playlists/${playlistId}/add-song/${songId}`);
            setActiveMenu(null);
        } catch (error) {
            console.error('Error adding song to playlist:', error);
        }
    };

    const handleRemoveFromPlaylist = async (songId, e) => {
        e.stopPropagation();

        if (window.confirm('Remove this song from playlist?')) {
            try {
                currentPlaylistId === null ?
                await musicApi.delete(`/api/playlists/remove-favourite-song/${songId}`) :
                await musicApi.delete(`/api/playlists/${currentPlaylistId}/remove-song/${songId}`);
                setActiveMenu(null);
                onSongRemoved?.(songId);
            } catch (error) {
                console.error('Error removing song:', error);
            }
        }
    };

    const handleClick = useCallback((song, e) => {
        if (!e.target.closest('.song-menu-button') && !e.target.closest('.song-menu-dropdown')) {
            onSongClick?.(song, e);
        }
    }, [onSongClick]);

    if (!uniqueSongs?.length) {
        return (
            <div className="no-songs">
                <p>No songs available</p>
            </div>
        );
    }

    return (
        <div className="music-container" ref={containerRef}>
            <div className="music-grid">
                {uniqueSongs.map((song, index) => (
                    <div
                        key={`${song.id}-${index}`}
                        className={`song-card ${selectedSongId === song.id ? 'selected' : ''}`}
                        onClick={(e) => handleClick(song, e)}
                        onMouseEnter={() => setHoveredSong(song.id)}
                        onMouseLeave={() => setHoveredSong(null)}
                        style={{
                            transform: getTransformStyle(song.id, selectedSongId, hoveredSong),
                            zIndex: getZIndex(song.id, selectedSongId, hoveredSong)
                        }}
                        data-id={song.id}
                        data-index={index}
                    >
                        <div className="song-menu">
                            <button
                                className="song-menu-button"
                                onClick={(e) => {
                                    toggleMenu(song.id, e);
                                    fetchUserPlaylists();
                                }}
                                aria-label="Song menu"
                            >
                                <DotsIcon />
                            </button>

                            {activeMenu === song.id && (
                                <div
                                    className="song-menu-dropdown"
                                    ref={el => menuRefs.current[song.id] = el}
                                >
                                    <div
                                        className="song-menu-item"
                                        onClick={(e) => handleAddToFavourites(song.id, e)}
                                    >
                                        <HeartIcon className="song-menu-icon" />
                                        Add to Favourites
                                    </div>

                                    <div className="song-menu-submenu">
                                        <div className="song-menu-header">
                                            <PlaylistIcon className="song-menu-icon" />
                                            Add to Playlist
                                        </div>
                                        {isLoadingPlaylists ? (
                                            <div className="song-menu-loading">Loading...</div>
                                        ) : (
                                            playlists.map(playlist => (
                                                <div
                                                    key={playlist.id}
                                                    className="song-menu-subitem"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleAddToPlaylist(playlist.id, song.id);
                                                    }}
                                                >
                                                    {playlist.name}
                                                </div>
                                            ))
                                        )}
                                    </div>

                                    {isPlaylistView && (
                                        <div
                                            className="song-menu-item delete"
                                            onClick={(e) => handleRemoveFromPlaylist(song.id, e)}
                                        >
                                            <TrashIcon className="song-menu-icon" />
                                            Remove
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>

                        <div className="song-info">
                            <h3 className="song-title">{song.title}</h3>
                            <p className="song-artist">{song.artist}</p>
                            {song.mood && <span className="mood-badge">{song.mood}</span>}
                        </div>
                    </div>
                ))}
            </div>

            {hasMore && (
                <div ref={loaderRef} className="loader-container">
                    {isLoadingMore && <LoadingSpinner />}
                </div>
            )}
        </div>
    );
};

const getTransformStyle = (id, selectedId, hoveredId) => {
    if (id === hoveredId && id === selectedId) return 'translateY(-12px)';
    if (id === selectedId) return 'translateY(-8px)';
    if (id === hoveredId) return 'translateY(-5px)';
    return 'translateY(0)';
};

const getZIndex = (id, selectedId, hoveredId) => {
    if (id === hoveredId && id === selectedId) return 30;
    if (id === selectedId) return 20;
    if (id === hoveredId) return 10;
    return 1;
};

export default MusicContainer;
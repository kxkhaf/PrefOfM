import { useState, useEffect } from 'react';
import { musicApi } from '@/api/axios';
import './Playlists.css';

const Playlists = ({ onPlaylistSelect, onBack }) => {
    const [playlists, setPlaylists] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [newPlaylist, setNewPlaylist] = useState({
        name: '',
        description: '',
        isCreating: false
    });

    useEffect(() => {
        const fetchPlaylists = async () => {
            try {
                const response = await musicApi.get('/api/playlists');
                setPlaylists(response.data);
                console.log('fetchPlaylists', response.data);
            } catch (err) {
                console.error('Error fetching playlists:', err);
                setError('Failed to load playlists');
            } finally {
                setLoading(false);
            }
        };

        fetchPlaylists();
    }, []);

    const handleCreatePlaylist = () => {
        setNewPlaylist(prev => ({ ...prev, isCreating: true }));
    };

    const handleCancelCreate = () => {
        setNewPlaylist({
            name: '',
            description: '',
            isCreating: false
        });
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setNewPlaylist(prev => ({ ...prev, [name]: value }));
    };

    const handleSubmitCreate = async () => {
        if (!newPlaylist.name.trim()) {
            alert('Playlist name is required');
            return;
        }

        try {
            const response = await musicApi.post('/api/playlists/create', {
                name: newPlaylist.name,
                description: newPlaylist.description,
                songs: []
            });

            const createdPlaylist = {
                id: response.data.id,
                name: response.data.name || newPlaylist.name,
                description: response.data.description || newPlaylist.description,
                songs: response.data.songs || [],
                createdAt: response.data.createdAt || new Date().toISOString()
            };

            setPlaylists(prev => [createdPlaylist, ...prev]);
            setNewPlaylist({
                name: '',
                description: '',
                isCreating: false
            });
        } catch (err) {
            console.error('Error creating playlist:', err);
            alert('Failed to create playlist');
        }
    };

    const handleDeletePlaylist = async (playlistId) => {
        if (!window.confirm('Are you sure you want to delete this playlist?')) {
            return;
        }

        try {
            await musicApi.delete(`/api/playlists/${playlistId}`);
            setPlaylists(prev => prev.filter(p => p.id !== playlistId));
        } catch (err) {
            console.error('Error deleting playlist:', err);
            alert('Failed to delete playlist');
        }
    };

    if (loading) {
        return <div className="loading-spinner">Loading playlists...</div>;
    }

    if (error) {
        return <div className="error-message">{error}</div>;
    }

    return (
        <div className="playlists-container">
            <h2 className="playlists-title">Your Playlists</h2>
            <div className="playlists-grid">
                {/* Create playlist card (always first) */}
                <div className="playlist-card create-playlist-card">
                    {newPlaylist.isCreating ? (
                        <div className="create-playlist-form">
                            <input
                                type="text"
                                name="name"
                                value={newPlaylist.name}
                                onChange={handleInputChange}
                                placeholder="Playlist name"
                                className="playlist-input"
                                required
                            />
                            <textarea
                                name="description"
                                value={newPlaylist.description}
                                onChange={handleInputChange}
                                placeholder="Description (optional)"
                                className="playlist-input playlist-textarea"
                            />
                            <div className="create-playlist-actions">
                                <button
                                    style={{backgroundColor: '#279700'}}
                                    onClick={handleSubmitCreate}
                                    className="playlist0play-button"
                                >
                                    Create
                                </button>
                                <button
                                    onClick={handleCancelCreate}
                                    className="playlist0play-button"
                                    style={{backgroundColor: '#d81000'}}
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    ) : (
                        <button
                            className="playlist0play-button"
                            onClick={handleCreatePlaylist}
                            style={{backgroundColor: '#4CAF50'}}
                        >
                            + Create New Playlist
                        </button>
                    )}
                </div>

                {/* Existing playlists */}
                {playlists.map((playlist) => (
                    <div key={playlist.id} className="playlist-card">
                        <div className="playlist-info">
                            <h3 className="playlist-name">{playlist.name}</h3>
                            <p className="playlist-count">{playlist.songs?.length || 0} songs</p>
                            {playlist.description && (
                                <div className="playlist-description-hover">
                                    {playlist.description}
                                </div>
                            )}
                        </div>
                        <div className="playlist-actions">
                            <button
                                className="playlist0play-button"
                                onClick={() => onPlaylistSelect(playlist)}
                            >
                                Play
                            </button>
                            <button
                                className="playlist0play-button"
                                onClick={() => handleDeletePlaylist(playlist.id)}
                                style={{backgroundColor: '#f44336'}}
                            >
                                Delete
                            </button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default Playlists;
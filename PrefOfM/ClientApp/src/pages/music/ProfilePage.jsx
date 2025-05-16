import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './ProfilePage.css';
import {musicApi} from "../../api/axios.js";

const ProfilePage = () => {
    const [userData, setUserData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const navigate = useNavigate();

    useEffect(() => {
        const fetchUserData = async () => {
            try {
                const response = await musicApi.get('/api/profile', {
                    withCredentials: true
                });
                setUserData(response.data);
            } catch (err) {
                setError(err.response?.data?.message || 'Failed to fetch profile data');
            } finally {
                setLoading(false);
            }
        };

        fetchUserData();
    }, []);


    const formatDate = (isoDateString) => {
        if (!isoDateString || isoDateString === 'Not specified') {
            return 'Not specified';
        }

        try {
            // Создаем объект Date из строки
            const date = new Date(isoDateString);

            // Проверяем валидность даты
            if (isNaN(date.getTime())) {
                return 'Invalid date';
            }

            // Получаем компоненты даты
            const day = String(date.getDate()).padStart(2, '0');
            const month = String(date.getMonth() + 1).padStart(2, '0'); // Месяцы 0-11
            const year = date.getFullYear();

            return `${day}.${month}.${year}`;
        } catch (e) {
            console.error('Date formatting error:', e);
            return 'Invalid date format';
        }
    };

    if (loading) return <div className="loading">Loading profile...</div>;
    if (error) return <div className="error">Error: {error}</div>;

    return (
        <div className="profile-page">
            <div className="profile-card">
                <div className="profile-header">
                    <div className="avatar">
                        {userData?.username?.charAt(0).toUpperCase()}
                    </div>
                    <h2>{userData?.username}</h2>
                    <p className="email">{userData?.email}</p>
                </div>

                <div className="profile-info">
                    <div className="info-row">
                        <span>Birthday:</span>
                        <strong>{formatDate(userData?.birthday)}</strong>
                    </div>
                </div>

                <div className="profile-stats">
                    <div className="stat-item">
                        <span className="stat-value">{userData?.songsPlayed || 0}</span>
                        <span className="stat-label">Songs</span>
                    </div>
                    <div className="stat-item">
                        <span className="stat-value">{userData?.playlistsCount || 0}</span>
                        <span className="stat-label">Playlists</span>
                    </div>
                    <div className="stat-item">
                        <span className="stat-value">{userData?.favoritesCount || 0}</span>
                        <span className="stat-label">Favorites</span>
                    </div>
                </div>

                <div className="profile-actions">
                    <button
                        className="edit-button"
                        onClick={() => navigate('/settings')}
                    >
                        Edit Profile
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ProfilePage;
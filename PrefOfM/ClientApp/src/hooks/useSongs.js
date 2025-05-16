import {act, useCallback, useEffect, useState} from 'react';
import {musicApi} from '@/api/axios';

export const useSongs = (activeTab, searchQuery, isSearching, playlistId, emotionSongs, needUpdateSongs) => {
    const [songs, setSongs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);

    const swappedMappingRules = {
        'happy': 'happy',
        'melancholic': 'sad',
        'relaxing': 'neutral',
        'energetic': 'angry',
        'emotion': 'other',
    };

    function getSwappedValue(input) {
        const lowerInput = input.toLowerCase();
        if (lowerInput in swappedMappingRules) {
            return swappedMappingRules[lowerInput];
        }
        return input;
    }
    
    const fetchSongs = useCallback(async (pageNum = 1, isNewRequest = false) => {
        try {
            setLoading(true);
            const params = { pageNum, limit: 50 };
            let endpoint = '/api/songs/main';
            let requestData = params;
            let method = 'post';

            if  (isSearching) {
                endpoint = '/api/songs/search';
                console.log(searchQuery);
                console.log(getSwappedValue(searchQuery))
                requestData = { ...params, query: getSwappedValue(searchQuery) };
            }
            else if (activeTab === 'emotion') {
            } else if (playlistId) {
                endpoint = `/api/playlists/${playlistId}`;
                requestData = {};
                method = 'get';
            } else if (activeTab === 'favourite') {
                endpoint = `/api/playlists/favourite`;
                requestData = {};
                method = 'get';
            } else if (activeTab === 'recent') {
                endpoint = `/api/songs/recent`;
                requestData = {};
                method = 'get';
            } else if (activeTab) {
                endpoint = `/api/songs/${activeTab}`;
            }

            const response = method === 'get'
                ? await musicApi.get(endpoint, { params: requestData })
                : await musicApi.post(endpoint, requestData);

            const receivedSongs = playlistId ? response.data?.songs || [] : response.data || [];

            setSongs(prev => {
                const combined = isNewRequest ? receivedSongs : [...prev, ...receivedSongs];
                const uniqueSongs = combined.reduce((acc, song) => {
                    if (!acc.some(s => s.id === song.id)) {
                        acc.push(song);
                    }
                    return acc;
                }, []);

                return uniqueSongs.map((song, index) => ({
                    ...song,
                    index,
                }));
            });
            setHasMore(!playlistId && receivedSongs.length >= params.limit);

            if (isNewRequest) {
                window.scrollTo(0, 0);
            }
        } catch (error) {
            console.error('Error fetching songs:', error);
            setHasMore(false);
        } finally {
            setLoading(false);
        }
    }, [isSearching, searchQuery, activeTab, playlistId]);
    
    useEffect(() => {
        setPage(1);
        fetchSongs(1, true);
    }, [isSearching, searchQuery, activeTab, playlistId, fetchSongs]);

    useEffect(() => {
        if (needUpdateSongs.current) {
            setSongs(emotionSongs);
            setHasMore(true);
            needUpdateSongs.current = false;
            setLoading(false);
        }
    }, [emotionSongs]);
    
    useEffect(() => {
        if (page > 1 && !playlistId && activeTab !== 'recent') {
            fetchSongs(page);
        }
    }, [page, fetchSongs, playlistId]);

    const loadMore = useCallback(() => {
        if (loading || !hasMore || playlistId) return;
        setPage(prev => prev + 1);
    }, [loading, hasMore, playlistId]);

    return { songs, loading, loadMore, hasMore };
};
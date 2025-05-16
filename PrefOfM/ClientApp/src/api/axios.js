import axios from 'axios';
import { v4 as uuidv4 } from 'uuid';

// Состояние только в памяти
let inMemoryAccessToken = null;
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });
    failedQueue = [];
};

const handleAuthResponse = (response) => {
    const authHeader = response.headers['authorization'];
    if (authHeader) {
        inMemoryAccessToken = authHeader.replace('Bearer ', '');
    }
    return response;
};

const createApiInstance = (baseURL, withCredentials = false) => {
    const api = axios.create({
        baseURL,
        withCredentials,
        headers: {
            'Content-Type': 'application/json',
            'X-Request-Source': 'web-app'
        }
    });

    api.interceptors.request.use(config => {
        // Добавляем уникальный ID для каждого запроса
        config.headers['X-Request-ID'] = uuidv4();

        if (inMemoryAccessToken) {
            config.headers.Authorization = `Bearer ${inMemoryAccessToken}`;
        }
        return config;
    });

    api.interceptors.response.use(
        response => handleAuthResponse(response),
        async error => {
            if (error.config?.__isRetry) {
                return Promise.reject(error);
            }

            const originalRequest = error.config;
            if (!originalRequest ||
                error.config.method === 'options' ||
                originalRequest.url?.includes('/refresh')) {
                return Promise.reject(error);
            }

            if (error.response?.status === 401 && !originalRequest._retry) {
                if (isRefreshing) {
                    //window.location = '/sign-in';
                    return new Promise((resolve, reject) => {
                        failedQueue.push({ resolve, reject });
                    }).then(token => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return api(originalRequest);
                    });
                }

                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    const refreshResponse = await authApi.get('/refresh');
                    handleAuthResponse(refreshResponse);
                    processQueue(null, inMemoryAccessToken);

                    // Помечаем повторный запрос
                    originalRequest.__isRetry = true;
                    originalRequest.headers.Authorization = `Bearer ${inMemoryAccessToken}`;

                    return api(originalRequest);
                } catch (refreshError) {
                    processQueue(refreshError, null);
                    inMemoryAccessToken = null;
                    return Promise.reject(refreshError);
                } finally {
                    isRefreshing = false;
                }
            }

            return Promise.reject(error);
        }
    );

    return api;
};


// Глобальные методы управления аутентификацией
export const setAuthToken = (accessToken) => {
    inMemoryAccessToken = accessToken;
};

export const logout = async () => {
    try {
        await authApi.post('/logout');
    } catch (e) {
        console.error('Logout error:', e);
    } finally {
        inMemoryAccessToken = null;
        window.location.href = '/sign-in';
    }
};

// Создаем экземпляры API
export const authApi = createApiInstance(import.meta.env.VITE_AUTH_API_URL, true);
export const emailApi = createApiInstance(import.meta.env.VITE_EMAIL_API_URL);
export const musicApi = createApiInstance(import.meta.env.VITE_MUSIC_API_URL);
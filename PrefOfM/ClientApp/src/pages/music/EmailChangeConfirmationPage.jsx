import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { authApi } from '@/api/axios';
import { FaCheckCircle, FaSpinner, FaTimesCircle } from 'react-icons/fa';
import '../../styles/auth.css';

const EmailChangeConfirmationPage = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const [status, setStatus] = useState('loading');
    const [message, setMessage] = useState('');
    const [countdown, setCountdown] = useState(5);

    useEffect(() => {
        const confirmEmailChange = async () => {
            try {
                // Правильное извлечение параметров из query string
                const userId = searchParams.get('userId');
                let token = searchParams.get('token');
                let newEmail = searchParams.get('newEmail');

                console.log('Raw params:', { userId, token, newEmail });

                if (!userId || !token || !newEmail) {
                    throw new Error('Invalid confirmation link: missing required parameters');
                }

                // Декодируем только один раз
                token = decodeURIComponent(token);
                newEmail = decodeURIComponent(newEmail);

                console.log('Decoded params:', { userId, token, newEmail });

                const response = await authApi.post('/profile/email/confirm', {
                    userId,
                    token,
                    newEmail
                });

                setStatus('success');
                setMessage('Email successfully changed. Redirecting in 5 seconds...');

                const timer = setInterval(() => {
                    setCountdown(prev => prev > 0 ? prev - 1 : 0);
                }, 1000);

                const timeout = setTimeout(() => {
                    clearInterval(timer);
                    navigate('/settings', {
                        state: {
                            message: 'Email changed successfully!'
                        }
                    });
                }, 5000);

                return () => {
                    clearInterval(timer);
                    clearTimeout(timeout);
                };
            } catch (error) {
                console.error('Confirmation error:', error);
                setStatus('error');
                setMessage(
                    error.response?.data?.message ||
                    error.response?.data?.error ||
                    error.message ||
                    'An error occurred while confirming email change'
                );
            }
        };

        confirmEmailChange();
    }, [searchParams, navigate]);

    return (
        <div className="auth-page">
            <div className="auth-container">
                <div className="auth-card">
                    <div className="auth-header">
                        <h1>Email Change Confirmation</h1>
                        <div className="divider"></div>
                    </div>

                    {status === 'loading' && (
                        <div className="confirmation-status">
                            <FaSpinner className="spin-animation" style={{ fontSize: '2rem' }} />
                            <p>Verifying your request...</p>
                        </div>
                    )}

                    {status === 'success' && (
                        <div className="confirmation-status">
                            <FaCheckCircle style={{
                                color: '#1DB954',
                                fontSize: '3rem',
                                marginBottom: '1rem'
                            }} />
                            <h3>Success!</h3>
                            <p>{message}</p>
                            <p>Redirecting in {countdown} seconds...</p>
                        </div>
                    )}

                    {status === 'error' && (
                        <div className="confirmation-status">
                            <FaTimesCircle style={{
                                color: '#ff3333',
                                fontSize: '3rem',
                                marginBottom: '1rem'
                            }} />
                            <h3>Error</h3>
                            <p>{message}</p>
                            <button
                                onClick={() => navigate('/settings')}
                                className="auth-button"
                                style={{ marginTop: '1rem' }}
                            >
                                Back to Settings
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default EmailChangeConfirmationPage;
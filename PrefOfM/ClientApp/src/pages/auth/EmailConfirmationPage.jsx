import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import '../../styles/auth.css';
import { authApi } from "../../api/axios.js";
import { FaSpinner } from 'react-icons/fa';

const EmailConfirmationPage = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [isSuccess, setIsSuccess] = useState(false);

    useEffect(() => {
        const confirmEmail = async () => {
            const token = searchParams.get('token');
            const userId = searchParams.get('userId');

            if (!token || !userId) {
                setMessage('Invalid confirmation link - missing parameters');
                setIsLoading(false);
                return;
            }

            try {
                const response = await authApi.post('/confirm-email', {
                    token: token,
                    userId: userId
                });

                setMessage(response.data.message || 'Email successfully confirmed!');
                setIsSuccess(true);
                setTimeout(() => navigate('/sign-in'), 3000);
            } catch (error) {
                setMessage(error.response?.data?.message || 'Email confirmation failed. Please try again later.');
                setIsSuccess(false);
            } finally {
                setIsLoading(false);
            }
        };

        confirmEmail();
    }, [searchParams, navigate]);

    return (
        <div className="auth-page">
            <div className="auth-container">
                <div className="auth-card">
                    <div className="auth-header">
                        <h1>Email Confirmation</h1>
                        <div className="divider"></div>
                    </div>

                    <div className="auth-form">
                        {isLoading ? (
                            <div className="auth-message">
                                <span className="loading-content">
                                    <FaSpinner className="spin-animation" />
                                    Processing confirmation...
                                </span>
                            </div>
                        ) : (
                            <div className={`auth-message ${isSuccess ? 'message-success' : 'message-error'}`}>
                                {message}
                                {isSuccess && (
                                    <p className="redirect-message">Redirecting to sign in page...</p>
                                )}
                            </div>
                        )}
                    </div>

                    <div className="auth-footer">
                        {!isSuccess && (
                            <button
                                className="auth-link-button"
                                onClick={() => navigate('/resend-email')}
                            >
                                Resend confirmation email
                            </button>
                        )}
                        <a href="/sign-in" className="auth-link">Go to Sign In</a>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default EmailConfirmationPage;
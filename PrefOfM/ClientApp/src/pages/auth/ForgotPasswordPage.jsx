import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from "../../api/axios";
import '../../styles/auth.css';
import { FaSpinner } from 'react-icons/fa';

const ForgotPasswordPage = () => {
    const [formData, setFormData] = useState({
        userName: '',
        email: ''
    });
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [showSuccess, setShowSuccess] = useState(false);
    const navigate = useNavigate();

    const handleChange = (e) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        setMessage('');
        setShowSuccess(false);

        try {
            await authApi.post('/forgot-password', {
                UserName: formData.userName,
                Email: formData.email
            });

            setMessage('Password reset link has been sent to your email. Please check your inbox.');
            setShowSuccess(true);
            setTimeout(() => navigate('/sign-in'), 5000);
        } catch (error) {
            setMessage(error.response?.data?.message || 'Error sending reset link');
            setShowSuccess(false);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="auth-page">
            <div className="auth-container">
                <div className="auth-card">
                    <div className="auth-header">
                        <h1>Reset Password</h1>
                        <div className="divider"></div>
                    </div>

                    {!showSuccess ? (
                        <form onSubmit={handleSubmit} className="auth-form">
                            <div className="input-group">
                                <input
                                    type="text"
                                    name="userName"
                                    value={formData.userName}
                                    onChange={handleChange}
                                    required
                                    className="auth-input"
                                    placeholder="Enter your username"
                                />
                                <label>Username</label>
                            </div>

                            <div className="input-group">
                                <input
                                    type="email"
                                    name="email"
                                    value={formData.email}
                                    onChange={handleChange}
                                    required
                                    className="auth-input"
                                    placeholder="Enter your email"
                                />
                                <label>Email</label>
                            </div>

                            <button
                                type="submit"
                                className={`auth-button ${isLoading ? 'loading' : ''}`}
                                disabled={isLoading}
                            >
                                {isLoading ? (
                                    <span className="loading-content">
                                        <FaSpinner className="spin-animation" />
                                        Sending...
                                    </span>
                                ) : 'Send Reset Link'}
                            </button>

                            {message && (
                                <div className={`auth-message ${
                                    message.includes('Error') ? 'message-error' : 'message-info'
                                }`}>
                                    {message}
                                </div>
                            )}
                        </form>
                    ) : (
                        <div className="auth-success-message">
                            <div className="message-success">{message}</div>
                            <p className="redirect-notice">You will be redirected to sign in page in 5 seconds...</p>
                        </div>
                    )}

                    <div className="auth-footer">
                        <p>Remembered your password? <a href="/sign-in" className="auth-link">Sign In</a></p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ForgotPasswordPage;
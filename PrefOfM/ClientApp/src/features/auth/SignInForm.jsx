import React, { useState } from 'react';
import { authApi } from "../../api/axios";
import { useNavigate, Link } from 'react-router-dom';
import { FaSpinner } from 'react-icons/fa';
import '../../styles/auth.css';
import '../../components/LoadingSpinner.css';

const SignInForm = () => {
    const [formData, setFormData] = useState({
        login: '',
        password: ''
    });
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [showResendLink, setShowResendLink] = useState(false);
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

        try {
            const response = await authApi.post('/sign-in', {
                Login: formData.login,
                Password: formData.password
            });
            navigate('/');
        } catch (error) {
            const errorMessage = error.response?.data?.message || error.message || 'Authentication failed';
            setMessage(errorMessage);
            setShowResendLink(errorMessage.includes('confirm'));
        } finally {
            setIsLoading(false);
        }
    };

    const handleResendClick = async (e) => {
        e.preventDefault();
        try {
            await authApi.post('/resend-confirmation', {
                email: formData.login
            });
            setMessage('Confirmation email resent successfully!');
            setShowResendLink(false);
        } catch (error) {
            setMessage('Failed to resend confirmation email');
        }
    };

    return (
        <div className="auth-page">
            <div className="auth-card intro-card">
                <div className="auth-card-intro">
                    <h2 className="intro-title">Welcome to PrefOfM</h2>
                    <div className="intro-divider"></div>
                    <p className="intro-text">
                        A smart service that selects music based on your current emotional state.
                    </p>
                    <ul className="intro-features">
                        <li>Search for songs using manual parameters</li>
                        <li>Automatic selection via neural network</li>
                        <li>Personalized recommendations</li>
                        <li>Analysis of your musical preferences</li>
                    </ul>
                    <p className="intro-note">
                        Authorization is required to access all features
                    </p>
                </div>
            </div>
            <div className="auth-container">
                <div className="auth-card">
                    <div className="auth-header">
                        <h1>Sign In</h1>
                        <div className="divider"></div>
                    </div>

                    <form onSubmit={handleSubmit} className="auth-form">
                        <div className="input-group">
                            <input
                                type="text"
                                name="login"
                                value={formData.login}
                                onChange={handleChange}
                                required
                                className="auth-input"
                                autoComplete="username"
                            />
                            <label>Username or Email</label>
                        </div>

                        <div className="input-group">
                            <input
                                type="password"
                                name="password"
                                value={formData.password}
                                onChange={handleChange}
                                required
                                className="auth-input"
                                autoComplete="current-password"
                            />
                            <label>Password</label>
                        </div>

                        <button
                            type="submit"
                            className={`auth-button ${isLoading ? 'loading' : ''}`}
                            disabled={isLoading}
                        >
                            {isLoading ? (
                                <span className="spinner-container">
                                    <FaSpinner className="spin-animation" />
                                    Signing In...
                                </span>
                            ) : 'Sign In'}
                        </button>

                        {message && (
                            <div className={`auth-message ${message.includes('failed') ? 'error' : ''}`}>
                                {message}
                                {showResendLink && (
                                    <Link to="/resend-email" className="auth-link">Resend confirmation email</Link>
                                )}
                            </div>
                        )}
                    </form>

                    <div className="auth-footer">
                        <p>Don't have an account? <Link to="/sign-up" className="auth-link">Sign Up</Link></p>
                        <Link to="/forgot-password" className="auth-link">Forgot password?</Link>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SignInForm;
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../../styles/auth.css';
import { authApi } from "../../api/axios.js";
import { FaSpinner } from 'react-icons/fa';

const EmailConfirmationRequestPage = () => {
    const [formData, setFormData] = useState({
        userName: '',
        password: '',
        email: ''
    });
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
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
            await authApi.post('/resend-confirmation', {
                UserName: formData.userName,
                Password: formData.password,
                Email: formData.email
            });
            setMessage(`Confirmation link sent to ${formData.email}. Check your inbox.`);
            setTimeout(() => navigate('/sign-in'), 5000);
        } catch (error) {
            setMessage(error.response?.data?.message || 'Failed to send confirmation');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="auth-page">
            <div className="auth-container">
                <div className="auth-card">
                    <div className="auth-header">
                        <h1>Confirm Your Email</h1>
                        <div className="divider"></div>
                    </div>

                    <form onSubmit={handleSubmit} className="auth-form">
                        <div className="input-group">
                            <input
                                type="text"
                                name="userName"
                                value={formData.userName}
                                onChange={handleChange}
                                required
                                className="auth-input"
                            />
                            <label>Username</label>
                        </div>

                        <div className="input-group">
                            <input
                                type="password"
                                name="password"
                                value={formData.password}
                                onChange={handleChange}
                                required
                                className="auth-input"
                            />
                            <label>Password</label>
                        </div>

                        <div className="input-group">
                            <input
                                type="email"
                                name="email"
                                value={formData.email}
                                onChange={handleChange}
                                required
                                className="auth-input"
                            />
                            <label>Email Address</label>
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
                            ) : 'Send Confirmation'}
                        </button>

                        {message && (
                            <div className={`auth-message ${
                                message.includes('sent to') ? 'message-success' : 'message-error'
                            }`}>
                                {message}
                            </div>
                        )}
                    </form>

                    <div className="auth-footer">
                        <p>Already confirmed? <a href="/sign-in" className="auth-link">Sign In</a></p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default EmailConfirmationRequestPage;
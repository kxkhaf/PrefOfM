import React, { useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from "../../api/axios";
import '../../styles/auth.css';
import { FaSpinner } from 'react-icons/fa';

const ResetPasswordPage = () => {
    const [searchParams] = useSearchParams();
    const location = useLocation();
    const navigate = useNavigate();
    const [passwords, setPasswords] = useState({
        newPassword: '',
        confirmPassword: ''
    });
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [passwordErrors, setPasswordErrors] = useState([]);

    const token = searchParams.get('token');
    const userId = searchParams.get('userId');
    const email = location.state?.email || '';

    const validatePassword = (password) => {
        const errors = [];
        const settings = {
            minLength: 8,
            requireDigit: true,
            requireLowercase: true,
            requireUppercase: true,
            requireNonAlphanumeric: false
        };

        if (password.length < settings.minLength) {
            errors.push(`Password must be at least ${settings.minLength} characters`);
        }

        if (settings.requireDigit && !/[0-9]/.test(password)) {
            errors.push('Password must contain at least one digit');
        }

        if (settings.requireLowercase && !/[a-z]/.test(password)) {
            errors.push('Password must contain at least one lowercase letter');
        }

        if (settings.requireUppercase && !/[A-Z]/.test(password)) {
            errors.push('Password must contain at least one uppercase letter');
        }

        return errors;
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        if (passwords.newPassword !== passwords.confirmPassword) {
            setMessage("Passwords don't match");
            return;
        }
        
        const errors = validatePassword(passwords.newPassword);
        if (errors.length > 0) {
            setPasswordErrors(errors);
            setMessage('Password does not meet requirements');
            return;
        }

        setIsLoading(true);
        setPasswordErrors([]);

        try {
            await authApi.post('/reset-password', {
                userId,
                token,
                newPassword: passwords.newPassword
            });
            navigate('/sign-in', {
                state: {
                    message: 'Password reset successfully! Please sign in.'
                }
            });
        } catch (error) {
            setMessage(error.response?.data?.message || 'Password reset failed');
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

                    {email && <p className="auth-text">Resetting password for: {email}</p>}

                    <form onSubmit={handleSubmit} className="auth-form">
                        <div className="input-group">
                            <input
                                type="password"
                                name="newPassword"
                                value={passwords.newPassword}
                                onChange={(e) => {
                                    setPasswords({...passwords, newPassword: e.target.value});
                                    setPasswordErrors([]);
                                }}
                                required
                                minLength="8"
                                className="auth-input"
                            />
                            <label>New Password</label>
                        </div>

                        <div className="input-group">
                            <input
                                type="password"
                                name="confirmPassword"
                                value={passwords.confirmPassword}
                                onChange={(e) => {
                                    setPasswords({...passwords, confirmPassword: e.target.value});
                                    setMessage('');
                                }}
                                required
                                className="auth-input"
                            />
                            <label>Confirm Password</label>
                        </div>

                        {passwordErrors.length > 0 && (
                            <div className="password-requirements">
                                <ul>
                                    {passwordErrors.map((error, index) => (
                                        <li key={index} className="requirement-error">
                                            {error}
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        )}

                        <button
                            type="submit"
                            className={`auth-button ${isLoading ? 'loading' : ''}`}
                            disabled={isLoading}
                        >
                            {isLoading ? (
                                <span className="loading-content">
                                    <FaSpinner className="spin-animation" />
                                    Resetting...
                                </span>
                            ) : 'Reset Password'}
                        </button>

                        {message && (
                            <div className={`auth-message ${
                                message.includes('success') ? 'message-success' : 'message-error'
                            }`}>
                                {message}
                            </div>
                        )}
                    </form>
                </div>
            </div>
        </div>
    );
};

export default ResetPasswordPage;
import React, { useState } from 'react';
import { authApi } from "../../api/axios";
import { useNavigate, Link } from 'react-router-dom';
import '../../styles/auth.css';
import {FaCheckCircle, FaSpinner} from 'react-icons/fa';
import '../../components/LoadingSpinner.css';

const SignUpForm = () => {
    const [formData, setFormData] = useState({
        userName: '',
        email: '',
        password: '',
        birthday: ''
    });
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [passwordErrors, setPasswordErrors] = useState([]);
    const navigate = useNavigate();

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

        if (settings.requireNonAlphanumeric && !/[^a-zA-Z0-9]/.test(password)) {
            errors.push('Password must contain at least one special character');
        }

        return errors;
    };

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: value
        });
        
        if (name === 'password') {
            setPasswordErrors([]);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        const errors = validatePassword(formData.password);
        if (errors.length > 0) {
            setPasswordErrors(errors);
            setMessage('Password does not meet requirements');
            return;
        }

        setIsLoading(true);

        try {
            const response = await authApi.post('/sign-up', {
                UserName: formData.userName,
                Email: formData.email,
                Password: formData.password,
                Birthday: formData.birthday
            });

            setMessage(
                <div className="success-message-container">
                    <p className="success-message">
                        <FaCheckCircle className="success-icon" />
                        Account created successfully. Please check your email to confirm your account.
                    </p>
                    <div className="resend-notice">
                        Didn't receive email?{' '}
                        <Link
                            to={`/resend-email`}
                            className="resend-link"
                        >
                            Resend now
                        </Link>
                    </div>
                </div>
            );
        } catch (error) {
            setMessage(error.response?.data?.message || 'Registration failed');
        } finally {
            setIsLoading(false);
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
                        <h1>Create Account</h1>
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
                                type="email"
                                name="email"
                                value={formData.email}
                                onChange={handleChange}
                                required
                                className="auth-input"
                            />
                            <label>Email</label>
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

                        <div className="input-group">
                            <input
                                type="date"
                                name="birthday"
                                value={formData.birthday}
                                onChange={handleChange}
                                required
                                className="auth-input"
                            />
                            <label>Birthday</label>
                        </div>

                        <button
                            type="submit"
                            className={`auth-button ${isLoading ? 'loading' : ''}`}
                            disabled={isLoading || passwordErrors.length > 0}
                        >
                            {isLoading ? (
                                <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                    <FaSpinner className="spin-animation" />
                                    Processing...
                                </span>
                            ) : (
                                'Sign Up'
                            )}
                        </button>

                        {message && (
                            <div className={`auth-message ${
                                typeof message === 'string' && message.includes('successfully')
                                    ? 'message-success'
                                    : 'message-error'
                            }`}>
                                {message}
                            </div>
                        )}
                    </form>

                    <div className="auth-footer">
                        <p>Already have an account? <Link to="/sign-in" className="auth-link">Sign In</Link></p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SignUpForm;
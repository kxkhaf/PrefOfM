import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '@/api/axios';
import { FaCheckCircle, FaSpinner, FaSignOutAlt } from 'react-icons/fa';
import './SettingsPage.css';

const SettingsPage = () => {
    const [user, setUser] = useState({
        id: '',
        username: '',
        email: '',
        birthday: ''
    });
    const [editMode, setEditMode] = useState({
        email: false,
        password: false
    });
    const [form, setForm] = useState({
        email: '',
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
        emailCurrentPassword: ''
    });
    const [loading, setLoading] = useState({
        email: false,
        password: false,
        logout: false,
        logoutAll: false,
        emailConfirmation: false
    });
    const [errors, setErrors] = useState({});
    const [success, setSuccess] = useState({
        email: '',
        password: ''
    });
    const [passwordErrors, setPasswordErrors] = useState([]);
    const [emailChangePending, setEmailChangePending] = useState(false);
    const navigate = useNavigate();

    const formatBirthday = (dateString) => {
        if (!dateString || dateString === 'Not specified') return dateString;
        const [year, month, day] = dateString.split('-');
        return `${day}.${month}.${year}`;
    };

    useEffect(() => {
        const fetchUser = async () => {
            try {
                const response = await authApi.get('/profile', {
                    withCredentials: true
                });

                if (response.status === 401) {
                    navigate('/sign-in');
                    return;
                }

                const { data } = response;
                setUser({
                    id: data.id,
                    username: data.username || data.userName,
                    email: data.email,
                    birthday: data.birthday || 'Not specified'
                });
                setForm(prev => ({ ...prev, email: data.email }));

                if (data.pendingEmail) {
                    setEmailChangePending(true);
                }
            } catch (err) {
                console.error('Failed to fetch profile:', err);
                if (err.response?.status === 401) {
                    navigate('/sign-in');
                }
            }
        };

        fetchUser();
    }, [navigate]);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setForm(prev => ({ ...prev, [name]: value }));

        if (name === 'newPassword') {
            setPasswordErrors([]);
        }
    };

    const toggleEdit = (field) => {
        setEditMode(prev => ({ ...prev, [field]: !prev[field] }));
        setErrors({});
        setSuccess(prev => ({ ...prev, [field]: '' }));
        // Сбрасываем пароль при отмене редактирования
        if (field === 'email') {
            setForm(prev => ({ ...prev, emailCurrentPassword: '' }));
        }
        if (field === 'password') {
            setForm(prev => ({
                ...prev,
                currentPassword: '',
                newPassword: '',
                confirmPassword: ''
            }));
        }
    };

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

    const validate = (type) => {
        const newErrors = {};

        if (type === 'email') {
            if (!form.email.includes('@')) {
                newErrors.email = 'Invalid email address';
            }
            if (!form.emailCurrentPassword) {
                newErrors.emailCurrentPassword = 'Current password is required';
            }
        }

        if (type === 'password') {
            const pwdErrors = validatePassword(form.newPassword);
            if (pwdErrors.length > 0) {
                setPasswordErrors(pwdErrors);
                newErrors.password = 'Password does not meet requirements';
            }

            if (form.newPassword && form.newPassword !== form.confirmPassword) {
                newErrors.confirmPassword = 'Passwords do not match';
            }

            if (form.newPassword && !form.currentPassword) {
                newErrors.currentPassword = 'Current password is required';
            }
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const updateEmail = async (e) => {
        e.preventDefault();
        if (!validate('email')) return;

        setLoading(prev => ({ ...prev, email: true }));
        try {
            await authApi.put('/profile/email', {
                newEmail: form.email,
                currentPassword: form.emailCurrentPassword
            }, {
                withCredentials: true
            });

            setSuccess(prev => ({
                ...prev,
                email: 'Confirmation email has been sent to your new email address'
            }));
            setEmailChangePending(true);
            setTimeout(() => setSuccess(prev => ({ ...prev, email: '' })), 5000);
            toggleEdit('email');
        } catch (err) {
            if (err.response?.status === 401) {
                navigate('/sign-in');
            } else {
                setErrors({
                    email: err.response?.data?.message ||
                        'Failed to initiate email change. Please try again.'
                });
            }
        } finally {
            setLoading(prev => ({ ...prev, email: false }));
        }
    };

    const updatePassword = async (e) => {
        e.preventDefault();
        if (!validate('password')) return;

        setLoading(prev => ({ ...prev, password: true }));
        try {
            await authApi.put('/profile/password', {
                userId: user.id,
                currentPassword: form.currentPassword,
                newPassword: form.newPassword
            }, {
                withCredentials: true
            });

            setSuccess(prev => ({
                ...prev,
                password: 'Password updated successfully'
            }));
            setTimeout(() => setSuccess(prev => ({ ...prev, password: '' })), 3000);
            setForm(prev => ({
                ...prev,
                currentPassword: '',
                newPassword: '',
                confirmPassword: ''
            }));
            toggleEdit('password');
        } catch (err) {
            if (err.response?.status === 401) {
                navigate('/sign-in');
            } else {
                setErrors({
                    password: err.response?.data?.message ||
                        err.response?.data?.errors?.join(', ') ||
                        'Failed to update password'
                });
            }
        } finally {
            setLoading(prev => ({ ...prev, password: false }));
        }
    };

    const handleLogout = async () => {
        setLoading(prev => ({ ...prev, logout: true }));
        try {
            await authApi.post('/logout', {}, {
                withCredentials: true
            });
            navigate('/sign-in');
        } catch (err) {
            console.error('Logout failed:', err);
        } finally {
            setLoading(prev => ({ ...prev, logout: false }));
        }
    };

    const handleLogoutAllDevices = async () => {
        setLoading(prev => ({ ...prev, logoutAll: true }));
        try {
            await authApi.post('/logout/all', {}, {
                withCredentials: true
            });
            navigate('/sign-in');
        } catch (err) {
            console.error('Logout all failed:', err);
        } finally {
            setLoading(prev => ({ ...prev, logoutAll: false }));
        }
    };

    return (
        <div className="settings-container">
            <div className="settings-header">
                <button onClick={() => navigate(-1)} className="back-btn">
                    Profile
                </button>
                <h2>Account Settings</h2>
            </div>

            <div className="settings-card">
                <div className="info-row">
                    <span>Username:</span>
                    <strong>{user.username}</strong>
                </div>

                <div className="info-row">
                    <span>Birthday:</span>
                    <strong>{formatBirthday(user.birthday)}</strong>
                </div>

                <div className="section">
                    <div className="section-header">
                        <h3>Email</h3>
                        {!editMode.email && !emailChangePending ? (
                            <button onClick={() => toggleEdit('email')} className="edit-btn">
                                Change
                            </button>
                        ) : null}
                    </div>

                    {emailChangePending ? (
                        <div className="email-change-pending">
                            <p>Email change is pending confirmation. Please check your email.</p>
                            {success.email && (
                                <div className="success">
                                    <FaCheckCircle /> {success.email}
                                </div>
                            )}
                        </div>
                    ) : editMode.email ? (
                        <form onSubmit={updateEmail} className="edit-form">
                            <input
                                type="email"
                                name="email"
                                value={form.email}
                                onChange={handleChange}
                                placeholder="New email"
                            />
                            {errors.email && <div className="error">{errors.email}</div>}

                            <input
                                type="password"
                                name="emailCurrentPassword"
                                value={form.emailCurrentPassword}
                                onChange={handleChange}
                                placeholder="Current password"
                            />
                            {errors.emailCurrentPassword && (
                                <div className="error">{errors.emailCurrentPassword}</div>
                            )}

                            <div className="form-actions">
                                <button
                                    type="button"
                                    onClick={() => toggleEdit('email')}
                                    className="cancel-btn"
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    disabled={loading.email}
                                    className="save-btn"
                                >
                                    {loading.email ? (
                                        <span className="btn-loading">
                                            <FaSpinner className="spin" /> Sending...
                                        </span>
                                    ) : 'Send Confirmation'}
                                </button>
                            </div>
                            {success.email && (
                                <div className="success">
                                    <FaCheckCircle /> {success.email}
                                </div>
                            )}
                        </form>
                    ) : (
                        <div className="current-value">{user.email}</div>
                    )}
                </div>

                <div className="section">
                    <div className="section-header">
                        <h3>Password</h3>
                        {!editMode.password ? (
                            <button onClick={() => toggleEdit('password')} className="edit-btn">
                                Change
                            </button>
                        ) : null}
                    </div>

                    {editMode.password ? (
                        <form onSubmit={updatePassword} className="edit-form">
                            <input
                                type="password"
                                name="currentPassword"
                                value={form.currentPassword}
                                onChange={handleChange}
                                placeholder="Current password"
                            />
                            {errors.currentPassword && (
                                <div className="error">{errors.currentPassword}</div>
                            )}

                            <input
                                type="password"
                                name="newPassword"
                                value={form.newPassword}
                                onChange={handleChange}
                                placeholder="New password"
                            />

                            <input
                                type="password"
                                name="confirmPassword"
                                value={form.confirmPassword}
                                onChange={handleChange}
                                placeholder="Confirm new password"
                            />
                            {errors.confirmPassword && (
                                <div className="error">{errors.confirmPassword}</div>
                            )}

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

                            <div className="form-actions">
                                <button
                                    type="button"
                                    onClick={() => toggleEdit('password')}
                                    className="cancel-btn"
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    disabled={loading.password || passwordErrors.length > 0}
                                    className="save-btn"
                                >
                                    {loading.password ? (
                                        <span className="btn-loading">
                                            <FaSpinner className="spin" /> Saving...
                                        </span>
                                    ) : 'Save Password'}
                                </button>
                            </div>
                            {success.password && (
                                <div className="success">
                                    <FaCheckCircle /> {success.password}
                                </div>
                            )}
                            {errors.password && (
                                <div className="error">{errors.password}</div>
                            )}
                        </form>
                    ) : (
                        <div className="current-value">••••••••</div>
                    )}
                </div>

                <div className="logout-section">
                    <button
                        onClick={handleLogout}
                        className="logout-btn"
                        disabled={loading.logout}
                    >
                        <FaSignOutAlt />
                        {loading.logout ? 'Logging out...' : 'Logout'}
                    </button>
                    <button
                        onClick={handleLogoutAllDevices}
                        className="logout-all-btn"
                        disabled={loading.logoutAll}
                    >
                        <FaSignOutAlt />
                        {loading.logoutAll ? 'Logging out all...' : 'Logout All Devices'}
                    </button>
                </div>
            </div>
        </div>
    );
};

export default SettingsPage;
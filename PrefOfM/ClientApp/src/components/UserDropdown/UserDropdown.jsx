import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import './UserDropdown.css';

const UserDropdown = ({ onLogout }) => {
    const [isOpen, setIsOpen] = useState(false);
    const username = localStorage.getItem('username') || "User";
    const timeoutRef = useRef(null);
    const dropdownRef = useRef(null);
    const navigate = useNavigate();

    const resetTimer = () => {
        if (timeoutRef.current) {
            clearTimeout(timeoutRef.current);
        }
        timeoutRef.current = setTimeout(() => {
            setIsOpen(false);
        }, 3000); // Уменьшил время до 3 секунд
    };

    const handleButtonClick = () => {
        setIsOpen(!isOpen);
        if (!isOpen) {
            resetTimer();
        }
    };

    const handleInteraction = () => {
        resetTimer();
    };

    const handleNavigation = (path) => {
        setIsOpen(false);
        navigate(path);
    };

    const handleLogout = () => {
        setIsOpen(false);
        onLogout();
    };

    // Закрытие dropdown при клике вне его области
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    return (
        <div
            className="user-dropdown-container"
            onMouseEnter={handleInteraction}
            onMouseLeave={() => timeoutRef.current && clearTimeout(timeoutRef.current)}
            ref={dropdownRef}
        >
            <button
                className={`user-button ${isOpen ? 'active' : ''}`}
                onClick={handleButtonClick}
                aria-haspopup="true"
                aria-expanded={isOpen}
            >
                <span className="username">{username}</span>
                <span className={`arrow ${isOpen ? 'up' : 'down'}`}>▼</span>
            </button>
            {isOpen && (
                <div
                    className="dropdown-menu"
                    onMouseEnter={handleInteraction}
                >
                    <button
                        className="dropdown-item"
                        onClick={() => handleNavigation('/profile')}
                    >
                        Profile
                    </button>
                    <button
                        className="dropdown-item"
                        onClick={() => handleNavigation('/settings')}
                    >
                        Settings
                    </button>
                    <div className="dropdown-divider"></div>
                    <button
                        className="dropdown-item logout"
                        onClick={handleLogout}
                    >
                        Logout
                    </button>
                </div>
            )}
        </div>
    );
};

export default UserDropdown;
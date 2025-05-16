import {useState, useRef, useEffect, useCallback} from 'react';
import {Link, Outlet, useNavigate} from 'react-router-dom';
import {logout} from '@/api/axios';
import UserDropdown from '@/components/UserDropdown/UserDropdown';
import {useSearch} from '@/contexts/SearchContext';
import './MainLayout.css';
import {useTab} from "@/contexts/TabsContext.jsx";
import {useEmotion} from "../contexts/EmotionContext.jsx";
import {useFastStart} from "../contexts/FastStartContext.jsx";

const MainLayout = () => {
    const { selectedEmotion, setSelectedEmotion } = useEmotion();
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const {searchQuery, setSearchQuery, setIsSearching} = useSearch();
    const emotions = ['Happy', 'Sad', 'Neutral', 'Angry', 'Emotion'];
    const timeoutRef = useRef(null);
    const navigate = useNavigate();
    const { setActiveTab } = useTab();
    const { setIsFastStart } = useFastStart();
    
    const mappingRules = {
        'Happy': 'Happy',
        'Sad': 'Melancholic',
        'Neutral': 'Relaxing',
        'Angry': 'Energetic',
        'Other': 'Emotion',
    };
    
    
    function emotionMapper(emotion) {
        let output = emotion;
        for (const [key, value] of Object.entries(mappingRules)) {
            output = output.replace(new RegExp(key, 'gi'), value);
        }
        return output;
    }

    const handleFastStart = () => {
        navigate('/');
        setActiveTab('main');
        setIsFastStart(true);
    };
    
    const handleLogout = async () => {
        await logout();
    };

    const handleSearch = useCallback((e) => {
        e.preventDefault();
        if (!searchQuery.trim()) {
            setIsSearching(false);
            return;
        }
        setIsSearching(true);
        navigate('/');
        setActiveTab('main');
    }, [searchQuery, setIsSearching, navigate, setActiveTab]);

    const resetTimeout = () => {
        if (timeoutRef.current) {
            clearTimeout(timeoutRef.current);
        }
        timeoutRef.current = setTimeout(() => {
            setIsDropdownOpen(false);
        }, 5000);
    };

    const handleMouseEnter = () => {
        setIsDropdownOpen(true);
        resetTimeout();
    };

    const handleMouseLeave = () => {
        resetTimeout();
    };

    useEffect(() => {
        return () => {
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
        };
    }, []);
    
    const scrollToTop = () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    };

    return (
        <div className="main-layout">
            <header className="header">
                <a href="/" className="logo-link" onClick={(e) => {
                    e.preventDefault();
                    window.location.href = "/";
                }}>
                    <div
                        className="logo">PrefOfM
                    </div>
                </a>

                <div className="search-container">
                    <button className="scroll-top-button" onClick={scrollToTop}></button>
                    <form onSubmit={handleSearch} className="search-form">
                        <input
                            type="text"
                            placeholder="Search songs or artists..."
                            className="search-input"
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            onKeyDown={(e) => e.key === 'Enter' && handleSearch(e)}
                        />
                        <button
                            type="submit"
                            className="search-button"
                        >
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                 strokeWidth="2">
                                <circle cx="11" cy="11" r="8"/>
                                <path d="M21 21l-4.35-4.35"/>
                            </svg>
                        </button>
                    </form>
                </div>

                <nav className="nav">
                    <div
                        className="emotions-container"
                        onMouseEnter={handleMouseEnter}
                        onMouseLeave={handleMouseLeave}
                    >
                        <button className="nav-button emotions-btn">
                            {selectedEmotion || 'Emotion'}
                        </button>
                        {isDropdownOpen && (
                            <div
                                className="emotions-panel"
                                onMouseEnter={resetTimeout}
                            >
                                {emotions.map(emotion => (
                                    <button
                                        key={emotion}
                                        className={`emotion-option ${selectedEmotion === emotion ? 'selected' : ''}`}
                                        onClick={() => {
                                            setSelectedEmotion(emotionMapper(emotion));
                                            setIsDropdownOpen(false);
                                        }}
                                        title={emotion}
                                    >
                                        {emotionMapper(emotion)}
                                    </button>
                                ))}
                            </div>
                        )}
                    </div>
                    <button className="nav-button fast-start" onClick={handleFastStart}>Fast Start</button>
                    <UserDropdown onLogout={handleLogout}/>
                </nav>
            </header>
            <main className="content">
                <Outlet/>
            </main>
        </div>
    );
};

export default MainLayout;
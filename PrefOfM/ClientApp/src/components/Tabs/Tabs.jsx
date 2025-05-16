import { useEffect, useRef, useState } from "react";
import './Tabs.css';

const Tabs = ({ activeTab, onTabChange, playlistName }) => {
    const baseTabs = [
        { id: 'main', label: 'Main' },
        { id: 'playlists', label: 'Playlists' },
        { id: 'favourite', label: 'Favourite' },
        { id: 'recent', label: 'Last' }
    ];

    const [tabs, setTabs] = useState(baseTabs);
    const [isHidden, setIsHidden] = useState(false);
    const lastScrollY = useRef(window.scrollY);
    const hoverTimeout = useRef(null);
    
    useEffect(() => {
        const updatedTabs = baseTabs.map(tab => {
            if (tab.id === 'playlists') {
                return {
                    ...tab,
                    label: activeTab === 'playlists' && playlistName
                        ? truncateLabel(playlistName)
                        : 'Playlists'
                };
            }
            return tab;
        });
        setTabs(updatedTabs);
    }, [activeTab, playlistName]);

    const truncateLabel = (label, maxLength = 12) => {
        if (!label) return 'Playlists';
        return label.length > maxLength
            ? `${label.substring(0, maxLength)}...`
            : label;
    };

    const handleMouseEnter = () => {
        clearTimeout(hoverTimeout.current);
        setIsHidden(false);
    };

    const handleMouseLeave = () => {
        hoverTimeout.current = setTimeout(() => {
            setIsHidden(true);
        }, 3000);
    };

    useEffect(() => {
        const handleScroll = () => {
            const currentScrollY = window.scrollY;
            setIsHidden(currentScrollY > lastScrollY.current);
            lastScrollY.current = currentScrollY;
        };

        window.addEventListener('scroll', handleScroll, { passive: true });
        return () => window.removeEventListener('scroll', handleScroll);
    }, []);

    return (
        <div
            className={`tabs ${isHidden ? 'hidden' : ''}`}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
        >
            {tabs.map(tab => (
                <button
                    key={tab.id}
                    className={`tab ${activeTab === tab.id ? 'active' : ''}`}
                    onClick={() => onTabChange(tab.id)}
                    title={tab.id === 'playlists' && playlistName?.length > 12 ? playlistName : undefined}
                >
                    {tab.label}
                </button>
            ))}
        </div>
    );
};

export default Tabs;
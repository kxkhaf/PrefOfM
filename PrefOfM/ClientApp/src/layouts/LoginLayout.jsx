import { Outlet, useNavigate } from 'react-router-dom';
import { logout } from '@/api/axios';
import { useRef, useEffect, useState } from 'react';
import './LogoLayout.css';

const LoginLayout = () => {
    const navigate = useNavigate();
    const logoContainerRef = useRef(null);
    const [logos, setLogos] = useState([]);


    useEffect(() => {
        const updateLogos = () => {
            if (logoContainerRef.current) {
                const containerWidth = logoContainerRef.current.offsetWidth;
                const logoWidth = 160;
                const count = Math.ceil(containerWidth / logoWidth) * 3;
                
                const styles = [
                    {
                        color: '#ffd67b',
                        background: 'linear-gradient(45deg, #F0F, #0FF)',
                        WebkitBackgroundClip: 'text',
                        animation: 'slide 10s infinite linear'
                    },
                    {
                        color: '#81ffff',
                        stroke: '1.2px #00F',
                        textShadow: '0 0 10px #FFF, 0 0 20px #0FF',
                        animation: 'pulse 10s infinite'
                    },
                    {
                        color: '#ffabff',
                        stroke: '1.5px #800080',
                        background: 'linear-gradient(45deg, #F0F, #0FF)',
                        WebkitBackgroundClip: 'text',
                        animation: 'slide 10s infinite linear'
                    },
                    {
                        color: '#ff9898',
                        stroke: '1px #8B0000',
                        transform: 'skewX(-10deg)',
                        animation: 'shake 10s infinite alternate'
                    },
                    {
                        color: '#c1ff85',
                        stroke: '1.3px #006400',
                        textShadow: '0 0 5px #FFF, 0 0 10px #7BFF00',
                        animation: 'zoom 10s infinite alternate'
                    }
                ];

                const logosArray = Array.from({ length: count }).map((_, i) => ({
                    id: i,
                    text: 'PrefOfM',
                    style: {
                        ...styles[i % styles.length],
                        fontSize: '2rem',
                        display: 'inline-block',
                        padding: '0 1.5rem',
                        fontWeight: 900
                    }
                }));

                setLogos(logosArray);
            }
        };

        updateLogos();
        window.addEventListener('resize', updateLogos);

        return () => {
            window.removeEventListener('resize', updateLogos);
        };
    }, []);

    return (
        <div className="main-layout">
            <header className="header-logo" ref={logoContainerRef}>
                <div className="logo-scroller"
                     onClick={() => navigate('/')}
                     style={{ cursor: 'pointer' }} >
                    {logos.map((logo) => (
                        <span
                            key={logo.id}
                            className="logotype"
                            style={logo.style}
                        >
                            {logo.text}
                        </span>
                    ))}
                </div>
            </header>

            <main className="content">
                <Outlet/>
            </main>
        </div>
    );
};

export default LoginLayout;
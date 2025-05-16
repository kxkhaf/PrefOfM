import { Navigate, Outlet } from 'react-router-dom';
import { useEffect, useState, useRef } from 'react';
import { authApi } from '@/api/axios';
import LoadingSpinner from "../LoadingSpinner.jsx";

const ProtectedRoute = () => {
    const [isAuthenticated, setIsAuthenticated] = useState(null);
    const isCalledRef = useRef(false); // Флаг для предотвращения дублирования

    useEffect(() => {
        if (isCalledRef.current) return; // Уже вызывался? Пропускаем.
        isCalledRef.current = true;

        const verifyAuth = async () => {
            try {
                await authApi.get('/refresh');
                setIsAuthenticated(true);
            } catch (err) {
                console.error("Auth error:", err);
                setIsAuthenticated(false);
            }
        };

        verifyAuth();
    }, []);

    if (isAuthenticated === null) {
        return <LoadingSpinner />;
    }

    return isAuthenticated ? <Outlet /> : <Navigate to="/sign-in" replace />;
};

export default ProtectedRoute;
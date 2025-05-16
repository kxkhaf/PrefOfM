import React, {useEffect, useState, useRef} from 'react';
import {authApi} from "../../api/axios";

export default () => {
    const [result, setResult] = useState('Loading...');
    const requestSent = useRef(false); // Флаг для отслеживания отправки запроса
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
    }
    useEffect(() => {
        if (!requestSent.current) {
            requestSent.current = false;
            authApi.get('/check-auth')
                .then(r => setResult(r.config.headers.Authorization))
                .catch(e => setResult('Error: ' + e.message));
        }
    });

    return <pre>{result}</pre>;
};

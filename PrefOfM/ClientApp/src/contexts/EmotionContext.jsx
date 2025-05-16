// src/contexts/EmotionContext.jsx
import { createContext, useState, useContext } from 'react';

const EmotionContext = createContext();

export const EmotionProvider = ({ children }) => {
    const [selectedEmotion, setSelectedEmotion] = useState(null);

    return (
        <EmotionContext.Provider value={{ selectedEmotion, setSelectedEmotion }}>
            {children}
        </EmotionContext.Provider>
    );
};

export const useEmotion = () => useContext(EmotionContext);
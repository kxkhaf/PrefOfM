import { createContext, useState, useContext } from 'react';

const FastStartContext = createContext();

export const FastStartProvider = ({ children }) => {
    const [isFastStart, setIsFastStart] = useState(false);

    return (
        <FastStartContext.Provider value={{ isFastStart, setIsFastStart }}>
            {children}
        </FastStartContext.Provider>
    );
};

export const useFastStart = () => useContext(FastStartContext);
import { createContext, useContext, useState } from 'react';

const TabContext = createContext();

export const TabProvider = ({ children }) => {
    const [activeTab, setActiveTab] = useState(null);

    return (
        <TabContext.Provider value={{ activeTab, setActiveTab }}>
            {children}
        </TabContext.Provider>
    );
};

export const useTab = () => useContext(TabContext);
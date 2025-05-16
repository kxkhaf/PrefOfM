import { createContext, useContext, useState } from 'react';

const SearchContext = createContext();

export const SearchProvider = ({ children }) => {
    const [searchResults, setSearchResults] = useState([]);
    const [isSearching, setIsSearching] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    

    return (
        <SearchContext.Provider
            value={{
                searchResults,
                setSearchResults,
                isSearching,
                setIsSearching,
                searchQuery,
                setSearchQuery
            }}
        >
            {children}
        </SearchContext.Provider>
    );
};

export const useSearch = () => useContext(SearchContext);
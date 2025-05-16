import { useState } from 'react';
import { musicApi } from '@/api/axios';
import './SeedPage.css'; // Создайте этот файл с вашими стилями

const SeedPage = () => {
    const [seedValue, setSeedValue] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState('');
    const [error, setError] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        setMessage('');
        setError('');

        try {
            const response = await musicApi.post(`/api/db/seed/${seedValue}`);
            setMessage(response.data.message || 'Database seeded successfully');
        } catch (err) {
            setError(err.response?.data?.error || 'Failed to seed database');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="seed-page home-page">
            <div className="seed-container">
                <h2>Database Seeding</h2>
                <form onSubmit={handleSubmit} className="seed-form">
                    <div className="form-group">
                        <label htmlFor="seedValue">Seed Parameter:</label>
                        <input
                            type="text"
                            id="seedValue"
                            value={seedValue}
                            onChange={(e) => setSeedValue(e.target.value)}
                            placeholder="Enter seed parameter"
                            required
                        />
                    </div>

                    <button
                        type="submit"
                        className="seed-button"
                        disabled={isLoading}
                    >
                        {isLoading ? 'Seeding...' : 'Seed Database'}
                    </button>
                </form>

                {message && (
                    <div className="seed-message success">
                        {message}
                    </div>
                )}

                {error && (
                    <div className="seed-message error">
                        {error}
                    </div>
                )}
            </div>
        </div>
    );
};

export default SeedPage;
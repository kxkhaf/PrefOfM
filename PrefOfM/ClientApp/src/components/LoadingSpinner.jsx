import { FaSpinner } from 'react-icons/fa';

const LoadingSpinner = ({ text = "Loading..." }) => (
    <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <FaSpinner className="spin-animation" />
        {text}
    </span>
);

export default LoadingSpinner;
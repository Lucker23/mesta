import { createRoot } from 'react-dom/client';
import { App } from './pages/HomePage';
import './global.css';

createRoot(
  document.querySelector('#app'),
).render(<App />);

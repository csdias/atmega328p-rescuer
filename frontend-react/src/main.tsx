import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './web/App';
import './web/styles.css';

const raiz = document.getElementById('raiz');

if (!raiz) throw new Error('Falta o elemento #raiz no index.html.');

createRoot(raiz).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

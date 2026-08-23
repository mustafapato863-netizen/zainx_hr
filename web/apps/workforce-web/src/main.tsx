import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryProvider, StoreProvider, RouterProvider } from './providers';
import './providers/i18n'; // Initialize i18n
import './styles.css'; // Ensure tailwind is loaded

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <StoreProvider>
      <QueryProvider>
        <RouterProvider />
      </QueryProvider>
    </StoreProvider>
  </React.StrictMode>
);

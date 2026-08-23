import React, { useEffect, useState } from 'react';

export function App() {
  const [health, setHealth] = useState<string>('checking...');

  useEffect(() => {
    fetch('/health')
      .then((res) => res.json())
      .then((data) => setHealth(data.status || 'healthy'))
      .catch(() => setHealth('offline'));
  }, []);

  return (
    <main style={{ fontFamily: 'system-ui, sans-serif', padding: '2rem' }}>
      <h1>ZainX Workforce</h1>
      <p data-testid="health-status">System Status: {health}</p>
    </main>
  );
}

export default App;

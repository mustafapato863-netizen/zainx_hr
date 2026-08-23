// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import App from './App';

describe('App', () => {
  it('should render title', () => {
    render(<App />);
    expect(screen.getByText(/ZainX Workforce/i)).toBeDefined();
  });
});

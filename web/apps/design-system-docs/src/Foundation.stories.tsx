import React from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import { BrandTokens } from '@zainx/design-system';

const meta: Meta = {
  title: 'Foundation/DesignTokens',
  component: () => (
    <div style={{ fontFamily: 'system-ui, sans-serif', padding: '1.5rem' }}>
      <h2>ZainX Design System Tokens</h2>
      <div style={{ display: 'flex', gap: '1rem', marginTop: '1rem' }}>
        <div style={{ padding: '1rem', background: BrandTokens.colors.primary, color: 'white', borderRadius: '8px' }}>
          Primary: {BrandTokens.colors.primary}
        </div>
        <div style={{ padding: '1rem', background: BrandTokens.colors.accent, color: 'white', borderRadius: '8px' }}>
          Accent: {BrandTokens.colors.accent}
        </div>
      </div>
    </div>
  ),
};

export default meta;
type Story = StoryObj;

export const Default: Story = {};

import type { Preview } from '@storybook/react';
import React from 'react';
import '../src/styles/theme.css';

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    layout: 'centered',
  },
  globalTypes: {
    theme: {
      description: 'Global theme for components',
      defaultValue: 'light',
      toolbar: {
        title: 'Theme',
        icon: 'circlehollow',
        items: [
          { value: 'light', icon: 'sun', title: 'Light' },
          { value: 'dark', icon: 'moon', title: 'Dark' },
        ],
        dynamicTitle: true,
      },
    },
    direction: {
      description: 'Direction for components',
      defaultValue: 'ltr',
      toolbar: {
        title: 'Direction',
        icon: 'transfer',
        items: [
          { value: 'ltr', title: 'LTR (Left-to-Right)' },
          { value: 'rtl', title: 'RTL (Right-to-Left / Arabic)' },
        ],
        dynamicTitle: true,
      },
    },
  },
  decorators: [
    (Story, context) => {
      const theme = context.globals.theme || 'light';
      const direction = context.globals.direction || 'ltr';

      return React.createElement(
        'div',
        {
          'data-theme': theme,
          dir: direction,
          className: `${theme === 'dark' ? 'dark' : ''} min-h-screen bg-canvas text-text-primary p-6 font-sans ${
            direction === 'rtl' ? 'font-arabic' : ''
          }`,
        },
        React.createElement(Story)
      );
    },
  ],
};

export default preview;

import type { StorybookConfig } from '@storybook/react-vite';
import path from 'node:path';

const config: StorybookConfig = {
  stories: ['../src/**/*.stories.@(js|jsx|ts|tsx|mdx)'],
  addons: [],
  framework: {
    name: '@storybook/react-vite',
    options: {},
  },
  async viteFinal(config) {
    config.resolve = config.resolve || {};
    config.resolve.alias = {
      ...config.resolve.alias,
      '@zainx/design-system': path.resolve(import.meta.dirname, '../../../packages/design-system/src/index.ts'),
      '@zainx/platform': path.resolve(import.meta.dirname, '../../../packages/platform/src/index.ts'),
      '@zainx/contracts': path.resolve(import.meta.dirname, '../../../packages/contracts/src/index.ts'),
      '@zainx/people': path.resolve(import.meta.dirname, '../../../packages/people/src/index.ts'),
    };
    return config;
  },
};

export default config;

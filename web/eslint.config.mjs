import js from '@eslint/js';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  {
    ignores: [
      '**/dist/**',
      '**/node_modules/**',
      '**/.nx/**',
      '**/coverage/**',
      '**/packages/contracts/src/generated/**',
      '**/storybook-static/**',
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ['**/*.{ts,tsx,js,jsx}'],
    languageOptions: {
      globals: {
        window: 'readonly',
        document: 'readonly',
        console: 'readonly',
        fetch: 'readonly',
        setTimeout: 'readonly',
        clearTimeout: 'readonly',
        HTMLElement: 'readonly',
        HTMLInputElement: 'readonly',
        HTMLTextAreaElement: 'readonly',
        HTMLButtonElement: 'readonly',
        HTMLDivElement: 'readonly',
        HTMLSpanElement: 'readonly',
        HTMLSelectElement: 'readonly',
        React: 'readonly',
        __dirname: 'readonly',
      },
    },
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-unused-vars': 'off',
      '@typescript-eslint/no-empty-object-type': 'off',
      'no-unused-vars': 'off',
      'no-undef': 'off',
    },
  },
  {
    // Enforce Nx / Architecture Dependency Boundaries on Feature Packages & Apps
    // Feature packages and apps must consume @zainx/design-system and cannot bypass it
    files: [
      'packages/people/**/*.{ts,tsx}',
      'packages/payroll/**/*.{ts,tsx}',
      'packages/attendance/**/*.{ts,tsx}',
      'packages/leave/**/*.{ts,tsx}',
      'packages/recruitment/**/*.{ts,tsx}',
      'packages/approvals/**/*.{ts,tsx}',
      'packages/reports/**/*.{ts,tsx}',
      'packages/ai/**/*.{ts,tsx}',
      'packages/administration/**/*.{ts,tsx}',
      'packages/platform/**/*.{ts,tsx}',
      'apps/**/*.{ts,tsx}',
    ],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: 'react-aria-components',
              message: 'Feature packages and apps must consume @zainx/design-system rather than importing react-aria-components directly.',
            },
            {
              name: 'ag-grid-enterprise',
              message: 'Feature packages and apps must consume ZainXDataGrid from @zainx/design-system rather than importing ag-grid-enterprise directly.',
            },
            {
              name: 'ag-grid-community',
              message: 'Feature packages and apps must consume ZainXDataGrid from @zainx/design-system rather than importing ag-grid-community directly.',
            },
            {
              name: 'ag-grid-react',
              message: 'Feature packages and apps must consume ZainXDataGrid from @zainx/design-system rather than importing ag-grid-react directly.',
            },
            {
              name: 'echarts',
              message: 'Feature packages and apps must consume ZainXChart from @zainx/design-system rather than importing echarts directly.',
            },
            {
              name: 'echarts-for-react',
              message: 'Feature packages and apps must consume ZainXChart from @zainx/design-system rather than importing echarts-for-react directly.',
            },
            {
              name: 'lucide-react',
              message: 'Feature packages and apps must consume Icon from @zainx/design-system rather than importing lucide-react directly.',
            },
            {
              name: 'motion',
              message: 'Feature packages and apps must consume animations from @zainx/design-system rather than importing motion directly.',
            },
          ],
          patterns: [
            {
              group: ['@radix-ui/*'],
              message: 'Radix UI is prohibited. Use React Aria Components via @zainx/design-system.',
            },
            {
              group: ['@fullcalendar/*'],
              message: 'Feature packages and apps must consume ZainXScheduler from @zainx/design-system rather than importing @fullcalendar directly.',
            },
            {
              group: ['@tiptap/*'],
              message: 'Feature packages and apps must consume ZainXRichTextEditor from @zainx/design-system rather than importing @tiptap directly.',
            },
            {
              group: ['@dnd-kit/*'],
              message: 'Feature packages and apps must consume interaction utilities from @zainx/design-system rather than importing @dnd-kit directly.',
            },
          ],
        },
      ],
    },
  }
);

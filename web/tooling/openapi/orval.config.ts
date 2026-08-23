import { defineConfig } from 'orval';

export default defineConfig({
  workforce: {
    input: {
      target: './workforce.openapi.json',
    },
    output: {
      target: '../../packages/contracts/src/api/generated.ts',
      client: 'react-query',
      mode: 'split',
      override: {
        mutator: {
          path: '../../packages/contracts/src/api/axios-instance.ts',
          name: 'customInstance'
        }
      }
    },
  },
});

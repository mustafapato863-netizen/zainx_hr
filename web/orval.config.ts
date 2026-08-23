import { defineConfig } from 'orval';

export default defineConfig({
  zainx: {
    input: './tooling/openapi/workforce.openapi.json',
    output: {
      target: './packages/contracts/rest-generated/workforceApi.ts',
      schemas: './packages/contracts/rest-generated/models',
      client: 'react-query',
      mock: false,
    },
  },
});

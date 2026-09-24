import tseslint from 'typescript-eslint';
import { reactConfig } from '@hypnonema/eslint-config/react';

export default tseslint.config([
  ...reactConfig,
  {
    files: ['**/*.{ts,tsx}'],
    ignores: ['vite.config.ts'],
    rules: {
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
    },
  },
]);

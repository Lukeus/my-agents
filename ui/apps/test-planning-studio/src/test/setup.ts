import { beforeAll, afterEach, afterAll } from 'vitest';
import { cleanup } from '@vue/test-utils';

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Global test setup
beforeAll(() => {
  // Add any global setup here
});

afterAll(() => {
  // Add any global cleanup here
});

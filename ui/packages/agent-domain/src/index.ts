/**
 * @agents/agent-domain
 * TypeScript domain contracts with Zod validation
 * Mirrors the C# domain layer
 */

// Core agent types
export * from './agents';

// Agent-specific types
export * from './notification';
export * from './devops';
export * from './testplanning';

// Re-export zod for convenience
export { z } from 'zod';

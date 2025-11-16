import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useTestSpecs } from './useTestSpecs';
import { TestPlanningClient } from '@agents/api-client';
import type { TestSpec } from '@agents/agent-domain';

// Mock the TestPlanningClient
vi.mock('@agents/api-client', () => ({
  TestPlanningClient: vi.fn().mockImplementation(() => ({
    listTestSpecs: vi.fn(),
    getTestSpec: vi.fn(),
    createTestSpec: vi.fn(),
    updateTestSpec: vi.fn(),
    deleteTestSpec: vi.fn(),
  })),
}));

describe('useTestSpecs', () => {
  let mockClient: any;

  beforeEach(() => {
    mockClient = new TestPlanningClient('http://localhost:7010');
    vi.clearAllMocks();
  });

  describe('fetchSpecs', () => {
    it('should fetch specs successfully', async () => {
      const mockSpecs: TestSpec[] = [
        {
          id: '1',
          feature: 'Login',
          description: 'User login feature',
          scenarios: [],
          status: 'active',
        },
      ];

      mockClient.listTestSpecs.mockResolvedValue({
        isSuccess: true,
        output: mockSpecs,
        duration: 'PT0.5S',
      });

      const { specs, loading, error, fetchSpecs } = useTestSpecs();

      expect(loading.value).toBe(false);
      await fetchSpecs();

      expect(loading.value).toBe(false);
      expect(error.value).toBe(null);
      expect(specs.value).toEqual(mockSpecs);
    });

    it('should handle fetch error', async () => {
      mockClient.listTestSpecs.mockResolvedValue({
        isSuccess: false,
        errorMessage: 'Server error',
        duration: 'PT0.1S',
      });

      const { specs, error, fetchSpecs } = useTestSpecs();

      await fetchSpecs();

      expect(error.value).toBe('Server error');
      expect(specs.value).toEqual([]);
    });

    it('should handle fetch exception', async () => {
      mockClient.listTestSpecs.mockRejectedValue(new Error('Network error'));

      const { error, fetchSpecs } = useTestSpecs();

      await fetchSpecs();

      expect(error.value).toBe('Network error');
    });
  });

  describe('getSpec', () => {
    it('should get a single spec successfully', async () => {
      const mockSpec: TestSpec = {
        id: '1',
        feature: 'Login',
        description: 'User login feature',
        scenarios: [],
        status: 'active',
      };

      mockClient.getTestSpec.mockResolvedValue({
        isSuccess: true,
        output: mockSpec,
        duration: 'PT0.3S',
      });

      const { getSpec } = useTestSpecs();
      const result = await getSpec('1');

      expect(result).toEqual(mockSpec);
    });

    it('should return null on error', async () => {
      mockClient.getTestSpec.mockResolvedValue({
        isSuccess: false,
        errorMessage: 'Not found',
        duration: 'PT0.1S',
      });

      const { getSpec, error } = useTestSpecs();
      const result = await getSpec('999');

      expect(result).toBe(null);
      expect(error.value).toBe('Not found');
    });
  });

  describe('createSpec', () => {
    it('should create spec successfully', async () => {
      const newSpec = {
        feature: 'Register',
        description: 'User registration',
        scenarios: [],
        status: 'draft' as const,
      };

      const createdSpec: TestSpec = { ...newSpec, id: '2' };

      mockClient.createTestSpec.mockResolvedValue({
        isSuccess: true,
        output: createdSpec,
        duration: 'PT0.8S',
      });

      mockClient.listTestSpecs.mockResolvedValue({
        isSuccess: true,
        output: [createdSpec],
        duration: 'PT0.2S',
      });

      const { createSpec } = useTestSpecs();
      const result = await createSpec(newSpec);

      expect(result).toEqual(createdSpec);
      expect(mockClient.createTestSpec).toHaveBeenCalledWith(newSpec);
    });

    it('should handle create error', async () => {
      const newSpec = {
        feature: 'Test',
        description: 'Test',
        scenarios: [],
        status: 'draft' as const,
      };

      mockClient.createTestSpec.mockResolvedValue({
        isSuccess: false,
        errorMessage: 'Validation failed',
        duration: 'PT0.1S',
      });

      const { createSpec, error } = useTestSpecs();
      const result = await createSpec(newSpec);

      expect(result).toBe(null);
      expect(error.value).toBe('Validation failed');
    });
  });

  describe('updateSpec', () => {
    it('should update spec successfully', async () => {
      const updates = {
        description: 'Updated description',
      };

      const updatedSpec: TestSpec = {
        id: '1',
        feature: 'Login',
        description: 'Updated description',
        scenarios: [],
        status: 'active',
      };

      mockClient.updateTestSpec.mockResolvedValue({
        isSuccess: true,
        output: updatedSpec,
        duration: 'PT0.5S',
      });

      mockClient.listTestSpecs.mockResolvedValue({
        isSuccess: true,
        output: [updatedSpec],
        duration: 'PT0.2S',
      });

      const { updateSpec } = useTestSpecs();
      const result = await updateSpec('1', updates);

      expect(result).toEqual(updatedSpec);
    });
  });

  describe('deleteSpec', () => {
    it('should delete spec successfully', async () => {
      mockClient.deleteTestSpec.mockResolvedValue({
        isSuccess: true,
        duration: 'PT0.3S',
      });

      mockClient.listTestSpecs.mockResolvedValue({
        isSuccess: true,
        output: [],
        duration: 'PT0.2S',
      });

      const { deleteSpec } = useTestSpecs();
      const result = await deleteSpec('1');

      expect(result).toBe(true);
      expect(mockClient.deleteTestSpec).toHaveBeenCalledWith('1');
    });

    it('should handle delete error', async () => {
      mockClient.deleteTestSpec.mockResolvedValue({
        isSuccess: false,
        errorMessage: 'Cannot delete',
        duration: 'PT0.1S',
      });

      const { deleteSpec, error } = useTestSpecs();
      const result = await deleteSpec('1');

      expect(result).toBe(false);
      expect(error.value).toBe('Cannot delete');
    });
  });
});

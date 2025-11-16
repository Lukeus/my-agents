/**
 * Base HTTP client with Zod validation
 */

import axios, { type AxiosInstance, type AxiosRequestConfig, type AxiosError } from 'axios';
import { z } from 'zod';

export interface ApiError {
  message: string;
  status?: number;
  code?: string;
  details?: unknown;
}

export class BaseClient {
  protected readonly http: AxiosInstance;

  constructor(baseURL: string) {
    this.http = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 30000, // 30 seconds
    });

    // Add response interceptor for error handling
    this.http.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        return Promise.reject(this.handleError(error));
      }
    );
  }

  /**
   * GET request with Zod validation
   */
  protected async get<T>(
    url: string,
    schema: z.ZodType<T>,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await this.http.get(url, config);
    return schema.parse(response.data);
  }

  /**
   * POST request with Zod validation
   */
  protected async post<T>(
    url: string,
    data: unknown,
    schema: z.ZodType<T>,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await this.http.post(url, data, config);
    return schema.parse(response.data);
  }

  /**
   * PUT request with Zod validation
   */
  protected async put<T>(
    url: string,
    data: unknown,
    schema: z.ZodType<T>,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await this.http.put(url, data, config);
    return schema.parse(response.data);
  }

  /**
   * DELETE request with Zod validation
   */
  protected async delete<T>(
    url: string,
    schema: z.ZodType<T>,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await this.http.delete(url, config);
    return schema.parse(response.data);
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: AxiosError): ApiError {
    if (error.response) {
      // Server responded with error status
      return {
        message: this.extractErrorMessage(error.response.data),
        status: error.response.status,
        code: error.code,
        details: error.response.data,
      };
    } else if (error.request) {
      // Request made but no response
      return {
        message: 'No response from server',
        code: error.code,
      };
    } else {
      // Request setup error
      return {
        message: error.message || 'Unknown error',
        code: error.code,
      };
    }
  }

  /**
   * Extract error message from response data
   */
  private extractErrorMessage(data: unknown): string {
    if (typeof data === 'string') {
      return data;
    }
    if (data && typeof data === 'object') {
      if ('errorMessage' in data && typeof data.errorMessage === 'string') {
        return data.errorMessage;
      }
      if ('message' in data && typeof data.message === 'string') {
        return data.message;
      }
      if ('error' in data && typeof data.error === 'string') {
        return data.error;
      }
    }
    return 'An error occurred';
  }

  /**
   * Set authorization header
   */
  public setAuthToken(token: string) {
    this.http.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  /**
   * Clear authorization header
   */
  public clearAuthToken() {
    delete this.http.defaults.headers.common['Authorization'];
  }
}

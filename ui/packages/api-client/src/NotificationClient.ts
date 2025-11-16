/**
 * Notification Agent API Client
 * Maps to /api/notification endpoints
 */

import { BaseClient } from './BaseClient';
import {
  NotificationRequestSchema,
  NotificationResultSchema,
  NotificationHistorySchema,
  AgentResultSchema,
  AgentHealthSchema,
  type NotificationRequest,
  type NotificationResult,
  type NotificationHistory,
  type AgentResult,
  type AgentHealth,
} from '@agents/agent-domain';
import { z } from 'zod';

export class NotificationClient extends BaseClient {
  /**
   * Send a notification
   * POST /api/notification/send
   */
  async sendNotification(request: NotificationRequest): Promise<AgentResult> {
    // Validate request before sending
    NotificationRequestSchema.parse(request);
    
    return this.post('/api/notification/send', request, AgentResultSchema);
  }

  /**
   * Get notification history
   * GET /api/notification/history
   */
  async getHistory(limit = 50): Promise<NotificationHistory[]> {
    return this.get(
      `/api/notification/history?limit=${limit}`,
      z.array(NotificationHistorySchema)
    );
  }

  /**
   * Get notification by ID
   * GET /api/notification/{id}
   */
  async getNotificationById(id: string): Promise<NotificationHistory> {
    return this.get(`/api/notification/${id}`, NotificationHistorySchema);
  }

  /**
   * Retry failed notification
   * POST /api/notification/{id}/retry
   */
  async retryNotification(id: string): Promise<AgentResult> {
    return this.post(`/api/notification/${id}/retry`, {}, AgentResultSchema);
  }

  /**
   * Get notification health status
   * GET /api/notification/health
   */
  async getHealth(): Promise<AgentHealth> {
    return this.get('/api/notification/health', AgentHealthSchema);
  }

  /**
   * Get notification statistics
   * GET /api/notification/stats
   */
  async getStats(): Promise<NotificationStats> {
    return this.get('/api/notification/stats', NotificationStatsSchema);
  }
}

// Stats schema
const NotificationStatsSchema = z.object({
  totalSent: z.number().int().min(0),
  successRate: z.number().min(0).max(100),
  byChannel: z.record(z.object({
    sent: z.number().int().min(0),
    failed: z.number().int().min(0),
  })),
  last24Hours: z.number().int().min(0),
});

export type NotificationStats = z.infer<typeof NotificationStatsSchema>;

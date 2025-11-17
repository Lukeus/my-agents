/**
 * Notification agent domain types
 * Mirrors C# from src/Application/Agents.Application.Notification/
 */

import { z } from 'zod';

// Notification channels
export const NotificationChannelSchema = z.enum(['email', 'sms', 'teams', 'slack']);

// Notification priority
export const NotificationPrioritySchema = z.enum(['low', 'normal', 'high', 'urgent']);

// Notification status
export const NotificationStatusSchema = z.enum([
  'pending',
  'formatting',
  'sending',
  'sent',
  'failed',
  'delivered',
]);

// Notification Request (maps to C# NotificationRequest)
export const NotificationRequestSchema = z.object({
  channel: NotificationChannelSchema,
  recipient: z.string().min(1, 'Recipient is required'),
  subject: z.string(),
  content: z.string().min(1, 'Content is required'),
  priority: NotificationPrioritySchema.default('normal'),
  metadata: z.record(z.unknown()).optional(),
});

// Notification Result
export const NotificationResultSchema = z.object({
  notificationId: z.string().uuid(),
  channel: NotificationChannelSchema,
  recipient: z.string(),
  status: NotificationStatusSchema,
  sentAt: z.string().datetime().optional(),
  deliveredAt: z.string().datetime().optional(),
  errorMessage: z.string().optional(),
  canRetry: z.boolean(),
});

// Notification History
export const NotificationHistorySchema = z.object({
  id: z.string().uuid(),
  channel: NotificationChannelSchema,
  recipient: z.string(),
  subject: z.string(),
  status: NotificationStatusSchema,
  createdAt: z.string().datetime(),
  sentAt: z.string().datetime().optional(),
  deliveredAt: z.string().datetime().optional(),
  failureReason: z.string().optional(),
  retryCount: z.number().int().min(0),
});

// Type exports
export type NotificationChannel = z.infer<typeof NotificationChannelSchema>;
export type NotificationPriority = z.infer<typeof NotificationPrioritySchema>;
export type NotificationStatus = z.infer<typeof NotificationStatusSchema>;
export type NotificationRequest = z.infer<typeof NotificationRequestSchema>;
export type NotificationResult = z.infer<typeof NotificationResultSchema>;
export type NotificationHistory = z.infer<typeof NotificationHistorySchema>;

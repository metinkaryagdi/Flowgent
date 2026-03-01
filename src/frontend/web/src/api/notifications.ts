import apiClient from './client';
import type { NotificationDto } from '../types';

export const notificationsApi = {
    getAll: async (): Promise<NotificationDto[]> => {
        const response = await apiClient.get<NotificationDto[]>('/api/v1/notifications');
        return response.data;
    },

    getUnreadCount: async (): Promise<number> => {
        const response = await apiClient.get<{ count: number }>('/api/v1/notifications/unread-count');
        return response.data.count;
    },

    markAsRead: async (id: string): Promise<void> => {
        await apiClient.post(`/api/v1/notifications/${id}/read`);
    },

    markAllAsRead: async (): Promise<void> => {
        await apiClient.post('/api/v1/notifications/read-all');
    },
};

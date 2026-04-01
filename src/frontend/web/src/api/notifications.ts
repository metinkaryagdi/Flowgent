import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type { NotificationDto } from '../types';

export const notificationsApi = {
    getAll: async (): Promise<NotificationDto[]> => {
        if (useMockApi) return mockApi.notifications.getAll();
        const response = await apiClient.get<NotificationDto[]>('/api/v1/bff/notifications');
        return response.data;
    },

    getUnreadCount: async (): Promise<number> => {
        if (useMockApi) return mockApi.notifications.getUnreadCount();
        const response = await apiClient.get<{ count: number }>('/api/v1/notifications/unread-count');
        return response.data.count;
    },

    markAsRead: async (id: string): Promise<void> => {
        if (useMockApi) return mockApi.notifications.markAsRead(id);
        await apiClient.post(`/api/v1/notifications/${id}/read`);
    },

    markAllAsRead: async (): Promise<void> => {
        if (useMockApi) return mockApi.notifications.markAllAsRead();
        await apiClient.post('/api/v1/notifications/read-all');
    },
};

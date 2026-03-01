import apiClient from './client';
import type { BoardResponse, UiFlags, SprintDto, NotificationDto } from '../types';

export const bffApi = {
    getBoard: async (projectId: string): Promise<BoardResponse> => {
        const response = await apiClient.get<BoardResponse>(`/api/v1/bff/board/${projectId}`);
        return response.data;
    },

    getFlags: async (): Promise<UiFlags> => {
        const response = await apiClient.get<UiFlags>('/api/v1/bff/flags');
        return response.data;
    },

    getActiveSprint: async (projectId: string): Promise<SprintDto | null> => {
        const response = await apiClient.get<SprintDto | null>(`/api/v1/bff/sprint/active/${projectId}`);
        return response.data;
    },

    getNotifications: async (): Promise<NotificationDto[]> => {
        const response = await apiClient.get<NotificationDto[]>('/api/v1/bff/notifications');
        return response.data;
    },
};

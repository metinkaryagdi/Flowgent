import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type { BoardResponse, UiFlags, SprintDto, NotificationDto } from '../types';

export const bffApi = {
    getBoard: async (projectId: string): Promise<BoardResponse> => {
        if (useMockApi) return mockApi.bff.getBoard(projectId);
        const response = await apiClient.get<BoardResponse>(`/api/v1/bff/board/${projectId}`);
        return response.data;
    },

    getFlags: async (): Promise<UiFlags> => {
        if (useMockApi) return mockApi.bff.getFlags();
        const response = await apiClient.get<UiFlags>('/api/v1/bff/flags');
        return response.data;
    },

    getActiveSprint: async (projectId: string): Promise<SprintDto | null> => {
        if (useMockApi) return mockApi.bff.getActiveSprint(projectId);
        const response = await apiClient.get<SprintDto | null>(`/api/v1/bff/sprint/active/${projectId}`);
        return response.data;
    },

    getNotifications: async (): Promise<NotificationDto[]> => {
        if (useMockApi) return mockApi.bff.getNotifications();
        const response = await apiClient.get<NotificationDto[]>('/api/v1/bff/notifications');
        return response.data;
    },
};

import apiClient from './client';
import type { SprintDto, CreateSprintRequest } from '../types';

export const sprintsApi = {
    getByProject: async (projectId: string): Promise<SprintDto[]> => {
        const response = await apiClient.get<SprintDto[]>(`/api/v1/sprints/project/${projectId}`);
        return response.data;
    },

    getById: async (id: string): Promise<SprintDto> => {
        const response = await apiClient.get<SprintDto>(`/api/v1/sprints/${id}`);
        return response.data;
    },

    create: async (data: CreateSprintRequest): Promise<SprintDto> => {
        const response = await apiClient.post<SprintDto>('/api/v1/sprints', data);
        return response.data;
    },

    start: async (id: string): Promise<SprintDto> => {
        const response = await apiClient.post<SprintDto>(`/api/v1/sprints/${id}/start`);
        return response.data;
    },

    complete: async (id: string): Promise<SprintDto> => {
        const response = await apiClient.post<SprintDto>(`/api/v1/sprints/${id}/complete`);
        return response.data;
    },
};

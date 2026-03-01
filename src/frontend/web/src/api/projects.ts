import apiClient from './client';
import type {
    ProjectDto,
    ProjectMemberDto,
    CreateProjectRequest,
    UpdateProjectRequest,
    AddMemberRequest,
} from '../types';

export const projectsApi = {
    getByUser: async (userId: string): Promise<ProjectDto[]> => {
        const response = await apiClient.get<ProjectDto[]>(`/api/v1/projects/user/${userId}`);
        return response.data;
    },

    getById: async (id: string): Promise<ProjectDto> => {
        const response = await apiClient.get<ProjectDto>(`/api/v1/projects/${id}`);
        return response.data;
    },

    create: async (data: CreateProjectRequest): Promise<ProjectDto> => {
        const response = await apiClient.post<ProjectDto>('/api/v1/projects', data);
        return response.data;
    },

    update: async (id: string, data: UpdateProjectRequest): Promise<ProjectDto> => {
        const response = await apiClient.put<ProjectDto>(`/api/v1/projects/${id}`, data);
        return response.data;
    },

    delete: async (id: string): Promise<void> => {
        await apiClient.delete(`/api/v1/projects/${id}`);
    },

    getMembers: async (projectId: string): Promise<ProjectMemberDto[]> => {
        const response = await apiClient.get<ProjectMemberDto[]>(`/api/v1/projects/${projectId}/members`);
        return response.data;
    },

    addMember: async (projectId: string, data: AddMemberRequest): Promise<ProjectDto> => {
        const response = await apiClient.post<ProjectDto>(`/api/v1/projects/${projectId}/members`, data);
        return response.data;
    },

    removeMember: async (projectId: string, userId: string): Promise<ProjectDto> => {
        const response = await apiClient.delete<ProjectDto>(`/api/v1/projects/${projectId}/members/${userId}`);
        return response.data;
    },
};

import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type {
    ProjectDto,
    ProjectMemberDto,
    CreateProjectRequest,
    UpdateProjectRequest,
    AddMemberRequest,
    PagedResult,
} from '../types';

export const projectsApi = {
    getByUser: async (userId: string): Promise<ProjectDto[]> => {
        if (useMockApi) return mockApi.projects.getByUser(userId);
        const response = await apiClient.get<ProjectDto[]>(`/api/v1/projects/user/${userId}`);
        return response.data;
    },

    getByUserPaged: async (
        userId: string,
        options: { page?: number; pageSize?: number; search?: string; includeArchived?: boolean } = {}
    ): Promise<PagedResult<ProjectDto>> => {
        if (useMockApi) return mockApi.projects.getByUserPaged(userId, options);
        const response = await apiClient.get<PagedResult<ProjectDto>>(`/api/v1/projects/user/${userId}/paged`, {
            params: options,
        });
        return response.data;
    },

    getByOrganizationPaged: async (
        options: { page?: number; pageSize?: number; search?: string; includeArchived?: boolean } = {}
    ): Promise<PagedResult<ProjectDto>> => {
        const response = await apiClient.get<PagedResult<ProjectDto>>('/api/v1/projects/organization/paged', {
            params: options,
        });
        return response.data;
    },

    getAllPaged: async (
        options: { page?: number; pageSize?: number; search?: string; includeArchived?: boolean } = {}
    ): Promise<PagedResult<ProjectDto>> => {
        const response = await apiClient.get<PagedResult<ProjectDto>>('/api/v1/projects/admin/paged', {
            params: options,
        });
        return response.data;
    },

    getById: async (id: string): Promise<ProjectDto> => {
        if (useMockApi) return mockApi.projects.getById(id);
        const response = await apiClient.get<ProjectDto>(`/api/v1/projects/${id}`);
        return response.data;
    },

    create: async (data: CreateProjectRequest): Promise<ProjectDto> => {
        if (useMockApi) return mockApi.projects.create(data);
        const response = await apiClient.post<ProjectDto>('/api/v1/projects', data);
        return response.data;
    },

    update: async (id: string, data: UpdateProjectRequest): Promise<ProjectDto> => {
        if (useMockApi) return mockApi.projects.update(id, data);
        const response = await apiClient.put<ProjectDto>(`/api/v1/projects/${id}`, data);
        return response.data;
    },

    delete: async (id: string): Promise<void> => {
        if (useMockApi) return mockApi.projects.delete(id);
        await apiClient.delete(`/api/v1/projects/${id}`);
    },

    getMembers: async (projectId: string): Promise<ProjectMemberDto[]> => {
        if (useMockApi) return mockApi.projects.getMembers(projectId);
        const response = await apiClient.get<ProjectMemberDto[]>(`/api/v1/projects/${projectId}/members`);
        return response.data;
    },

    addMember: async (projectId: string, data: AddMemberRequest): Promise<ProjectDto> => {
        if (useMockApi) return mockApi.projects.addMember(projectId, data);
        const response = await apiClient.post<ProjectDto>(`/api/v1/projects/${projectId}/members`, data);
        return response.data;
    },

    removeMember: async (projectId: string, userId: string): Promise<ProjectDto> => {
        if (useMockApi) return mockApi.projects.removeMember(projectId, userId);
        const response = await apiClient.delete<ProjectDto>(`/api/v1/projects/${projectId}/members/${userId}`);
        return response.data;
    },
};

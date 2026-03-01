import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type { UserDto, RoleDto } from '../types';

export const adminApi = {
    getUsers: async (): Promise<UserDto[]> => {
        if (useMockApi) return mockApi.admin.getUsers();
        const response = await apiClient.get<UserDto[]>('/api/v1/identity/users');
        return response.data;
    },

    getRoles: async (): Promise<RoleDto[]> => {
        if (useMockApi) return mockApi.admin.getRoles();
        const response = await apiClient.get<RoleDto[]>('/api/v1/identity/roles');
        return response.data;
    },

    getUserRoles: async (userId: string): Promise<string[]> => {
        if (useMockApi) return mockApi.admin.getUserRoles(userId);
        const response = await apiClient.get<string[]>(`/api/v1/identity/users/${userId}/roles`);
        return response.data;
    },

    assignRole: async (userId: string, roleName: string): Promise<void> => {
        if (useMockApi) return mockApi.admin.assignRole(userId, roleName);
        await apiClient.post(`/api/v1/identity/users/${userId}/roles`, { roleName });
    },

    removeRole: async (userId: string, roleName: string): Promise<void> => {
        if (useMockApi) return mockApi.admin.removeRole(userId, roleName);
        await apiClient.delete(`/api/v1/identity/users/${userId}/roles/${roleName}`);
    },

    deactivateUser: async (userId: string): Promise<void> => {
        if (useMockApi) return mockApi.admin.deactivateUser(userId);
        await apiClient.post(`/api/v1/identity/users/${userId}/deactivate`);
    },

    activateUser: async (userId: string): Promise<void> => {
        if (useMockApi) return mockApi.admin.activateUser(userId);
        await apiClient.post(`/api/v1/identity/users/${userId}/activate`);
    },
};

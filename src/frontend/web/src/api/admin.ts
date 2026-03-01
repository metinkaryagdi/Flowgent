import apiClient from './client';
import type { UserDto, RoleDto } from '../types';

export const adminApi = {
    getUsers: async (): Promise<UserDto[]> => {
        const response = await apiClient.get<UserDto[]>('/api/v1/identity/users');
        return response.data;
    },

    getRoles: async (): Promise<RoleDto[]> => {
        const response = await apiClient.get<RoleDto[]>('/api/v1/identity/roles');
        return response.data;
    },

    getUserRoles: async (userId: string): Promise<string[]> => {
        const response = await apiClient.get<string[]>(`/api/v1/identity/users/${userId}/roles`);
        return response.data;
    },

    assignRole: async (userId: string, roleName: string): Promise<void> => {
        await apiClient.post(`/api/v1/identity/users/${userId}/roles`, { roleName });
    },

    removeRole: async (userId: string, roleName: string): Promise<void> => {
        await apiClient.delete(`/api/v1/identity/users/${userId}/roles/${roleName}`);
    },

    deactivateUser: async (userId: string): Promise<void> => {
        await apiClient.post(`/api/v1/identity/users/${userId}/deactivate`);
    },

    activateUser: async (userId: string): Promise<void> => {
        await apiClient.post(`/api/v1/identity/users/${userId}/activate`);
    },
};

import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type { UserDto } from '../types';

export const usersApi = {
    getById: async (id: string): Promise<UserDto> => {
        if (useMockApi) return mockApi.admin.getUsers().then(users => {
            const found = users.find(u => u.id === id);
            if (!found) throw new Error('User not found');
            return found;
        });
        const response = await apiClient.get<UserDto>(`/api/v1/identity/users/${id}`);
        return response.data;
    },
};

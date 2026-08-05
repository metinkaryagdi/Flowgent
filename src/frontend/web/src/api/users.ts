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

    /**
     * Erases the signed-in user's own account. Irreversible: the address and username are
     * overwritten server-side, not just flagged deleted. The password is re-entered because
     * a borrowed session should not be enough to destroy an account.
     */
    deleteMyAccount: async (password: string): Promise<void> => {
        if (useMockApi) return;
        await apiClient.post('/api/v1/identity/users/me/delete', { password });
    },
};

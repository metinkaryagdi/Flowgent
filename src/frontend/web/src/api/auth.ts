import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type { AuthResponseDto, LoginRequest, RegisterRequest, UiFlags } from '../types';

export const authApi = {
    login: async (data: LoginRequest): Promise<AuthResponseDto> => {
        if (useMockApi) return mockApi.auth.login(data);
        const response = await apiClient.post<AuthResponseDto>('/api/v1/identity/login', data);
        return response.data;
    },

    register: async (data: RegisterRequest): Promise<AuthResponseDto> => {
        if (useMockApi) return mockApi.auth.register(data);
        const response = await apiClient.post<AuthResponseDto>('/api/v1/identity/register', data);
        return response.data;
    },

    logout: async (): Promise<void> => {
        if (useMockApi) return;
        await apiClient.post('/api/v1/identity/logout');
    },

    getFlags: async (): Promise<UiFlags> => {
        if (useMockApi) return mockApi.auth.getFlags();
        const response = await apiClient.get<UiFlags>('/api/v1/bff/flags');
        return response.data;
    },
};

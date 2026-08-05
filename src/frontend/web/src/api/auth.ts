import apiClient from './client';
import { mockApi, useMockApi } from './mock';
import type {
    AuthResponseDto,
    LoginRequest,
    RegisterRequest,
    RegisterResponseDto,
    UiFlags,
} from '../types';

export const authApi = {
    login: async (data: LoginRequest): Promise<AuthResponseDto> => {
        if (useMockApi) return mockApi.auth.login(data);
        const response = await apiClient.post<AuthResponseDto>('/api/v1/identity/login', data);
        return response.data;
    },

    // Returns no tokens and sets no cookies: the account is created unverified and
    // cannot sign in until the emailed link is followed.
    register: async (data: RegisterRequest): Promise<RegisterResponseDto> => {
        if (useMockApi) return mockApi.auth.register(data);
        const response = await apiClient.post<RegisterResponseDto>('/api/v1/identity/register', data);
        return response.data;
    },

    verifyEmail: async (token: string): Promise<void> => {
        if (useMockApi) return;
        await apiClient.post('/api/v1/identity/verify-email', { token });
    },

    // Always resolves when the server is reachable, whether or not the address has an
    // account -- the endpoint deliberately does not reveal that.
    resendVerification: async (email: string): Promise<void> => {
        if (useMockApi) return;
        await apiClient.post('/api/v1/identity/resend-verification', { email });
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

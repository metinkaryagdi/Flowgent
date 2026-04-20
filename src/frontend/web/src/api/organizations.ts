import apiClient from './client';
import type {
    OrganizationDto,
    OrganizationMemberDto,
    InviteDto,
    ValidateInviteTokenResult,
    SendInviteRequest,
    AcceptInviteRequest,
    CreateOrganizationRequest,
    ChangeMemberRoleRequest,
    UserDto,
} from '../types';

export interface SwitchOrganizationResponse {
    orgId: string;
    orgName: string;
    orgRole: string;
}

export const organizationsApi = {
    create: async (data: CreateOrganizationRequest): Promise<SwitchOrganizationResponse> => {
        const response = await apiClient.post<SwitchOrganizationResponse>('/api/v1/identity/organizations', data);
        return response.data;
    },

    getMy: async (): Promise<OrganizationDto | null> => {
        try {
            const response = await apiClient.get<OrganizationDto>('/api/v1/identity/organizations/my');
            return response.data;
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { status?: number } };
                if (e.response?.status === 404) return null;
            }
            throw err;
        }
    },

    getAll: async (): Promise<OrganizationDto[]> => {
        const response = await apiClient.get<OrganizationDto[]>('/api/v1/identity/organizations/my/all');
        return response.data;
    },

    switchOrg: async (orgId: string): Promise<SwitchOrganizationResponse> => {
        const response = await apiClient.post<SwitchOrganizationResponse>(
            `/api/v1/identity/organizations/${orgId}/switch`
        );
        return response.data;
    },

    getMembers: async (organizationId: string): Promise<OrganizationMemberDto[]> => {
        const response = await apiClient.get<OrganizationMemberDto[]>(
            `/api/v1/identity/organizations/${organizationId}/members`
        );
        return response.data;
    },

    removeMember: async (organizationId: string, userId: string): Promise<void> => {
        await apiClient.delete(`/api/v1/identity/organizations/${organizationId}/members/${userId}`);
    },

    changeMemberRole: async (
        organizationId: string,
        userId: string,
        data: ChangeMemberRoleRequest
    ): Promise<void> => {
        await apiClient.put(
            `/api/v1/identity/organizations/${organizationId}/members/${userId}/role`,
            data
        );
    },

    sendInvite: async (data: SendInviteRequest): Promise<InviteDto> => {
        const response = await apiClient.post<InviteDto>('/api/v1/identity/invites', data);
        return response.data;
    },

    validateInviteToken: async (token: string): Promise<ValidateInviteTokenResult> => {
        const response = await apiClient.get<ValidateInviteTokenResult>(
            `/api/v1/identity/invites/validate/${token}`
        );
        return response.data;
    },

    acceptInvite: async (data: AcceptInviteRequest): Promise<UserDto> => {
        const response = await apiClient.post<UserDto>('/api/v1/identity/invites/accept', data);
        return response.data;
    },

    acceptInviteExisting: async (token: string): Promise<UserDto> => {
        const response = await apiClient.post<UserDto>('/api/v1/identity/invites/accept-existing', { token });
        return response.data;
    },

    getPendingInvites: async (organizationId: string): Promise<InviteDto[]> => {
        const response = await apiClient.get<InviteDto[]>(
            `/api/v1/identity/invites/pending?organizationId=${organizationId}`
        );
        return response.data;
    },

    revokeInvite: async (inviteId: string): Promise<void> => {
        await apiClient.delete(`/api/v1/identity/invites/${inviteId}`);
    },
};

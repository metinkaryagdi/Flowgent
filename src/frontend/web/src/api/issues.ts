import apiClient from './client';
import type {
    IssueDto,
    IssueBoardItemDto,
    IssueCommentDto,
    IssueAttachmentDto,
    IssueAuditDto,
    WorkflowConfigDto,
    CreateIssueRequest,
    AssignIssueRequest,
    ChangeIssueStatusRequest,
    AddCommentRequest,
} from '../types';

export const issuesApi = {
    create: async (data: CreateIssueRequest): Promise<IssueDto> => {
        const response = await apiClient.post<IssueDto>('/api/v1/issues', data);
        return response.data;
    },

    getById: async (id: string): Promise<IssueDto> => {
        const response = await apiClient.get<IssueDto>(`/api/v1/issues/${id}`);
        return response.data;
    },

    getByProject: async (projectId: string): Promise<IssueBoardItemDto[]> => {
        const response = await apiClient.get<IssueBoardItemDto[]>(`/api/v1/issues/project/${projectId}`);
        return response.data;
    },

    getByAssignee: async (assigneeUserId: string): Promise<IssueDto[]> => {
        const response = await apiClient.get<IssueDto[]>(`/api/v1/issues/assignee/${assigneeUserId}`);
        return response.data;
    },

    getBySprint: async (sprintId: string): Promise<IssueDto[]> => {
        const response = await apiClient.get<IssueDto[]>(`/api/v1/issues/sprint/${sprintId}`);
        return response.data;
    },

    assign: async (id: string, data: AssignIssueRequest): Promise<IssueDto> => {
        const response = await apiClient.post<IssueDto>(`/api/v1/issues/${id}/assign`, data);
        return response.data;
    },

    changeStatus: async (id: string, data: ChangeIssueStatusRequest): Promise<IssueDto> => {
        const response = await apiClient.post<IssueDto>(`/api/v1/issues/${id}/status`, data);
        return response.data;
    },

    addComment: async (id: string, data: AddCommentRequest): Promise<IssueCommentDto> => {
        const response = await apiClient.post<IssueCommentDto>(`/api/v1/issues/${id}/comments`, data);
        return response.data;
    },

    getAttachments: async (id: string): Promise<IssueAttachmentDto[]> => {
        const response = await apiClient.get<IssueAttachmentDto[]>(`/api/v1/issues/${id}/attachments`);
        return response.data;
    },

    getHistory: async (id: string): Promise<IssueAuditDto[]> => {
        const response = await apiClient.get<IssueAuditDto[]>(`/api/v1/issues/${id}/history`);
        return response.data;
    },

    getWorkflow: async (): Promise<WorkflowConfigDto> => {
        const response = await apiClient.get<WorkflowConfigDto>('/api/v1/issues/workflow');
        return response.data;
    },
};

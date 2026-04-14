import apiClient from './client';

export interface GeneratePlanResult {
    sessionId: string;
    sprints: {
        id: string;
        name: string;
        goal: string;
        issues: { id: string; title: string; priority: string }[];
    }[];
}

export interface EnrichIssueResult {
    sessionId: string;
    description: string;
    acceptanceCriteria: string;
    edgeCases: string;
    storyPoints: number;
}

export interface DetectDuplicateResult {
    sessionId: string;
    similarIssues: {
        issueId: string;
        title: string;
        reason: string;
        similarityScore: number;
    }[];
}

export interface ChatResponse {
    sessionId: string;
    message: string;
    answer: string;
    timestamp: string;
}

export interface RetrospectiveResult {
    sessionId: string;
    sprintId: string;
    summary: string;
    wentWell: string;
    improvements: string;
    actionItems: string;
}

export interface SuggestBalanceResult {
    sessionId: string;
    sprintId: string;
    analysis: string;
    recommendation: string;
    suggestions: { issueTitle: string; currentPriority: string; suggestedAction: string }[];
}

export interface SprintRiskResult {
    sessionId: string;
    sprintId: string;
    riskLevel: string;
    reason: string;
    recommendation: string;
    totalIssues: number;
    doneIssues: number;
    inProgressIssues: number;
    openIssues: number;
}

export const aiApi = {
    generatePlan: async (projectId: string, description: string): Promise<GeneratePlanResult> => {
        const res = await apiClient.post('/api/v1/ai/generate-plan', { projectId, description });
        return res.data;
    },

    enrichIssue: async (issueId: string, projectId: string, title: string): Promise<EnrichIssueResult> => {
        const res = await apiClient.post('/api/v1/ai/enrich-issue', { issueId, projectId, title });
        return res.data;
    },

    detectDuplicate: async (projectId: string, title: string): Promise<DetectDuplicateResult> => {
        const res = await apiClient.post('/api/v1/ai/detect-duplicate', { projectId, title });
        return res.data;
    },

    chat: async (projectId: string, message: string, sessionId?: string): Promise<ChatResponse> => {
        const res = await apiClient.post('/api/v1/ai/chat', { projectId, message, sessionId });
        return res.data;
    },

    retrospective: async (sprintId: string, projectId: string): Promise<RetrospectiveResult> => {
        const res = await apiClient.post('/api/v1/ai/retrospective', { sprintId, projectId });
        return res.data;
    },

    suggestBalance: async (sprintId: string, projectId: string): Promise<SuggestBalanceResult> => {
        const res = await apiClient.post('/api/v1/ai/suggest-balance', { sprintId, projectId });
        return res.data;
    },

    sprintRisk: async (sprintId: string, projectId: string): Promise<SprintRiskResult> => {
        const res = await apiClient.post('/api/v1/ai/sprint-risk', { sprintId, projectId });
        return res.data;
    },
};

import type {
    AuthResponseDto,
    LoginRequest,
    RegisterRequest,
    UiFlags,
    UserDto,
    ProjectDto,
    ProjectMemberDto,
    CreateProjectRequest,
    UpdateProjectRequest,
    AddMemberRequest,
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
    SprintDto,
    CreateSprintRequest,
    NotificationDto,
    RoleDto,
    PagedResult,
} from '../types';
import {
    IssuePriority,
    IssueStatus,
    NotificationChannel,
    NotificationStatus,
    SprintStatus,
} from '../types';

export const useMockApi =
    import.meta.env.DEV &&
    String(import.meta.env.VITE_USE_MOCK_API ?? 'true').toLowerCase() !== 'false';

const STORAGE_KEYS = {
    projects: 'mock_projects',
    issues: 'mock_issues',
    notifications: 'mock_notifications',
    sprints: 'mock_sprints',
    users: 'mock_users',
    roles: 'mock_roles',
    userRoles: 'mock_user_roles',
    projectMembers: 'mock_project_members',
    issueComments: 'mock_issue_comments',
    issueAttachments: 'mock_issue_attachments',
};

const nowIso = () => new Date().toISOString();
const addDays = (days: number) => new Date(Date.now() + days * 86400000).toISOString();
const uid = (prefix: string) => `${prefix}-${Math.random().toString(36).slice(2, 8)}-${Date.now().toString(36)}`;

const canUseStorage = () => typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';

const safeGet = <T,>(key: string, fallback: T): T => {
    if (!canUseStorage()) return fallback;
    const raw = localStorage.getItem(key);
    if (!raw) {
        localStorage.setItem(key, JSON.stringify(fallback));
        return fallback;
    }
    try {
        return JSON.parse(raw) as T;
    } catch {
        localStorage.setItem(key, JSON.stringify(fallback));
        return fallback;
    }
};

const safeSet = (key: string, value: unknown) => {
    if (!canUseStorage()) return;
    localStorage.setItem(key, JSON.stringify(value));
};

const defaultUser: UserDto = {
    id: 'demo-user-1',
    userName: 'Demo Kullanici',
    email: 'demo@bitirme.dev',
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z',
};

const getCurrentUser = (): UserDto => {
    if (!canUseStorage()) return defaultUser;
    const raw = localStorage.getItem('user');
    if (!raw) {
        localStorage.setItem('user', JSON.stringify(defaultUser));
        return defaultUser;
    }
    try {
        return JSON.parse(raw) as UserDto;
    } catch {
        localStorage.setItem('user', JSON.stringify(defaultUser));
        return defaultUser;
    }
};

const seedProjects = (): ProjectDto[] => {
    const user = getCurrentUser();
    return [
        {
            id: 'proj-1',
            name: 'Bitirme Platformu',
            key: 'BP',
            ownerUserId: user.id,
            isArchived: false,
            issueCount: 0,
            openIssueCount: 0,
            inProgressIssueCount: 0,
            doneIssueCount: 0,
            createdAt: '2024-01-05T09:00:00Z',
        },
        {
            id: 'proj-2',
            name: 'Mobil Prototip',
            key: 'MP',
            ownerUserId: user.id,
            isArchived: false,
            issueCount: 0,
            openIssueCount: 0,
            inProgressIssueCount: 0,
            doneIssueCount: 0,
            createdAt: '2024-02-10T11:30:00Z',
        },
    ];
};

const seedIssues = (): IssueDto[] => {
    const user = getCurrentUser();
    return [
        {
            id: 'issue-1',
            projectId: 'proj-1',
            title: 'Login akisi',
            description: 'Kullanici giris ekrani ve token akisi',
            status: IssueStatus.Open,
            priority: IssuePriority.High,
            createdByUserId: user.id,
            assigneeUserId: user.id,
            sprintId: null,
            createdAt: addDays(-5),
            updatedAt: null,
            version: 1,
        },
        {
            id: 'issue-2',
            projectId: 'proj-1',
            title: 'Bildirim servisi',
            description: 'In-app bildirimleri listele',
            status: IssueStatus.InProgress,
            priority: IssuePriority.Medium,
            createdByUserId: user.id,
            assigneeUserId: null,
            sprintId: null,
            createdAt: addDays(-4),
            updatedAt: null,
            version: 1,
        },
        {
            id: 'issue-3',
            projectId: 'proj-2',
            title: 'Board tasarimi',
            description: 'Dnd destekli kolon tasarimi',
            status: IssueStatus.Done,
            priority: IssuePriority.Low,
            createdByUserId: user.id,
            assigneeUserId: user.id,
            sprintId: null,
            createdAt: addDays(-10),
            updatedAt: addDays(-8),
            version: 2,
        },
    ];
};

const seedNotifications = (): NotificationDto[] => {
    const user = getCurrentUser();
    return [
        {
            id: 'notif-1',
            userId: user.id,
            title: 'Issue atanmasi',
            message: 'Login akisi sana atandi.',
            channel: NotificationChannel.InApp,
            status: NotificationStatus.Delivered,
            isRead: false,
            readAt: null,
            deliveryAttemptCount: 1,
            lastDeliveryAttemptAt: addDays(-2),
            nextDeliveryAttemptAt: null,
            deliveredAt: addDays(-2),
            lastFailureReason: null,
            entityType: 'Issue',
            entityId: 'issue-1',
            createdAt: addDays(-2),
            updatedAt: null,
        },
    ];
};

const seedSprints = (): SprintDto[] => [];
const seedIssueComments = (): Record<string, IssueCommentDto[]> => ({});

const seedUsers = (): UserDto[] => [
    defaultUser,
    {
        id: 'demo-user-2',
        userName: 'Ayse Yilmaz',
        email: 'ayse@bitirme.dev',
        isActive: true,
        createdAt: '2024-01-20T08:10:00Z',
    },
    {
        id: 'demo-user-3',
        userName: 'Mehmet Kaya',
        email: 'mehmet@bitirme.dev',
        isActive: false,
        createdAt: '2024-02-12T10:45:00Z',
    },
];

const seedRoles = (): RoleDto[] => [
    { id: 'role-1', name: 'Admin' },
    { id: 'role-2', name: 'Manager' },
    { id: 'role-3', name: 'User' },
];

const seedUserRoles = (): Record<string, string[]> => ({
    'demo-user-1': ['Admin'],
    'demo-user-2': ['Manager'],
    'demo-user-3': ['User'],
});

const seedProjectMembers = (): ProjectMemberDto[] => {
    const user = getCurrentUser();
    return [
        {
            projectId: 'proj-1',
            userId: user.id,
            addedByUserId: user.id,
            addedAt: addDays(-5),
        },
        {
            projectId: 'proj-2',
            userId: user.id,
            addedByUserId: user.id,
            addedAt: addDays(-10),
        },
    ];
};

const ensureSeed = () => {
    safeGet<ProjectDto[]>(STORAGE_KEYS.projects, seedProjects());
    safeGet<IssueDto[]>(STORAGE_KEYS.issues, seedIssues());
    safeGet<NotificationDto[]>(STORAGE_KEYS.notifications, seedNotifications());
    safeGet<SprintDto[]>(STORAGE_KEYS.sprints, seedSprints());
    safeGet<UserDto[]>(STORAGE_KEYS.users, seedUsers());
    safeGet<RoleDto[]>(STORAGE_KEYS.roles, seedRoles());
    safeGet<Record<string, string[]>>(STORAGE_KEYS.userRoles, seedUserRoles());
    safeGet<ProjectMemberDto[]>(STORAGE_KEYS.projectMembers, seedProjectMembers());
    safeGet<Record<string, IssueCommentDto[]>>(STORAGE_KEYS.issueComments, seedIssueComments());
};

const recalcProjectStats = (projects: ProjectDto[], issues: IssueDto[]): ProjectDto[] => {
    return projects.map((p) => {
        const issueList = issues.filter((i) => i.projectId === p.id);
        const open = issueList.filter((i) => i.status === IssueStatus.Open).length;
        const inProgress = issueList.filter((i) => i.status === IssueStatus.InProgress).length;
        const done = issueList.filter((i) => i.status === IssueStatus.Done).length;
        return {
            ...p,
            issueCount: issueList.length,
            openIssueCount: open,
            inProgressIssueCount: inProgress,
            doneIssueCount: done,
        };
    });
};

const getProjects = () => {
    ensureSeed();
    const projects = safeGet<ProjectDto[]>(STORAGE_KEYS.projects, seedProjects());
    const issues = safeGet<IssueDto[]>(STORAGE_KEYS.issues, seedIssues());
    const updated = recalcProjectStats(projects, issues);
    safeSet(STORAGE_KEYS.projects, updated);
    return updated;
};

const setProjects = (projects: ProjectDto[]) => safeSet(STORAGE_KEYS.projects, projects);

const getIssues = () => {
    ensureSeed();
    return safeGet<IssueDto[]>(STORAGE_KEYS.issues, seedIssues());
};

const setIssues = (issues: IssueDto[]) => safeSet(STORAGE_KEYS.issues, issues);

const getNotifications = () => {
    ensureSeed();
    return safeGet<NotificationDto[]>(STORAGE_KEYS.notifications, seedNotifications());
};

const setNotifications = (notifications: NotificationDto[]) =>
    safeSet(STORAGE_KEYS.notifications, notifications);

const getSprints = () => {
    ensureSeed();
    return safeGet<SprintDto[]>(STORAGE_KEYS.sprints, seedSprints());
};

const setSprints = (sprints: SprintDto[]) => safeSet(STORAGE_KEYS.sprints, sprints);

const getUsers = () => {
    ensureSeed();
    return safeGet<UserDto[]>(STORAGE_KEYS.users, seedUsers());
};

const setUsers = (users: UserDto[]) => safeSet(STORAGE_KEYS.users, users);

const getRoles = () => {
    ensureSeed();
    return safeGet<RoleDto[]>(STORAGE_KEYS.roles, seedRoles());
};

const getUserRoles = () => {
    ensureSeed();
    return safeGet<Record<string, string[]>>(STORAGE_KEYS.userRoles, seedUserRoles());
};

const setUserRoles = (map: Record<string, string[]>) => safeSet(STORAGE_KEYS.userRoles, map);

const getProjectMembers = () => {
    ensureSeed();
    return safeGet<ProjectMemberDto[]>(STORAGE_KEYS.projectMembers, seedProjectMembers());
};

const setProjectMembers = (members: ProjectMemberDto[]) =>
    safeSet(STORAGE_KEYS.projectMembers, members);

const getIssueComments = () => {
    ensureSeed();
    return safeGet<Record<string, IssueCommentDto[]>>(STORAGE_KEYS.issueComments, seedIssueComments());
};

const setIssueComments = (comments: Record<string, IssueCommentDto[]>) =>
    safeSet(STORAGE_KEYS.issueComments, comments);

const getIssueAttachments = () =>
    safeGet<Record<string, IssueAttachmentDto[]>>(STORAGE_KEYS.issueAttachments, {});

const setIssueAttachments = (attachments: Record<string, IssueAttachmentDto[]>) =>
    safeSet(STORAGE_KEYS.issueAttachments, attachments);

const statusLabel = (status: IssueStatus) => {
    if (status === IssueStatus.Open) return 'Acik';
    if (status === IssueStatus.InProgress) return 'Devam Ediyor';
    return 'Tamamlandi';
};

const pushNotification = (userId: string, title: string, message: string, entityId?: string) => {
    const list = getNotifications();
    const notif: NotificationDto = {
        id: uid('notif'),
        userId,
        title,
        message,
        channel: NotificationChannel.InApp,
        status: NotificationStatus.Delivered,
        isRead: false,
        readAt: null,
        deliveryAttemptCount: 1,
        lastDeliveryAttemptAt: nowIso(),
        nextDeliveryAttemptAt: null,
        deliveredAt: nowIso(),
        lastFailureReason: null,
        entityType: entityId ? 'Issue' : null,
        entityId: entityId || null,
        createdAt: nowIso(),
        updatedAt: null,
    };
    const updated = [notif, ...list];
    setNotifications(updated);
};

const boardConfig = () => ({
    columns: [
        { key: 'Open', title: 'Acik', wipLimit: null },
        { key: 'InProgress', title: 'Devam', wipLimit: null },
        { key: 'Done', title: 'Bitti', wipLimit: null },
    ],
    allowedTransitions: {
        Open: ['InProgress', 'Done'],
        InProgress: ['Open', 'Done'],
        Done: ['Open'],
    },
});

const toBoardItem = (issue: IssueDto): IssueBoardItemDto & { id: string } => ({
    issueId: issue.id,
    id: issue.id,
    projectId: issue.projectId,
    title: issue.title,
    status: issue.status,
    priority: issue.priority,
    assigneeUserId: issue.assigneeUserId,
    sprintId: issue.sprintId,
    createdAt: issue.createdAt,
    updatedAt: issue.updatedAt,
    version: issue.version,
});

const defaultFlags: UiFlags = {
    canManageProjects: true,
    canEditIssues: true,
    canAssignIssues: true,
    canChangeStatus: true,
    canViewAdmin: true,
};

export const mockApi = {
    auth: {
        login: async (_data: LoginRequest): Promise<AuthResponseDto> => {
            const user = getCurrentUser();
            return {
                accessToken: 'mock-token',
                expiresAt: addDays(7),
                user,
                roles: ['Admin'],
            };
        },
        register: async (data: RegisterRequest): Promise<AuthResponseDto> => {
            const users = getUsers();
            const existing = users.find((u) => u.email.toLowerCase() === data.email.toLowerCase());
            if (existing) {
                return {
                    accessToken: 'mock-token',
                    expiresAt: addDays(7),
                    user: existing,
                    roles: ['User'],
                };
            }
            const user: UserDto = {
                id: uid('user'),
                userName: data.userName,
                email: data.email,
                isActive: true,
                createdAt: nowIso(),
            };
            setUsers([user, ...users]);
            return {
                accessToken: 'mock-token',
                expiresAt: addDays(7),
                user,
                roles: ['User'],
            };
        },
        getFlags: async (): Promise<UiFlags> => defaultFlags,
    },
    bff: {
        getBoard: async (projectId: string) => {
            const projects = getProjects();
            const issues = getIssues();
            const project = projects.find((p) => p.id === projectId) || null;
            const items = issues.filter((i) => i.projectId === projectId).map(toBoardItem);
            return {
                project,
                config: boardConfig(),
                items,
            };
        },
        getFlags: async (): Promise<UiFlags> => defaultFlags,
        getActiveSprint: async (projectId: string): Promise<SprintDto | null> => {
            const sprints = getSprints().filter((s) => s.projectId === projectId);
            return sprints.find((s) => s.status === SprintStatus.Active) || null;
        },
        getNotifications: async (): Promise<NotificationDto[]> => {
            const user = getCurrentUser();
            return getNotifications().filter((n) => n.userId === user.id);
        },
    },
    projects: {
        getByUser: async (userId: string): Promise<ProjectDto[]> => {
            const memberProjectIds = new Set(
                getProjectMembers().filter((m) => m.userId === userId).map((m) => m.projectId)
            );
            return getProjects().filter((p) => memberProjectIds.has(p.id));
        },
        getByUserPaged: async (
            userId: string,
            options: { page?: number; pageSize?: number; search?: string; includeArchived?: boolean } = {}
        ): Promise<PagedResult<ProjectDto>> => {
            const page = options.page ?? 1;
            const pageSize = options.pageSize ?? 12;
            const includeArchived = options.includeArchived ?? false;
            const search = options.search?.trim().toLowerCase();

            const memberProjectIds = new Set(
                getProjectMembers().filter((m) => m.userId === userId).map((m) => m.projectId)
            );
            let items = getProjects().filter((p) => memberProjectIds.has(p.id));
            if (!includeArchived) {
                items = items.filter((p) => !p.isArchived);
            }
            if (search) {
                items = items.filter((p) =>
                    p.name.toLowerCase().includes(search) || p.key.toLowerCase().includes(search)
                );
            }

            const totalCount = items.length;
            const paged = items.slice((page - 1) * pageSize, page * pageSize);

            return {
                items: paged,
                totalCount,
                page,
                pageSize,
            };
        },
        getById: async (id: string): Promise<ProjectDto> => {
            const project = getProjects().find((p) => p.id === id);
            if (!project) {
                throw new Error('Project not found');
            }
            return project;
        },
        create: async (data: CreateProjectRequest): Promise<ProjectDto> => {
            const user = getCurrentUser();
            const projects = getProjects();
            const newProject: ProjectDto = {
                id: uid('proj'),
                name: data.name,
                key: data.key.toUpperCase().slice(0, 4),
                ownerUserId: data.ownerUserId || user.id,
                isArchived: false,
                issueCount: 0,
                openIssueCount: 0,
                inProgressIssueCount: 0,
                doneIssueCount: 0,
                createdAt: nowIso(),
            };
            const updated = [newProject, ...projects];
            setProjects(updated);
            return newProject;
        },
        update: async (id: string, data: UpdateProjectRequest): Promise<ProjectDto> => {
            const projects = getProjects();
            const updated = projects.map((p) =>
                p.id === id
                    ? {
                        ...p,
                        name: data.name ?? p.name,
                        key: data.key ?? p.key,
                    }
                    : p
            );
            setProjects(updated);
            const project = updated.find((p) => p.id === id);
            if (!project) throw new Error('Project not found');
            return project;
        },
        delete: async (id: string): Promise<void> => {
            const projects = getProjects().map((p) =>
                p.id === id ? { ...p, isArchived: true } : p
            );
            setProjects(projects);
        },
        getMembers: async (projectId: string): Promise<ProjectMemberDto[]> => {
            const members = getProjectMembers();
            return members.filter((m) => m.projectId === projectId);
        },
        addMember: async (projectId: string, data: AddMemberRequest): Promise<ProjectDto> => {
            const members = getProjectMembers();
            const user = getCurrentUser();
            const exists = members.find((m) => m.projectId === projectId && m.userId === data.userId);
            if (!exists) {
                members.push({
                    projectId,
                    userId: data.userId,
                    addedByUserId: user.id,
                    addedAt: nowIso(),
                });
                setProjectMembers(members);
            }
            return mockApi.projects.getById(projectId);
        },
        removeMember: async (projectId: string, userId: string): Promise<ProjectDto> => {
            const members = getProjectMembers().filter(
                (m) => !(m.projectId === projectId && m.userId === userId)
            );
            setProjectMembers(members);
            return mockApi.projects.getById(projectId);
        },
    },
    issues: {
        create: async (data: CreateIssueRequest): Promise<IssueDto> => {
            const user = getCurrentUser();
            const issues = getIssues();
            const issue: IssueDto = {
                id: uid('issue'),
                projectId: data.projectId,
                title: data.title,
                description: data.description || null,
                status: IssueStatus.Open,
                priority: data.priority,
                createdByUserId: user.id,
                assigneeUserId: data.assigneeUserId || null,
                sprintId: null,
                createdAt: nowIso(),
                updatedAt: null,
                version: 1,
            };
            setIssues([issue, ...issues]);
            pushNotification(
                user.id,
                'Yeni issue olusturuldu',
                `Issue olusturuldu: ${issue.title}`,
                issue.id
            );
            return issue;
        },
        getById: async (id: string): Promise<IssueDto> => {
            const issue = getIssues().find((i) => i.id === id);
            if (!issue) throw new Error('Issue not found');
            return issue;
        },
        getByProject: async (projectId: string): Promise<IssueBoardItemDto[]> => {
            const items = getIssues().filter((i) => i.projectId === projectId).map(toBoardItem);
            return items;
        },
        getByProjectPaged: async (
            projectId: string,
            options: { page?: number; pageSize?: number; sprintId?: string; backlogOnly?: boolean } = {}
        ): Promise<PagedResult<IssueBoardItemDto>> => {
            const page = options.page ?? 1;
            const pageSize = options.pageSize ?? 20;
            const sprintId = options.sprintId ?? null;
            const backlogOnly = options.backlogOnly ?? false;

            let items = getIssues().filter((i) => i.projectId === projectId);
            if (backlogOnly) {
                items = items.filter((i) => !i.sprintId);
            } else if (sprintId) {
                items = items.filter((i) => i.sprintId === sprintId);
            }

            const totalCount = items.length;
            const paged = items
                .map(toBoardItem)
                .slice((page - 1) * pageSize, page * pageSize);

            return {
                items: paged,
                totalCount,
                page,
                pageSize,
            };
        },
        getByAssignee: async (assigneeUserId: string): Promise<IssueDto[]> => {
            return getIssues().filter((i) => i.assigneeUserId === assigneeUserId);
        },
        getBySprint: async (sprintId: string): Promise<IssueDto[]> => {
            return getIssues().filter((i) => i.sprintId === sprintId);
        },
        assign: async (id: string, data: AssignIssueRequest): Promise<IssueDto> => {
            const issues = getIssues();
            const updated = issues.map((i) =>
                i.id === id ? { ...i, assigneeUserId: data.assigneeUserId, version: i.version + 1 } : i
            );
            setIssues(updated);
            const issue = updated.find((i) => i.id === id);
            if (!issue) throw new Error('Issue not found');
            return issue;
        },
        changeStatus: async (id: string, data: ChangeIssueStatusRequest): Promise<IssueDto> => {
            const issues = getIssues();
            const updated = issues.map((i) =>
                i.id === id
                    ? {
                        ...i,
                        status: data.newStatus,
                        updatedAt: nowIso(),
                        version: i.version + 1,
                    }
                    : i
            );
            setIssues(updated);
            const issue = updated.find((i) => i.id === id);
            if (!issue) throw new Error('Issue not found');
            const user = getCurrentUser();
            pushNotification(
                user.id,
                'Issue durumu guncellendi',
                `${issue.title} -> ${statusLabel(issue.status)}`,
                issue.id
            );
            return issue;
        },
        update: async (id: string, data: any): Promise<IssueDto> => {
            await new Promise(r => setTimeout(r, 400));
            const issues = getIssues();
            const index = issues.findIndex((i) => i.id === id);
            if (index === -1) throw new Error('Issue not found');

            const updatedIssue = {
                ...issues[index],
                title: data.title ?? issues[index].title,
                description: data.description ?? issues[index].description,
                priority: data.priority ?? issues[index].priority,
                updatedAt: nowIso(),
                version: issues[index].version + 1,
            };

            issues[index] = updatedIssue;
            setIssues(issues);
            return updatedIssue;
        },
        delete: async (id: string): Promise<void> => {
            await new Promise(r => setTimeout(r, 400));
            const issues = getIssues();
            const filtered = issues.filter(i => i.id !== id);
            if (issues.length === filtered.length) throw new Error('Issue not found');
            setIssues(filtered);
        },
        addComment: async (_id: string, data: AddCommentRequest): Promise<IssueCommentDto> => {
            const comment = {
                id: uid('comment'),
                issueId: _id,
                authorUserId: data.authorUserId,
                content: data.content,
                createdAt: nowIso(),
            };
            const map = getIssueComments();
            const list = map[_id] || [];
            map[_id] = [...list, comment];
            setIssueComments(map);
            return comment;
        },
        getComments: async (_id: string): Promise<IssueCommentDto[]> => {
            const map = getIssueComments();
            return map[_id] || [];
        },
        getAttachments: async (_id: string): Promise<IssueAttachmentDto[]> => {
            const map = getIssueAttachments();
            return map[_id] || [];
        },
        attachFile: async (_id: string, data: { fileId: string }): Promise<IssueAttachmentDto> => {
            const attachment: IssueAttachmentDto = {
                id: uid('attachment'),
                issueId: _id,
                fileId: data.fileId,
                fileName: `file-${data.fileId.slice(0, 6)}`,
                contentType: 'application/octet-stream',
                sizeBytes: 0,
                uploadedByUserId: 'demo-user-1',
                uploadedAt: nowIso(),
            };
            const map = getIssueAttachments();
            const list = map[_id] || [];
            map[_id] = [...list, attachment];
            setIssueAttachments(map);
            return attachment;
        },
        getHistory: async (_id: string): Promise<IssueAuditDto[]> => [],
        getWorkflow: async (): Promise<WorkflowConfigDto> => ({
            statuses: ['Open', 'InProgress', 'Done'],
            allowedTransitions: boardConfig().allowedTransitions,
        }),
    },
    notifications: {
        getAll: async (): Promise<NotificationDto[]> => {
            const user = getCurrentUser();
            return getNotifications().filter((n) => n.userId === user.id);
        },
        getUnreadCount: async (): Promise<number> => {
            const user = getCurrentUser();
            return getNotifications().filter(
                (n) => n.userId === user.id && !n.isRead
            ).length;
        },
        markAsRead: async (id: string): Promise<void> => {
            const list = getNotifications().map((n) =>
                n.id === id ? { ...n, isRead: true, readAt: nowIso(), updatedAt: nowIso() } : n
            );
            setNotifications(list);
        },
        markAllAsRead: async (): Promise<void> => {
            const user = getCurrentUser();
            const list = getNotifications().map((n) =>
                n.userId === user.id
                    ? { ...n, isRead: true, readAt: nowIso(), updatedAt: nowIso() }
                    : n
            );
            setNotifications(list);
        },
    },
    sprints: {
        getByProject: async (projectId: string): Promise<SprintDto[]> => {
            return getSprints().filter((s) => s.projectId === projectId);
        },
        getById: async (id: string): Promise<SprintDto> => {
            const sprint = getSprints().find((s) => s.id === id);
            if (!sprint) throw new Error('Sprint not found');
            return sprint;
        },
        create: async (data: CreateSprintRequest): Promise<SprintDto> => {
            const user = getCurrentUser();
            const sprints = getSprints();
            const sprint: SprintDto = {
                id: uid('sprint'),
                projectId: data.projectId,
                name: data.name,
                goal: data.goal || null,
                status: SprintStatus.Planned,
                createdByUserId: user.id,
                startDate: data.startDate,
                endDate: data.endDate,
                startedAt: null,
                completedAt: null,
                totalIssueCount: 0,
                completedIssueCount: 0,
                createdAt: nowIso(),
                updatedAt: null,
            };
            setSprints([sprint, ...sprints]);
            return sprint;
        },
        start: async (id: string): Promise<SprintDto> => {
            const sprints = getSprints();
            const updated = sprints.map((s) =>
                s.id === id ? { ...s, status: SprintStatus.Active, startedAt: nowIso() } : s
            );
            setSprints(updated);
            const sprint = updated.find((s) => s.id === id);
            if (!sprint) throw new Error('Sprint not found');
            return sprint;
        },
        complete: async (id: string): Promise<SprintDto> => {
            const sprints = getSprints();
            const updated = sprints.map((s) =>
                s.id === id ? { ...s, status: SprintStatus.Completed, completedAt: nowIso() } : s
            );
            setSprints(updated);
            const sprint = updated.find((s) => s.id === id);
            if (!sprint) throw new Error('Sprint not found');
            return sprint;
        },
        addIssue: async (sprintId: string, issueId: string): Promise<void> => {
            const issues = getIssues();
            const updated = issues.map((i) =>
                i.id === issueId ? { ...i, sprintId } : i
            );
            setIssues(updated);
            // Recalc sprint counts
            const sprints = getSprints();
            const sprintIssues = updated.filter((i) => i.sprintId === sprintId);
            const updatedSprints = sprints.map((s) =>
                s.id === sprintId
                    ? {
                        ...s,
                        totalIssueCount: sprintIssues.length,
                        completedIssueCount: sprintIssues.filter((i) => i.status === IssueStatus.Done).length,
                    }
                    : s
            );
            setSprints(updatedSprints);
        },
        removeIssue: async (sprintId: string, issueId: string): Promise<void> => {
            void sprintId;
            const issues = getIssues();
            const updated = issues.map((i) =>
                i.id === issueId ? { ...i, sprintId: null } : i
            );
            setIssues(updated);
        },
    },
    admin: {
        getUsers: async (): Promise<UserDto[]> => getUsers(),
        getRoles: async (): Promise<RoleDto[]> => getRoles(),
        getUserRoles: async (userId: string): Promise<string[]> => {
            const map = getUserRoles();
            return map[userId] || [];
        },
        assignRole: async (userId: string, roleName: string): Promise<void> => {
            const map = getUserRoles();
            const current = map[userId] || [];
            map[userId] = current.includes(roleName) ? current : [...current, roleName];
            setUserRoles(map);
        },
        removeRole: async (userId: string, roleName: string): Promise<void> => {
            const map = getUserRoles();
            map[userId] = (map[userId] || []).filter((r) => r !== roleName);
            setUserRoles(map);
        },
        deactivateUser: async (userId: string): Promise<void> => {
            const users = getUsers().map((u) =>
                u.id === userId ? { ...u, isActive: false } : u
            );
            setUsers(users);
        },
        activateUser: async (userId: string): Promise<void> => {
            const users = getUsers().map((u) =>
                u.id === userId ? { ...u, isActive: true } : u
            );
            setUsers(users);
        },
        getStats: async () => {
            const users = getUsers();
            return {
                totalUsers: users.length,
                activeUsers: users.filter((u) => u.isActive).length,
                totalOrgs: 0,
            };
        },
        getAdminOrgs: async () => [] as import('../types').OrganizationDto[],
    },
};

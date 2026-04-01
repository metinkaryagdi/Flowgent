/* ===================================
   BitirmeProject — TypeScript Types
   Backend DTO mirror'ları
   =================================== */

// ─── Enums ───────────────────────────
export const IssueStatus = {
  Open: 'Open',
  InProgress: 'InProgress',
  Done: 'Done',
} as const;
export type IssueStatus = typeof IssueStatus[keyof typeof IssueStatus];

export const IssuePriority = {
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical',
} as const;
export type IssuePriority = typeof IssuePriority[keyof typeof IssuePriority];

export const SprintStatus = {
  Planned: 'Planned',
  Active: 'Active',
  Completed: 'Completed',
} as const;
export type SprintStatus = typeof SprintStatus[keyof typeof SprintStatus];

export const NotificationStatus = {
  Queued: 0,
  Sent: 1,
  Failed: 2,
  Delivered: 3,
} as const;
export type NotificationStatus = typeof NotificationStatus[keyof typeof NotificationStatus];

export const NotificationChannel = {
  InApp: 0,
  Email: 1,
} as const;
export type NotificationChannel = typeof NotificationChannel[keyof typeof NotificationChannel];

// ─── Identity DTOs ───────────────────
export interface UserDto {
  id: string;
  userName: string;
  email: string;
  isActive: boolean;
  createdAt: string;
}

export interface AuthResponseDto {
  accessToken: string;
  expiresAt: string;
  user: UserDto;
  roles: string[];
}

export interface RoleDto {
  id: string;
  name: string;
}

export interface LoginRequest {
  userNameOrEmail: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
}

// ─── Project DTOs ────────────────────
export interface ProjectDto {
  id: string;
  name: string;
  key: string;
  ownerUserId: string;
  isArchived: boolean;
  issueCount: number;
  openIssueCount: number;
  inProgressIssueCount: number;
  doneIssueCount: number;
  createdAt: string;
}

export interface ProjectMemberDto {
  projectId: string;
  userId: string;
  addedByUserId: string;
  addedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  key: string;
  ownerUserId?: string;
}

export interface UpdateProjectRequest {
  name?: string;
  key?: string;
}

export interface AddMemberRequest {
  userId: string;
}

// ─── Issue DTOs ──────────────────────
export interface IssueDto {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  status: IssueStatus;
  priority: IssuePriority;
  createdByUserId: string;
  assigneeUserId: string | null;
  sprintId: string | null;
  createdAt: string;
  updatedAt: string | null;
  version: number;
}

export interface IssueBoardItemDto {
  issueId: string;
  projectId: string;
  title: string;
  status: IssueStatus;
  priority: IssuePriority;
  assigneeUserId: string | null;
  sprintId: string | null;
  createdAt: string;
  updatedAt: string | null;
  version: number;
}

export interface IssueCommentDto {
  id: string;
  issueId: string;
  authorUserId: string;
  content: string;
  createdAt: string;
}

export interface IssueAttachmentDto {
  id: string;
  issueId: string;
  fileId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByUserId: string;
  uploadedAt: string;
}

export interface IssueAuditDto {
  issueId: string;
  fromStatus: IssueStatus;
  toStatus: IssueStatus;
  changedByUserId: string;
  changedAt: string;
}

export interface WorkflowConfigDto {
  statuses: string[];
  allowedTransitions: Record<string, string[]>;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateIssueRequest {
  projectId: string;
  title: string;
  description?: string;
  priority: IssuePriority;
  assigneeUserId?: string;
}

export interface UpdateIssueRequest {
  title?: string;
  description?: string;
  priority?: IssuePriority;
  expectedVersion: number;
}

export interface AssignIssueRequest {
  assigneeUserId: string;
  expectedVersion: number;
}

export interface ChangeIssueStatusRequest {
  newStatus: IssueStatus;
  expectedVersion: number;
}

export interface AddCommentRequest {
  authorUserId: string;
  content: string;
}

// ─── Sprint DTOs ─────────────────────
export interface SprintDto {
  id: string;
  projectId: string;
  name: string;
  goal: string | null;
  status: SprintStatus;
  createdByUserId: string;
  startDate: string;
  endDate: string;
  startedAt: string | null;
  completedAt: string | null;
  totalIssueCount: number;
  completedIssueCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface SprintIssueDto {
  issueId: string;
  projectId: string;
  sprintId: string | null;
  title: string;
  issueType: string;
  priority: string;
  status: string;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface SprintVelocityDto {
  sprintId: string;
  totalIssues: number;
  doneIssues: number;
}

export interface CreateSprintRequest {
  projectId: string;
  name: string;
  startDate: string;
  endDate: string;
  goal?: string;
}

// ─── Notification DTOs ───────────────
export interface NotificationDto {
  id: string;
  userId: string;
  title: string;
  message: string;
  channel: NotificationChannel;
  status: NotificationStatus;
  isRead: boolean;
  readAt: string | null;
  deliveryAttemptCount: number;
  lastDeliveryAttemptAt: string | null;
  nextDeliveryAttemptAt: string | null;
  deliveredAt: string | null;
  lastFailureReason: string | null;
  entityType: string | null;
  entityId: string | null;
  createdAt: string;
  updatedAt: string | null;
}

// ─── Storage DTOs ────────────────────
export interface StoredFileDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByUserId: string;
  uploadedAt: string;
}

// ─── Organization DTOs ───────────────
export const OrganizationRole = {
  Owner: 'Owner',
  Manager: 'Manager',
  Member: 'Member',
} as const;
export type OrganizationRole = typeof OrganizationRole[keyof typeof OrganizationRole];

export interface OrganizationDto {
  id: string;
  name: string;
  createdByUserId: string;
  memberCount: number;
  createdAt: string;
}

export interface OrganizationMemberDto {
  userId: string;
  userName: string;
  email: string;
  role: OrganizationRole;
  joinedAt: string;
}

export interface InviteDto {
  id: string;
  email: string;
  role: string;
  expiresAt: string;
  createdAt: string;
}

export interface ValidateInviteTokenResult {
  email: string;
  organizationName: string;
  role: string;
  expiresAt: string;
}

export interface SendInviteRequest {
  organizationId: string;
  email: string;
  role: OrganizationRole;
  inviteLinkBaseUrl: string;
}

export interface AcceptInviteRequest {
  token: string;
  userName: string;
  password: string;
}

export interface CreateOrganizationRequest {
  name: string;
}

export interface ChangeMemberRoleRequest {
  newRole: OrganizationRole;
}

// ─── BFF DTOs ────────────────────────
export interface UiFlags {
  canManageProjects: boolean;
  canEditIssues: boolean;
  canAssignIssues: boolean;
  canChangeStatus: boolean;
  canViewAdmin: boolean;
}

export interface BoardColumn {
  key: string;
  title: string;
  wipLimit: number | null;
}

export interface BoardConfig {
  columns: BoardColumn[];
  allowedTransitions: Record<string, string[]>;
}

export interface BoardResponse {
  project: ProjectDto | null;
  config: BoardConfig;
  items: IssueBoardItemDto[];
}

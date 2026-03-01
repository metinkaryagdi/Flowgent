/* ===================================
   BitirmeProject — TypeScript Types
   Backend DTO mirror'ları
   =================================== */

// ─── Enums ───────────────────────────
export enum IssueStatus {
  Open = 0,
  InProgress = 1,
  Done = 2,
}

export enum IssuePriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export enum SprintStatus {
  Planned = 0,
  Active = 1,
  Completed = 2,
}

export enum NotificationStatus {
  Unread = 0,
  Read = 1,
}

export enum NotificationChannel {
  InApp = 0,
  Email = 1,
}

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
  email: string;
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

export interface CreateIssueRequest {
  projectId: string;
  title: string;
  description?: string;
  priority: IssuePriority;
  assigneeUserId?: string;
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

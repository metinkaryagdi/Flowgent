import { useState, useEffect, useCallback, useMemo, useRef, type FormEvent } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import {
    DndContext,
    DragOverlay,
    PointerSensor,
    useSensor,
    useSensors,
    closestCorners,
    type DragStartEvent,
    type DragEndEvent,
} from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useDroppable } from '@dnd-kit/core';

import { bffApi } from '../../api/bff';
import { issuesApi } from '../../api/issues';
import { sprintsApi } from '../../api/sprints';
import { aiApi } from '../../api/ai';
import { useAuthStore } from '../../store/authStore';
import { useToastStore } from '../../store/toastStore';
import { IssueStatus, IssuePriority } from '../../types';
import type { IssueBoardItemDto, BoardColumn, BoardResponse, SprintDto, IssueDto } from '../../types';
import IssueDetailPanel from '../issues/IssueDetailPanel';
import { useUserLookup } from '../../hooks/useUserLookup';
import styles from './Board.module.css';

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Priority helpers
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
const priorityLabel: Record<string, string> = {
    [IssuePriority.Low]: 'Düşük',
    [IssuePriority.Medium]: 'Orta',
    [IssuePriority.High]: 'Yüksek',
    [IssuePriority.Critical]: 'Kritik',
};

const priorityClass: Record<string, string> = {
    [IssuePriority.Low]: styles.priorityLow,
    [IssuePriority.Medium]: styles.priorityMedium,
    [IssuePriority.High]: styles.priorityHigh,
    [IssuePriority.Critical]: styles.priorityCritical,
};

const statusFromKey = (key: string): IssueStatus => {
    switch (key.toLowerCase()) {
        case 'open': return IssueStatus.Open;
        case 'inprogress': return IssueStatus.InProgress;
        case 'done': return IssueStatus.Done;
        default: return IssueStatus.Open;
    }
};

const columnDotClass = (key: string) => {
    switch (key.toLowerCase()) {
        case 'open': return styles.columnDotOpen;
        case 'inprogress': return styles.columnDotInProgress;
        case 'done': return styles.columnDotDone;
        default: return '';
    }
};

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Sortable Issue Card
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
function SortableIssueCard({
    item,
    onClick,
    sprints,
    onAssignSprint,
    getUserName,
    getInitials,
}: {
    item: IssueBoardItemDto;
    onClick: () => void;
    sprints: SprintDto[];
    onAssignSprint: (issueId: string, sprintId: string) => void;
    getUserName: (id: string | null | undefined) => string;
    getInitials: (id: string | null | undefined) => string;
}) {
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: item.issueId,
    });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
    };

    return (
        <div
            ref={setNodeRef}
            style={style}
            {...attributes}
            {...listeners}
            className={`${styles.issueCard} ${isDragging ? styles.issueCardDragging : ''}`}
            data-testid="issue-card"
            data-issue-id={item.issueId}
            onClick={onClick}
        >
            <IssueCardContent
                item={item}
                sprints={sprints}
                onAssignSprint={onAssignSprint}
                getUserName={getUserName}
                getInitials={getInitials}
            />
        </div>
    );
}

function IssueCardContent({
    item,
    sprints,
    onAssignSprint,
    getUserName,
    getInitials,
}: {
    item: IssueBoardItemDto;
    sprints?: SprintDto[];
    onAssignSprint?: (issueId: string, sprintId: string) => void;
    getUserName: (id: string | null | undefined) => string;
    getInitials: (id: string | null | undefined) => string;
}) {
    const initials = item.assigneeUserId ? getInitials(item.assigneeUserId) : null;
    const [selectingSprint, setSelectingSprint] = useState(false);

    return (
        <>
            <div className={styles.issueTitle}>{item.title}</div>

            {sprints && onAssignSprint && (
                <div style={{ marginBottom: 8, fontSize: '0.75rem' }}>
                    {selectingSprint ? (
                        <select
                            autoFocus
                            defaultValue={item.sprintId || ""}
                            onChange={(e) => {
                                onAssignSprint(item.issueId, e.target.value);
                                setSelectingSprint(false);
                            }}
                            onBlur={() => setSelectingSprint(false)}
                            onPointerDown={e => e.stopPropagation()} // Prevent drag start
                            onClick={e => e.stopPropagation()}
                            style={{ padding: '2px 4px', borderRadius: 4, background: 'var(--bg-surface)', border: '1px solid var(--border-color)', width: '100%', fontSize: '0.75rem', color: 'var(--text-primary)' }}
                        >
                            <option value="">Sprint'ten Çıkar</option>
                            {sprints.map(s => (
                                <option key={s.id} value={s.id}>{s.name}</option>
                            ))}
                        </select>
                    ) : (
                        <div
                            style={{ color: 'var(--text-tertiary)', cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 4 }}
                            onPointerDown={e => e.stopPropagation()}
                            onClick={(e) => {
                                e.stopPropagation();
                                setSelectingSprint(true);
                            }}
                        >
                            <span style={{ fontSize: 10 }}>👤</span>
                            {item.sprintId ? sprints.find(s => s.id === item.sprintId)?.name || item.sprintId.slice(0, 8) : 'Sprint ata...'}
                        </div>
                    )}
                </div>
            )}

            <div className={styles.issueMeta}>
                <div className={styles.issueMetaLeft}>
                    <span className={`${styles.priorityBadge} ${priorityClass[item.priority] || ''}`}>
                        {priorityLabel[item.priority] || '?'}
                    </span>
                </div>
                {initials ? (
                    <div className={styles.assigneeAvatar} title={getUserName(item.assigneeUserId)}>
                        {initials}
                    </div>
                ) : (
                    <div className={styles.unassigned} title="Atanmamış">?</div>
                )}
            </div>
        </>
    );
}

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Droppable Column
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
function DroppableColumn({
    column,
    items,
    onIssueClick,
    sprints,
    onAssignSprint,
    getUserName,
    getInitials,
}: {
    column: BoardColumn;
    items: IssueBoardItemDto[];
    onIssueClick: (id: string) => void;
    sprints: SprintDto[];
    onAssignSprint: (issueId: string, sprintId: string) => void;
    getUserName: (id: string | null | undefined) => string;
    getInitials: (id: string | null | undefined) => string;
}) {
    const { setNodeRef, isOver } = useDroppable({ id: column.key });
    const itemIds = items.map((i) => i.issueId);

    return (
        <div
            className={styles.column}
            data-testid={`board-column-${column.key.toLowerCase()}`}
        >
            <div className={styles.columnHeader}>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    <span className={`${styles.columnDot} ${columnDotClass(column.key)}`} />
                    <span className={styles.columnTitle}>{column.title}</span>
                </div>
                <span className={styles.columnCount}>{items.length}</span>
            </div>
            <div
                ref={setNodeRef}
                className={`${styles.columnBody} ${isOver ? styles.columnDropTarget : ''}`}
            >
                <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
                    {items.map((item) => (
                        <SortableIssueCard
                            key={item.issueId}
                            item={item}
                            sprints={sprints}
                            onAssignSprint={onAssignSprint}
                            getUserName={getUserName}
                            getInitials={getInitials}
                            onClick={() => onIssueClick(item.issueId)}
                        />
                    ))}
                </SortableContext>
            </div>
        </div>
    );
}

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Board Page
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
export default function BoardPage() {
    const { projectId } = useParams<{ projectId: string }>();
    const navigate = useNavigate();
    const location = useLocation();
    const { flags } = useAuthStore();

    const [board, setBoard] = useState<BoardResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [sprints, setSprints] = useState<SprintDto[]>([]);
    const [activeItem, setActiveItem] = useState<IssueBoardItemDto | null>(null);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [selectedIssueId, setSelectedIssueId] = useState<string | null>(null);
    const [searchTerm, setSearchTerm] = useState('');
    const [priorityFilter, setPriorityFilter] = useState<'all' | IssuePriority>('all');
    const [assigneeFilter, setAssigneeFilter] = useState<'all' | 'unassigned' | string>('all');
    const [statusFilter, setStatusFilter] = useState<'all' | 'open' | 'inprogress' | 'done'>('all');
    const [itemsPerColumn, setItemsPerColumn] = useState(50);

    const { addToast: showToast } = useToastStore();

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    // Ã¢â€â‚¬Ã¢â€â‚¬ Load board Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const loadBoard = useCallback(async () => {
        if (!projectId) return;
        try {
            const boardData = await bffApi.getBoard(projectId);
            setBoard(boardData);
            setError('');
        } catch {
            setError('Board veya sprintler yüklenirken hata oluştu.');
        } finally {
            setLoading(false);
        }
        try {
            const sprintsData = await sprintsApi.getByProject(projectId);
            setSprints(sprintsData);
        } catch {
            setSprints([]);
        }
    }, [projectId]);

    useEffect(() => {
        loadBoard();
    }, [loadBoard]);

    // ── Sekme/pencere odağa dönünce taze veri çek ──
    // Detay panelinden veya başka bir sekmeden gelen değişiklikler için fallback.
    useEffect(() => {
        const onVisible = () => {
            if (document.visibilityState === 'visible') loadBoard();
        };
        window.addEventListener('focus', loadBoard);
        document.addEventListener('visibilitychange', onVisible);
        return () => {
            window.removeEventListener('focus', loadBoard);
            document.removeEventListener('visibilitychange', onVisible);
        };
    }, [loadBoard]);

    // ── Klavye kısayolları ────────
    useEffect(() => {
        const handleKey = (e: KeyboardEvent) => {
            // Input/textarea odaklıyken kısayolları engelle
            const tag = (e.target as HTMLElement).tagName;
            if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;

            if (e.key === 'n' || e.key === 'N') {
                if (flags?.canEditIssues !== false) {
                    e.preventDefault();
                    setShowCreateModal(true);
                }
            }
            if (e.key === '/') {
                e.preventDefault();
                const searchInput = document.querySelector<HTMLInputElement>('input[placeholder*="ara"]');
                searchInput?.focus();
            }
        };
        window.addEventListener('keydown', handleKey);
        return () => window.removeEventListener('keydown', handleKey);
    }, [flags]);

    // Notification deep-link: router state'den gelen openIssueId'yi aç
    useEffect(() => {
        const openIssueId = (location.state as { openIssueId?: string } | null)?.openIssueId;
        if (openIssueId) {
            setSelectedIssueId(openIssueId);
            // State'i temizle (sayfa yenilenince tekrar açılmasın)
            window.history.replaceState({}, '', window.location.pathname);
        }
    }, [location.state]);

    const assigneeIds = useMemo(
        () => (board?.items || []).map((item) => item.assigneeUserId).filter(Boolean) as string[],
        [board]
    );
    const { getUserName, getInitials } = useUserLookup(assigneeIds);

    const assigneeOptions = useMemo(() => {
        const unique = Array.from(new Set(assigneeIds));
        return unique.map((id) => ({
            id,
            label: getUserName(id),
        }));
    }, [assigneeIds, getUserName]);

    const searchLower = searchTerm.trim().toLowerCase();
    const matchesFilters = (item: IssueBoardItemDto) => {
        if (searchLower && !item.title.toLowerCase().includes(searchLower)) return false;
        if (priorityFilter !== 'all' && item.priority !== priorityFilter) return false;
        if (assigneeFilter !== 'all') {
            if (assigneeFilter === 'unassigned' && item.assigneeUserId) return false;
            if (assigneeFilter !== 'unassigned' && item.assigneeUserId !== assigneeFilter) return false;
        }
        if (statusFilter !== 'all') {
            const statusValue = statusFromKey(statusFilter);
            if (item.status !== statusValue) return false;
        }
        return true;
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Group items by column Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const getItemsByColumn = (columnKey: string): IssueBoardItemDto[] => {
        if (!board) return [];
        const status = statusFromKey(columnKey);
        if (statusFilter !== 'all' && statusFromKey(statusFilter) !== status) return [];
        const filtered = board.items.filter((item) => item.status === status && matchesFilters(item));
        return filtered.slice(0, itemsPerColumn);
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Drag start Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const handleDragStart = (event: DragStartEvent) => {
        const item = board?.items.find((i) => i.issueId === event.active.id);
        setActiveItem(item || null);
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Drag end Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const handleDragEnd = async (event: DragEndEvent) => {
        setActiveItem(null);
        const { active, over } = event;
        if (!over || !board || !projectId) return;

        const draggedId = active.id as string;
        const draggedItem = board.items.find((i) => i.issueId === draggedId);
        if (!draggedItem) return;

        // Determine target column
        let targetColumnKey: string | null = null;

        // Check if dropped over a column directly
        const isColumn = board.config.columns.some((c) => c.key === over.id);
        if (isColumn) {
            targetColumnKey = over.id as string;
        } else {
            // Dropped over another card Ã¢â‚¬â€ find which column that card belongs to
            const overItem = board.items.find((i) => i.issueId === over.id);
            if (overItem) {
                const col = board.config.columns.find(
                    (c) => statusFromKey(c.key) === overItem.status
                );
                targetColumnKey = col?.key || null;
            }
        }

        if (!targetColumnKey) return;

        const newStatus = statusFromKey(targetColumnKey);
        if (newStatus === draggedItem.status) return; // Same column, no change

        // Check allowed transitions
        const currentKey = board.config.columns.find(
            (c) => statusFromKey(c.key) === draggedItem.status
        )?.key;

        if (currentKey && board.config.allowedTransitions[currentKey]) {
            if (!board.config.allowedTransitions[currentKey].includes(targetColumnKey)) {
                showToast('Bu durum geçişine izin verilmiyor.', 'warning');
                return;
            }
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬ Optimistic update Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        const previousItems = [...board.items];
        setBoard({
            ...board,
            items: board.items.map((item) =>
                item.issueId === draggedId ? { ...item, status: newStatus } : item
            ),
        });

        // Ã¢â€â‚¬Ã¢â€â‚¬ API call Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        try {
            const updated = await issuesApi.changeStatus(draggedId, {
                newStatus,
                expectedVersion: draggedItem.version,
            });
            showToast('Durum güncellendi!');
            // Patch new version from server response. Avoid full loadBoard() —
            // out-of-order responses can overwrite fresh optimistic state.
            setBoard((prev) => {
                if (!prev) return prev;
                return {
                    ...prev,
                    items: prev.items.map((item) =>
                        item.issueId === draggedId
                            ? { ...item, status: updated.status, version: updated.version }
                            : item
                    ),
                };
            });
        } catch (err: unknown) {
            // Rollback on error
            setBoard((prev) => (prev ? { ...prev, items: previousItems } : prev));

            if (err && typeof err === 'object' && 'response' in err) {
                const axiosErr = err as { response?: { status?: number } };
                if (axiosErr.response?.status === 409) {
                    showToast('Bu issue başkası tarafından güncellendi! Sayfa yenileniyor...', 'warning');
                    setTimeout(() => loadBoard(), 1500);
                    return;
                }
                if (axiosErr.response?.status === 403) {
                    showToast('Bu işlem için yetkiniz yok.', 'warning');
                    return;
                }
            }
            showToast('Durum güncellenirken hata oluştu.', 'error');
        }
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Create issue Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const handleCreateIssue = async (title: string, description: string, priority: IssuePriority) => {
        if (!projectId) return;
        // Hata fırlatılırsa modal yakalar, başarıda board yenilenir
        await issuesApi.create({ projectId, title, description, priority });
        showToast('Issue oluşturuldu!');
        setShowCreateModal(false);
        await loadBoard();
    };

    // ── Issue updated from detail panel: optimistic patch only ──
    // Background loadBoard() removed: out-of-order responses were overwriting fresh
    // optimistic state with stale snapshots. The API response already contains the
    // authoritative new state (status/assignee/version), so the patch is sufficient.
    const handleIssueUpdated = useCallback((updated: IssueDto) => {
        setBoard((prev) => {
            if (!prev) return prev;
            return {
                ...prev,
                items: prev.items.map((item) =>
                    item.issueId === updated.id
                        ? {
                            ...item,
                            title: updated.title,
                            status: updated.status,
                            priority: updated.priority,
                            assigneeUserId: updated.assigneeUserId,
                            sprintId: updated.sprintId ?? item.sprintId,
                            version: updated.version,
                        }
                        : item
                ),
            };
        });
    }, []);

    // Ã¢â€â‚¬Ã¢â€â‚¬ Assign Sprint Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const handleAssignSprint = async (issueId: string, sprintId: string) => {
        try {
            if (sprintId) {
                await sprintsApi.addIssue(sprintId, issueId);
            } else {
                const item = board?.items.find(i => i.issueId === issueId);
                if (item?.sprintId) {
                    await sprintsApi.removeIssue(item.sprintId, issueId);
                }
            }
            showToast('Sprint güncellendi!');
            await loadBoard();
        } catch {
            showToast('Sprint güncellenirken hata oluştu.', 'error');
        }
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Loading Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    if (loading) {
        return (
            <div className={styles.boardPage}>
                <div className={styles.boardHeader}>
                    <div className={styles.boardHeaderLeft}>
                        <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                        <h1 className={styles.boardTitle}>Yükleniyor...</h1>
                    </div>
                </div>
                <div className={styles.boardLoading}>
                    {[1, 2, 3].map((i) => (
                        <div key={i} className={styles.columnSkeleton}>
                            <div className={styles.skeletonHeader} />
                            <div className={styles.skeletonCard} />
                            <div className={styles.skeletonCard} />
                        </div>
                    ))}
                </div>
            </div>
        );
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬ Error Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    if (error || !board) {
        return (
            <div className={styles.boardPage}>
                <div className={styles.errorState}>
                    <div className={styles.errorIcon}>⚠️</div>
                    <h2 className={styles.errorTitle}>{error || 'Board bulunamadı'}</h2>
                    <p className={styles.errorText}>Lütfen tekrar deneyin veya projeye geri dönün.</p>
                    <button className={styles.retryBtn} onClick={loadBoard}>Tekrar Dene</button>
                    <button
                        className={styles.backBtn}
                        style={{ marginTop: 8 }}
                        onClick={() => navigate('/projects')}
                    >
                        ← Projelere Dön
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className={styles.boardPage}>
            {/* Ã¢â€â‚¬Ã¢â€â‚¬ Header Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ */}
            <div className={styles.boardHeader}>
                <div className={styles.boardHeaderLeft}>
                    <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                    <h1 className={styles.boardTitle}>
                        {board.project?.name || 'Board'}
                        {board.project?.key && <span className={styles.boardKey}>{board.project.key}</span>}
                    </h1>
                </div>
                <div className={styles.boardHeaderRight}>
                    <button
                        className={styles.addIssueBtn}
                        style={{ background: 'var(--bg-surface)', color: 'var(--text-primary)', border: '1px solid var(--border-color)' }}
                        onClick={() => navigate(`/projects/${projectId}/sprints`)}
                    >
                        Sprint'ler
                    </button>
                    {flags?.canEditIssues !== false && (
                        <button className={styles.addIssueBtn} data-testid="issue-create-open" onClick={() => setShowCreateModal(true)} aria-label="Yeni issue oluştur (N)">
                            <span>+</span> Yeni Issue
                        </button>
                    )}
                </div>
            </div>

            {/* Ã¢â€â‚¬Ã¢â€â‚¬ Board Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ */}
            <div className={styles.filterBar}>
                <input
                    className={styles.filterInput}
                    type="text"
                    placeholder="Issue ara..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    aria-label="Issue ara (/ kısayolu)"
                />
                <select
                    className={styles.filterSelect}
                    value={priorityFilter}
                    onChange={(e) => setPriorityFilter(e.target.value as 'all' | IssuePriority)}
                >
                    <option value="all">Tüm Öncelikler</option>
                    <option value={IssuePriority.Low}>Düşük</option>
                    <option value={IssuePriority.Medium}>Orta</option>
                    <option value={IssuePriority.High}>Yüksek</option>
                    <option value={IssuePriority.Critical}>Kritik</option>
                </select>
                <select
                    className={styles.filterSelect}
                    value={assigneeFilter}
                    onChange={(e) => setAssigneeFilter(e.target.value)}
                >
                    <option value="all">Tüm Atananlar</option>
                    <option value="unassigned">Atanmamış</option>
                    {assigneeOptions.map((assignee) => (
                        <option key={assignee.id} value={assignee.id}>{assignee.label}</option>
                    ))}
                </select>
                <select
                    className={styles.filterSelect}
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value as 'all' | 'open' | 'inprogress' | 'done')}
                >
                    <option value="all">Tüm Durumlar</option>
                    <option value="open">Açık</option>
                    <option value="inprogress">Devam</option>
                    <option value="done">Bitti</option>
                </select>
                <select
                    className={styles.filterSelect}
                    value={itemsPerColumn}
                    onChange={(e) => setItemsPerColumn(Number(e.target.value))}
                >
                    <option value={20}>20 / Kolon</option>
                    <option value={50}>50 / Kolon</option>
                    <option value={100}>100 / Kolon</option>
                </select>
                <button className={styles.filterClear}
                    onClick={() => {
                        setSearchTerm('');
                        setPriorityFilter('all');
                        setAssigneeFilter('all');
                        setStatusFilter('all');
                        setItemsPerColumn(50);
                    }}
                >
                    Filtreleri Temizle
                </button>
            </div>
            <DndContext
                sensors={sensors}
                collisionDetection={closestCorners}
                onDragStart={handleDragStart}
                onDragEnd={handleDragEnd}
            >
                <div className={styles.boardContainer}>
                    {board.config.columns.map((column) => (
                        <DroppableColumn
                            key={column.key}
                            column={column}
                            items={getItemsByColumn(column.key)}
                            sprints={sprints}
                            onAssignSprint={handleAssignSprint}
                            getUserName={getUserName}
                            getInitials={getInitials}
                            onIssueClick={(id) => setSelectedIssueId(id)}
                        />
                    ))}
                </div>

                <DragOverlay>
                    {activeItem ? (
                        <div className={styles.issueCardOverlay}>
                            <IssueCardContent
                                item={activeItem}
                                sprints={sprints}
                                onAssignSprint={handleAssignSprint}
                                getUserName={getUserName}
                                getInitials={getInitials}
                            />
                        </div>
                    ) : null}
                </DragOverlay>
            </DndContext>

            {/* Ã¢â€â‚¬Ã¢â€â‚¬ Create Modal Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ */}
            {showCreateModal && (
                <CreateIssueModal
                    projectId={projectId ?? ''}
                    onSubmit={handleCreateIssue}
                    onClose={() => setShowCreateModal(false)}
                />
            )}

            {/* Ã¢â€â‚¬Ã¢â€â‚¬ Issue Detail Panel Ã¢â€â‚¬Ã¢â€â‚¬ */}
            {selectedIssueId && (
                <IssueDetailPanel
                    issueId={selectedIssueId}
                    onClose={() => { setSelectedIssueId(null); loadBoard(); }}
                    onUpdated={handleIssueUpdated}
                />
            )}
        </div>
    );
}

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Create Issue Modal
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
function CreateIssueModal({
    projectId,
    onSubmit,
    onClose,
}: {
    projectId: string;
    onSubmit: (title: string, description: string, priority: IssuePriority) => Promise<void>;
    onClose: () => void;
}) {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [priority, setPriority] = useState<IssuePriority>(IssuePriority.Medium);
    const [submitting, setSubmitting] = useState(false);
    const [errorMsg, setErrorMsg] = useState('');
    const [duplicateChecking, setDuplicateChecking] = useState(false);
    const [similarIssues, setSimilarIssues] = useState<{ issueId: string; title: string; reason: string; similarityScore: number }[]>([]);
    const duplicateTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const checkDuplicates = (value: string) => {
        if (duplicateTimerRef.current) clearTimeout(duplicateTimerRef.current);
        setSimilarIssues([]);
        if (value.trim().length < 8 || !projectId) return;
        duplicateTimerRef.current = setTimeout(async () => {
            setDuplicateChecking(true);
            try {
                const result = await aiApi.detectDuplicate(projectId, value.trim());
                setSimilarIssues(result.similarIssues ?? []);
            } catch {
                // silent — don't block issue creation
            } finally {
                setDuplicateChecking(false);
            }
        }, 900);
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!title.trim()) return;
        setSubmitting(true);
        setErrorMsg('');
        try {
            await onSubmit(title.trim(), description.trim(), priority);
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const axiosErr = err as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } };
                const msgs = axiosErr.response?.data?.errors;
                if (msgs) {
                    const first = Object.values(msgs)[0]?.[0];
                    setErrorMsg(first || 'Doğrulama hatası.');
                } else {
                    setErrorMsg(axiosErr.response?.data?.message || 'Issue oluşturulurken hata oluştu.');
                }
            } else {
                setErrorMsg('Issue oluşturulurken hata oluştu.');
            }
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className={styles.modalOverlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2 className={styles.modalTitle}>Yeni Issue Oluştur</h2>
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="issueTitle">Başlık *</label>
                        <input
                            id="issueTitle"
                            data-testid="issue-title"
                            type="text"
                            className={styles.formInput}
                            value={title}
                            onChange={(e) => { setTitle(e.target.value); checkDuplicates(e.target.value); }}
                            placeholder="Issue başlığı"
                            autoFocus
                        />
                        {duplicateChecking && (
                            <p style={{ fontSize: '0.72rem', color: 'var(--color-text-muted)', margin: '4px 0 0' }}>✦ Benzer issue'lar aranıyor...</p>
                        )}
                        {similarIssues.length > 0 && (
                            <div style={{ background: 'var(--color-warning-light, #fef3c7)', border: '1px solid var(--color-warning, #f59e0b)', borderRadius: 'var(--border-radius-md)', padding: '8px 12px', marginTop: 6 }}>
                                <p style={{ fontSize: '0.72rem', fontWeight: 700, color: 'var(--color-warning, #b45309)', margin: '0 0 6px' }}>Benzer issue'lar tespit edildi:</p>
                                {similarIssues.map((s) => (
                                    <div key={s.issueId} style={{ fontSize: '0.72rem', color: 'var(--color-text-secondary)', marginBottom: 3 }}>
                                        <strong>{s.title}</strong> — {s.reason} <span style={{ opacity: 0.6 }}>(%{s.similarityScore})</span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="issueDesc">Açıklama</label>
                        <textarea
                            id="issueDesc"
                            data-testid="issue-description"
                            className={styles.formTextarea}
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="İsteğe bağlı açıklama"
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="issuePriority">Ãƒâ€“ncelik</label>
                        <select
                            id="issuePriority"
                            data-testid="issue-priority"
                            className={styles.formSelect}
                            value={priority}
                            onChange={(e) => setPriority(e.target.value as IssuePriority)}
                        >
                            <option value={IssuePriority.Low}>Düşük</option>
                            <option value={IssuePriority.Medium}>Orta</option>
                            <option value={IssuePriority.High}>Yüksek</option>
                            <option value={IssuePriority.Critical}>Kritik</option>
                        </select>
                    </div>
                    {errorMsg && (
                        <p style={{ color: 'var(--color-error, #ef4444)', fontSize: '0.8rem', margin: '0 0 12px' }}>{errorMsg}</p>
                    )}
                    <div className={styles.modalFooter}>
                        <button type="button" className={styles.btnSecondary} onClick={onClose}>İptal</button>
                        <button
                            type="submit"
                            className={styles.btnPrimary}
                            disabled={submitting || !title.trim()}
                            data-testid="issue-create-submit"
                        >
                            {submitting ? 'Oluşturuluyor...' : 'Oluştur'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}


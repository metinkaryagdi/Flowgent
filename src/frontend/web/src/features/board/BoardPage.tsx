import { useState, useEffect, useCallback, useMemo, type FormEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
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
import { useAuthStore } from '../../store/authStore';
import { IssueStatus, IssuePriority } from '../../types';
import type { IssueBoardItemDto, BoardColumn, BoardResponse, SprintDto } from '../../types';
import IssueDetailPanel from '../issues/IssueDetailPanel';
import { useUserLookup } from '../../hooks/useUserLookup';
import styles from './Board.module.css';

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Priority helpers
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
const priorityLabel: Record<number, string> = {
    [IssuePriority.Low]: 'DÃƒÂ¼Ã…Å¸ÃƒÂ¼k',
    [IssuePriority.Medium]: 'Orta',
    [IssuePriority.High]: 'YÃƒÂ¼ksek',
    [IssuePriority.Critical]: 'Kritik',
};

const priorityClass: Record<number, string> = {
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
                            <option value="">Sprint'ten Ãƒâ€¡Ã„Â±kar</option>
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
                            <span style={{ fontSize: 10 }}>ÄŸÅ¸â€â€</span>
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
                    <div className={styles.unassigned} title="AtanmamÃ„Â±Ã…Å¸">?</div>
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
    const { flags } = useAuthStore();

    const [board, setBoard] = useState<BoardResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [sprints, setSprints] = useState<SprintDto[]>([]);
    const [activeItem, setActiveItem] = useState<IssueBoardItemDto | null>(null);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [selectedIssueId, setSelectedIssueId] = useState<string | null>(null);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'warning' } | null>(null);
    const [searchTerm, setSearchTerm] = useState('');
    const [priorityFilter, setPriorityFilter] = useState<'all' | '0' | '1' | '2' | '3'>('all');
    const [assigneeFilter, setAssigneeFilter] = useState<'all' | 'unassigned' | string>('all');
    const [statusFilter, setStatusFilter] = useState<'all' | 'open' | 'inprogress' | 'done'>('all');
    const [itemsPerColumn, setItemsPerColumn] = useState(50);

    const showToast = (message: string, type: 'success' | 'error' | 'warning' = 'success') => {
        setToast({ message, type });
        setTimeout(() => setToast(null), 3500);
    };

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    // Ã¢â€â‚¬Ã¢â€â‚¬ Load board Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const loadBoard = useCallback(async () => {
        if (!projectId) return;
        try {
            const [boardData, sprintsData] = await Promise.all([
                bffApi.getBoard(projectId),
                sprintsApi.getByProject(projectId)
            ]);
            setBoard(boardData);
            setSprints(sprintsData);
            setError('');
        } catch {
            setError('Board veya sprintler yÃƒÂ¼klenirken hata oluÃ…Å¸tu.');
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        loadBoard();
    }, [loadBoard]);

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
        if (priorityFilter !== 'all' && item.priority !== Number(priorityFilter)) return false;
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
                showToast('Bu durum geÃƒÂ§iÃ…Å¸ine izin verilmiyor.', 'warning');
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
            await issuesApi.changeStatus(draggedId, {
                newStatus,
                expectedVersion: draggedItem.version,
            });
            showToast('Durum gÃƒÂ¼ncellendi!');
            // Reload to get fresh versions
            await loadBoard();
        } catch (err: unknown) {
            // Rollback on error
            setBoard((prev) => (prev ? { ...prev, items: previousItems } : prev));

            if (err && typeof err === 'object' && 'response' in err) {
                const axiosErr = err as { response?: { status?: number } };
                if (axiosErr.response?.status === 409) {
                    showToast('Bu issue baÃ…Å¸kasÃ„Â± tarafÃ„Â±ndan gÃƒÂ¼ncellendi! Sayfa yenileniyor...', 'warning');
                    setTimeout(() => loadBoard(), 1500);
                    return;
                }
            }
            showToast('Durum gÃƒÂ¼ncellenirken hata oluÃ…Å¸tu.', 'error');
        }
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Create issue Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    const handleCreateIssue = async (title: string, description: string, priority: IssuePriority) => {
        if (!projectId) return;
        try {
            await issuesApi.create({ projectId, title, description, priority });
            showToast('Issue oluÃ…Å¸turuldu!');
            setShowCreateModal(false);
            await loadBoard();
        } catch {
            showToast('Issue oluÃ…Å¸turulurken hata oluÃ…Å¸tu.', 'error');
        }
    };

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
            showToast('Sprint gÃƒÂ¼ncellendi!');
            await loadBoard();
        } catch {
            showToast('Sprint gÃƒÂ¼ncellenirken hata oluÃ…Å¸tu.', 'error');
        }
    };

    // Ã¢â€â‚¬Ã¢â€â‚¬ Loading Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    if (loading) {
        return (
            <div className={styles.boardPage}>
                <div className={styles.boardHeader}>
                    <div className={styles.boardHeaderLeft}>
                        <button className={styles.backBtn} onClick={() => navigate('/projects')}>Ã¢â€ Â</button>
                        <h1 className={styles.boardTitle}>YÃƒÂ¼kleniyor...</h1>
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
                    <div className={styles.errorIcon}>Ã¢Å¡Â Ã¯Â¸Â</div>
                    <h2 className={styles.errorTitle}>{error || 'Board bulunamadÃ„Â±'}</h2>
                    <p className={styles.errorText}>LÃƒÂ¼tfen tekrar deneyin veya projeye geri dÃƒÂ¶nÃƒÂ¼n.</p>
                    <button className={styles.retryBtn} onClick={loadBoard}>Tekrar Dene</button>
                    <button
                        className={styles.backBtn}
                        style={{ marginTop: 8 }}
                        onClick={() => navigate('/projects')}
                    >
                        Ã¢â€ Â Projelere DÃƒÂ¶n
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
                    <button className={styles.backBtn} onClick={() => navigate('/projects')}>Ã¢â€ Â</button>
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
                        ÄŸÅ¸â€œÅ  Sprint'ler
                    </button>
                    {flags?.canEditIssues !== false && (
                        <button className={styles.addIssueBtn} data-testid="issue-create-open" onClick={() => setShowCreateModal(true)}>
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
                />
                <select
                    className={styles.filterSelect}
                    value={priorityFilter}
                    onChange={(e) => setPriorityFilter(e.target.value as 'all' | '0' | '1' | '2' | '3')}
                >
                    <option value="all">TÃ¼m Ã–ncelikler</option>
                    <option value={IssuePriority.Low}>DÃ¼ÅŸÃ¼k</option>
                    <option value={IssuePriority.Medium}>Orta</option>
                    <option value={IssuePriority.High}>YÃ¼ksek</option>
                    <option value={IssuePriority.Critical}>Kritik</option>
                </select>
                <select
                    className={styles.filterSelect}
                    value={assigneeFilter}
                    onChange={(e) => setAssigneeFilter(e.target.value)}
                >
                    <option value="all">TÃ¼m Atananlar</option>
                    <option value="unassigned">AtanmamÄ±ÅŸ</option>
                    {assigneeOptions.map((assignee) => (
                        <option key={assignee.id} value={assignee.id}>{assignee.label}</option>
                    ))}
                </select>
                <select
                    className={styles.filterSelect}
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value as 'all' | 'open' | 'inprogress' | 'done')}
                >
                    <option value="all">TÃ¼m Durumlar</option>
                    <option value="open">AÃ§Ä±k</option>
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
                    onSubmit={handleCreateIssue}
                    onClose={() => setShowCreateModal(false)}
                />
            )}

            {/* Ã¢â€â‚¬Ã¢â€â‚¬ Toast Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ */}
            {toast && (
                <div
                    className={`${styles.toast} ${toast.type === 'success'
                        ? styles.toastSuccess
                        : toast.type === 'error'
                            ? styles.toastError
                            : styles.toastWarning
                        }`}
                >
                    {toast.message}
                </div>
            )}

            {/* Ã¢â€â‚¬Ã¢â€â‚¬ Issue Detail Panel Ã¢â€â‚¬Ã¢â€â‚¬ */}
            {selectedIssueId && (
                <IssueDetailPanel
                    issueId={selectedIssueId}
                    onClose={() => setSelectedIssueId(null)}
                    onUpdated={loadBoard}
                />
            )}
        </div>
    );
}

// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
// Create Issue Modal
// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
function CreateIssueModal({
    onSubmit,
    onClose,
}: {
    onSubmit: (title: string, description: string, priority: IssuePriority) => Promise<void>;
    onClose: () => void;
}) {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [priority, setPriority] = useState<IssuePriority>(IssuePriority.Medium);
    const [submitting, setSubmitting] = useState(false);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!title.trim()) return;
        setSubmitting(true);
        try {
            await onSubmit(title.trim(), description.trim(), priority);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className={styles.modalOverlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2 className={styles.modalTitle}>Yeni Issue OluÃ…Å¸tur</h2>
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="issueTitle">BaÃ…Å¸lÃ„Â±k *</label>
                        <input
                            id="issueTitle"
                            data-testid="issue-title"
                            type="text"
                            className={styles.formInput}
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="Issue baÃ…Å¸lÃ„Â±Ã„Å¸Ã„Â±"
                            autoFocus
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="issueDesc">AÃƒÂ§Ã„Â±klama</label>
                        <textarea
                            id="issueDesc"
                            data-testid="issue-description"
                            className={styles.formTextarea}
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="Ã„Â°steÃ„Å¸e baÃ„Å¸lÃ„Â± aÃƒÂ§Ã„Â±klama"
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="issuePriority">Ãƒâ€“ncelik</label>
                        <select
                            id="issuePriority"
                            data-testid="issue-priority"
                            className={styles.formSelect}
                            value={priority}
                            onChange={(e) => setPriority(Number(e.target.value) as IssuePriority)}
                        >
                            <option value={IssuePriority.Low}>DÃƒÂ¼Ã…Å¸ÃƒÂ¼k</option>
                            <option value={IssuePriority.Medium}>Orta</option>
                            <option value={IssuePriority.High}>YÃƒÂ¼ksek</option>
                            <option value={IssuePriority.Critical}>Kritik</option>
                        </select>
                    </div>
                    <div className={styles.modalFooter}>
                        <button type="button" className={styles.btnSecondary} onClick={onClose}>Ã„Â°ptal</button>
                        <button
                            type="submit"
                            className={styles.btnPrimary}
                            disabled={submitting || !title.trim()}
                            data-testid="issue-create-submit"
                        >
                            {submitting ? 'OluÃ…Å¸turuluyor...' : 'OluÃ…Å¸tur'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}


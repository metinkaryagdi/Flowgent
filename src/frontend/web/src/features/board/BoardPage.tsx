import { useState, useEffect, useCallback, type FormEvent } from 'react';
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
import { useAuthStore } from '../../store/authStore';
import { IssueStatus, IssuePriority } from '../../types';
import type { IssueBoardItemDto, BoardColumn, BoardResponse } from '../../types';
import IssueDetailPanel from '../issues/IssueDetailPanel';
import styles from './Board.module.css';

// ═════════════════════════════════════
// Priority helpers
// ═════════════════════════════════════
const priorityLabel: Record<number, string> = {
    [IssuePriority.Low]: 'Düşük',
    [IssuePriority.Medium]: 'Orta',
    [IssuePriority.High]: 'Yüksek',
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

// ═════════════════════════════════════
// Sortable Issue Card
// ═════════════════════════════════════
function SortableIssueCard({ item, onClick }: { item: IssueBoardItemDto; onClick: () => void }) {
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
            <IssueCardContent item={item} />
        </div>
    );
}

function IssueCardContent({ item }: { item: IssueBoardItemDto }) {
    const initials = item.assigneeUserId ? item.assigneeUserId.slice(0, 2).toUpperCase() : null;

    return (
        <>
            <div className={styles.issueTitle}>{item.title}</div>
            <div className={styles.issueMeta}>
                <div className={styles.issueMetaLeft}>
                    <span className={`${styles.priorityBadge} ${priorityClass[item.priority] || ''}`}>
                        {priorityLabel[item.priority] || '?'}
                    </span>
                </div>
                {initials ? (
                    <div className={styles.assigneeAvatar} title={item.assigneeUserId || ''}>
                        {initials}
                    </div>
                ) : (
                    <div className={styles.unassigned} title="Atanmamış">?</div>
                )}
            </div>
        </>
    );
}

// ═════════════════════════════════════
// Droppable Column
// ═════════════════════════════════════
function DroppableColumn({
    column,
    items,
    onIssueClick,
}: {
    column: BoardColumn;
    items: IssueBoardItemDto[];
    onIssueClick: (id: string) => void;
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
                            onClick={() => onIssueClick(item.issueId)}
                        />
                    ))}
                </SortableContext>
            </div>
        </div>
    );
}

// ═════════════════════════════════════
// Board Page
// ═════════════════════════════════════
export default function BoardPage() {
    const { projectId } = useParams<{ projectId: string }>();
    const navigate = useNavigate();
    const { flags } = useAuthStore();

    const [board, setBoard] = useState<BoardResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [activeItem, setActiveItem] = useState<IssueBoardItemDto | null>(null);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [selectedIssueId, setSelectedIssueId] = useState<string | null>(null);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'warning' } | null>(null);

    const showToast = (message: string, type: 'success' | 'error' | 'warning' = 'success') => {
        setToast({ message, type });
        setTimeout(() => setToast(null), 3500);
    };

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    // ── Load board ────────────────
    const loadBoard = useCallback(async () => {
        if (!projectId) return;
        try {
            const data = await bffApi.getBoard(projectId);
            setBoard(data);
            setError('');
        } catch {
            setError('Board yüklenirken hata oluştu.');
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        loadBoard();
    }, [loadBoard]);

    // ── Group items by column ─────
    const getItemsByColumn = (columnKey: string): IssueBoardItemDto[] => {
        if (!board) return [];
        const status = statusFromKey(columnKey);
        return board.items.filter((item) => item.status === status);
    };

    // ── Drag start ────────────────
    const handleDragStart = (event: DragStartEvent) => {
        const item = board?.items.find((i) => i.issueId === event.active.id);
        setActiveItem(item || null);
    };

    // ── Drag end ──────────────────
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
            // Dropped over another card — find which column that card belongs to
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

        // ── Optimistic update ─────
        const previousItems = [...board.items];
        setBoard({
            ...board,
            items: board.items.map((item) =>
                item.issueId === draggedId ? { ...item, status: newStatus } : item
            ),
        });

        // ── API call ──────────────
        try {
            await issuesApi.changeStatus(draggedId, {
                newStatus,
                expectedVersion: draggedItem.version,
            });
            showToast('Durum güncellendi!');
            // Reload to get fresh versions
            await loadBoard();
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
            }
            showToast('Durum güncellenirken hata oluştu.', 'error');
        }
    };

    // ── Create issue ──────────────
    const handleCreateIssue = async (title: string, description: string, priority: IssuePriority) => {
        if (!projectId) return;
        try {
            await issuesApi.create({ projectId, title, description, priority });
            showToast('Issue oluşturuldu!');
            setShowCreateModal(false);
            await loadBoard();
        } catch {
            showToast('Issue oluşturulurken hata oluştu.', 'error');
        }
    };

    // ── Loading ───────────────────
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

    // ── Error ─────────────────────
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
            {/* ── Header ─────────────── */}
            <div className={styles.boardHeader}>
                <div className={styles.boardHeaderLeft}>
                    <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                    <h1 className={styles.boardTitle}>
                        {board.project?.name || 'Board'}
                        {board.project?.key && <span className={styles.boardKey}>{board.project.key}</span>}
                    </h1>
                </div>
                <div className={styles.boardHeaderRight}>
                    {flags?.canEditIssues !== false && (
                        <button className={styles.addIssueBtn} data-testid="issue-create-open" onClick={() => setShowCreateModal(true)}>
                            <span>+</span> Yeni Issue
                        </button>
                    )}
                </div>
            </div>

            {/* ── Board ──────────────── */}
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
                            onIssueClick={(id) => setSelectedIssueId(id)}
                        />
                    ))}
                </div>

                <DragOverlay>
                    {activeItem ? (
                        <div className={styles.issueCardOverlay}>
                            <IssueCardContent item={activeItem} />
                        </div>
                    ) : null}
                </DragOverlay>
            </DndContext>

            {/* ── Create Modal ──────── */}
            {showCreateModal && (
                <CreateIssueModal
                    onSubmit={handleCreateIssue}
                    onClose={() => setShowCreateModal(false)}
                />
            )}

            {/* ── Toast ─────────────── */}
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

            {/* ── Issue Detail Panel ── */}
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

// ═════════════════════════════════════
// Create Issue Modal
// ═════════════════════════════════════
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
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="Issue başlığı"
                            autoFocus
                        />
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
                        <label className={styles.formLabel} htmlFor="issuePriority">Öncelik</label>
                        <select
                            id="issuePriority"
                            data-testid="issue-priority"
                            className={styles.formSelect}
                            value={priority}
                            onChange={(e) => setPriority(Number(e.target.value) as IssuePriority)}
                        >
                            <option value={IssuePriority.Low}>Düşük</option>
                            <option value={IssuePriority.Medium}>Orta</option>
                            <option value={IssuePriority.High}>Yüksek</option>
                            <option value={IssuePriority.Critical}>Kritik</option>
                        </select>
                    </div>
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


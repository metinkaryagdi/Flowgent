import { useState, useEffect, type FormEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { sprintsApi } from '../../api/sprints';
import { issuesApi } from '../../api/issues';
import { IssuePriority, SprintStatus } from '../../types';
import type { SprintDto, IssueDto } from '../../types';
import styles from './Sprint.module.css';

const priorityDot: Record<number, string> = {
    [IssuePriority.Low]: styles.dotLow,
    [IssuePriority.Medium]: styles.dotMedium,
    [IssuePriority.High]: styles.dotHigh,
    [IssuePriority.Critical]: styles.dotCritical,
};

const formatDate = (d: string) =>
    new Date(d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' });

export default function SprintPage() {
    const { projectId } = useParams<{ projectId: string }>();
    const navigate = useNavigate();

    const [sprints, setSprints] = useState<SprintDto[]>([]);
    const [backlogIssues, setBacklogIssues] = useState<IssueDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

    const showToast = (message: string, type: 'success' | 'error' = 'success') => {
        setToast({ message, type });
        setTimeout(() => setToast(null), 3000);
    };

    useEffect(() => {
        loadData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [projectId]);

    const loadData = async () => {
        if (!projectId) return;
        setLoading(true);
        try {
            const [sprintData, issueData] = await Promise.all([
                sprintsApi.getByProject(projectId),
                issuesApi.getByProject(projectId),
            ]);
            setSprints(sprintData);
            // Backlog = issues without a sprint
            const backlog = (issueData as unknown as IssueDto[]).filter((i) => !i.sprintId);
            setBacklogIssues(backlog);
        } catch {
            showToast('Veriler yüklenirken hata oluştu.', 'error');
        } finally {
            setLoading(false);
        }
    };

    const activeSprint = sprints.find((s) => s.status === SprintStatus.Active);
    const plannedSprints = sprints.filter((s) => s.status === SprintStatus.Planned);
    const completedSprints = sprints.filter((s) => s.status === SprintStatus.Completed);

    // ── Sprint actions ────────────
    const handleStartSprint = async (id: string) => {
        try {
            await sprintsApi.start(id);
            showToast('Sprint başlatıldı!');
            await loadData();
        } catch {
            showToast('Sprint başlatılırken hata oluştu.', 'error');
        }
    };

    const handleCompleteSprint = async (id: string) => {
        try {
            await sprintsApi.complete(id);
            showToast('Sprint tamamlandı!');
            await loadData();
        } catch {
            showToast('Sprint tamamlanırken hata oluştu.', 'error');
        }
    };

    const handleCreateSprint = async (name: string, startDate: string, endDate: string) => {
        if (!projectId) return;
        try {
            await sprintsApi.create({ projectId, name, startDate, endDate });
            showToast('Sprint oluşturuldu!');
            setShowCreateModal(false);
            await loadData();
        } catch {
            showToast('Sprint oluşturulurken hata oluştu.', 'error');
        }
    };

    if (loading) {
        return (
            <div className={styles.sprintPage}>
                <div className={styles.header}>
                    <div className={styles.headerLeft}>
                        <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                        <h1 className={styles.title}>Yükleniyor...</h1>
                    </div>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                    {[1, 2, 3].map((i) => <div key={i} className={styles.skeleton} />)}
                </div>
            </div>
        );
    }

    return (
        <div className={styles.sprintPage}>
            {/* ── Header ─────────────── */}
            <div className={styles.header}>
                <div className={styles.headerLeft}>
                    <button className={styles.backBtn} onClick={() => navigate(`/projects/${projectId}/board`)}>←</button>
                    <h1 className={styles.title}>Sprint & Backlog</h1>
                </div>
                <button className={styles.createBtn} onClick={() => setShowCreateModal(true)}>
                    + Yeni Sprint
                </button>
            </div>

            {/* ── Active Sprint ──────── */}
            {activeSprint && (
                <div className={styles.activeSprint}>
                    <div className={styles.activeSprintHeader}>
                        <h2 className={styles.activeSprintTitle}>{activeSprint.name}</h2>
                        <span className={styles.activeSprintBadge}>Aktif</span>
                    </div>
                    <div className={styles.sprintDates}>
                        <div className={styles.sprintDate}>
                            <span className={styles.sprintDateLabel}>Başlangıç:</span>
                            {formatDate(activeSprint.startDate)}
                        </div>
                        <div className={styles.sprintDate}>
                            <span className={styles.sprintDateLabel}>Bitiş:</span>
                            {formatDate(activeSprint.endDate)}
                        </div>
                    </div>
                    <div className={styles.progressBar}>
                        <div
                            className={styles.progressFill}
                            style={{ width: `${Math.min(100, ((activeSprint.completedIssueCount || 0) / Math.max(1, activeSprint.totalIssueCount || 1)) * 100)}%` }}
                        />
                    </div>
                    <div className={styles.progressText}>
                        {activeSprint.completedIssueCount || 0} / {activeSprint.totalIssueCount || 0} tamamlandı
                    </div>
                    <div className={styles.sprintActions}>
                        <button className={styles.completeBtn} onClick={() => handleCompleteSprint(activeSprint.id)}>
                            ✓ Sprint'i Tamamla
                        </button>
                    </div>
                </div>
            )}

            {/* ── Planned Sprints ────── */}
            {plannedSprints.length > 0 && (
                <>
                    <h3 className={styles.sectionTitle}>Planlanan Sprint'ler</h3>
                    <div className={styles.sprintList}>
                        {plannedSprints.map((sprint) => (
                            <div key={sprint.id} className={styles.sprintCard}>
                                <div className={styles.sprintCardInfo}>
                                    <h3>{sprint.name}</h3>
                                    <p>{formatDate(sprint.startDate)} — {formatDate(sprint.endDate)}</p>
                                </div>
                                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                    <span className={`${styles.statusBadge} ${styles.statusPlanned}`}>Planlandı</span>
                                    {!activeSprint && (
                                        <button className={styles.startBtn} onClick={() => handleStartSprint(sprint.id)}>
                                            Başlat
                                        </button>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                </>
            )}

            {/* ── Completed Sprints ──── */}
            {completedSprints.length > 0 && (
                <>
                    <h3 className={styles.sectionTitle}>Tamamlanan Sprint'ler</h3>
                    <div className={styles.sprintList}>
                        {completedSprints.slice(0, 5).map((sprint) => (
                            <div key={sprint.id} className={styles.sprintCard}>
                                <div className={styles.sprintCardInfo}>
                                    <h3>{sprint.name}</h3>
                                    <p>{formatDate(sprint.startDate)} — {formatDate(sprint.endDate)}</p>
                                </div>
                                <span className={`${styles.statusBadge} ${styles.statusCompleted}`}>Tamamlandı</span>
                            </div>
                        ))}
                    </div>
                </>
            )}

            {/* ── Backlog ────────────── */}
            <div className={styles.backlogSection}>
                <h3 className={styles.sectionTitle}>Backlog ({backlogIssues.length})</h3>
                {backlogIssues.length === 0 ? (
                    <div className={styles.empty}>
                        <div className={styles.emptyIcon}>📋</div>
                        <p>Backlog'da issue yok.</p>
                    </div>
                ) : (
                    <div className={styles.backlogList}>
                        {backlogIssues.map((issue) => (
                            <div key={issue.id} className={styles.backlogItem}>
                                <div className={styles.backlogItemLeft}>
                                    <span className={`${styles.priorityDot} ${priorityDot[issue.priority] || ''}`} />
                                    <span className={styles.backlogItemTitle}>{issue.title}</span>
                                </div>
                                {plannedSprints.length > 0 && (
                                    <button className={styles.assignSprintBtn}>Sprint'e Ekle</button>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* ── Create Modal ──────── */}
            {showCreateModal && (
                <CreateSprintModal
                    onSubmit={handleCreateSprint}
                    onClose={() => setShowCreateModal(false)}
                />
            )}

            {/* ── Toast ─────────────── */}
            {toast && (
                <div className={`${styles.toast} ${toast.type === 'success' ? styles.toastSuccess : styles.toastError}`}>
                    {toast.message}
                </div>
            )}
        </div>
    );
}

// ═════════════════════════════════════
// Create Sprint Modal
// ═════════════════════════════════════
function CreateSprintModal({
    onSubmit,
    onClose,
}: {
    onSubmit: (name: string, startDate: string, endDate: string) => Promise<void>;
    onClose: () => void;
}) {
    const [name, setName] = useState('');
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');
    const [submitting, setSubmitting] = useState(false);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!name.trim() || !startDate || !endDate) return;
        setSubmitting(true);
        try {
            await onSubmit(name.trim(), startDate, endDate);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className={styles.modalOverlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2 className={styles.modalTitle}>Yeni Sprint Oluştur</h2>
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel}>Sprint Adı</label>
                        <input
                            type="text"
                            className={styles.formInput}
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="Örn: Sprint 1"
                            autoFocus
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel}>Başlangıç Tarihi</label>
                        <input
                            type="date"
                            className={styles.formInput}
                            value={startDate}
                            onChange={(e) => setStartDate(e.target.value)}
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel}>Bitiş Tarihi</label>
                        <input
                            type="date"
                            className={styles.formInput}
                            value={endDate}
                            onChange={(e) => setEndDate(e.target.value)}
                        />
                    </div>
                    <div className={styles.modalFooter}>
                        <button type="button" className={styles.btnSecondary} onClick={onClose}>İptal</button>
                        <button type="submit" className={styles.btnPrimary} disabled={submitting || !name.trim() || !startDate || !endDate}>
                            {submitting ? 'Oluşturuluyor...' : 'Oluştur'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

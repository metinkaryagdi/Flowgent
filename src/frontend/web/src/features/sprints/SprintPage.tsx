import { useState, useEffect, type FormEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { sprintsApi } from '../../api/sprints';
import { useToastStore } from '../../store/toastStore';
import { issuesApi } from '../../api/issues';
import { aiApi, type SprintRiskResult, type SuggestBalanceResult, type RetrospectiveResult } from '../../api/ai';
import { IssuePriority, SprintStatus } from '../../types';
import type { SprintDto, IssueBoardItemDto } from '../../types';
import styles from './Sprint.module.css';

const priorityDot: Record<string, string> = {
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
    const [riskResult, setRiskResult] = useState<SprintRiskResult | null>(null);
    const [riskLoading, setRiskLoading] = useState(false);
    const [balanceResult, setBalanceResult] = useState<SuggestBalanceResult | null>(null);
    const [balanceLoading, setBalanceLoading] = useState(false);
    const [retroResults, setRetroResults] = useState<Record<string, RetrospectiveResult>>({});
    const [retroLoading, setRetroLoading] = useState<Record<string, boolean>>({});
    const [backlog, setBacklog] = useState<{ items: IssueBoardItemDto[]; page: number; total: number; loading: boolean }>({
        items: [],
        page: 1,
        total: 0,
        loading: false,
    });
    const [sprintIssues, setSprintIssues] = useState<Record<string, { items: IssueBoardItemDto[]; page: number; total: number; loading: boolean }>>({});
    const [loading, setLoading] = useState(true);
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [addingIssues, setAddingIssues] = useState<Set<string>>(new Set());
    const pageSize = 20;

    const { addToast: showToast } = useToastStore();

    useEffect(() => {
        loadData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [projectId]);

        const loadBacklog = async (page: number) => {
        if (!projectId) return;
        setBacklog((prev) => ({ ...prev, loading: true }));
        try {
            const result = await issuesApi.getByProjectPaged(projectId, {
                page,
                pageSize,
                backlogOnly: true,
            });
            setBacklog((prev) => ({
                items: page === 1 ? result.items : [...prev.items, ...result.items],
                page: result.page,
                total: result.totalCount,
                loading: false,
            }));
        } catch {
            setBacklog((prev) => ({ ...prev, loading: false }));
        }
    };

    const loadSprintIssues = async (sprintId: string, page: number) => {
        if (!projectId) return;
        setSprintIssues((prev) => ({
            ...prev,
            [sprintId]: {
                items: prev[sprintId]?.items || [],
                page: prev[sprintId]?.page || 1,
                total: prev[sprintId]?.total || 0,
                loading: true,
            }
        }));
        try {
            const result = await issuesApi.getByProjectPaged(projectId, {
                page,
                pageSize,
                sprintId,
            });
            setSprintIssues((prev) => {
                const current = prev[sprintId] || { items: [], page: 1, total: 0, loading: false };
                return {
                    ...prev,
                    [sprintId]: {
                        items: page === 1 ? result.items : [...current.items, ...result.items],
                        page: result.page,
                        total: result.totalCount,
                        loading: false,
                    }
                };
            });
        } catch {
            setSprintIssues((prev) => ({
                ...prev,
                [sprintId]: { ...(prev[sprintId] || { items: [], page: 1, total: 0 }), loading: false },
            }));
        }
    };

    const loadData = async () => {
        if (!projectId) return;
        setLoading(true);
        try {
            const sprintData = await sprintsApi.getByProject(projectId);
            setSprints(sprintData);

            setBacklog({ items: [], page: 1, total: 0, loading: true });
            setSprintIssues({});

            const activeSprint = sprintData.find((s) => s.status === SprintStatus.Active);
            const plannedSprints = sprintData.filter((s) => s.status === SprintStatus.Planned);
            const sprintIdsToLoad = [
                ...(activeSprint ? [activeSprint.id] : []),
                ...plannedSprints.map((s) => s.id),
            ];

            await Promise.all([
                loadBacklog(1),
                ...sprintIdsToLoad.map((id) => loadSprintIssues(id, 1)),
            ]);
        } catch {
            showToast('Veriler yüklenirken hata oluştu.', 'error');
        } finally {
            setLoading(false);
        }
    };

    const activeSprint = sprints.find((s) => s.status === SprintStatus.Active);
    const plannedSprints = sprints.filter((s) => s.status === SprintStatus.Planned);
    const completedSprints = sprints.filter((s) => s.status === SprintStatus.Completed);

    const backlogIssues = backlog.items;

    // â”€â”€ Sprint actions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

    const handleRiskAnalysis = async (sprintId: string) => {
        if (!projectId) return;
        setRiskLoading(true);
        setRiskResult(null);
        try {
            const result = await aiApi.sprintRisk(sprintId, projectId);
            setRiskResult(result);
        } catch {
            showToast('Risk analizi başarısız. Tekrar deneyin.', 'error');
        } finally {
            setRiskLoading(false);
        }
    };

    const handleBalanceAnalysis = async (sprintId: string) => {
        if (!projectId) return;
        setBalanceLoading(true);
        setBalanceResult(null);
        try {
            const result = await aiApi.suggestBalance(sprintId, projectId);
            setBalanceResult(result);
        } catch {
            showToast('Yük analizi başarısız. Tekrar deneyin.', 'error');
        } finally {
            setBalanceLoading(false);
        }
    };

    const handleRetrospective = async (sprintId: string) => {
        if (!projectId) return;
        setRetroLoading((prev) => ({ ...prev, [sprintId]: true }));
        try {
            const result = await aiApi.retrospective(sprintId, projectId);
            setRetroResults((prev) => ({ ...prev, [sprintId]: result }));
        } catch {
            showToast('Retrospektif oluşturulamadı.', 'error');
        } finally {
            setRetroLoading((prev) => ({ ...prev, [sprintId]: false }));
        }
    };

    const handleAddIssueToSprint = async (sprintId: string, issueId: string) => {
        if (addingIssues.has(issueId)) return;
        setAddingIssues((prev) => new Set(prev).add(issueId));
        try {
            await sprintsApi.addIssue(sprintId, issueId);
            showToast('Issue sprint\'e eklendi!');
            await loadData();
        } catch {
            showToast('Issue eklenirken hata oluştu.', 'error');
        } finally {
            setAddingIssues((prev) => { const next = new Set(prev); next.delete(issueId); return next; });
        }
    };

    if (loading) {
        return (
            <div className={styles.sprintPage}>
                <div className={styles.header}>
                    <div className={styles.headerLeft}>
                        <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
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
            {/* â”€â”€ Header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ */}
            <div className={styles.header}>
                <div className={styles.headerLeft}>
                    <button className={styles.backBtn} onClick={() => navigate(`/projects/${projectId}/board`)}>←</button>
                    <h1 className={styles.title}>Sprint & Backlog</h1>
                </div>
                <button className={styles.createBtn} onClick={() => setShowCreateModal(true)}>
                    + Yeni Sprint
                </button>
            </div>

            {/* â”€â”€ Active Sprint â”€â”€â”€â”€â”€â”€â”€â”€ */}
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
                        <button
                            className={styles.startBtn}
                            style={{ background: 'var(--color-error, #ef4444)', color: '#fff', opacity: riskLoading ? 0.6 : 1 }}
                            onClick={() => handleRiskAnalysis(activeSprint.id)}
                            disabled={riskLoading}
                        >
                            {riskLoading ? '...' : '✦ Risk Analizi'}
                        </button>
                        <button
                            className={styles.startBtn}
                            style={{ background: 'var(--color-primary)', color: '#fff', opacity: balanceLoading ? 0.6 : 1 }}
                            onClick={() => handleBalanceAnalysis(activeSprint.id)}
                            disabled={balanceLoading}
                        >
                            {balanceLoading ? '...' : '✦ Yük Analizi'}
                        </button>
                    </div>

                    {/* AI Risk Result */}
                    {riskResult && (
                        <div style={{ background: 'var(--color-surface-raised)', border: `2px solid ${riskResult.riskLevel === 'Low' ? '#22c55e' : riskResult.riskLevel === 'Medium' ? '#f59e0b' : '#ef4444'}`, borderRadius: 'var(--border-radius-md)', padding: '12px 16px', marginTop: 12 }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
                                <span style={{ fontWeight: 700, fontSize: 'var(--font-size-sm)' }}>✦ Risk Analizi</span>
                                <span style={{ padding: '2px 10px', borderRadius: 'var(--border-radius-full)', fontWeight: 700, fontSize: 'var(--font-size-xs)', background: riskResult.riskLevel === 'Low' ? '#22c55e' : riskResult.riskLevel === 'Medium' ? '#f59e0b' : '#ef4444', color: '#fff' }}>
                                    {riskResult.riskLevel}
                                </span>
                                <span style={{ fontSize: 'var(--font-size-xs)', color: 'var(--color-text-muted)' }}>{riskResult.doneIssues}/{riskResult.totalIssues} tamamlandı</span>
                            </div>
                            <p style={{ fontSize: 'var(--font-size-xs)', margin: '0 0 4px' }}><strong>Neden:</strong> {riskResult.reason}</p>
                            <p style={{ fontSize: 'var(--font-size-xs)', margin: 0 }}><strong>Öneri:</strong> {riskResult.recommendation}</p>
                        </div>
                    )}

                    {/* AI Balance Result */}
                    {balanceResult && (
                        <div style={{ background: 'var(--color-surface-raised)', border: '1px solid var(--color-primary)', borderRadius: 'var(--border-radius-md)', padding: '12px 16px', marginTop: 8 }}>
                            <div style={{ fontWeight: 700, fontSize: 'var(--font-size-sm)', marginBottom: 8 }}>✦ Yük Analizi</div>
                            <p style={{ fontSize: 'var(--font-size-xs)', margin: '0 0 4px' }}><strong>Analiz:</strong> {balanceResult.analysis}</p>
                            <p style={{ fontSize: 'var(--font-size-xs)', margin: '0 0 8px' }}><strong>Öneri:</strong> {balanceResult.recommendation}</p>
                            {balanceResult.suggestions.length > 0 && (
                                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                                    {balanceResult.suggestions.map((s, i) => (
                                        <div key={i} style={{ fontSize: '0.72rem', padding: '4px 8px', background: 'var(--color-surface)', borderRadius: 'var(--border-radius-sm)', border: '1px solid var(--color-border)' }}>
                                            <strong>{s.issueTitle}</strong> [{s.currentPriority}] → {s.suggestedAction}
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    )}

                    {/* Active Sprint Issues */}
                    <div className={styles.backlogList} style={{ marginTop: 16 }}>
                        {(sprintIssues[activeSprint.id]?.items || []).map(issue => (
                            <div key={issue.issueId} className={styles.backlogItem}>
                                <div className={styles.backlogItemLeft}>
                                    <span className={`${styles.priorityDot} ${priorityDot[issue.priority] || ''}`} />
                                    <span className={styles.backlogItemTitle}>{issue.title}</span>
                                </div>
                            </div>
                        ))}
                        {sprintIssues[activeSprint.id]?.loading && (
                            <div className={styles.backlogItem} style={{ color: 'var(--text-tertiary)', fontSize: 13, border: 'none' }}>
                                Yükleniyor...
                            </div>
                        )}
                        {(sprintIssues[activeSprint.id]?.items?.length || 0) < (sprintIssues[activeSprint.id]?.total || 0) && (
                            <button
                                className={styles.assignSprintBtn}
                                onClick={() => loadSprintIssues(activeSprint.id, (sprintIssues[activeSprint.id]?.page || 1) + 1)}
                            >
                                Daha Fazla
                            </button>
                        )}
                    </div>
                </div>
            )}

            {/* â”€â”€ Planned Sprints â”€â”€â”€â”€â”€â”€ */}
            {plannedSprints.length > 0 && (
                <>
                    <h3 className={styles.sectionTitle}>Planlanan Sprint'ler</h3>
                    <div className={styles.sprintList}>
                        {plannedSprints.map((sprint) => (
                            <div key={sprint.id} className={styles.sprintCard}>
                                <div className={styles.sprintCardInfo}>
                                    <h3>{sprint.name}</h3>
                                    <p>{formatDate(sprint.startDate)} â€” {formatDate(sprint.endDate)}</p>
                                </div>
                                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                    <span className={`${styles.statusBadge} ${styles.statusPlanned}`}>Planlandı</span>
                                    {!activeSprint && (
                                        <button className={styles.startBtn} onClick={() => handleStartSprint(sprint.id)}>
                                            Başlat
                                        </button>
                                    )}
                                </div>
                                <div className={styles.backlogList} style={{ marginTop: 16 }}>
                                    {(sprintIssues[sprint.id]?.items || []).map(issue => (
                                        <div key={issue.issueId} className={styles.backlogItem}>
                                            <div className={styles.backlogItemLeft}>
                                                <span className={`${styles.priorityDot} ${priorityDot[issue.priority] || ''}`} />
                                                <span className={styles.backlogItemTitle}>{issue.title}</span>
                                            </div>
                                        </div>
                                    ))}
                                    {sprintIssues[sprint.id]?.loading && (
                                        <div className={styles.backlogItem} style={{ color: 'var(--text-tertiary)', fontSize: 13, border: 'none' }}>
                                            Yükleniyor...
                                        </div>
                                    )}
                                    {!sprintIssues[sprint.id]?.loading && (sprintIssues[sprint.id]?.items?.length || 0) === 0 && (
                                        <div className={styles.backlogItem} style={{ color: 'var(--text-tertiary)', fontSize: 13, border: 'none' }}>
                                            Henüz issue eklenmemiş.
                                        </div>
                                    )}
                                    {(sprintIssues[sprint.id]?.items?.length || 0) < (sprintIssues[sprint.id]?.total || 0) && (
                                        <button
                                            className={styles.assignSprintBtn}
                                            onClick={() => loadSprintIssues(sprint.id, (sprintIssues[sprint.id]?.page || 1) + 1)}
                                        >
                                            Daha Fazla
                                        </button>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                </>
            )}

            {/* â”€â”€ Completed Sprints â”€â”€â”€â”€ */}
            {completedSprints.length > 0 && (
                <>
                    <h3 className={styles.sectionTitle}>Tamamlanan Sprint'ler</h3>
                    <div className={styles.sprintList}>
                        {completedSprints.slice(0, 5).map((sprint) => {
                            const retro = retroResults[sprint.id];
                            const retroLoad = retroLoading[sprint.id];
                            return (
                                <div key={sprint.id} className={styles.sprintCard} style={{ flexDirection: 'column', alignItems: 'stretch', gap: 12 }}>
                                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                                        <div className={styles.sprintCardInfo}>
                                            <h3>{sprint.name}</h3>
                                            <p>{formatDate(sprint.startDate)} — {formatDate(sprint.endDate)}</p>
                                        </div>
                                        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                                            <span className={`${styles.statusBadge} ${styles.statusCompleted}`}>Tamamlandı</span>
                                            <button
                                                className={styles.startBtn}
                                                style={{ background: 'var(--color-primary)', color: '#fff', opacity: retroLoad ? 0.6 : 1 }}
                                                onClick={() => handleRetrospective(sprint.id)}
                                                disabled={retroLoad}
                                            >
                                                {retroLoad ? '...' : '✦ Retrospektif'}
                                            </button>
                                        </div>
                                    </div>
                                    {retro && (
                                        <div style={{ background: 'var(--color-surface-raised)', border: '1px solid var(--color-primary)', borderRadius: 'var(--border-radius-md)', padding: '12px 16px', display: 'flex', flexDirection: 'column', gap: 8 }}>
                                            <div style={{ fontWeight: 700, fontSize: 'var(--font-size-sm)' }}>✦ Sprint Retrospektifi</div>
                                            <div style={{ fontSize: 'var(--font-size-xs)' }}><strong>Özet:</strong> {retro.summary}</div>
                                            <div style={{ fontSize: 'var(--font-size-xs)' }}><strong>İyi giden:</strong> {retro.wentWell}</div>
                                            <div style={{ fontSize: 'var(--font-size-xs)' }}><strong>İyileştirilecek:</strong> {retro.improvements}</div>
                                            <div style={{ fontSize: 'var(--font-size-xs)' }}><strong>Aksiyon Maddeleri:</strong> {retro.actionItems}</div>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </>
            )}

            {/* â”€â”€ Backlog â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ */}
            <div className={styles.backlogSection}>
                <h3 className={styles.sectionTitle}>Backlog ({backlog.total || backlogIssues.length})</h3>
                {backlogIssues.length === 0 && !backlog.loading ? (
                    <div className={styles.empty}>
                        <div className={styles.emptyIcon}>📋</div>
                        <p>Backlog'da issue yok.</p>
                    </div>
                ) : (
                    <div className={styles.backlogList}>
                        {backlogIssues.map((issue) => (
                            <div key={issue.issueId} className={styles.backlogItem}>
                                <div className={styles.backlogItemLeft}>
                                    <span className={`${styles.priorityDot} ${priorityDot[issue.priority] || ''}`} />
                                    <span className={styles.backlogItemTitle}>{issue.title}</span>
                                </div>
                                {plannedSprints.length === 1 && (
                                    <button
                                        className={styles.assignSprintBtn}
                                        disabled={addingIssues.has(issue.issueId)}
                                        onClick={() => handleAddIssueToSprint(plannedSprints[0].id, issue.issueId)}
                                    >
                                        {addingIssues.has(issue.issueId) ? 'Ekleniyor...' : "Sprint'e Ekle"}
                                    </button>
                                )}
                                {plannedSprints.length > 1 && (
                                    <select
                                        className={styles.assignSprintBtn}
                                        defaultValue=""
                                        disabled={addingIssues.has(issue.issueId)}
                                        onChange={(e) => {
                                            if (e.target.value) handleAddIssueToSprint(e.target.value, issue.issueId);
                                            e.target.value = '';
                                        }}
                                    >
                                        <option value="" disabled>
                                            {addingIssues.has(issue.issueId) ? 'Ekleniyor...' : "Sprint'e Ekle"}
                                        </option>
                                        {plannedSprints.map((s) => (
                                            <option key={s.id} value={s.id}>{s.name}</option>
                                        ))}
                                    </select>
                                )}
                            </div>
                        ))}
                        {backlog.loading && (
                            <div className={styles.backlogItem} style={{ color: 'var(--text-tertiary)', fontSize: 13, border: 'none' }}>
                                Yükleniyor...
                            </div>
                        )}
                        {backlog.items.length < backlog.total && !backlog.loading && (
                            <button
                                className={styles.assignSprintBtn}
                                onClick={() => loadBacklog(backlog.page + 1)}
                            >
                                Daha Fazla
                            </button>
                        )}
                    </div>
                )}
            </div>

            {/* â”€â”€ Create Modal â”€â”€â”€â”€â”€â”€â”€â”€ */}
            {showCreateModal && (
                <CreateSprintModal
                    onSubmit={handleCreateSprint}
                    onClose={() => setShowCreateModal(false)}
                />
            )}

        </div>
    );
}

// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
// Create Sprint Modal
// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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

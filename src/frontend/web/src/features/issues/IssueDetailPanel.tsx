import { useState, useEffect, useRef, type FormEvent, type ChangeEvent } from 'react';
import { issuesApi } from '../../api/issues';
import { useAuthStore } from '../../store/authStore';
import { IssueStatus, IssuePriority } from '../../types';
import type { IssueDto, IssueCommentDto, IssueAttachmentDto, IssueAuditDto } from '../../types';
import styles from './IssueDetail.module.css';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

// ── Helpers ─────────────────────
const statusLabel: Record<string, string> = {
    [IssueStatus.Open]: 'Açık',
    [IssueStatus.InProgress]: 'Devam Ediyor',
    [IssueStatus.Done]: 'Tamamlandı',
};

const statusClass: Record<string, string> = {
    [IssueStatus.Open]: styles.statusOpen,
    [IssueStatus.InProgress]: styles.statusInProgress,
    [IssueStatus.Done]: styles.statusDone,
};

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

const formatDate = (d: string) =>
    new Date(d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

type Tab = 'details' | 'comments' | 'attachments' | 'history';

// ═════════════════════════════════════
// Issue Detail Panel
// ═════════════════════════════════════
export default function IssueDetailPanel({
    issueId,
    onClose,
    onUpdated,
}: {
    issueId: string;
    onClose: () => void;
    onUpdated?: () => void;
}) {
    const { user, flags } = useAuthStore();
    const [issue, setIssue] = useState<IssueDto | null>(null);
    const [comments, setComments] = useState<IssueCommentDto[]>([]);
    const [attachments, setAttachments] = useState<IssueAttachmentDto[]>([]);
    const [history, setHistory] = useState<IssueAuditDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<Tab>('details');

    // ── Load data ─────────────────
    useEffect(() => {
        loadIssue();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [issueId]);

    const loadIssue = async () => {
        setLoading(true);
        try {
            const data = await issuesApi.getById(issueId);
            setIssue(data);
        } catch {
            // silently fail
        } finally {
            setLoading(false);
        }
    };

    const loadComments = async () => {
        try {
            const data = await issuesApi.getComments(issueId);
            setComments(data);
        } catch { /* ignore */ }
    };

    const loadAttachments = async () => {
        try {
            const data = await issuesApi.getAttachments(issueId);
            setAttachments(data);
        } catch { /* ignore */ }
    };

    const loadHistory = async () => {
        try {
            const data = await issuesApi.getHistory(issueId);
            setHistory(data);
        } catch { /* ignore */ }
    };

    // Load tab-specific data
    useEffect(() => {
        if (activeTab === 'comments') loadComments();
        if (activeTab === 'attachments') loadAttachments();
        if (activeTab === 'history') loadHistory();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [activeTab, issueId]);

    // ── Close on Escape ───────────
    useEffect(() => {
        const handleKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onClose();
        };
        window.addEventListener('keydown', handleKey);
        return () => window.removeEventListener('keydown', handleKey);
    }, [onClose]);

    if (loading) {
        return (
            <>
                <div className={styles.overlay} onClick={onClose} />
                <div className={styles.panel}>
                    <div className={styles.panelLoading}>Yükleniyor...</div>
                </div>
            </>
        );
    }

    if (!issue) {
        return (
            <>
                <div className={styles.overlay} onClick={onClose} />
                <div className={styles.panel}>
                    <div className={styles.panelLoading}>Issue bulunamadı.</div>
                </div>
            </>
        );
    }

    return (
        <>
            <div className={styles.overlay} onClick={onClose} />
            <div className={styles.panel}>
                {/* ── Header ────────────── */}
                <div className={styles.panelHeader}>
                    <div className={styles.panelHeaderLeft}>
                        <h2 className={styles.panelTitle}>{issue.title}</h2>
                        <div className={styles.panelMeta}>
                            <span className={`${styles.statusBadge} ${statusClass[issue.status] || ''}`}>
                                {statusLabel[issue.status] || 'Bilinmiyor'}
                            </span>
                            <span className={`${styles.priorityBadge} ${priorityClass[issue.priority] || ''}`}>
                                {priorityLabel[issue.priority] || '?'}
                            </span>
                        </div>
                    </div>
                    <button className={styles.closeBtn} onClick={onClose} title="Kapat" aria-label="Paneli kapat">✕</button>
                </div>

                {/* ── Tabs ──────────────── */}
                <div className={styles.tabs}>
                    {(['details', 'comments', 'attachments', 'history'] as Tab[]).map((tab) => (
                        <button
                            key={tab}
                            className={`${styles.tab} ${activeTab === tab ? styles.tabActive : ''}`}
                            onClick={() => setActiveTab(tab)}
                        >
                            {tab === 'details' && '📋 Detay'}
                            {tab === 'comments' && `💬 Yorumlar`}
                            {tab === 'attachments' && `📎 Ekler`}
                            {tab === 'history' && '📜 Geçmiş'}
                        </button>
                    ))}
                </div>

                {/* ── Body ──────────────── */}
                <div className={styles.panelBody}>
                    {activeTab === 'details' && (
                        <DetailsTab issue={issue} flags={flags} onUpdated={(updated) => { setIssue(updated); onUpdated?.(); }} />
                    )}
                    {activeTab === 'comments' && (
                        <CommentsTab
                            issueId={issueId}
                            comments={comments}
                            setComments={setComments}
                            userId={user?.id || ''}
                        />
                    )}
                    {activeTab === 'attachments' && (
                        <AttachmentsTab
                            issueId={issueId}
                            attachments={attachments}
                            onReload={loadAttachments}
                        />
                    )}
                    {activeTab === 'history' && (
                        <HistoryTab history={history} />
                    )}
                </div>
            </div>
        </>
    );
}

// ═════════════════════════════════════
// Details Tab
// ═════════════════════════════════════
function DetailsTab({
    issue,
    flags,
    onUpdated,
}: {
    issue: IssueDto;
    flags: { canEditIssues?: boolean; canChangeStatus?: boolean; canAssignIssues?: boolean } | null;
    onUpdated: (updated: IssueDto) => void;
}) {
    const [editing, setEditing] = useState(false);
    const [title, setTitle] = useState(issue.title);
    const [description, setDescription] = useState(issue.description ?? '');
    const [priority, setPriority] = useState<IssuePriority>(issue.priority);
    const [saving, setSaving] = useState(false);
    const [statusChanging, setStatusChanging] = useState(false);
    const [assigneeInput, setAssigneeInput] = useState('');
    const [assigning, setAssigning] = useState(false);
    const [showAssignInput, setShowAssignInput] = useState(false);
    const [errorMsg, setErrorMsg] = useState('');

    const canEdit = flags?.canEditIssues !== false;
    const canChangeStatus = flags?.canChangeStatus !== false;
    const canAssign = flags?.canAssignIssues !== false;

    const handleSave = async () => {
        if (!title.trim()) return;
        setSaving(true);
        setErrorMsg('');
        try {
            const updated = await issuesApi.update(issue.id, {
                title: title.trim(),
                description: description.trim() || undefined,
                priority,
                expectedVersion: issue.version,
            });
            onUpdated(updated);
            setEditing(false);
        } catch {
            setErrorMsg('Kaydetme başarısız. Sayfa yenilenmiş olabilir — tekrar deneyin.');
        } finally {
            setSaving(false);
        }
    };

    const handleCancel = () => {
        setTitle(issue.title);
        setDescription(issue.description ?? '');
        setPriority(issue.priority);
        setEditing(false);
        setErrorMsg('');
    };

    const handleStatusChange = async (e: ChangeEvent<HTMLSelectElement>) => {
        const newStatus = e.target.value as IssueStatus;
        if (newStatus === issue.status) return;
        setStatusChanging(true);
        setErrorMsg('');
        try {
            const updated = await issuesApi.changeStatus(issue.id, {
                newStatus,
                expectedVersion: issue.version,
            });
            onUpdated(updated);
        } catch {
            setErrorMsg('Durum değiştirme başarısız.');
        } finally {
            setStatusChanging(false);
        }
    };

    const handleAssign = async () => {
        const trimmed = assigneeInput.trim();
        if (!trimmed) return;
        setAssigning(true);
        setErrorMsg('');
        try {
            const updated = await issuesApi.assign(issue.id, {
                assigneeUserId: trimmed,
                expectedVersion: issue.version,
            });
            onUpdated(updated);
            setShowAssignInput(false);
            setAssigneeInput('');
        } catch {
            setErrorMsg('Atama başarısız.');
        } finally {
            setAssigning(false);
        }
    };

    return (
        <>
            {/* ── Başlık & Öncelik Edit ── */}
            <div className={styles.section}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
                    <h3 className={styles.sectionTitle} style={{ margin: 0 }}>Detaylar</h3>
                    {canEdit && !editing && (
                        <button className={styles.assignBtn} onClick={() => setEditing(true)}>Düzenle</button>
                    )}
                </div>

                {editing ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                        <input
                            className={styles.editInput}
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="Başlık"
                        />
                        <textarea
                            className={styles.editTextarea}
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="Açıklama (isteğe bağlı)"
                            rows={4}
                        />
                        <select
                            className={styles.editSelect}
                            value={priority}
                            onChange={(e) => setPriority(e.target.value as IssuePriority)}
                        >
                            <option value={IssuePriority.Low}>Düşük</option>
                            <option value={IssuePriority.Medium}>Orta</option>
                            <option value={IssuePriority.High}>Yüksek</option>
                            <option value={IssuePriority.Critical}>Kritik</option>
                        </select>
                        <div style={{ display: 'flex', gap: 8 }}>
                            <button className={styles.assignBtn} onClick={handleSave} disabled={saving || !title.trim()}>
                                {saving ? 'Kaydediliyor...' : 'Kaydet'}
                            </button>
                            <button className={styles.assignBtn} onClick={handleCancel} disabled={saving}
                                style={{ background: 'var(--bg-surface-hover)' }}>
                                İptal
                            </button>
                        </div>
                    </div>
                ) : (
                    <>
                        <div className={styles.section}>
                            <h3 className={styles.sectionTitle}>Açıklama</h3>
                            {issue.description ? (
                                <p className={styles.description}>{issue.description}</p>
                            ) : (
                                <p className={styles.noDescription}>Açıklama eklenmemiş.</p>
                            )}
                        </div>
                    </>
                )}
            </div>

            {/* ── Durum ── */}
            {canChangeStatus && (
                <div className={styles.section}>
                    <h3 className={styles.sectionTitle}>Durum</h3>
                    <select
                        className={styles.editSelect}
                        value={issue.status}
                        onChange={handleStatusChange}
                        disabled={statusChanging}
                    >
                        <option value={IssueStatus.Open}>Açık</option>
                        <option value={IssueStatus.InProgress}>Devam Ediyor</option>
                        <option value={IssueStatus.Done}>Tamamlandı</option>
                    </select>
                    {statusChanging && <span style={{ fontSize: '0.75rem', color: 'var(--text-tertiary)', marginTop: 4 }}>Güncelleniyor...</span>}
                </div>
            )}

            {/* ── Atanan Kişi ── */}
            <div className={styles.section}>
                <h3 className={styles.sectionTitle}>Atanan Kişi</h3>
                <div className={styles.assigneeRow}>
                    {issue.assigneeUserId ? (
                        <>
                            <div className={styles.assigneeAvatar}>
                                {issue.assigneeUserId.slice(0, 2).toUpperCase()}
                            </div>
                            <span style={{ fontSize: 'var(--font-size-sm)', fontWeight: 500 }}>
                                {issue.assigneeUserId}
                            </span>
                            {canAssign && (
                                <button className={styles.assignBtn} onClick={() => setShowAssignInput((v) => !v)}>
                                    Değiştir
                                </button>
                            )}
                        </>
                    ) : (
                        <>
                            <div className={styles.assigneeAvatar} style={{ background: 'var(--bg-surface-hover)', color: 'var(--text-tertiary)' }}>?</div>
                            <span style={{ fontSize: 'var(--font-size-sm)', color: 'var(--text-tertiary)' }}>Atanmamış</span>
                            {canAssign && (
                                <button className={styles.assignBtn} onClick={() => setShowAssignInput((v) => !v)}>Ata</button>
                            )}
                        </>
                    )}
                </div>
                {showAssignInput && (
                    <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
                        <input
                            className={styles.editInput}
                            style={{ flex: 1 }}
                            placeholder="Kullanıcı ID'si"
                            value={assigneeInput}
                            onChange={(e) => setAssigneeInput(e.target.value)}
                        />
                        <button className={styles.assignBtn} onClick={handleAssign} disabled={assigning || !assigneeInput.trim()}>
                            {assigning ? '...' : 'Ata'}
                        </button>
                        <button className={styles.assignBtn} onClick={() => { setShowAssignInput(false); setAssigneeInput(''); }}
                            style={{ background: 'var(--bg-surface-hover)' }}>
                            İptal
                        </button>
                    </div>
                )}
            </div>

            {/* ── Hata mesajı ── */}
            {errorMsg && (
                <p style={{ color: 'var(--color-error, #ef4444)', fontSize: '0.8rem', margin: '0 0 8px' }}>{errorMsg}</p>
            )}

            {/* ── Bilgiler ── */}
            <div className={styles.section}>
                <h3 className={styles.sectionTitle}>Bilgiler</h3>
                <div className={styles.infoGrid}>
                    <div className={styles.infoItem}>
                        <div className={styles.infoLabel}>Oluşturan</div>
                        <div className={styles.infoValue}>{issue.createdByUserId.slice(0, 8)}...</div>
                    </div>
                    <div className={styles.infoItem}>
                        <div className={styles.infoLabel}>Oluşturulma</div>
                        <div className={styles.infoValue}>{formatDate(issue.createdAt)}</div>
                    </div>
                    <div className={styles.infoItem}>
                        <div className={styles.infoLabel}>Sprint</div>
                        <div className={styles.infoValue}>{issue.sprintId ? issue.sprintId.slice(0, 8) + '...' : 'Yok'}</div>
                    </div>
                    <div className={styles.infoItem}>
                        <div className={styles.infoLabel}>Version</div>
                        <div className={styles.infoValue}>v{issue.version}</div>
                    </div>
                </div>
            </div>
        </>
    );
}

// ═════════════════════════════════════
// Comments Tab
// ═════════════════════════════════════
function CommentsTab({
    issueId,
    comments,
    setComments,
    userId,
}: {
    issueId: string;
    comments: IssueCommentDto[];
    setComments: React.Dispatch<React.SetStateAction<IssueCommentDto[]>>;
    userId: string;
}) {
    const [content, setContent] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const inputRef = useRef<HTMLTextAreaElement>(null);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!content.trim() || submitting) return;
        setSubmitting(true);
        try {
            const newComment = await issuesApi.addComment(issueId, {
                authorUserId: userId,
                content: content.trim(),
            });
            setComments((prev) => [...prev, newComment]);
            setContent('');
            inputRef.current?.focus();
        } catch {
            // toast could be added here
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <>
            {comments.length === 0 ? (
                <div className={styles.emptyTab}>
                    <div className={styles.emptyTabIcon}>💬</div>
                    <p className={styles.emptyTabText}>Henüz yorum yok.</p>
                </div>
            ) : (
                <div className={styles.commentList}>
                    {comments.map((c) => (
                        <div key={c.id} className={styles.comment}>
                            <div className={styles.commentHeader}>
                                <span className={styles.commentAuthor}>{c.authorUserId.slice(0, 8)}...</span>
                                <span className={styles.commentDate}>{formatDate(c.createdAt)}</span>
                            </div>
                            <p className={styles.commentContent}>{c.content}</p>
                        </div>
                    ))}
                </div>
            )}

            <form className={styles.commentForm} onSubmit={handleSubmit}>
                <textarea
                    ref={inputRef}
                    className={styles.commentInput}
                    placeholder="Yorum yazın..."
                    value={content}
                    onChange={(e) => setContent(e.target.value)}
                    rows={1}
                />
                <button
                    type="submit"
                    className={styles.commentSubmitBtn}
                    disabled={!content.trim() || submitting}
                >
                    {submitting ? '...' : 'Gönder'}
                </button>
            </form>
        </>
    );
}

// ═════════════════════════════════════
// Attachments Tab
// ═════════════════════════════════════
function AttachmentsTab({
    issueId,
    attachments,
    onReload,
}: {
    issueId: string;
    attachments: IssueAttachmentDto[];
    onReload: () => void;
}) {
    const fileInputRef = useRef<HTMLInputElement>(null);

    const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const formData = new FormData();
        formData.append('file', file);

        try {
            // 1. Upload file to StorageService (temporary)
            const uploadResponse = await fetch(`${API_BASE}/api/v1/storage/files`, {
                method: 'POST',
                credentials: 'include',
                body: formData,
            });
            if (!uploadResponse.ok) throw new Error('Upload failed');
            const stored = await uploadResponse.json();

            // 2. Finalize the file so IssueService can validate it server-side
            const finalizeResponse = await fetch(`${API_BASE}/api/v1/storage/files/${stored.id}/finalize`, {
                method: 'POST',
                credentials: 'include',
            });
            if (!finalizeResponse.ok) throw new Error('Finalize failed');

            // 3. Attach to issue — only fileId is sent; metadata is resolved server-side
            const attachResponse = await fetch(`${API_BASE}/api/v1/issues/${issueId}/attachments`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ fileId: stored.id }),
            });
            if (!attachResponse.ok) throw new Error('Attach failed');

            onReload();
        } catch {
            // error handling
        }

        if (fileInputRef.current) fileInputRef.current.value = '';
    };

    return (
        <>
            {attachments.length === 0 ? (
                <div className={styles.emptyTab}>
                    <div className={styles.emptyTabIcon}>📎</div>
                    <p className={styles.emptyTabText}>Henüz dosya eklenmemiş.</p>
                </div>
            ) : (
                <div className={styles.attachmentList}>
                    {attachments.map((a) => (
                        <div key={a.id} className={styles.attachment}>
                            <div className={styles.attachmentIcon}>📄</div>
                            <div className={styles.attachmentInfo}>
                                <div className={styles.attachmentName}>{a.fileName}</div>
                                <div className={styles.attachmentSize}>{formatSize(a.sizeBytes)}</div>
                            </div>
                            <a
                                href={`${API_BASE}/api/v1/storage/files/${a.fileId}/content`}
                                className={styles.attachmentDownload}
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                İndir
                            </a>
                        </div>
                    ))}
                </div>
            )}

            <input
                ref={fileInputRef}
                type="file"
                style={{ display: 'none' }}
                onChange={handleUpload}
            />
            <button className={styles.uploadBtn} onClick={() => fileInputRef.current?.click()}>
                📁 Dosya Ekle
            </button>
        </>
    );
}

// ═════════════════════════════════════
// History Tab
// ═════════════════════════════════════
function HistoryTab({ history }: { history: IssueAuditDto[] }) {
    if (history.length === 0) {
        return (
            <div className={styles.emptyTab}>
                <div className={styles.emptyTabIcon}>📜</div>
                <p className={styles.emptyTabText}>Henüz geçmiş kaydı yok.</p>
            </div>
        );
    }

    return (
        <div className={styles.timeline}>
            {history.map((entry, i) => (
                <div key={i} className={styles.timelineItem}>
                    <div className={styles.timelineDot} />
                    <div className={styles.timelineText}>
                        <strong>{entry.changedByUserId.slice(0, 8)}...</strong>{' '}
                        durumu{' '}
                        <span className={`${styles.statusBadge} ${statusClass[entry.fromStatus] || ''}`}>
                            {statusLabel[entry.fromStatus]}
                        </span>
                        {' → '}
                        <span className={`${styles.statusBadge} ${statusClass[entry.toStatus] || ''}`}>
                            {statusLabel[entry.toStatus]}
                        </span>
                        {' '}olarak değiştirdi.
                    </div>
                    <div className={styles.timelineDate}>{formatDate(entry.changedAt)}</div>
                </div>
            ))}
        </div>
    );
}

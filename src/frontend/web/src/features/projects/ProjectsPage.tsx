import { useState, useEffect, type FormEvent } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import { projectsApi } from '../../api/projects';
import { useToastStore } from '../../store/toastStore';
import { useAuthStore } from '../../store/authStore';
import type { ProjectDto } from '../../types';
import styles from './Projects.module.css';

export default function ProjectsPage() {
    const { user, flags, activeOrg } = useAuthStore();
    const navigate = useNavigate();

    const [projects, setProjects] = useState<ProjectDto[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [showArchived, setShowArchived] = useState(false);
    const [page, setPage] = useState(1);
    const pageSize = 9;

    // Modal state
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [editProject, setEditProject] = useState<ProjectDto | null>(null);
    const [deleteProject, setDeleteProject] = useState<ProjectDto | null>(null);

    const { addToast: showToast } = useToastStore();

    const getApiErrorMessage = (error: unknown, fallback: string) => {
        if (!axios.isAxiosError(error)) {
            return fallback;
        }

        const data = error.response?.data as
            | { detail?: string; message?: string; title?: string; errors?: Record<string, string[]> }
            | undefined;

        if (data?.detail === 'This project key is already used in the current organization.') {
            return 'Bu proje anahtarı mevcut organizasyonda zaten kullanılıyor.';
        }

        if (data?.detail) return data.detail;
        if (data?.message) return data.message;

        const firstValidationError = data?.errors
            ? Object.values(data.errors).flat()[0]
            : undefined;

        return firstValidationError || fallback;
    };

    // ── Fetch projects ────────────
    useEffect(() => {
        if (!activeOrg?.id && !user?.id) return;
        loadProjects();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [activeOrg?.id, page, searchTerm, showArchived]);

    useEffect(() => {
        setPage(1);
    }, [searchTerm, showArchived]);

    const loadProjects = async () => {
        setLoading(true);
        try {
            const result = await projectsApi.getByOrganizationPaged({
                page,
                pageSize,
                search: searchTerm.trim() || undefined,
                includeArchived: showArchived,
            });
            setProjects(result.items);
            setTotalCount(result.totalCount);
        } catch {
            showToast('Projeler yüklenirken hata oluştu.', 'error');
        } finally {
            setLoading(false);
        }
    };

    // ── Create project ────────────
    const handleCreate = async (name: string, key: string) => {
        try {
            await projectsApi.create({ name, key });
            showToast('Proje başarıyla oluşturuldu!');
            setShowCreateModal(false);
            await loadProjects();
        } catch (error) {
            showToast(getApiErrorMessage(error, 'Proje oluşturulurken hata oluştu.'), 'error');
        }
    };

    // ── Update project ────────────
    const handleUpdate = async (name: string, key: string) => {
        if (!editProject) return;
        try {
            await projectsApi.update(editProject.id, { name, key });
            showToast('Proje güncellendi!');
            setEditProject(null);
            await loadProjects();
        } catch (error) {
            showToast(getApiErrorMessage(error, 'Proje güncellenirken hata oluştu.'), 'error');
        }
    };

    // ── Delete project ────────────
    const handleDelete = async () => {
        if (!deleteProject) return;
        try {
            await projectsApi.delete(deleteProject.id);
            showToast('Proje arşivlendi.');
            setDeleteProject(null);
            await loadProjects();
        } catch {
            showToast('Proje silinirken hata oluştu.', 'error');
        }
    };

    // ── Navigate to board ─────────
    const handleOpenProject = (project: ProjectDto) => {
        navigate(`/projects/${project.id}/board`);
    };

    const formatDate = (dateStr: string) => {
        return new Date(dateStr).toLocaleDateString('tr-TR', {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
        });
    };

    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const pagedProjects = projects;

    useEffect(() => {
        if (page > totalPages) setPage(totalPages);
    }, [page, totalPages]);

    return (
        <div className={styles.projectsPage}>
            {/* ── Header ─────────────── */}
            <div className={styles.header}>
                <div className={styles.headerLeft}>
                    <h1>Projeler</h1>
                    <p>{totalCount} proje</p>
                </div>
                {flags?.canManageProjects !== false && (
                    <button className={styles.createBtn} data-testid="project-create-open" onClick={() => setShowCreateModal(true)}>
                        <span>+</span> Yeni Proje
                    </button>
                )}
            </div>

            <div className={styles.toolbar}>
                <input
                    className={styles.searchInput}
                    type="text"
                    placeholder="Proje ara..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                />
                <label className={styles.toggle}>
                    <input
                        type="checkbox"
                        checked={showArchived}
                        onChange={(e) => setShowArchived(e.target.checked)}
                    />
                    <span>Arşivlenenleri göster</span>
                </label>
            </div>

            {/* ── Loading ────────────── */}
            {loading && (
                <div className={styles.loadingGrid}>
                    {[1, 2, 3].map((i) => (
                        <div key={i} className={styles.skeleton} />
                    ))}
                </div>
            )}

            {/* ── Project Grid ──────── */}
            {!loading && (
                <div className={styles.grid}>
                    {pagedProjects.length === 0 ? (
                        <div className={styles.emptyState}>
                            <div className={styles.emptyIcon}>📁</div>
                            <h2 className={styles.emptyTitle}>
                                {totalCount === 0 ? 'Henüz proje yok' : 'Sonuç bulunamadı'}
                            </h2>
                            <p className={styles.emptyText}>
                                {totalCount === 0
                                    ? 'İlk projenizi oluşturarak başlayın.'
                                    : 'Arama veya filtreleri değiştirin.'}
                            </p>
                            {flags?.canManageProjects !== false && (
                                <button className={styles.createBtn} data-testid="project-create-open" onClick={() => setShowCreateModal(true)}>
                                    <span>+</span> Yeni Proje Oluştur
                                </button>
                            )}
                        </div>
                    ) : (
                        pagedProjects.map((project) => (
                            <div
                                key={project.id}
                                className={styles.card} data-testid="project-card" data-project-id={project.id}
                                onClick={() => handleOpenProject(project)}
                            >
                                <div className={styles.cardHeader}>
                                    <div className={styles.cardIcon}>
                                        {project.key.slice(0, 2)}
                                    </div>
                                    {flags?.canManageProjects !== false && (
                                        <div className={styles.cardActions}>
                                            <button
                                                className={styles.cardActionBtn}
                                                title="Düzenle"
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    setEditProject(project);
                                                }}
                                            >
                                                ✏️
                                            </button>
                                            <button
                                                className={`${styles.cardActionBtn} ${styles.cardActionBtnDanger}`}
                                                title="Arşivle"
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    setDeleteProject(project);
                                                }}
                                            >
                                                🗑️
                                            </button>
                                        </div>
                                    )}
                                </div>

                                <h3 className={styles.cardTitle}>{project.name}</h3>
                                <span className={styles.cardKey}>{project.key}</span>
                                {project.isArchived && (
                                    <span className={styles.archivedBadge}>Arşivli</span>
                                )}

                                <div className={styles.cardStats}>
                                    <div className={styles.stat}>
                                        <span className={`${styles.statDot} ${styles.statDotOpen}`} />
                                        <span className={styles.statValue}>{project.openIssueCount}</span>
                                        <span className={styles.statLabel}>Açık</span>
                                    </div>
                                    <div className={styles.stat}>
                                        <span className={`${styles.statDot} ${styles.statDotInProgress}`} />
                                        <span className={styles.statValue}>{project.inProgressIssueCount}</span>
                                        <span className={styles.statLabel}>Devam</span>
                                    </div>
                                    <div className={styles.stat}>
                                        <span className={`${styles.statDot} ${styles.statDotDone}`} />
                                        <span className={styles.statValue}>{project.doneIssueCount}</span>
                                        <span className={styles.statLabel}>Bitti</span>
                                    </div>
                                </div>

                                <div className={styles.cardDate}>
                                    Oluşturulma: {formatDate(project.createdAt)}
                                </div>
                            </div>
                        ))
                    )}
                </div>
            )}

            {!loading && totalPages > 1 && (
                <div className={styles.pagination}>
                    <button
                        className={styles.pageBtn}
                        onClick={() => setPage((prev) => Math.max(1, prev - 1))}
                        disabled={page === 1}
                    >
                        Önceki
                    </button>
                    <span className={styles.pageInfo}>Sayfa {page} / {totalPages}</span>
                    <button
                        className={styles.pageBtn}
                        onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
                        disabled={page === totalPages}
                    >
                        Sonraki
                    </button>
                </div>
            )}

            {/* ── Create Modal ──────── */}
            {showCreateModal && (
                <ProjectFormModal
                    title="Yeni Proje Oluştur"
                    onSubmit={handleCreate}
                    onClose={() => setShowCreateModal(false)}
                />
            )}

            {/* ── Edit Modal ────────── */}
            {editProject && (
                <ProjectFormModal
                    title="Projeyi Düzenle"
                    initialName={editProject.name}
                    initialKey={editProject.key}
                    onSubmit={handleUpdate}
                    onClose={() => setEditProject(null)}
                />
            )}

            {/* ── Delete Confirm ────── */}
            {deleteProject && (
                <div className={styles.modalOverlay} onClick={() => setDeleteProject(null)}>
                    <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                        <h2 className={styles.modalTitle}>Projeyi Arşivle</h2>
                        <p className={styles.confirmText}>
                            <span className={styles.confirmProjectName}>{deleteProject.name}</span> projesini
                            arşivlemek istediğinize emin misiniz? Bu işlem geri alınamaz.
                        </p>
                        <div className={styles.modalFooter}>
                            <button className={styles.btnSecondary} onClick={() => setDeleteProject(null)}>
                                İptal
                            </button>
                            <button className={styles.btnDanger} onClick={handleDelete}>
                                Arşivle
                            </button>
                        </div>
                    </div>
                </div>
            )}

        </div>
    );
}

/* ═══════════════════════════════
   Project Form Modal
   ═══════════════════════════════ */
function ProjectFormModal({
    title,
    initialName = '',
    initialKey = '',
    onSubmit,
    onClose,
}: {
    title: string;
    initialName?: string;
    initialKey?: string;
    onSubmit: (name: string, key: string) => Promise<void>;
    onClose: () => void;
}) {
    const [name, setName] = useState(initialName);
    const [key, setKey] = useState(initialKey);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState('');

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');

        if (!name.trim()) {
            setError('Proje adı gereklidir.');
            return;
        }
        if (!key.trim()) {
            setError('Proje anahtarı gereklidir.');
            return;
        }
        if (key.length > 6) {
            setError('Anahtar en fazla 6 karakter olabilir.');
            return;
        }

        setSubmitting(true);
        try {
            await onSubmit(name.trim(), key.trim().toUpperCase());
        } catch {
            setError('Bir hata oluştu.');
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className={styles.modalOverlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2 className={styles.modalTitle}>{title}</h2>

                {error && (
                    <div style={{
                        padding: '8px 12px',
                        background: 'var(--color-error-light)',
                        color: 'var(--color-error)',
                        borderRadius: 'var(--border-radius-md)',
                        fontSize: 'var(--font-size-sm)',
                        marginBottom: 16,
                    }}>
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit}>
                    <div style={{ marginBottom: 16 }}>
                        <label
                            htmlFor="projectName"
                            style={{
                                display: 'block',
                                fontSize: 'var(--font-size-sm)',
                                fontWeight: 600,
                                marginBottom: 6,
                            }}
                        >
                            Proje Adı
                        </label>
                        <input
                            id="projectName"
                            data-testid="project-name"
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="Örn: E-Ticaret Platformu"
                            autoFocus
                            style={{
                                width: '100%',
                                padding: '10px 14px',
                                border: '1px solid var(--border-color)',
                                borderRadius: 'var(--border-radius-md)',
                                fontSize: 'var(--font-size-base)',
                                outline: 'none',
                            }}
                        />
                    </div>

                    <div style={{ marginBottom: 8 }}>
                        <label
                            htmlFor="projectKey"
                            style={{
                                display: 'block',
                                fontSize: 'var(--font-size-sm)',
                                fontWeight: 600,
                                marginBottom: 6,
                            }}
                        >
                            Proje Anahtarı
                        </label>
                        <input
                            id="projectKey"
                            data-testid="project-key"
                            type="text"
                            value={key}
                            onChange={(e) => setKey(e.target.value.toUpperCase())}
                            placeholder="Örn: ETP"
                            maxLength={6}
                            style={{
                                width: '100%',
                                padding: '10px 14px',
                                border: '1px solid var(--border-color)',
                                borderRadius: 'var(--border-radius-md)',
                                fontSize: 'var(--font-size-base)',
                                outline: 'none',
                                textTransform: 'uppercase',
                                letterSpacing: '0.05em',
                            }}
                        />
                        <p style={{ fontSize: 'var(--font-size-xs)', color: 'var(--text-tertiary)', marginTop: 4 }}>
                            Kısa anahtar (ör: BP, ETP). Issue'larda kullanılır.
                        </p>
                    </div>

                    <div className={styles.modalFooter}>
                        <button type="button" className={styles.btnSecondary} onClick={onClose}>
                            İptal
                        </button>
                        <button
                            type="submit"
                            className={styles.btnPrimary}
                            disabled={submitting}
                            data-testid="project-submit"
                        >
                            {submitting ? 'Kaydediliyor...' : 'Kaydet'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}


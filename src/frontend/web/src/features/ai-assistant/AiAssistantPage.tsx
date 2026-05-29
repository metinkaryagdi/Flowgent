import { useState, useRef, useEffect, type FormEvent, type KeyboardEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useToastStore } from '../../store/toastStore';
import { useAuthStore } from '../../store/authStore';
import { aiApi, type AgentTurn, type ModelInfo, type ProjectScaffoldDraft } from '../../api/ai';
import { projectsApi } from '../../api/projects';
import { sprintsApi } from '../../api/sprints';
import { issuesApi } from '../../api/issues';
import { IssuePriority } from '../../types';
import type { ProjectDto } from '../../types';
import styles from './AiAssistant.module.css';

type Mode = 'agent' | 'scaffold';

function ModelBadge({ info }: { info: ModelInfo | null }) {
    if (!info) return null;
    const label = info.isFinetuned
        ? `🤖 ${info.finetunedModel || info.active} (fine-tuned)`
        : `🤖 ${info.baseModel || info.active} (base)`;
    return <span className={styles.badge}>{label}</span>;
}

function ModelToggle({ info, onChange }: { info: ModelInfo | null; onChange: (next: ModelInfo) => void }) {
    const { addToast } = useToastStore();
    const [busy, setBusy] = useState(false);
    if (!info) return null;

    const toggle = async () => {
        if (busy) return;
        setBusy(true);
        try {
            const next = await aiApi.setModelMode(!info.isFinetuned);
            onChange(next);
            addToast(next.isFinetuned
                ? `Fine-tune modele geçildi: ${next.finetunedModel}`
                : `Base modele geçildi: ${next.baseModel}`, 'success');
        } catch {
            addToast('Model değiştirilemedi.', 'error');
        } finally {
            setBusy(false);
        }
    };

    return (
        <button
            type="button"
            onClick={toggle}
            disabled={busy}
            title={info.isFinetuned
                ? 'Şu an fine-tune (bp-agent) aktif — base modele dön'
                : 'Şu an base model aktif — fine-tune (bp-agent) modele geç'}
            style={{
                padding: '6px 12px',
                borderRadius: 'var(--border-radius-md)',
                border: '1px solid var(--color-border)',
                background: info.isFinetuned ? 'rgba(124, 58, 237, 0.12)' : 'var(--color-surface)',
                color: info.isFinetuned ? '#7c3aed' : 'var(--color-text-primary)',
                fontSize: 'var(--font-size-xs)',
                fontWeight: 600,
                cursor: busy ? 'wait' : 'pointer',
                opacity: busy ? 0.6 : 1,
            }}>
            {busy ? '…' : (info.isFinetuned ? '↺ Base modele geç' : '✨ Fine-tune modele geç')}
        </button>
    );
}

interface ChatMessage {
    id: string;
    role: 'user' | 'assistant';
    text: string;
    turns?: AgentTurn[];
    iterations?: number;
    hitLimit?: boolean;
    formatUnrecognized?: boolean;
    rawOutput?: string;
    timestamp: string;
}

export default function AiAssistantPage() {
    const [mode, setMode] = useState<Mode>('agent');
    const [modelInfo, setModelInfo] = useState<ModelInfo | null>(null);

    useEffect(() => {
        let cancelled = false;
        aiApi.modelInfo()
            .then((res) => { if (!cancelled) setModelInfo(res); })
            .catch(() => { /* rozet gizli kalır */ });
        return () => { cancelled = true; };
    }, []);

    return (
        <div className={styles.page}>
            <div className={styles.header}>
                <h1 className={styles.title}>
                    <span>✦</span> AI Asistan
                </h1>
                <p className={styles.subtitle}>
                    Mevcut bir projede iş yaptır veya doğal dilde tarif ederek yeni proje oluştur.
                </p>
            </div>

            <div style={{ display: 'flex', gap: 8, borderBottom: '1px solid var(--color-border)', paddingBottom: 0 }}>
                <ModeTab active={mode === 'agent'} onClick={() => setMode('agent')} icon="🛠️" label="Mevcut Projede İşlem" />
                <ModeTab active={mode === 'scaffold'} onClick={() => setMode('scaffold')} icon="🆕" label="Yeni Proje Oluştur" />
            </div>

            {mode === 'agent'
                ? <AgentMode modelInfo={modelInfo} onModelChange={setModelInfo} />
                : <ScaffoldMode modelInfo={modelInfo} onModelChange={setModelInfo} />}
        </div>
    );
}

function ModeTab({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon: string; label: string }) {
    return (
        <button
            onClick={onClick}
            style={{
                padding: '10px 18px',
                background: 'none',
                border: 'none',
                borderBottom: active ? '2px solid var(--color-primary)' : '2px solid transparent',
                color: active ? 'var(--color-primary)' : 'var(--color-text-secondary)',
                fontWeight: 600,
                fontSize: 'var(--font-size-sm)',
                cursor: 'pointer',
                marginBottom: -1,
            }}
        >
            {icon} {label}
        </button>
    );
}

// ─────────────────────────────────────────────────────────────────────
// Mode 1: Agent (mevcut, korundu)
// ─────────────────────────────────────────────────────────────────────
function AgentMode({ modelInfo, onModelChange }: { modelInfo: ModelInfo | null; onModelChange: (next: ModelInfo) => void }) {
    const { addToast } = useToastStore();
    const { user, activeOrg } = useAuthStore();

    const [projects, setProjects] = useState<ProjectDto[]>([]);
    const [projectId, setProjectId] = useState<string>('');
    const [projectsLoading, setProjectsLoading] = useState(false);
    const [sessionId, setSessionId] = useState<string | undefined>(undefined);

    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [thinking, setThinking] = useState(false);
    const chatEndRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLTextAreaElement>(null);

    useEffect(() => {
        chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, thinking]);

    useEffect(() => {
        if (!activeOrg?.id) return;
        setProjectsLoading(true);
        projectsApi
            .getByOrganizationPaged({ page: 1, pageSize: 100 })
            .then((res) => {
                setProjects(res.items);
                if (res.items.length > 0 && !projectId) {
                    setProjectId(res.items[0].id);
                }
            })
            .catch(() => addToast('Projeler yüklenemedi.', 'error'))
            .finally(() => setProjectsLoading(false));
    }, [activeOrg?.id]); // eslint-disable-line react-hooks/exhaustive-deps

    const send = async (e?: FormEvent) => {
        e?.preventDefault();
        const text = input.trim();
        if (!text || thinking) return;
        if (!projectId) {
            addToast('Önce bir proje seç.', 'warning');
            return;
        }

        const userMsg: ChatMessage = { id: `u-${Date.now()}`, role: 'user', text, timestamp: new Date().toISOString() };
        setMessages((prev) => [...prev, userMsg]);
        setInput('');
        setThinking(true);

        try {
            const res = await aiApi.agent(projectId, text, sessionId);
            const modelLabel = modelInfo?.active ?? 'model';
            const displayText = res.formatUnrecognized
                ? '' // bubble yerine açıklama balonu render edilecek
                : res.hitIterationLimit
                    ? `🤖 ${modelLabel} ${res.iterationsUsed} adım çalıştırdı ama özet üretemedi. Tool çağrı sonuçlarını "🔧" panelinden inceleyebilirsin. Daha basit bir istekle tekrar deneyebilirsin.`
                    : (res.finalText || '(boş yanıt)');
            const aiMsg: ChatMessage = {
                id: `a-${Date.now()}`,
                role: 'assistant',
                text: displayText,
                turns: res.turns,
                iterations: res.iterationsUsed,
                hitLimit: res.hitIterationLimit,
                formatUnrecognized: res.formatUnrecognized,
                rawOutput: res.formatUnrecognized ? res.finalText : undefined,
                timestamp: new Date().toISOString(),
            };
            setMessages((prev) => [...prev, aiMsg]);
        } catch {
            addToast('AI yanıtı alınamadı.', 'error');
            setMessages((prev) => [...prev, {
                id: `e-${Date.now()}`, role: 'assistant',
                text: 'Üzgünüm, şu an yanıt veremiyorum. Lütfen tekrar deneyin.',
                timestamp: new Date().toISOString(),
            }]);
        } finally {
            setThinking(false);
            inputRef.current?.focus();
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            void send();
        }
    };

    const handleNewSession = () => {
        setSessionId(undefined);
        setMessages([]);
        inputRef.current?.focus();
    };

    return (
        <>
            <div className={styles.badgeRow}>
                {activeOrg?.name && <span className={styles.badge}>🏢 {activeOrg.name}</span>}
                {user?.userName && <span className={styles.badge}>👤 {user.userName}</span>}
                <ModelBadge info={modelInfo} />
                <ModelToggle info={modelInfo} onChange={onModelChange} />
            </div>

            <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
                <label style={{ fontSize: 'var(--font-size-sm)', color: 'var(--color-text-secondary)', fontWeight: 600 }}>
                    Proje:
                </label>
                <select
                    value={projectId}
                    onChange={(e) => { setProjectId(e.target.value); handleNewSession(); }}
                    disabled={projectsLoading || thinking}
                    style={{
                        padding: '8px 12px', borderRadius: 'var(--border-radius-md)',
                        border: '1px solid var(--color-border)', background: 'var(--color-surface)',
                        color: 'var(--color-text-primary)', fontSize: 'var(--font-size-sm)', minWidth: 220,
                    }}
                >
                    {projects.length === 0 && <option value="">— Proje yok —</option>}
                    {projects.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.key})</option>)}
                </select>
                {messages.length > 0 && (
                    <button type="button" onClick={handleNewSession}
                        style={{
                            padding: '6px 14px', borderRadius: 'var(--border-radius-md)',
                            border: '1px solid var(--color-border)', background: 'var(--color-surface)',
                            color: 'var(--color-text-primary)', fontSize: 'var(--font-size-xs)', cursor: 'pointer',
                        }}>
                        🗑️ Yeni Sohbet
                    </button>
                )}
            </div>

            <div className={styles.chatPanel}>
                <div className={styles.chatBody}>
                    {messages.length === 0 && !thinking && (
                        <div className={styles.empty}>
                            <div className={styles.emptyIcon}>✦</div>
                            <p className={styles.emptyTitle}>Mevcut projede iş yap</p>
                            <p className={styles.emptyHint}>
                                Örn: "Login bug'ı için yüksek öncelikli issue oluştur" veya "Aktif sprint'te kaç açık issue var?"
                            </p>
                        </div>
                    )}
                    {messages.map((msg) => (
                        <div key={msg.id} className={`${styles.message} ${msg.role === 'user' ? styles.messageUser : styles.messageAi}`}>
                            <div className={`${styles.avatar} ${msg.role === 'user' ? styles.avatarUser : styles.avatarAi}`}>
                                {msg.role === 'user' ? (user?.userName?.slice(0, 2).toUpperCase() ?? '??') : '✦'}
                            </div>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, flex: 1 }}>
                                {msg.role === 'assistant' && msg.formatUnrecognized
                                    ? <FormatFailureBalloon raw={msg.rawOutput ?? ''} isFinetuned={modelInfo?.isFinetuned ?? false} onSwitchToBase={async () => {
                                        try {
                                            const next = await aiApi.setModelMode(false);
                                            onModelChange(next);
                                            addToast(`Base modele geçildi: ${next.baseModel}`, 'success');
                                        } catch {
                                            addToast('Model değiştirilemedi.', 'error');
                                        }
                                    }} />
                                    : <div className={`${styles.bubble} ${msg.role === 'user' ? styles.bubbleUser : styles.bubbleAi}`}>{msg.text}</div>
                                }
                                {msg.role === 'assistant' && msg.turns && msg.turns.length > 0 && (
                                    <TurnsDetail turns={msg.turns} iterations={msg.iterations ?? 0} hitLimit={msg.hitLimit ?? false} />
                                )}
                            </div>
                        </div>
                    ))}
                    {thinking && (
                        <div className={`${styles.message} ${styles.messageAi}`}>
                            <div className={`${styles.avatar} ${styles.avatarAi}`}>✦</div>
                            <div className={`${styles.bubble} ${styles.bubbleAi}`}>
                                <span className={styles.thinking}>AI düşünüyor — tool çağrıları sürebilir...</span>
                            </div>
                        </div>
                    )}
                    <div ref={chatEndRef} />
                </div>
                <form className={styles.composer} onSubmit={send}>
                    <textarea ref={inputRef} className={styles.composerInput}
                        placeholder={projectId ? 'AI\'ya bir şey söyle... (Enter = gönder, Shift+Enter = yeni satır)' : 'Önce bir proje seç...'}
                        value={input} onChange={(e) => setInput(e.target.value)} onKeyDown={handleKeyDown}
                        disabled={thinking || !projectId} rows={1} />
                    <button type="submit" className={styles.sendBtn} disabled={!input.trim() || thinking || !projectId}>
                        {thinking ? '...' : 'Gönder'}
                    </button>
                </form>
            </div>
        </>
    );
}

function FormatFailureBalloon({ raw, isFinetuned, onSwitchToBase }: { raw: string; isFinetuned: boolean; onSwitchToBase: () => void | Promise<void> }) {
    const [showRaw, setShowRaw] = useState(false);
    return (
        <div style={{
            background: 'rgba(245, 158, 11, 0.08)',
            border: '1px solid rgba(245, 158, 11, 0.35)',
            borderRadius: 'var(--border-radius-md)',
            padding: '12px 14px',
            display: 'flex',
            flexDirection: 'column',
            gap: 10,
        }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ fontSize: 18 }}>⚠️</span>
                <strong style={{ color: '#b45309', fontSize: 'var(--font-size-sm)' }}>
                    {isFinetuned ? 'Fine-tune model geçerli format üretemedi' : 'Model geçerli format üretemedi'}
                </strong>
            </div>
            <div style={{ fontSize: 'var(--font-size-xs)', color: 'var(--color-text-secondary)', lineHeight: 1.55 }}>
                <p style={{ margin: '0 0 6px 0' }}>
                    <strong>Sebep:</strong> Model
                    <code style={{ margin: '0 4px', padding: '1px 5px', background: 'var(--color-surface)', borderRadius: 4 }}>tool_calls</code>
                    /
                    <code style={{ margin: '0 4px', padding: '1px 5px', background: 'var(--color-surface)', borderRadius: 4 }}>final</code>
                    şeması yerine farklı bir JSON üretti. AgentLoop bunu parse edemediği için ham çıktı geri döndü.
                </p>
                <p style={{ margin: 0 }}>
                    <strong>Çözüm yolu:</strong> {isFinetuned
                        ? 'Base modele geçip aynı isteği tekrar dene; fine-tune model bu prompt için kararlı format üretmiyor.'
                        : 'İsteği daha basit veya net bir cümleyle tekrar yaz; alternatif olarak fine-tune modele geçip dene.'}
                </p>
            </div>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                {isFinetuned && (
                    <button
                        type="button"
                        onClick={() => { void onSwitchToBase(); }}
                        style={{
                            padding: '6px 12px',
                            borderRadius: 'var(--border-radius-md)',
                            background: 'var(--color-primary)',
                            color: 'white',
                            border: 'none',
                            fontSize: 'var(--font-size-xs)',
                            fontWeight: 600,
                            cursor: 'pointer',
                        }}>
                        ↺ Base modele geç
                    </button>
                )}
                <button
                    type="button"
                    onClick={() => setShowRaw((v) => !v)}
                    style={{
                        padding: '6px 12px',
                        borderRadius: 'var(--border-radius-md)',
                        background: 'var(--color-surface)',
                        color: 'var(--color-text-primary)',
                        border: '1px solid var(--color-border)',
                        fontSize: 'var(--font-size-xs)',
                        fontWeight: 600,
                        cursor: 'pointer',
                    }}>
                    {showRaw ? '▼ Ham çıktıyı gizle' : '▶ Ham çıktıyı göster'}
                </button>
            </div>
            {showRaw && (
                <pre style={{
                    margin: 0,
                    padding: '8px 10px',
                    background: 'var(--color-surface)',
                    border: '1px solid var(--color-border)',
                    borderRadius: 'var(--border-radius-sm)',
                    fontSize: 'var(--font-size-xs)',
                    fontFamily: 'var(--font-mono, monospace)',
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word',
                    maxHeight: 260,
                    overflow: 'auto',
                }}>{raw}</pre>
            )}
        </div>
    );
}

function TurnsDetail({ turns, iterations, hitLimit }: { turns: AgentTurn[]; iterations: number; hitLimit: boolean }) {
    const [open, setOpen] = useState(false);
    const toolTurns = turns.filter((t) => t.kind === 'tool');
    return (
        <div style={{
            background: 'var(--color-surface-raised)', border: '1px solid var(--color-border)',
            borderRadius: 'var(--border-radius-md)', padding: '8px 12px', fontSize: 'var(--font-size-xs)',
        }}>
            <button onClick={() => setOpen((v) => !v)}
                style={{
                    background: 'none', border: 'none', color: 'var(--color-text-secondary)', cursor: 'pointer',
                    fontSize: 'var(--font-size-xs)', fontWeight: 600, padding: 0,
                    display: 'flex', alignItems: 'center', gap: 6,
                }}>
                <span>{open ? '▼' : '▶'}</span>
                <span>🔧 {iterations} adım, {toolTurns.length} tool çağrısı{hitLimit ? ' — model "final" üretemedi' : ''}</span>
            </button>
            {open && (
                <div style={{ marginTop: 8, display: 'flex', flexDirection: 'column', gap: 6 }}>
                    {turns.map((t, i) => (
                        <div key={i} style={{
                            padding: '6px 8px', background: 'var(--color-surface)',
                            borderRadius: 'var(--border-radius-sm)', border: '1px solid var(--color-border)',
                            fontFamily: 'var(--font-mono, monospace)', whiteSpace: 'pre-wrap', wordBreak: 'break-word',
                        }}>
                            <div style={{ fontWeight: 700, marginBottom: 2, color: 'var(--color-primary)' }}>{t.kind}</div>
                            <div style={{ color: 'var(--color-text-secondary)' }}>{t.content.length > 400 ? t.content.slice(0, 400) + '…' : t.content}</div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

// ─────────────────────────────────────────────────────────────────────
// Mode 2: Scaffold (yeni proje oluştur)
// ─────────────────────────────────────────────────────────────────────
function ScaffoldMode({ modelInfo, onModelChange }: { modelInfo: ModelInfo | null; onModelChange: (next: ModelInfo) => void }) {
    const { addToast } = useToastStore();
    const { user, activeOrg } = useAuthStore();
    const navigate = useNavigate();

    const [description, setDescription] = useState('');
    const [draftLoading, setDraftLoading] = useState(false);
    const [draft, setDraft] = useState<ProjectScaffoldDraft | null>(null);
    const [creating, setCreating] = useState(false);
    const [progress, setProgress] = useState<string>('');

    const addIssueWithRetry = async (sprintId: string, issueId: string, attempt = 0): Promise<void> => {
        try {
            await sprintsApi.addIssue(sprintId, issueId);
        } catch (err: unknown) {
            const status = (err as { response?: { status?: number } })?.response?.status;
            if (status === 409 && attempt < 3) {
                await new Promise((resolve) => setTimeout(resolve, 800 * (attempt + 1)));
                return addIssueWithRetry(sprintId, issueId, attempt + 1);
            }
            throw err;
        }
    };

    const handleGenerate = async () => {
        if (!description.trim() || draftLoading) return;
        setDraftLoading(true);
        setDraft(null);
        try {
            const res = await aiApi.scaffoldDraft(description.trim());
            setDraft(res);
        } catch {
            addToast('AI taslağı üretemedi. Açıklamayı yeniden ifade edip tekrar dene.', 'error');
        } finally {
            setDraftLoading(false);
        }
    };

    const handleApprove = async () => {
        if (!draft || creating) return;
        setCreating(true);
        try {
            // 1) Proje oluştur
            setProgress('Proje oluşturuluyor...');
            const project = await projectsApi.create({
                name: draft.projectName,
                key: draft.projectKey,
                ownerUserId: user?.id,
            });

            // 2) Sprint'leri ve issue'leri sıralı oluştur (her sprint 14 gün)
            const today = new Date();
            for (let s = 0; s < draft.sprints.length; s++) {
                const sprintDraft = draft.sprints[s];
                setProgress(`Sprint ${s + 1}/${draft.sprints.length} oluşturuluyor: ${sprintDraft.name}`);

                const startDate = new Date(today);
                startDate.setDate(today.getDate() + s * 14);
                const endDate = new Date(startDate);
                endDate.setDate(startDate.getDate() + 13);

                const sprint = await sprintsApi.create({
                    projectId: project.id,
                    name: sprintDraft.name,
                    goal: sprintDraft.goal,
                    startDate: startDate.toISOString().slice(0, 10),
                    endDate: endDate.toISOString().slice(0, 10),
                });

                for (let i = 0; i < sprintDraft.issues.length; i++) {
                    const issueDraft = sprintDraft.issues[i];
                    setProgress(`Sprint ${s + 1}: issue ${i + 1}/${sprintDraft.issues.length} — ${issueDraft.title}`);
                    const issue = await issuesApi.create({
                        projectId: project.id,
                        title: issueDraft.title,
                        description: issueDraft.description,
                        priority: issueDraft.priority as IssuePriority,
                    });
                    await addIssueWithRetry(sprint.id, issue.id);
                }
            }

            const totalIssues = draft.sprints.reduce((acc, s) => acc + s.issues.length, 0);
            addToast(`✓ "${draft.projectName}" oluşturuldu — ${draft.sprints.length} sprint, ${totalIssues} issue.`);
            navigate(`/projects/${project.id}`);
        } catch (err) {
            const msg = err instanceof Error ? err.message : 'Oluşturma başarısız.';
            addToast(`Hata: ${msg}. Bazı kayıtlar oluşmuş olabilir.`, 'error');
        } finally {
            setCreating(false);
            setProgress('');
        }
    };

    const handleReject = () => {
        setDraft(null);
        setDescription((prev) => prev + '\n\nDeğişiklik isteği: ');
    };

    const totalIssues = draft?.sprints.reduce((acc, s) => acc + s.issues.length, 0) ?? 0;

    return (
        <>
            <div className={styles.badgeRow}>
                {activeOrg?.name && <span className={styles.badge}>🏢 {activeOrg.name}</span>}
                {user?.userName && <span className={styles.badge}>👤 {user.userName}</span>}
                <ModelBadge info={modelInfo} />
                <ModelToggle info={modelInfo} onChange={onModelChange} />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                <label style={{ fontSize: 'var(--font-size-sm)', fontWeight: 600 }}>
                    Yeni proje için doğal dilde tarif et:
                </label>
                <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="Örn: E-ticaret platformu kurmak istiyoruz, .NET 9 + PostgreSQL + React. Sepet, ödeme, kullanıcı yönetimi, ürün katalog ve admin paneli olsun."
                    rows={5}
                    disabled={draftLoading || creating || !!draft}
                    style={{
                        padding: '12px', borderRadius: 'var(--border-radius-md)',
                        border: '1px solid var(--color-border)', background: 'var(--color-surface)',
                        color: 'var(--color-text-primary)', fontSize: 'var(--font-size-sm)',
                        fontFamily: 'inherit', resize: 'vertical', lineHeight: 1.5,
                    }}
                />
                {!draft && (
                    <button
                        onClick={handleGenerate}
                        disabled={!description.trim() || draftLoading}
                        style={{
                            padding: '10px 20px', borderRadius: 'var(--border-radius-md)',
                            background: 'var(--color-primary)', color: 'white', border: 'none',
                            fontWeight: 600, fontSize: 'var(--font-size-sm)', cursor: 'pointer',
                            opacity: (!description.trim() || draftLoading) ? 0.5 : 1, alignSelf: 'flex-start',
                        }}>
                        {draftLoading ? '✦ AI taslak hazırlıyor (10-30s)...' : '✦ Taslak Oluştur'}
                    </button>
                )}
            </div>

            {draft && (
                <div className={styles.draftCard}>
                    <div>
                        <h3 className={styles.draftTitle}>
                            📋 {draft.projectName} <span className={styles.draftMeta}>({draft.projectKey})</span>
                        </h3>
                        <div className={styles.draftMeta}>
                            {draft.sprints.length} sprint · {totalIssues} issue
                        </div>
                    </div>
                    {draft.description && (
                        <div className={styles.draftMeta} style={{ lineHeight: 1.5 }}>{draft.description}</div>
                    )}
                    {draft.sprints.map((sprint, i) => (
                        <div key={i} className={styles.sprintBlock}>
                            <div className={styles.sprintName}>Sprint {i + 1}: {sprint.name}</div>
                            <div className={styles.sprintGoal}>{sprint.goal}</div>
                            <div className={styles.issueList}>
                                {sprint.issues.map((issue, j) => (
                                    <div key={j} className={styles.issueItem}>
                                        <span className={styles.issuePriority} style={priorityStyle(issue.priority)}>
                                            {issue.priority}
                                        </span>
                                        <span>{issue.title}</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}

                    {progress && (
                        <div style={{
                            padding: '8px 12px', background: 'var(--color-surface)',
                            borderRadius: 'var(--border-radius-sm)', fontSize: 'var(--font-size-xs)',
                            color: 'var(--color-text-secondary)', fontStyle: 'italic',
                        }}>
                            {progress}
                        </div>
                    )}

                    <div className={styles.actions}>
                        <button className={styles.btnPrimary} onClick={handleApprove} disabled={creating}>
                            {creating ? 'Oluşturuluyor...' : '✓ Onayla ve Oluştur'}
                        </button>
                        <button className={styles.btnSecondary} onClick={handleReject} disabled={creating}>
                            Değişiklik İste
                        </button>
                    </div>
                </div>
            )}
        </>
    );
}

function priorityStyle(p: string): React.CSSProperties {
    switch (p) {
        case 'Critical': return { background: 'rgba(124, 58, 237, 0.15)', color: '#7c3aed' };
        case 'High': return { background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444' };
        case 'Medium': return { background: 'rgba(245, 158, 11, 0.15)', color: '#b45309' };
        default: return { background: 'rgba(34, 197, 94, 0.15)', color: '#16a34a' };
    }
}

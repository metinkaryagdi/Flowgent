import { useState, useRef, useEffect, type FormEvent, type KeyboardEvent } from 'react';
import { useToastStore } from '../../store/toastStore';
import { useAuthStore } from '../../store/authStore';
import styles from './AiAssistant.module.css';

interface ChatMessage {
    id: string;
    role: 'user' | 'assistant';
    text: string;
    draft?: ProjectDraft;
    timestamp: string;
}

interface DraftIssue {
    title: string;
    priority: 'Low' | 'Medium' | 'High' | 'Critical';
}

interface DraftSprint {
    name: string;
    goal: string;
    issues: DraftIssue[];
}

interface ProjectDraft {
    projectName: string;
    projectKey: string;
    description: string;
    sprints: DraftSprint[];
}

const priorityClass = (p: string) => {
    switch (p) {
        case 'Critical': return { background: 'rgba(124, 58, 237, 0.15)', color: '#7c3aed' };
        case 'High': return { background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444' };
        case 'Medium': return { background: 'rgba(245, 158, 11, 0.15)', color: '#b45309' };
        default: return { background: 'rgba(34, 197, 94, 0.15)', color: '#16a34a' };
    }
};

export default function AiAssistantPage() {
    const { addToast } = useToastStore();
    const { user, activeOrg } = useAuthStore();

    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [thinking, setThinking] = useState(false);
    const [creating, setCreating] = useState(false);
    const chatEndRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLTextAreaElement>(null);

    useEffect(() => {
        chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, thinking]);

    const send = async (e?: FormEvent) => {
        e?.preventDefault();
        const text = input.trim();
        if (!text || thinking) return;

        const userMsg: ChatMessage = {
            id: `u-${Date.now()}`,
            role: 'user',
            text,
            timestamp: new Date().toISOString(),
        };
        setMessages((prev) => [...prev, userMsg]);
        setInput('');
        setThinking(true);

        try {
            // TODO(AI integration): replace this stub with a real call to /api/v1/ai/scaffold-project.
            // The endpoint should accept the user's free-text description and return a ProjectDraft.
            await new Promise((r) => setTimeout(r, 800));

            const draft: ProjectDraft = mockDraftFor(text);
            const aiMsg: ChatMessage = {
                id: `a-${Date.now()}`,
                role: 'assistant',
                text: 'Anlattıklarına göre aşağıdaki proje yapısını öneriyorum. Onaylarsan proje, sprint\'ler ve issue\'lar otomatik oluşturulur. Detayları değiştirmek istersen mesajla söyle, taslağı güncellerim.',
                draft,
                timestamp: new Date().toISOString(),
            };
            setMessages((prev) => [...prev, aiMsg]);
        } catch {
            addToast('AI yanıtı alınamadı.', 'error');
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

    const handleApprove = async (draft: ProjectDraft) => {
        if (creating) return;
        setCreating(true);
        try {
            // TODO(AI integration): call /api/v1/ai/scaffold-project/apply with the approved draft.
            // Backend should create the project, sprints, and issues atomically and return the project id.
            await new Promise((r) => setTimeout(r, 1000));
            addToast('Bu özellik henüz aktif değil — backend entegrasyonu yakında.', 'warning');
        } finally {
            setCreating(false);
        }
    };

    const handleReject = (msgId: string) => {
        setMessages((prev) => prev.map((m) => (m.id === msgId ? { ...m, draft: undefined } : m)));
        setInput('Şu noktayı değiştirmek istiyorum: ');
        inputRef.current?.focus();
    };

    return (
        <div className={styles.page}>
            <div className={styles.header}>
                <h1 className={styles.title}>
                    <span>✦</span> AI Proje Asistanı
                </h1>
                <p className={styles.subtitle}>
                    Hedeflediğin projeyi anlat — AI senin için proje, sprint ve issue taslağı oluştursun.
                </p>
                <div className={styles.badgeRow}>
                    {activeOrg?.name && <span className={styles.badge}>🏢 {activeOrg.name}</span>}
                    {user?.userName && <span className={styles.badge}>👤 {user.userName}</span>}
                    <span className={styles.badge}>🤖 gemma3:4b</span>
                </div>
            </div>

            <div className={styles.warning}>
                ⚠ Bu özellik şu an UI iskeleti olarak hazır. AI entegrasyonu ve gerçek proje oluşturma yakında devreye girecek.
            </div>

            <div className={styles.chatPanel}>
                <div className={styles.chatBody}>
                    {messages.length === 0 && !thinking && (
                        <div className={styles.empty}>
                            <div className={styles.emptyIcon}>✦</div>
                            <p className={styles.emptyTitle}>Yeni bir proje başlatalım</p>
                            <p className={styles.emptyHint}>
                                "E-ticaret backend'i kurmak istiyoruz, .NET 9 + PostgreSQL, sepet, ödeme,
                                kullanıcı yönetimi modülleri olsun" gibi serbest metinle anlat. AI sana
                                önerilen proje yapısını çıkarır.
                            </p>
                        </div>
                    )}

                    {messages.map((msg) => (
                        <div
                            key={msg.id}
                            className={`${styles.message} ${msg.role === 'user' ? styles.messageUser : styles.messageAi}`}
                        >
                            <div
                                className={`${styles.avatar} ${msg.role === 'user' ? styles.avatarUser : styles.avatarAi}`}
                            >
                                {msg.role === 'user' ? (user?.userName?.slice(0, 2).toUpperCase() ?? '??') : '✦'}
                            </div>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, flex: 1 }}>
                                <div
                                    className={`${styles.bubble} ${msg.role === 'user' ? styles.bubbleUser : styles.bubbleAi}`}
                                >
                                    {msg.text}
                                </div>
                                {msg.draft && (
                                    <DraftPreview
                                        draft={msg.draft}
                                        onApprove={() => handleApprove(msg.draft!)}
                                        onReject={() => handleReject(msg.id)}
                                        creating={creating}
                                    />
                                )}
                            </div>
                        </div>
                    ))}

                    {thinking && (
                        <div className={`${styles.message} ${styles.messageAi}`}>
                            <div className={`${styles.avatar} ${styles.avatarAi}`}>✦</div>
                            <div className={`${styles.bubble} ${styles.bubbleAi}`}>
                                <span className={styles.thinking}>düşünüyorum...</span>
                            </div>
                        </div>
                    )}

                    <div ref={chatEndRef} />
                </div>

                <form className={styles.composer} onSubmit={send}>
                    <textarea
                        ref={inputRef}
                        className={styles.composerInput}
                        placeholder="Projeyi tarif et... (Enter = gönder, Shift+Enter = yeni satır)"
                        value={input}
                        onChange={(e) => setInput(e.target.value)}
                        onKeyDown={handleKeyDown}
                        disabled={thinking}
                        rows={1}
                    />
                    <button
                        type="submit"
                        className={styles.sendBtn}
                        disabled={!input.trim() || thinking}
                    >
                        {thinking ? '...' : 'Gönder'}
                    </button>
                </form>
            </div>
        </div>
    );
}

function DraftPreview({
    draft,
    onApprove,
    onReject,
    creating,
}: {
    draft: ProjectDraft;
    onApprove: () => void;
    onReject: () => void;
    creating: boolean;
}) {
    const totalIssues = draft.sprints.reduce((acc, s) => acc + s.issues.length, 0);

    return (
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
                                <span className={styles.issuePriority} style={priorityClass(issue.priority)}>
                                    {issue.priority}
                                </span>
                                <span>{issue.title}</span>
                            </div>
                        ))}
                    </div>
                </div>
            ))}

            <div className={styles.actions}>
                <button className={styles.btnPrimary} onClick={onApprove} disabled={creating}>
                    {creating ? 'Oluşturuluyor...' : '✓ Onayla ve Oluştur'}
                </button>
                <button className={styles.btnSecondary} onClick={onReject} disabled={creating}>
                    Değişiklik İste
                </button>
            </div>
        </div>
    );
}

// ─────────────────────────────────────────────────────────────────────
// TODO: Remove once /api/v1/ai/scaffold-project is wired.
// Generates a deterministic placeholder draft so the UI flow is testable.
function mockDraftFor(description: string): ProjectDraft {
    const words = description.split(/\s+/).filter(Boolean);
    const guessName = words.slice(0, 3).join(' ') || 'Yeni Proje';
    const key = (words[0] ?? 'NP').slice(0, 4).toUpperCase();

    return {
        projectName: guessName.charAt(0).toUpperCase() + guessName.slice(1),
        projectKey: key,
        description: `AI tarafından öneri olarak hazırlanmıştır. Asıl içerik: "${description.slice(0, 140)}${description.length > 140 ? '…' : ''}"`,
        sprints: [
            {
                name: 'Temel Altyapı',
                goal: 'Proje iskeletini ve temel servisleri kurmak',
                issues: [
                    { title: 'Repository ve CI/CD pipeline kurulumu', priority: 'High' },
                    { title: 'Veritabanı şeması ve migration', priority: 'High' },
                    { title: 'Authentication / Authorization yapısı', priority: 'Critical' },
                ],
            },
            {
                name: 'Çekirdek Özellikler',
                goal: 'Ana iş akışlarını implement etmek',
                issues: [
                    { title: 'Domain modelleri ve repository\'ler', priority: 'High' },
                    { title: 'API endpoint\'leri ve validation', priority: 'Medium' },
                    { title: 'Birim test kapsamı', priority: 'Medium' },
                ],
            },
            {
                name: 'Cilalama ve Hazırlık',
                goal: 'Üretim ortamına hazırlık',
                issues: [
                    { title: 'Logging ve monitoring entegrasyonu', priority: 'Medium' },
                    { title: 'Performans testi', priority: 'Low' },
                    { title: 'Dokümantasyon', priority: 'Low' },
                ],
            },
        ],
    };
}

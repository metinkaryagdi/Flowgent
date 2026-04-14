import { useState, useRef, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { aiApi, type GeneratePlanResult, type ChatResponse } from '../../api/ai';
import { useToastStore } from '../../store/toastStore';
import styles from './AiPlanner.module.css';

type Tab = 'plan' | 'chat';

interface ChatMessage {
    role: 'user' | 'assistant';
    text: string;
    timestamp: string;
}

const priorityColor: Record<string, string> = {
    Low: '#22c55e',
    Medium: '#f59e0b',
    High: '#ef4444',
    Critical: '#7c3aed',
};

export default function AiPlannerPage() {
    const { projectId } = useParams<{ projectId: string }>();
    const { addToast } = useToastStore();
    const [tab, setTab] = useState<Tab>('plan');

    // Plan tab state
    const [description, setDescription] = useState('');
    const [planLoading, setPlanLoading] = useState(false);
    const [planResult, setPlanResult] = useState<GeneratePlanResult | null>(null);

    // Chat tab state
    const [chatMessages, setChatMessages] = useState<ChatMessage[]>([]);
    const [chatInput, setChatInput] = useState('');
    const [chatLoading, setChatLoading] = useState(false);
    const [chatSessionId, setChatSessionId] = useState<string | undefined>();
    const chatEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [chatMessages]);

    const handleGeneratePlan = async () => {
        if (!projectId || !description.trim()) return;
        setPlanLoading(true);
        setPlanResult(null);
        try {
            const result = await aiApi.generatePlan(projectId, description.trim());
            setPlanResult(result);
            addToast('Plan başarıyla oluşturuldu!');
        } catch {
            addToast('Plan oluşturulamadı. Lütfen tekrar deneyin.', 'error');
        } finally {
            setPlanLoading(false);
        }
    };

    const handleSendMessage = async () => {
        if (!projectId || !chatInput.trim() || chatLoading) return;
        const userMessage = chatInput.trim();
        setChatInput('');

        const userEntry: ChatMessage = {
            role: 'user',
            text: userMessage,
            timestamp: new Date().toISOString(),
        };
        setChatMessages((prev) => [...prev, userEntry]);
        setChatLoading(true);

        try {
            const res: ChatResponse = await aiApi.chat(projectId, userMessage, chatSessionId);
            setChatSessionId(res.sessionId);
            setChatMessages((prev) => [
                ...prev,
                { role: 'assistant', text: res.answer, timestamp: res.timestamp },
            ]);
        } catch {
            setChatMessages((prev) => [
                ...prev,
                {
                    role: 'assistant',
                    text: 'Üzgünüm, şu an yanıt veremiyorum. Lütfen tekrar deneyin.',
                    timestamp: new Date().toISOString(),
                },
            ]);
        } finally {
            setChatLoading(false);
        }
    };

    return (
        <div className={styles.page}>
            <div className={styles.header}>
                <div>
                    <h1 className={styles.title}>AI Planlayıcı</h1>
                    <p className={styles.subtitle}>Projeniz için yapay zeka destekli sprint planı ve sohbet</p>
                </div>
                <div className={styles.badge}>✦ Ollama / gemma3:4b</div>
            </div>

            {/* ── Tabs ── */}
            <div className={styles.tabs}>
                <button
                    className={`${styles.tab} ${tab === 'plan' ? styles.tabActive : ''}`}
                    onClick={() => setTab('plan')}
                >
                    Plan Oluştur
                </button>
                <button
                    className={`${styles.tab} ${tab === 'chat' ? styles.tabActive : ''}`}
                    onClick={() => setTab('chat')}
                >
                    Sohbet
                </button>
            </div>

            {/* ── Plan Tab ── */}
            {tab === 'plan' && (
                <div className={styles.tabContent}>
                    <div className={styles.planInputCard}>
                        <label className={styles.inputLabel}>
                            Proje Açıklaması
                        </label>
                        <textarea
                            className={styles.textarea}
                            placeholder="Projenizi açıklayın... Örn: E-ticaret platformu — ürün listeleme, sepet, ödeme ve kullanıcı yönetimi özellikleri olan bir web uygulaması."
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            rows={5}
                            disabled={planLoading}
                        />
                        <button
                            className={styles.submitBtn}
                            onClick={handleGeneratePlan}
                            disabled={planLoading || !description.trim()}
                        >
                            {planLoading ? (
                                <span className={styles.loadingDots}>
                                    <span />
                                    <span />
                                    <span />
                                </span>
                            ) : (
                                '✦ Sprint Planı Oluştur'
                            )}
                        </button>
                        {planLoading && (
                            <p className={styles.loadingNote}>
                                Yapay zeka planı oluşturuyor... Bu işlem 20-60 saniye sürebilir.
                            </p>
                        )}
                    </div>

                    {planResult && (
                        <div className={styles.planResult}>
                            <h2 className={styles.resultTitle}>
                                Oluşturulan Plan — {planResult.sprints.length} Sprint
                            </h2>
                            <div className={styles.sprintGrid}>
                                {planResult.sprints.map((sprint) => (
                                    <div key={sprint.id} className={styles.sprintCard}>
                                        <div className={styles.sprintHeader}>
                                            <span className={styles.sprintName}>{sprint.name}</span>
                                            <span className={styles.issueBadge}>{sprint.issues.length} issue</span>
                                        </div>
                                        {sprint.goal && (
                                            <p className={styles.sprintGoal}>{sprint.goal}</p>
                                        )}
                                        <ul className={styles.issueList}>
                                            {sprint.issues.map((issue) => (
                                                <li key={issue.id} className={styles.issueRow}>
                                                    <span
                                                        className={styles.priorityDot}
                                                        style={{ background: priorityColor[issue.priority] ?? '#94a3b8' }}
                                                    />
                                                    <span className={styles.issueTitle}>{issue.title}</span>
                                                    <span className={styles.priorityLabel}>{issue.priority}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                </div>
            )}

            {/* ── Chat Tab ── */}
            {tab === 'chat' && (
                <div className={styles.tabContent}>
                    <div className={styles.chatContainer}>
                        <div className={styles.chatMessages}>
                            {chatMessages.length === 0 && (
                                <div className={styles.chatEmpty}>
                                    <p>Projeniz hakkında soru sorabilirsiniz.</p>
                                    <p className={styles.chatEmptyHint}>
                                        Örn: "Bu sprint'te hangi issue'lar açık?" veya "Takımın iş yükü nasıl?"
                                    </p>
                                </div>
                            )}
                            {chatMessages.map((msg, i) => (
                                <div
                                    key={i}
                                    className={`${styles.chatBubble} ${msg.role === 'user' ? styles.chatBubbleUser : styles.chatBubbleAssistant}`}
                                >
                                    <div className={styles.chatBubbleText}>{msg.text}</div>
                                    <div className={styles.chatBubbleTime}>
                                        {new Date(msg.timestamp).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}
                                    </div>
                                </div>
                            ))}
                            {chatLoading && (
                                <div className={`${styles.chatBubble} ${styles.chatBubbleAssistant}`}>
                                    <span className={styles.loadingDots}>
                                        <span />
                                        <span />
                                        <span />
                                    </span>
                                </div>
                            )}
                            <div ref={chatEndRef} />
                        </div>

                        <div className={styles.chatInputRow}>
                            <input
                                className={styles.chatInput}
                                placeholder="Bir şey sorun..."
                                value={chatInput}
                                onChange={(e) => setChatInput(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter' && !e.shiftKey) {
                                        e.preventDefault();
                                        void handleSendMessage();
                                    }
                                }}
                                disabled={chatLoading}
                            />
                            <button
                                className={styles.chatSendBtn}
                                onClick={handleSendMessage}
                                disabled={chatLoading || !chatInput.trim()}
                            >
                                Gönder
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

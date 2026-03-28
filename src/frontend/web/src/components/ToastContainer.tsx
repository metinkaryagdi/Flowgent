import { useToastStore } from '../store/toastStore';

const typeStyles: Record<string, { background: string; color: string; icon: string }> = {
    success: { background: 'var(--color-success, #22c55e)', color: '#fff', icon: '✓' },
    error:   { background: 'var(--color-error, #ef4444)',   color: '#fff', icon: '✕' },
    warning: { background: 'var(--color-warning, #f59e0b)', color: '#fff', icon: '!' },
    info:    { background: 'var(--color-primary, #6366f1)', color: '#fff', icon: 'i' },
};

export default function ToastContainer() {
    const { toasts, removeToast } = useToastStore();

    if (toasts.length === 0) return null;

    return (
        <div
            style={{
                position: 'fixed',
                bottom: '1.5rem',
                right: '1.5rem',
                zIndex: 9999,
                display: 'flex',
                flexDirection: 'column',
                gap: '0.5rem',
                pointerEvents: 'none',
            }}
            aria-live="polite"
            aria-atomic="false"
        >
            {toasts.map((toast) => {
                const s = typeStyles[toast.type] ?? typeStyles.info;
                return (
                    <div
                        key={toast.id}
                        role="alert"
                        style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.625rem',
                            padding: '0.625rem 1rem',
                            borderRadius: '0.5rem',
                            background: s.background,
                            color: s.color,
                            fontSize: '0.875rem',
                            fontWeight: 500,
                            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                            pointerEvents: 'all',
                            maxWidth: '360px',
                            animation: 'toast-slide-in 0.2s ease',
                        }}
                    >
                        <span style={{ fontWeight: 700, flexShrink: 0 }}>{s.icon}</span>
                        <span style={{ flex: 1 }}>{toast.message}</span>
                        <button
                            onClick={() => removeToast(toast.id)}
                            aria-label="Kapat"
                            style={{
                                background: 'none',
                                border: 'none',
                                color: 'inherit',
                                cursor: 'pointer',
                                padding: '0 0.25rem',
                                fontSize: '1rem',
                                lineHeight: 1,
                                opacity: 0.8,
                                flexShrink: 0,
                            }}
                        >
                            ✕
                        </button>
                    </div>
                );
            })}
        </div>
    );
}

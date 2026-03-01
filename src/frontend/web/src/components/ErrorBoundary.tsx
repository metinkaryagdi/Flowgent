import { Component, type ReactNode, type ErrorInfo } from 'react';

interface Props {
    children: ReactNode;
    fallback?: ReactNode;
}

interface State {
    hasError: boolean;
    error: Error | null;
}

export default class ErrorBoundary extends Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        console.error('ErrorBoundary caught:', error, info.componentStack);
    }

    handleReset = () => {
        this.setState({ hasError: false, error: null });
    };

    render() {
        if (this.state.hasError) {
            if (this.props.fallback) return this.props.fallback;

            return (
                <div style={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    minHeight: '60vh',
                    padding: '2rem',
                    textAlign: 'center',
                }}>
                    <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>💥</div>
                    <h2 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem' }}>
                        Bir şeyler ters gitti
                    </h2>
                    <p style={{ color: '#6b7280', fontSize: '0.875rem', maxWidth: 400, marginBottom: '1.5rem' }}>
                        Beklenmedik bir hata oluştu. Lütfen sayfayı yeniden yükleyin veya ana sayfaya dönün.
                    </p>
                    <div style={{ display: 'flex', gap: '0.75rem' }}>
                        <button
                            onClick={this.handleReset}
                            style={{
                                padding: '0.5rem 1.25rem',
                                border: '1px solid #e5e7eb',
                                borderRadius: '0.5rem',
                                background: '#fff',
                                cursor: 'pointer',
                                fontSize: '0.875rem',
                            }}
                        >
                            Tekrar Dene
                        </button>
                        <button
                            onClick={() => window.location.href = '/projects'}
                            style={{
                                padding: '0.5rem 1.25rem',
                                border: 'none',
                                borderRadius: '0.5rem',
                                background: '#6366f1',
                                color: '#fff',
                                cursor: 'pointer',
                                fontSize: '0.875rem',
                                fontWeight: 600,
                            }}
                        >
                            Ana Sayfaya Dön
                        </button>
                    </div>
                    {this.state.error && (
                        <details style={{ marginTop: '1.5rem', fontSize: '0.75rem', color: '#9ca3af', maxWidth: 500 }}>
                            <summary style={{ cursor: 'pointer' }}>Hata detayı</summary>
                            <pre style={{ textAlign: 'left', whiteSpace: 'pre-wrap', marginTop: '0.5rem' }}>
                                {this.state.error.message}
                            </pre>
                        </details>
                    )}
                </div>
            );
        }

        return this.props.children;
    }
}

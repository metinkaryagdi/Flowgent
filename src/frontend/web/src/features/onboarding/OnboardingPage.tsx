import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { organizationsApi } from '../../api/organizations';
import { useAuthStore } from '../../store/authStore';
import styles from '../auth/Auth.module.css';

export default function OnboardingPage() {
    const [orgName, setOrgName] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const { user } = useAuthStore();
    const navigate = useNavigate();

    const handleCreate = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        if (!orgName.trim()) {
            setError('Organizasyon adı boş olamaz.');
            return;
        }
        setLoading(true);
        try {
            await organizationsApi.create({ name: orgName.trim() });
            navigate('/projects');
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { data?: { message?: string } } };
                setError(e.response?.data?.message || 'Organizasyon oluşturulamadı.');
            } else {
                setError('Sunucuya bağlanılamadı.');
            }
        } finally {
            setLoading(false);
        }
    };

    const handleSkip = () => {
        navigate('/projects');
    };

    return (
        <div style={{ maxWidth: 480, margin: '0 auto', padding: '48px 24px' }}>
            <div style={{ textAlign: 'center', marginBottom: 40 }}>
                <div style={{ fontSize: 48, marginBottom: 12 }}>🏢</div>
                <h1 style={{ fontSize: '1.75rem', fontWeight: 700, marginBottom: 8 }}>
                    Hoş Geldiniz, {user?.userName}!
                </h1>
                <p style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                    Ekibinizle çalışmak için bir organizasyon oluşturun veya atlamak için devam edin.
                </p>
            </div>

            <div
                style={{
                    background: 'var(--bg-surface)',
                    border: '1px solid var(--border-color)',
                    borderRadius: 12,
                    padding: '32px 28px',
                    marginBottom: 16,
                }}
            >
                <h2 style={{ fontSize: '1.1rem', fontWeight: 600, marginBottom: 20 }}>
                    Yeni Organizasyon Oluştur
                </h2>

                {error && (
                    <div className={`${styles.formAlert} ${styles.formAlertError}`}>{error}</div>
                )}

                <form onSubmit={handleCreate}>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="orgName">
                            Organizasyon Adı
                        </label>
                        <input
                            id="orgName"
                            type="text"
                            className={styles.formInput}
                            placeholder="Şirket veya ekip adı"
                            value={orgName}
                            onChange={(e) => setOrgName(e.target.value)}
                            autoFocus
                            data-testid="onboarding-org-name"
                        />
                    </div>
                    <button
                        type="submit"
                        className={styles.formButton}
                        disabled={loading}
                        data-testid="onboarding-create"
                    >
                        {loading ? 'Oluşturuluyor...' : 'Organizasyon Oluştur'}
                    </button>
                </form>
            </div>

            <div style={{ textAlign: 'center' }}>
                <button
                    onClick={handleSkip}
                    style={{
                        background: 'none',
                        border: 'none',
                        color: 'var(--text-secondary)',
                        fontSize: '0.875rem',
                        cursor: 'pointer',
                        textDecoration: 'underline',
                    }}
                    data-testid="onboarding-skip"
                >
                    Şimdilik atla, projelerime git
                </button>
            </div>
        </div>
    );
}

import { useState, useEffect, type FormEvent } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { organizationsApi } from '../../api/organizations';
import { authApi } from '../../api/auth';
import { useAuthStore } from '../../store/authStore';
import type { ValidateInviteTokenResult } from '../../types';
import styles from '../auth/Auth.module.css';

export default function AcceptInvitePage() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token') ?? '';
    const navigate = useNavigate();
    const { setAuth, setFlags, setActiveOrg } = useAuthStore();

    const [inviteInfo, setInviteInfo] = useState<ValidateInviteTokenResult | null>(null);
    const [validating, setValidating] = useState(true);
    const [validationError, setValidationError] = useState('');

    const [userName, setUserName] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [formError, setFormError] = useState('');
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        if (!token) {
            setValidationError('Geçersiz davet bağlantısı.');
            setValidating(false);
            return;
        }

        organizationsApi
            .validateInviteToken(token)
            .then((info) => {
                setInviteInfo(info);
                setValidating(false);
            })
            .catch(() => {
                setValidationError('Bu davet bağlantısı geçersiz veya süresi dolmuş.');
                setValidating(false);
            });
    }, [token]);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setFormError('');

        if (!userName.trim() || !password.trim()) {
            setFormError('Lütfen tüm alanları doldurun.');
            return;
        }
        if (password.length < 6) {
            setFormError('Şifre en az 6 karakter olmalıdır.');
            return;
        }
        if (password !== confirmPassword) {
            setFormError('Şifreler eşleşmiyor.');
            return;
        }

        setLoading(true);
        try {
            await organizationsApi.acceptInvite({ token, userName: userName.trim(), password });
            setSuccess(true);

            // Auto-login after 1.5s
            setTimeout(async () => {
                try {
                    const result = await authApi.login({
                        userNameOrEmail: userName.trim(),
                        password,
                    });
                    setAuth(result.user, result.roles);
                    if (result.activeOrgId && result.activeOrgName) {
                        setActiveOrg({ id: result.activeOrgId, name: result.activeOrgName, role: result.activeOrgRole ?? '' });
                    }
                    try {
                        const flags = await authApi.getFlags();
                        setFlags(flags);
                    } catch { /* ignore */ }
                    navigate('/projects');
                } catch {
                    navigate('/login');
                }
            }, 1500);
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { data?: { message?: string } } };
                setFormError(e.response?.data?.message || 'Davet kabul edilemedi.');
            } else {
                setFormError('Sunucuya bağlanılamadı.');
            }
        } finally {
            setLoading(false);
        }
    };

    if (validating) {
        return (
            <div style={{ textAlign: 'center', padding: 48, color: 'var(--text-secondary)' }}>
                Davet doğrulanıyor...
            </div>
        );
    }

    if (validationError) {
        return (
            <div style={{ textAlign: 'center', padding: 48 }}>
                <div style={{ fontSize: 48, marginBottom: 16 }}>⚠️</div>
                <h2 style={{ fontWeight: 700, marginBottom: 8 }}>Geçersiz Davet</h2>
                <p style={{ color: 'var(--text-secondary)', marginBottom: 24 }}>{validationError}</p>
                <button
                    className={styles.formButton}
                    style={{ maxWidth: 240, margin: '0 auto' }}
                    onClick={() => navigate('/login')}
                >
                    Giriş Sayfasına Git
                </button>
            </div>
        );
    }

    if (success) {
        return (
            <div style={{ textAlign: 'center', padding: 48 }}>
                <div style={{ fontSize: 48, marginBottom: 16 }}>✅</div>
                <h2 style={{ fontWeight: 700, marginBottom: 8 }}>Davet Kabul Edildi!</h2>
                <p style={{ color: 'var(--text-secondary)' }}>
                    {inviteInfo?.organizationName} organizasyonuna katıldınız. Yönlendiriliyorsunuz...
                </p>
            </div>
        );
    }

    const roleLabel = inviteInfo?.role === 'Manager' ? 'Yönetici' : 'Üye';

    return (
        <>
            <div style={{ textAlign: 'center', marginBottom: 28 }}>
                <div style={{ fontSize: 40, marginBottom: 8 }}>📩</div>
                <h2 style={{ fontSize: '1.4rem', fontWeight: 700, marginBottom: 4 }}>
                    Daveti Kabul Et
                </h2>
                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                    <strong>{inviteInfo?.organizationName}</strong> organizasyonuna{' '}
                    <strong>{roleLabel}</strong> olarak davet edildiniz.
                </p>
                <p style={{ fontSize: '0.8rem', color: 'var(--text-tertiary)', marginTop: 4 }}>
                    E-posta: {inviteInfo?.email}
                </p>
            </div>

            {formError && (
                <div className={`${styles.formAlert} ${styles.formAlertError}`}>{formError}</div>
            )}

            <form onSubmit={handleSubmit}>
                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="userName">
                        Kullanıcı Adı
                    </label>
                    <input
                        id="userName"
                        type="text"
                        className={styles.formInput}
                        placeholder="kullaniciadi"
                        value={userName}
                        onChange={(e) => setUserName(e.target.value)}
                        autoComplete="username"
                        autoFocus
                        data-testid="accept-invite-username"
                    />
                </div>

                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="password">
                        Şifre
                    </label>
                    <input
                        id="password"
                        type="password"
                        className={styles.formInput}
                        placeholder="En az 6 karakter"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        autoComplete="new-password"
                        data-testid="accept-invite-password"
                    />
                </div>

                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="confirmPassword">
                        Şifre Tekrar
                    </label>
                    <input
                        id="confirmPassword"
                        type="password"
                        className={styles.formInput}
                        placeholder="Şifrenizi tekrar girin"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        autoComplete="new-password"
                        data-testid="accept-invite-confirm"
                    />
                </div>

                <button
                    type="submit"
                    className={styles.formButton}
                    disabled={loading}
                    data-testid="accept-invite-submit"
                >
                    {loading ? 'Kaydediliyor...' : 'Hesap Oluştur ve Katıl'}
                </button>
            </form>

            <div className={styles.formFooter}>
                Zaten hesabınız var mı?{' '}
                <a href="/login" onClick={(e) => { e.preventDefault(); navigate('/login'); }}>
                    Giriş Yap
                </a>
            </div>
        </>
    );
}

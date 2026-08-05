import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../../api/auth';
import { useAuthStore } from '../../store/authStore';
import styles from './Auth.module.css';

export default function LoginPage() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    // Set when the server refuses the login because the address was never confirmed.
    // Shown as its own banner with a resend action rather than a generic error string.
    const [unverifiedEmail, setUnverifiedEmail] = useState<string | null>(null);
    const [resendState, setResendState] = useState<'idle' | 'sending' | 'sent' | 'throttled'>('idle');

    const { setAuth, setFlags, setActiveOrg } = useAuthStore();
    const navigate = useNavigate();

    const handleResend = async () => {
        if (!unverifiedEmail || resendState === 'sending') return;
        setResendState('sending');
        try {
            await authApi.resendVerification(unverifiedEmail);
            setResendState('sent');
        } catch (err: unknown) {
            // Endpoint answers 200 regardless of whether the address exists, so the only
            // failure worth its own message is the gateway's rate limit -- claiming
            // "sent" there leaves the user waiting for a mail that was never sent.
            const status = (err as { response?: { status?: number } }).response?.status;
            setResendState(status === 429 ? 'throttled' : 'sent');
        }
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        setUnverifiedEmail(null);
        setResendState('idle');

        if (!email.trim() || !password.trim()) {
            setError('Lütfen tüm alanları doldurun.');
            return;
        }

        setLoading(true);
        try {
            const result = await authApi.login({ userNameOrEmail: email, password });
            setAuth(result.user, result.roles);

            // Restore active org from login response
            if (result.activeOrgId && result.activeOrgName) {
                setActiveOrg({ id: result.activeOrgId, name: result.activeOrgName, role: result.activeOrgRole ?? '' });
            }

            // BFF flags çek
            try {
                const flags = await authApi.getFlags();
                setFlags(flags);
            } catch {
                // Flags alınamazsa devam et, varsayılan değerler kullanılır
            }

            navigate('/projects');
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                // `email` is echoed back by the email_not_verified branch so the resend
                // action targets the address the server matched, not the raw input.
                const axiosErr = err as { response?: { data?: { message?: string; code?: string; email?: string }; status?: number } };
                if (axiosErr.response?.data?.code === 'account_locked') {
                    // Lockout also answers 401, so it has to be checked first —
                    // otherwise the generic message would hide why login is failing.
                    setError('Çok fazla hatalı deneme nedeniyle hesabınız geçici olarak kilitlendi. Lütfen 15 dakika sonra tekrar deneyin.');
                } else if (axiosErr.response?.data?.code === 'email_not_verified') {
                    // Also a 401, same reasoning as above. The credentials were correct,
                    // so "E-posta veya şifre hatalı" would send the user chasing the
                    // wrong problem — offer the resend link instead.
                    setUnverifiedEmail(axiosErr.response?.data?.email ?? email);
                    setError('');
                } else if (axiosErr.response?.status === 401) {
                    setError('E-posta veya şifre hatalı.');
                } else {
                    setError(axiosErr.response?.data?.message || 'Giriş yapılırken bir hata oluştu.');
                }
            } else {
                setError('Sunucuya bağlanılamadı. Lütfen tekrar deneyin.');
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <>
            <h2 className="authLayout__cardTitle" style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: 4 }}>
                Giriş Yap
            </h2>
            <p className="authLayout__cardSubtitle" style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 32 }}>
                Hesabınıza giriş yaparak devam edin
            </p>

            {error && (
                <div className={`${styles.formAlert} ${styles.formAlertError}`}>
                    {error}
                </div>
            )}

            {unverifiedEmail && (
                <div className={styles.formAlert} data-testid="login-unverified">
                    <p style={{ marginBottom: 8 }}>
                        Bu hesabın e-posta adresi henüz doğrulanmadı. Giriş yapabilmek için
                        <strong> {unverifiedEmail}</strong> adresine gönderdiğimiz bağlantıya tıklayın.
                    </p>
                    <button
                        type="button"
                        onClick={handleResend}
                        disabled={resendState !== 'idle'}
                        data-testid="login-resend"
                        style={{
                            background: 'none',
                            border: 'none',
                            padding: 0,
                            textDecoration: 'underline',
                            cursor: resendState === 'idle' ? 'pointer' : 'default',
                            color: 'inherit',
                            font: 'inherit',
                        }}
                    >
                        {resendState === 'sending' && 'Gönderiliyor...'}
                        {resendState === 'sent' && 'Bağlantı yeniden gönderildi'}
                        {resendState === 'throttled' && 'Çok fazla istek — birkaç dakika bekleyin'}
                        {resendState === 'idle' && 'Bağlantıyı yeniden gönder'}
                    </button>
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="email">
                        E-posta
                    </label>
                    <input
                        id="email"
                        type="email"
                        className={styles.formInput}
                        placeholder="ornek@email.com"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        autoComplete="email"
                        autoFocus
                        data-testid="login-email"
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
                        placeholder="••••••••"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        autoComplete="current-password"
                        data-testid="login-password"
                    />
                </div>

                <button
                    type="submit"
                    className={styles.formButton}
                    disabled={loading}
                    data-testid="login-submit"
                >
                    {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
                </button>
            </form>

            <div className={styles.formFooter}>
                Hesabınız yok mu? <Link to="/register" data-testid="login-to-register">Kayıt Ol</Link>
            </div>
        </>
    );
}


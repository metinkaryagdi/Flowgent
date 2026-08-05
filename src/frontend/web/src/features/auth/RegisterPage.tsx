import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../api/auth';
import styles from './Auth.module.css';

export default function RegisterPage() {
    const [userName, setUserName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
    const [loading, setLoading] = useState(false);
    // Set once registration succeeds. Registration no longer signs anyone in, so there is
    // nothing to navigate to -- the account stays unusable until the emailed link is used.
    const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);
    const [resendState, setResendState] = useState<'idle' | 'sending' | 'sent' | 'throttled'>('idle');

    const handleResend = async () => {
        if (!registeredEmail || resendState === 'sending') return;
        setResendState('sending');
        try {
            await authApi.resendVerification(registeredEmail);
            setResendState('sent');
        } catch (err: unknown) {
            // The endpoint answers 200 whether or not the address exists, so the only
            // failure worth distinguishing is the gateway's rate limit -- reporting
            // "sent" there would leave the user waiting for a mail that never went out.
            const status = (err as { response?: { status?: number } }).response?.status;
            setResendState(status === 429 ? 'throttled' : 'sent');
        }
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        setFieldErrors({});

        if (!userName.trim() || !email.trim() || !password.trim()) {
            setError('Lütfen tüm alanları doldurun.');
            return;
        }

        if (password !== confirmPassword) {
            setError('Şifreler eşleşmiyor.');
            return;
        }

        if (password.length < 6) {
            setError('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        setLoading(true);
        try {
            const result = await authApi.register({ userName, email, password });
            setRegisteredEmail(result.email);
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const axiosErr = err as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } };
                const backendErrors = axiosErr.response?.data?.errors;
                if (backendErrors) {
                    const mapped: Record<string, string> = {};
                    for (const [key, msgs] of Object.entries(backendErrors)) {
                        mapped[key.toLowerCase()] = msgs[0];
                    }
                    setFieldErrors(mapped);
                    setError('');
                } else {
                    setError(axiosErr.response?.data?.message || 'Kayıt olurken bir hata oluştu.');
                }
            } else {
                setError('Sunucuya bağlanılamadı. Lütfen tekrar deneyin.');
            }
        } finally {
            setLoading(false);
        }
    };

    if (registeredEmail) {
        return (
            <div data-testid="register-check-email">
                <h2 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: 4 }}>
                    E-postanızı kontrol edin
                </h2>
                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 24 }}>
                    <strong>{registeredEmail}</strong> adresine bir doğrulama bağlantısı gönderdik.
                    Hesabınızı etkinleştirmek ve giriş yapabilmek için bağlantıya tıklayın.
                    Bağlantı 24 saat geçerlidir.
                </p>

                <div className={`${styles.formAlert}`} style={{ marginBottom: 24 }}>
                    E-posta birkaç dakika içinde gelmezse spam klasörünüzü kontrol edin.
                </div>

                <button
                    type="button"
                    className={styles.formButton}
                    onClick={handleResend}
                    disabled={resendState !== 'idle'}
                    data-testid="register-resend"
                >
                    {resendState === 'sending' && 'Gönderiliyor...'}
                    {resendState === 'sent' && 'Bağlantı yeniden gönderildi'}
                    {resendState === 'throttled' && 'Çok fazla istek — birkaç dakika bekleyin'}
                    {resendState === 'idle' && 'Bağlantıyı yeniden gönder'}
                </button>

                <div className={styles.formFooter}>
                    Doğruladınız mı? <Link to="/login" data-testid="register-to-login">Giriş Yap</Link>
                </div>
            </div>
        );
    }

    return (
        <>
            <h2 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: 4 }}>
                Kayıt Ol
            </h2>
            <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 32 }}>
                Yeni bir hesap oluşturun
            </p>

            {error && (
                <div className={`${styles.formAlert} ${styles.formAlertError}`}>
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="userName">
                        Kullanıcı Adı
                    </label>
                    <input
                        id="userName"
                        type="text"
                        className={`${styles.formInput} ${fieldErrors['username'] ? styles.formInputError : ''}`}
                        placeholder="kullaniciadi"
                        value={userName}
                        onChange={(e) => setUserName(e.target.value)}
                        autoComplete="username"
                        autoFocus
                        data-testid="register-username"
                    />
                    {fieldErrors['username'] && <p className={styles.formError}>{fieldErrors['username']}</p>}
                </div>

                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="email">
                        E-posta
                    </label>
                    <input
                        id="email"
                        type="email"
                        className={`${styles.formInput} ${fieldErrors['email'] ? styles.formInputError : ''}`}
                        placeholder="ornek@email.com"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        autoComplete="email"
                        data-testid="register-email"
                    />
                    {fieldErrors['email'] && <p className={styles.formError}>{fieldErrors['email']}</p>}
                </div>

                <div className={styles.formGroup}>
                    <label className={styles.formLabel} htmlFor="password">
                        Şifre
                    </label>
                    <input
                        id="password"
                        type="password"
                        className={`${styles.formInput} ${fieldErrors['password'] ? styles.formInputError : ''}`}
                        placeholder="En az 6 karakter"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        autoComplete="new-password"
                        data-testid="register-password"
                    />
                    {fieldErrors['password'] && <p className={styles.formError}>{fieldErrors['password']}</p>}
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
                        data-testid="register-confirm"
                    />
                </div>

                <button
                    type="submit"
                    className={styles.formButton}
                    disabled={loading}
                    data-testid="register-submit"
                >
                    {loading ? 'Kayıt yapılıyor...' : 'Kayıt Ol'}
                </button>
            </form>

            <div className={styles.formFooter}>
                Zaten hesabınız var mı? <Link to="/login" data-testid="register-to-login">Giriş Yap</Link>
            </div>
        </>
    );
}



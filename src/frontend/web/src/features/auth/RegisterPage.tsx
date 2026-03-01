import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../../api/auth';
import { useAuthStore } from '../../store/authStore';
import styles from './Auth.module.css';

export default function RegisterPage() {
    const [userName, setUserName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const { setAuth, setFlags } = useAuthStore();
    const navigate = useNavigate();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');

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
            setAuth(result.accessToken, result.user, result.roles);

            try {
                const flags = await authApi.getFlags();
                setFlags(flags);
            } catch {
                // Flags alınamazsa devam et
            }

            navigate('/projects');
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const axiosErr = err as { response?: { data?: { message?: string } } };
                setError(axiosErr.response?.data?.message || 'Kayıt olurken bir hata oluştu.');
            } else {
                setError('Sunucuya bağlanılamadı. Lütfen tekrar deneyin.');
            }
        } finally {
            setLoading(false);
        }
    };

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
                        className={styles.formInput}
                        placeholder="kullaniciadi"
                        value={userName}
                        onChange={(e) => setUserName(e.target.value)}
                        autoComplete="username"
                        autoFocus
                        data-testid="register-username"
                    />
                </div>

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
                        data-testid="register-email"
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
                        data-testid="register-password"
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



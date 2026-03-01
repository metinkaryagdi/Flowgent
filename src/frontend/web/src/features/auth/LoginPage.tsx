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

    const { setAuth, setFlags } = useAuthStore();
    const navigate = useNavigate();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');

        if (!email.trim() || !password.trim()) {
            setError('Lütfen tüm alanları doldurun.');
            return;
        }

        setLoading(true);
        try {
            const result = await authApi.login({ email, password });
            setAuth(result.accessToken, result.user, result.roles);

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
                const axiosErr = err as { response?: { data?: { message?: string }; status?: number } };
                if (axiosErr.response?.status === 401) {
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
                    />
                </div>

                <button
                    type="submit"
                    className={styles.formButton}
                    disabled={loading}
                >
                    {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
                </button>
            </form>

            <div className={styles.formFooter}>
                Hesabınız yok mu? <Link to="/register">Kayıt Ol</Link>
            </div>
        </>
    );
}

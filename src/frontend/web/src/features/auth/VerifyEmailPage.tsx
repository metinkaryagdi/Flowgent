import { useEffect, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../../api/auth';
import styles from './Auth.module.css';

type Status = 'verifying' | 'success' | 'error';

/**
 * Landing page for the link in the verification email: /verify-email?token=...
 *
 * The token is single-use, so this must fire exactly once. React 18's StrictMode runs
 * effects twice in development, which would spend the token on the first call and show a
 * spurious "link is invalid" from the second -- the guard ref below prevents that.
 */
export default function VerifyEmailPage() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');

    // A missing token is knowable at render time, so it seeds the initial state instead of
    // being set from inside the effect -- a synchronous setState in an effect body forces
    // an extra render pass and is rejected by the lint rules.
    const [status, setStatus] = useState<Status>(token ? 'verifying' : 'error');
    const [message, setMessage] = useState(token ? '' : 'Doğrulama bağlantısı eksik veya bozuk.');
    const hasRun = useRef(false);

    useEffect(() => {
        if (hasRun.current || !token) return;
        hasRun.current = true;

        authApi
            .verifyEmail(token)
            .then(() => setStatus('success'))
            .catch((err: unknown) => {
                setStatus('error');
                const axiosErr = err as { response?: { data?: { message?: string }; status?: number } };
                // The gateway answers 429 with an empty body, so without this branch the
                // fallback below would tell the user their link expired when it is fine.
                if (axiosErr.response?.status === 429) {
                    setMessage('Çok fazla deneme yapıldı. Lütfen birkaç dakika sonra tekrar deneyin.');
                    return;
                }
                setMessage(
                    axiosErr.response?.data?.message ||
                        'Doğrulama bağlantısı geçersiz veya süresi dolmuş.',
                );
            });
    }, [token]);

    if (status === 'verifying') {
        return (
            <div data-testid="verify-email-pending">
                <h2 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: 4 }}>
                    Doğrulanıyor...
                </h2>
                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>
                    E-posta adresiniz doğrulanıyor, lütfen bekleyin.
                </p>
            </div>
        );
    }

    if (status === 'success') {
        return (
            <div data-testid="verify-email-success">
                <h2 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: 4 }}>
                    E-postanız doğrulandı
                </h2>
                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 24 }}>
                    Hesabınız etkinleştirildi. Artık giriş yapabilirsiniz.
                </p>
                <Link to="/login" className={styles.formButton} data-testid="verify-email-to-login">
                    Giriş Yap
                </Link>
            </div>
        );
    }

    return (
        <div data-testid="verify-email-error">
            <h2 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: 4 }}>
                Doğrulama başarısız
            </h2>
            <div className={`${styles.formAlert} ${styles.formAlertError}`} style={{ marginBottom: 24 }}>
                {message}
            </div>
            <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 24 }}>
                Bağlantının süresi dolmuş olabilir. Giriş sayfasından yeni bir doğrulama
                bağlantısı isteyebilirsiniz.
            </p>
            <div className={styles.formFooter}>
                <Link to="/login" data-testid="verify-email-to-login">Giriş sayfasına dön</Link>
            </div>
        </div>
    );
}

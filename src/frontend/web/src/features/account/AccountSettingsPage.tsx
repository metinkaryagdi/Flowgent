import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { usersApi } from '../../api/users';
import { useAuthStore } from '../../store/authStore';
import { useToastStore } from '../../store/toastStore';
import styles from '../organization/OrganizationSettings.module.css';

/**
 * Personal account settings, deliberately outside the organization guard: someone who has
 * left every organization must still be able to delete their account.
 */
export default function AccountSettingsPage() {
    const { user, logout } = useAuthStore();
    const { addToast } = useToastStore();
    const navigate = useNavigate();

    // Two steps on purpose. The password field only appears after the user has said they
    // mean it, so a stray click on a form that is already filled cannot erase an account.
    const [confirming, setConfirming] = useState(false);
    const [password, setPassword] = useState('');
    const [deleting, setDeleting] = useState(false);
    const [error, setError] = useState('');

    const handleDelete = async (e: FormEvent) => {
        e.preventDefault();
        if (!password) {
            setError('Devam etmek için şifrenizi girin.');
            return;
        }

        setDeleting(true);
        setError('');
        try {
            await usersApi.deleteMyAccount(password);
            // The server already cleared the cookies; this clears the client-side store so
            // the app does not keep rendering as a signed-in user on the way out.
            logout();
            addToast('Hesabınız ve kişisel verileriniz silindi.', 'success');
            navigate('/login', { replace: true });
        } catch (err: unknown) {
            const status = (err as { response?: { status?: number } })?.response?.status;
            const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
            if (status === 401) {
                setError('Şifre hatalı.');
            } else {
                setError(message || 'Hesap silinemedi. Lütfen tekrar deneyin.');
            }
            setDeleting(false);
        }
    };

    return (
        <div className={styles.page}>
            <div className={styles.pageHeader}>
                <div>
                    <h1 className={styles.pageTitle}>Hesap Ayarları</h1>
                    <p className={styles.pageMeta}>{user?.email}</p>
                </div>
            </div>

            <div className={styles.section}>
                <h2 className={styles.sectionTitle}>Hesabımı sil</h2>

                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 16 }}>
                    Hesabınızı sildiğinizde e-posta adresiniz ve kullanıcı adınız kalıcı olarak
                    silinir, tüm oturumlarınız kapatılır ve organizasyon üyelikleriniz kaldırılır.
                    <strong> Bu işlem geri alınamaz.</strong>
                </p>
                <p style={{ fontSize: '0.875rem', color: 'var(--text-secondary)', marginBottom: 20 }}>
                    Oluşturduğunuz issue, yorum ve sprint kayıtları ekip arkadaşlarınızın çalışma
                    geçmişinin parçası olduğu için silinmez; ancak bunlar artık size değil,
                    kime ait olduğu belirlenemeyen bir kimliğe bağlı kalır.
                </p>

                {error && <div className={styles.errorBox}>{error}</div>}

                {!confirming ? (
                    <button
                        type="button"
                        className={styles.btnDanger}
                        onClick={() => setConfirming(true)}
                        data-testid="account-delete-open"
                    >
                        Hesabımı sil
                    </button>
                ) : (
                    <form onSubmit={handleDelete}>
                        <label
                            htmlFor="deleteConfirmPassword"
                            style={{ display: 'block', fontSize: '0.875rem', marginBottom: 8 }}
                        >
                            Onaylamak için şifrenizi girin
                        </label>
                        <input
                            id="deleteConfirmPassword"
                            type="password"
                            className={styles.input}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            autoComplete="current-password"
                            autoFocus
                            data-testid="account-delete-password"
                        />
                        <div className={styles.actionGroup} style={{ marginTop: 16 }}>
                            <button
                                type="submit"
                                className={styles.btnDanger}
                                disabled={deleting}
                                data-testid="account-delete-confirm"
                            >
                                {deleting ? 'Siliniyor...' : 'Hesabımı kalıcı olarak sil'}
                            </button>
                            <button
                                type="button"
                                className={styles.btnSecondary}
                                onClick={() => {
                                    setConfirming(false);
                                    setPassword('');
                                    setError('');
                                }}
                                disabled={deleting}
                            >
                                Vazgeç
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
}

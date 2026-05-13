import { Outlet } from 'react-router-dom';
import styles from './AuthLayout.module.css';

export default function AuthLayout() {
    return (
        <div className={styles.authLayout}>
            <div className={styles.authLayout__left}>
                <div className={styles.authLayout__brand}>
                    <div className={styles.authLayout__logo}>
                        <span className={styles.authLayout__logoIcon}>⚡</span>
                    </div>
                    <h1 className={styles.authLayout__title}>Flowgent</h1>
                    <p className={styles.authLayout__subtitle}>
                        Projelerinizi yönetin, görevleri takip edin, sprint'lerinizi planlayın
                        — hepsi tek bir platformda.
                    </p>
                </div>
            </div>

            <div className={styles.authLayout__right}>
                <div className={styles.authLayout__card}>
                    <Outlet />
                </div>
            </div>
        </div>
    );
}

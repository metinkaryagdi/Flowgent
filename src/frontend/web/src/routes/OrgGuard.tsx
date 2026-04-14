import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

/**
 * Organizasyon guard'ı — ADR-002
 * Admin ise bypass, activeOrg varsa geç, yoksa /onboarding'e yönlendir.
 */
export default function OrgGuard() {
    const { roles, activeOrg } = useAuthStore();

    if (roles.includes('Admin')) return <Outlet />;
    if (activeOrg !== null) return <Outlet />;
    return <Navigate to="/onboarding" replace />;
}

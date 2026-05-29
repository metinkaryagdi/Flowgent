import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export default function AdminGuard() {
    const { roles } = useAuthStore();
    if (!roles.includes('Admin')) return <Navigate to="/projects" replace />;
    return <Outlet />;
}

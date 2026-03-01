import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import { useAuthStore } from './store/authStore';
import { useThemeStore } from './store/themeStore';
import ErrorBoundary from './components/ErrorBoundary';

import AuthLayout from './layouts/AuthLayout';
import AppLayout from './layouts/AppLayout';
import ProtectedRoute from './routes/ProtectedRoute';

import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import ProjectsPage from './features/projects/ProjectsPage';
import BoardPage from './features/board/BoardPage';
import SprintPage from './features/sprints/SprintPage';
import NotificationsPage from './features/notifications/NotificationsPage';
import AdminPage from './features/admin/AdminPage';

export default function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  const theme = useThemeStore((s) => s.theme);

  useEffect(() => {
    // 🔧 DEV ONLY: Backend olmadan test için otomatik mock login
    // Prodüksiyon öncesi bu bloğu kaldırın!
    if (import.meta.env.DEV && !localStorage.getItem('accessToken')) {
      localStorage.setItem('accessToken', 'dev-mock-token');
      localStorage.setItem('user', JSON.stringify({
        id: 'dev-user-1',
        userName: 'TestKullanıcı',
        email: 'test@bitirme.dev',
        isActive: true,
        createdAt: '2024-01-01T00:00:00Z',
      }));
      localStorage.setItem('roles', JSON.stringify(['Admin']));
    }
    hydrate();
  }, [hydrate]);

  // Apply saved theme on mount
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
  }, [theme]);

  return (
    <ErrorBoundary>
      <a href="#main-content" className="skip-to-content">
        İçeriğe atla
      </a>
      <BrowserRouter>
        <Routes>
          {/* ── Public Routes (Auth) ── */}
          <Route element={<AuthLayout />}>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
          </Route>

          {/* ── Protected Routes ── */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/projects" element={<ProjectsPage />} />
              <Route path="/projects/:projectId/board" element={<BoardPage />} />
              <Route path="/projects/:projectId/sprints" element={<SprintPage />} />
              <Route path="/notifications" element={<NotificationsPage />} />
              <Route path="/admin" element={<AdminPage />} />
            </Route>
          </Route>

          {/* ── Fallback ── */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

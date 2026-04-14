import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import { useAuthStore } from './store/authStore';
import { useThemeStore } from './store/themeStore';
import ErrorBoundary from './components/ErrorBoundary';
import ToastContainer from './components/ToastContainer';

import AuthLayout from './layouts/AuthLayout';
import AppLayout from './layouts/AppLayout';
import ProtectedRoute from './routes/ProtectedRoute';
import OrgGuard from './routes/OrgGuard';

import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import AcceptInvitePage from './features/invite/AcceptInvitePage';
import ProjectsPage from './features/projects/ProjectsPage';
import ProjectDetailPage from './features/projects/ProjectDetailPage';
import BoardPage from './features/board/BoardPage';
import SprintPage from './features/sprints/SprintPage';
import NotificationsPage from './features/notifications/NotificationsPage';
import AdminPage from './features/admin/AdminPage';
import OnboardingPage from './features/onboarding/OnboardingPage';
import OrganizationSettingsPage from './features/organization/OrganizationSettingsPage';
import AiPlannerPage from './features/ai/AiPlannerPage';

export default function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  const theme = useThemeStore((s) => s.theme);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  // Apply saved theme on mount
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
  }, [theme]);

  return (
    <ErrorBoundary>
      <ToastContainer />
      <a href="#main-content" className="skip-to-content">
        İçeriğe atla
      </a>
      <BrowserRouter>
        <Routes>
          {/* ── Public Routes (Auth) ── */}
          <Route element={<AuthLayout />}>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/invite/accept" element={<AcceptInvitePage />} />
          </Route>

          {/* ── Protected Routes ── */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              {/* Org gerektirmez — user_id bazlı */}
              <Route path="/notifications" element={<NotificationsPage />} />

              {/* Org guard — activeOrg yoksa /onboarding'e yönlendir */}
              <Route element={<OrgGuard />}>
                <Route path="/projects" element={<ProjectsPage />} />
                <Route path="/projects/:projectId" element={<ProjectDetailPage />} />
                <Route path="/projects/:projectId/board" element={<BoardPage />} />
                <Route path="/projects/:projectId/sprints" element={<SprintPage />} />
                <Route path="/projects/:projectId/ai-planner" element={<AiPlannerPage />} />
                <Route path="/admin" element={<AdminPage />} />
                <Route path="/settings/organization" element={<OrganizationSettingsPage />} />
              </Route>
            </Route>
            <Route path="/onboarding" element={<OnboardingPage />} />
          </Route>

          {/* ── Fallback ── */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </ErrorBoundary>
  );
}


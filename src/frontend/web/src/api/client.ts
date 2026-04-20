import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true,
    headers: {
        'Content-Type': 'application/json',
    },
});

// ── Request interceptor ──
apiClient.interceptors.request.use(
    (config) => config,
    (error) => Promise.reject(error)
);

// ── Response interceptor: 401 → refresh → retry, sonra logout ──
let isRefreshing = false;
let failedQueue: Array<{ resolve: (value: unknown) => void; reject: (reason?: unknown) => void }> = [];

const processQueue = (error: unknown) => {
    failedQueue.forEach((prom) => {
        if (error) prom.reject(error);
        else prom.resolve(null);
    });
    failedQueue = [];
};

// Multi-tab coordination: broadcast refresh state across tabs
const authChannel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('auth_refresh') : null;

authChannel?.addEventListener('message', (event: MessageEvent) => {
    if (event.data === 'refresh_success') {
        processQueue(null);
    } else if (event.data === 'refresh_failure') {
        processQueue(new Error('Refresh failed in another tab'));
        window.location.href = '/login';
    }
});

apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response?.status !== 401) {
            return Promise.reject(error);
        }

        const currentPath = window.location.pathname;
        const isAuthPage = currentPath === '/login' || currentPath === '/register';
        const isRefreshEndpoint = originalRequest?.url?.includes('/api/v1/identity/refresh');

        // Auth sayfasında veya refresh isteğinin kendisi başarısız olduysa → logout
        if (isAuthPage || isRefreshEndpoint || originalRequest?._retry) {
            localStorage.removeItem('user');
            localStorage.removeItem('roles');
            if (!isAuthPage) window.location.href = '/login';
            return Promise.reject(error);
        }

        // Zaten refresh yapılıyorsa kuyruğa al
        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                failedQueue.push({ resolve, reject });
            })
                .then(() => apiClient(originalRequest))
                .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
            // refreshToken cookie otomatik gönderilir (withCredentials: true)
            await apiClient.post('/api/v1/identity/refresh');
            authChannel?.postMessage('refresh_success');
            processQueue(null);
            return apiClient(originalRequest);
        } catch (refreshError) {
            authChannel?.postMessage('refresh_failure');
            processQueue(refreshError);
            localStorage.removeItem('user');
            localStorage.removeItem('roles');
            window.location.href = '/login';
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
        }
    }
);

export default apiClient;

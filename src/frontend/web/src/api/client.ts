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

// ── Response interceptor: handle 401 & 409 ──
apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        // Sadece gerçek 401 yanıtlarında login'e yönlendir
        // Login/register sayfalarında ise yönlendirme yapma (sonsuz döngü önlemi)
        if (error.response && error.response.status === 401) {
            const currentPath = window.location.pathname;
            const isAuthPage = currentPath === '/login' || currentPath === '/register';

            if (!isAuthPage) {
                localStorage.removeItem('user');
                localStorage.removeItem('roles');
                window.location.href = '/login';
            }
        }
        // 409 Conflict durumu çağrıda handle edilecek
        return Promise.reject(error);
    }
);

export default apiClient;

import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// ── Request interceptor: attach JWT token ──
apiClient.interceptors.request.use(
    (config) => {
        // Geçici: localStorage'dan token al
        // TODO: HttpOnly Cookie'ye migrate edilecek
        const token = localStorage.getItem('accessToken');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// ── Response interceptor: handle 401 & 409 ──
apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        // Sadece gerçek 401 yanıtlarında login'e yönlendir
        // Network hataları (ERR_CONNECTION_REFUSED) durumunda yönlendirme yapma
        if (error.response && error.response.status === 401) {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('user');
            window.location.href = '/login';
        }
        // 409 Conflict durumu çağrıda handle edilecek
        return Promise.reject(error);
    }
);

export default apiClient;

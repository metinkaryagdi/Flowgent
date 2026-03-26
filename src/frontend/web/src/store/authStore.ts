import { create } from 'zustand';
import type { UserDto, UiFlags } from '../types';

interface AuthState {
    // State
    token: string | null;
    user: UserDto | null;
    roles: string[];
    flags: UiFlags | null;
    isAuthenticated: boolean;

    // Actions
    setAuth: (user: UserDto, roles: string[]) => void;
    setFlags: (flags: UiFlags) => void;
    logout: () => void;
    hydrate: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
    token: null,
    user: null,
    roles: [],
    flags: null,
    isAuthenticated: false,

    setAuth: (user, roles) => {
        // Token is now managed by HttpOnly cookies
        localStorage.setItem('user', JSON.stringify(user));
        localStorage.setItem('roles', JSON.stringify(roles));
        set({ token: null, user, roles, isAuthenticated: true });
    },

    setFlags: (flags) => {
        set({ flags });
    },

    logout: () => {
        localStorage.removeItem('user');
        localStorage.removeItem('roles');
        set({ token: null, user: null, roles: [], flags: null, isAuthenticated: false });
    },

    hydrate: () => {
        const userStr = localStorage.getItem('user');
        const rolesStr = localStorage.getItem('roles');

        if (userStr) {
            try {
                const user = JSON.parse(userStr) as UserDto;
                const roles = rolesStr ? JSON.parse(rolesStr) as string[] : [];
                set({ token: null, user, roles, isAuthenticated: true });
            } catch {
                localStorage.removeItem('user');
                localStorage.removeItem('roles');
            }
        }
    },
}));

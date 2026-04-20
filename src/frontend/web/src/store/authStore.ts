import { create } from 'zustand';
import type { UserDto, UiFlags } from '../types';

export interface ActiveOrg {
    id: string;
    name: string;
    role: string;
}

interface AuthState {
    // State
    token: string | null;
    user: UserDto | null;
    roles: string[];
    flags: UiFlags | null;
    isAuthenticated: boolean;
    activeOrg: ActiveOrg | null;
    skippedOnboarding: boolean;

    // Actions
    setAuth: (user: UserDto, roles: string[]) => void;
    setFlags: (flags: UiFlags) => void;
    setActiveOrg: (org: ActiveOrg | null) => void;
    skipOnboarding: () => void;
    logout: () => void;
    hydrate: () => void;
}

// Synchronous initial state read — prevents OrgGuard from redirecting on first render
const getPersistedState = (): {
    user: UserDto | null;
    roles: string[];
    isAuthenticated: boolean;
    activeOrg: ActiveOrg | null;
    skippedOnboarding: boolean;
} => {
    try {
        const userStr = localStorage.getItem('user');
        if (userStr) {
            const user = JSON.parse(userStr) as UserDto;
            const rolesStr = localStorage.getItem('roles');
            const activeOrgStr = localStorage.getItem('activeOrg');
            const skipped = localStorage.getItem('skippedOnboarding') === 'true';
            return {
                user,
                roles: rolesStr ? (JSON.parse(rolesStr) as string[]) : [],
                isAuthenticated: true,
                activeOrg: activeOrgStr ? (JSON.parse(activeOrgStr) as ActiveOrg) : null,
                skippedOnboarding: skipped,
            };
        }
    } catch {
        localStorage.removeItem('user');
        localStorage.removeItem('roles');
        localStorage.removeItem('activeOrg');
        localStorage.removeItem('skippedOnboarding');
    }
    return { user: null, roles: [], isAuthenticated: false, activeOrg: null, skippedOnboarding: false };
};

export const useAuthStore = create<AuthState>((set) => ({
    token: null,
    flags: null,
    ...getPersistedState(),

    setAuth: (user, roles) => {
        localStorage.setItem('user', JSON.stringify(user));
        localStorage.setItem('roles', JSON.stringify(roles));
        set({ token: null, user, roles, isAuthenticated: true });
    },

    setFlags: (flags) => {
        set({ flags });
    },

    setActiveOrg: (org) => {
        if (org) {
            localStorage.setItem('activeOrg', JSON.stringify(org));
        } else {
            localStorage.removeItem('activeOrg');
        }
        set({ activeOrg: org });
    },

    skipOnboarding: () => {
        localStorage.setItem('skippedOnboarding', 'true');
        set({ skippedOnboarding: true });
    },

    logout: () => {
        localStorage.removeItem('user');
        localStorage.removeItem('roles');
        localStorage.removeItem('activeOrg');
        localStorage.removeItem('skippedOnboarding');
        set({ token: null, user: null, roles: [], flags: null, isAuthenticated: false, activeOrg: null, skippedOnboarding: false });
    },

    // hydrate() is kept for backward compat but no longer needed for OrgGuard correctness
    hydrate: () => {
        const persisted = getPersistedState();
        set({ ...persisted });
    },
}));

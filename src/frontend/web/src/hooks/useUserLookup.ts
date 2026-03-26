import { useEffect, useMemo, useState } from 'react';
import type { UserDto } from '../types';
import { usersApi } from '../api/users';

const userCache = new Map<string, UserDto>();
const inflight = new Map<string, Promise<UserDto | null>>();

const normalizeIds = (ids: Array<string | null | undefined>) => {
    const unique = Array.from(new Set(ids.filter(Boolean) as string[]));
    unique.sort();
    return unique;
};

export function useUserLookup(ids: Array<string | null | undefined>) {
    const [users, setUsers] = useState<Map<string, UserDto>>(() => new Map(userCache));

    const normalizedIds = useMemo(() => normalizeIds(ids), [ids]);

    useEffect(() => {
        if (normalizedIds.length === 0) return;

        const missing = normalizedIds.filter((id) => !userCache.has(id));
        if (missing.length === 0) {
            setUsers(new Map(userCache));
            return;
        }

        let cancelled = false;

        const load = async () => {
            const results = await Promise.all(
                missing.map(async (id) => {
                    if (!inflight.has(id)) {
                        inflight.set(
                            id,
                            usersApi.getById(id).catch(() => null)
                        );
                    }
                    const result = await inflight.get(id)!;
                    inflight.delete(id);
                    return { id, user: result };
                })
            );

            if (cancelled) return;

            results.forEach(({ id, user }) => {
                if (user) userCache.set(id, user);
            });
            setUsers(new Map(userCache));
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [normalizedIds.join('|')]);

    const getUserName = (id: string | null | undefined, fallbackLength = 8) => {
        if (!id) return '—';
        const user = users.get(id);
        return user?.userName || `${id.slice(0, fallbackLength)}...`;
    };

    const getInitials = (id: string | null | undefined, fallback = '??') => {
        if (!id) return fallback;
        const user = users.get(id);
        if (user?.userName) return user.userName.slice(0, 2).toUpperCase();
        return id.slice(0, 2).toUpperCase();
    };

    return { users, getUserName, getInitials };
}

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
    // Callers pass a freshly built array on every render, so memoising on `ids`
    // directly would recompute forever. Collapsing to a string first gives a value
    // that compares equal across renders, which keeps `normalizedIds` -- and the
    // effect that depends on it -- stable.
    const idsKey = useMemo(() => normalizeIds(ids).join('|'), [ids]);
    const normalizedIds = useMemo(() => (idsKey ? idsKey.split('|') : []), [idsKey]);

    // Bumped whenever a fetch adds entries to the shared cache. The rendered map is
    // derived from the cache rather than copied into state, so the "everything is
    // already cached" path needs no setState at all.
    const [cacheVersion, setCacheVersion] = useState(0);

    useEffect(() => {
        if (normalizedIds.length === 0) return;

        const missing = normalizedIds.filter((id) => !userCache.has(id));
        if (missing.length === 0) return;

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
            setCacheVersion((version) => version + 1);
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [normalizedIds]);

    // Recomputed when a fetch lands (cacheVersion) or the requested ids change --
    // the latter also picks up entries another component put in the cache meanwhile.
    // `userCache` is a module-level mutable map, so these two are the invalidation
    // signals; the rule cannot see that because neither appears in the body.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    const users = useMemo(() => new Map(userCache), [cacheVersion, idsKey]);

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

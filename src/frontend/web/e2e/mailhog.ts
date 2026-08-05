import type { APIRequestContext } from '@playwright/test';

/**
 * Reads the verification mail out of MailHog, the SMTP sink docker-compose runs in
 * development. Registration no longer signs anyone in, so a browser-only test cannot get
 * past the "check your inbox" screen -- the link has to come from the mailbox.
 */

const MAILHOG_BASE_URL = process.env.MAILHOG_BASE_URL ?? 'http://localhost:8025';

interface MailHogMessage {
    Content: {
        Headers: Record<string, string[]>;
        Body: string;
    };
}

/**
 * MailHog hands back the body exactly as it arrived on the wire, so it still carries
 * whatever Content-Transfer-Encoding the sender used. .NET's SmtpClient picks base64 for
 * the non-ASCII Turkish text in these mails, and a raw base64 blob obviously will not
 * match a URL regex.
 */
function decodeBody(message: MailHogMessage): string {
    const encoding = (message.Content.Headers['Content-Transfer-Encoding']?.[0] ?? '')
        .trim()
        .toLowerCase();
    const body = message.Content.Body;

    if (encoding === 'base64') {
        return Buffer.from(body, 'base64').toString('utf-8');
    }

    if (encoding === 'quoted-printable') {
        return body
            .replace(/=\r?\n/g, '')
            .replace(/=([0-9A-Fa-f]{2})/g, (_, hex: string) =>
                String.fromCharCode(parseInt(hex, 16)),
            );
    }

    return body;
}

/**
 * Polls until a mail addressed to {@link email} contains a link matching {@link pattern}.
 * Polling rather than a single read because the mail is sent inside the registration
 * request but SMTP delivery is still a separate hop -- the response can win the race.
 */
async function waitForLink(
    request: APIRequestContext,
    email: string,
    pattern: RegExp,
    timeoutMs = 30_000,
): Promise<string> {
    const deadline = Date.now() + timeoutMs;
    let lastBodies: string[] = [];

    while (Date.now() < deadline) {
        const response = await request.get(
            `${MAILHOG_BASE_URL}/api/v2/search?kind=to&query=${encodeURIComponent(email)}&limit=20`,
        );

        if (response.ok()) {
            const payload = (await response.json()) as { items?: MailHogMessage[] };
            lastBodies = (payload.items ?? []).map(decodeBody);

            for (const body of lastBodies) {
                const match = body.match(pattern);
                if (match) return match[0];
            }
        }

        await new Promise((resolve) => setTimeout(resolve, 500));
    }

    throw new Error(
        `No mail matching ${pattern} arrived for ${email} within ${timeoutMs}ms. ` +
            `Bodies seen: ${JSON.stringify(lastBodies)}`,
    );
}

/**
 * Returns the absolute /verify-email?token=... URL from the registration mail.
 * Each run registers a unique address, so the search is already scoped to this test and
 * the mailbox never needs purging between runs.
 */
export function waitForVerificationLink(
    request: APIRequestContext,
    email: string,
    timeoutMs?: number,
): Promise<string> {
    return waitForLink(request, email, /https?:\/\/\S+\/verify-email\?token=\S+/, timeoutMs);
}

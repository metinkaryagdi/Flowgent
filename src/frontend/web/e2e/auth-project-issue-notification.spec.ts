import { test, expect } from '@playwright/test';
import { waitForVerificationLink } from './mailhog';

// Note when running this repeatedly: the gateway allows 3 verify-email calls per IP per
// 5 minutes, so a fourth run inside that window gets a 429 and fails at the verification
// step. That is the rate limiter working, not a broken test.

test('register -> verify email -> project -> issue -> notification', async ({ page, request }) => {
  const unique = `${Date.now()}`;
  const userName = `e2e_${unique}`;
  const email = `e2e_${unique}@example.com`;
  const password = 'TestPass123!';
  const orgName = `E2E Org ${unique}`;
  const projectName = `E2E Project ${unique}`;
  const projectKey = `E${unique.slice(-5)}`.toUpperCase();
  const issueTitle = `E2E Issue ${unique}`;

  await page.route('**/api/v1/bff/flags', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        canManageProjects: true,
        canEditIssues: true,
        canAssignIssues: true,
        canChangeStatus: true,
        canViewAdmin: true,
      }),
    });
  });

  await page.goto('/login');
  await expect(page).toHaveURL(/\/login/);
  await page.getByTestId('login-to-register').click();
  await expect(page).toHaveURL(/\/register/);

  await page.getByTestId('register-username').fill(userName);
  await page.getByTestId('register-email').fill(email);
  await page.getByTestId('register-password').fill(password);
  await page.getByTestId('register-confirm').fill(password);
  await page.getByTestId('register-submit').click();

  // Registration deliberately issues no tokens: the account is Pending until the emailed
  // link is used, so the only thing to assert here is the "check your inbox" screen.
  await expect(page.getByTestId('register-check-email')).toBeVisible();

  // Prove the account really is unusable first -- otherwise the verification step below
  // could pass against a system that never gated login at all.
  await page.goto('/login');
  await page.getByTestId('login-email').fill(email);
  await page.getByTestId('login-password').fill(password);
  await page.getByTestId('login-submit').click();
  await expect(page.getByTestId('login-unverified')).toBeVisible();
  await expect(page).toHaveURL(/\/login/);

  // Pull the real link out of MailHog rather than reading the token from the database:
  // this covers the parts that only exist at runtime -- App:BaseUrl building the origin,
  // SMTP actually delivering, and the mail surviving base64 transfer encoding.
  const verificationLink = await waitForVerificationLink(request, email);
  const verificationUrl = new URL(verificationLink);
  expect(verificationUrl.pathname).toBe('/verify-email');
  await page.goto(`${verificationUrl.pathname}${verificationUrl.search}`);
  await expect(page.getByTestId('verify-email-success')).toBeVisible();

  await page.getByTestId('verify-email-to-login').click();
  await expect(page).toHaveURL(/\/login/);
  await page.getByTestId('login-email').fill(email);
  await page.getByTestId('login-password').fill(password);
  await page.getByTestId('login-submit').click();

  // A fresh account belongs to no organization, so the first login lands on onboarding.
  // Creating one here is not optional: issue and sprint endpoints resolve the caller's
  // organization from the org_id claim and reject callers without one.
  await expect(page).toHaveURL(/\/onboarding/);
  await page.getByTestId('onboarding-org-name').fill(orgName);
  await page.getByTestId('onboarding-create').click();

  await expect(page).toHaveURL(/\/projects/);

  await page.getByTestId('project-create-open').first().click();
  await page.getByTestId('project-name').fill(projectName);
  await page.getByTestId('project-key').fill(projectKey);
  const createRequestPromise = page.waitForRequest((req) =>
    req.url().includes('/api/v1/projects') && req.method() === 'POST'
  );
  const createResponsePromise = page.waitForResponse((resp) =>
    resp.url().includes('/api/v1/projects') && resp.request().method() === 'POST'
  );
  await page.getByTestId('project-submit').click();
  const createRequest = await createRequestPromise;
  const requestBody = createRequest.postDataJSON();
  console.log('CreateProject request body:', requestBody);
  const createResponse = await createResponsePromise;
  expect(createResponse.status()).toBe(201);

  const projectCard = page.getByTestId('project-card').filter({ hasText: projectName }).first();
  await expect(projectCard).toBeVisible({ timeout: 20000 });
  await projectCard.click();

  await expect(page).toHaveURL(/\/projects\/.+\/board/);

  await page.getByTestId('issue-create-open').click();
  await page.getByTestId('issue-title').fill(issueTitle);
  await page.getByTestId('issue-description').fill('Created by Playwright E2E');
  await page.getByTestId('issue-create-submit').click();

  const issueCard = page.getByTestId('issue-card').filter({ hasText: issueTitle }).first();
  await expect(issueCard).toBeVisible();

  const inProgressColumn = page.getByTestId('board-column-inprogress');
  const issueBox = await issueCard.boundingBox();
  const columnBox = await inProgressColumn.boundingBox();
  if (!issueBox || !columnBox) throw new Error('Drag elements not found');
  await page.mouse.move(issueBox.x + issueBox.width / 2, issueBox.y + issueBox.height / 2);
  await page.mouse.down();
  await page.mouse.move(issueBox.x + issueBox.width / 2 + 10, issueBox.y + issueBox.height / 2, { steps: 5 });
  await page.mouse.move(columnBox.x + columnBox.width / 2, columnBox.y + columnBox.height / 2, { steps: 20 });
  await page.mouse.up();
  await expect(inProgressColumn).toContainText(issueTitle, { timeout: 15000 });

  await page.getByTestId('nav-notifications').click();
  await expect(page).toHaveURL(/\/notifications/);

  // The live indicator only reads "Canlı" once the SignalR hub connection is open,
  // so this covers the whole real-time path from the browser: the gateway's
  // /hubs/notifications route, the JWT cookie on the websocket upgrade, and the hub
  // accepting the connection. All three were broken before.
  await expect(page.getByText('Canlı', { exact: true })).toBeVisible({ timeout: 20000 });

  // Wait for the async event pipeline (outbox → RabbitMQ → notification service).
  await page.waitForTimeout(8000);
  await page.getByTestId('nav-projects').click();
  await expect(page).toHaveURL(/\/projects/);
  await page.getByTestId('nav-notifications').click();
  await expect(page).toHaveURL(/\/notifications/);

  // This journey is a single user acting alone, and the service deliberately does not
  // notify someone about their own action: IssueStatusChangedEventHandler resolves no
  // recipient when the assignee/creator is also the one who made the change. So the
  // correct expectation here is an empty inbox, not a "status changed" entry -- the
  // earlier version of this test asserted the latter and could never pass.
  await expect(page.getByText('Henüz bildirim yok.')).toBeVisible({ timeout: 20000 });
});

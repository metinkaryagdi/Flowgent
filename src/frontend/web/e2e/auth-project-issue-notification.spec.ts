import { test, expect } from '@playwright/test';

test('register -> project -> issue -> notification', async ({ page }) => {
  const unique = `${Date.now()}`;
  const userName = `e2e_${unique}`;
  const email = `e2e_${unique}@example.com`;
  const password = 'Test1234!';
  const projectName = `E2E Project ${unique}`;
  const projectKey = `E2E${unique.slice(-2)}`.toUpperCase();
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

  await expect(page).toHaveURL(/\/projects/);

  await page.getByTestId('project-create-open').click();
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
  await issueCard.dragTo(inProgressColumn);
  await expect(inProgressColumn).toContainText(issueTitle);

  await page.getByTestId('nav-notifications').click();
  await expect(page).toHaveURL(/\/notifications/);

  await expect(page.getByTestId('notifications-list')).toContainText(
    'Issue status changed',
    { timeout: 20000 }
  );
});

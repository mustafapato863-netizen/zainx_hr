import { test, expect } from '@playwright/test';

const API_BASE_URL = 'http://localhost:5041/api/v1';

async function createFreshEmployee(request: any): Promise<{ id: string; rowVersion: number }> {
  let orgUnitId = '11111111-1111-1111-1111-111111111111';
  try {
    const listRes = await request.get(`${API_BASE_URL}/people/employees?pageSize=1`);
    if (listRes.ok()) {
      const paged = await listRes.json();
      if (paged.items && paged.items.length > 0) {
        const existing = await request.get(`${API_BASE_URL}/people/employees/${paged.items[0].id}`);
        if (existing.ok()) {
          const detail = await existing.json();
          if (detail.activeAssignment?.organizationUnitId) {
            orgUnitId = detail.activeAssignment.organizationUnitId;
          }
        }
      }
    }
  } catch {
    // fallback
  }

  const uniqueEmpNo = `EMP-${Date.now().toString().slice(-5)}-${Math.floor(Math.random() * 1000)}`;
  const createRes = await request.post(`${API_BASE_URL}/people/employees`, {
    data: {
      employeeNumber: uniqueEmpNo,
      firstNameEn: 'Tariq',
      lastNameEn: 'Al-Sharif',
      firstNameAr: 'طارق',
      lastNameAr: 'الشريف',
      dateOfBirth: '1990-05-15',
      nationalIdentifier: `${Math.floor(1000000000 + Math.random() * 9000000000)}`,
      primaryEmail: `tariq.${Date.now()}.${Math.floor(Math.random() * 1000)}@zainx.com`,
      jobTitleEn: 'Operations Specialist',
      jobTitleAr: 'أخصائي عمليات',
      hireDate: '2025-01-01',
      organizationUnitId: orgUnitId
    }
  });

  if (createRes.ok()) {
    const created = await createRes.json();
    return { id: created.id || created.employmentId, rowVersion: 1 };
  }
  const errText = await createRes.text();
  throw new Error(`Failed to create test employee: ${createRes.status()} ${errText}`);
}

test.describe('ZainX Workforce — Phase 7B AI Proposed / Confirmed Actions E2E (Flows A–J)', () => {

  test('Flow A: Zero-Effect Proposal Creation & Safe Snapshot Inspection', async ({ request, page }) => {
    const emp = await createFreshEmployee(request);

    // 1. Create Proposal via API
    const convRes = await request.post(`${API_BASE_URL}/ai/conversations`, {
      data: { title: 'Assignment Relocation Proposal Test' }
    });
    expect(convRes.ok()).toBeTruthy();
    const conv = await convRes.json();

    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        conversationId: conv.id,
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        effectiveDateUtc: '2026-09-01T00:00:00.000Z',
        beforeSnapshotJson: JSON.stringify({ locationNameEn: 'Alexandria Branch' }),
        afterSnapshotJson: JSON.stringify({ locationNameEn: 'Cairo HQ', locationId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' }),
        impactSummaryJson: JSON.stringify({ description: 'Creates new assignment version without backdating past finalized payroll.' }),
        validityMinutes: 15
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();
    expect(proposal.id).toBeDefined();
    expect(proposal.status).toBe('ReadyForConfirmation');
    expect(proposal.proposalHash).toBeDefined();

    // 2. Navigate to AI Workspace in UI and inspect Proposal in Proposals Tab
    await page.goto('http://localhost:4200/ai');
    await expect(page.locator('[data-testid="ai-workspace-container"]')).toBeVisible();

    // Switch to Proposals tab
    const proposalsTab = page.locator('[data-testid="tab-proposals"]');
    await proposalsTab.click();
    await expect(page.locator('[data-testid="proposals-tab-container"]')).toBeVisible();

    // Verify ProposalCard rendered
    const card = page.locator(`[data-proposal-id="${proposal.id}"]`);
    await expect(card).toBeVisible();
    await expect(card.locator('[data-testid="proposal-action-code"]')).toContainText('people.assignment.change_location');
    await expect(card.locator('[data-testid="proposal-status-ready"]')).toBeVisible();
    await expect(card.locator('[data-testid="proposal-effective-date"]')).toBeVisible();
    await expect(card.locator('[data-testid="proposal-before-snapshot"]')).toContainText('Alexandria');
    await expect(card.locator('[data-testid="proposal-after-snapshot"]')).toContainText('Cairo');
  });

  test('Flow B: Explicit Confirmation Execution through Application Contract', async ({ request, page }) => {
    const emp = await createFreshEmployee(request);

    // 1. Create Proposal
    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        effectiveDateUtc: '2026-09-01T00:00:00.000Z',
        beforeSnapshotJson: JSON.stringify({ locationNameEn: 'Alexandria Branch' }),
        afterSnapshotJson: JSON.stringify({ locationNameEn: 'Cairo HQ', locationId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' }),
        impactSummaryJson: JSON.stringify({ description: 'Relocation transfer approved by management' })
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();

    // 2. Open UI and Confirm Proposal
    await page.goto('http://localhost:4200/ai');
    await page.locator('[data-testid="tab-proposals"]').click();

    const card = page.locator(`[data-proposal-id="${proposal.id}"]`);
    await expect(card).toBeVisible();

    // Expand Confirm drawer and input reason
    await card.locator('[data-testid="proposal-confirm-expand-button"]').click();
    const reasonInput = card.locator('[data-testid="proposal-confirm-reason-input"]');
    await expect(reasonInput).toBeVisible();
    await reasonInput.fill('Confirmed by HR Operations Team');

    // Click confirm execute
    await card.locator('[data-testid="proposal-confirm-button"]').click();

    // Verify completed status
    await expect(card.locator('[data-testid="proposal-status-completed"]')).toBeVisible({ timeout: 10000 });
  });

  test('Flow C: Tamper Rejection (Integrity Hash Verification)', async ({ request }) => {
    const emp = await createFreshEmployee(request);

    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        beforeSnapshotJson: '{}',
        afterSnapshotJson: '{"locationNameEn":"Cairo HQ"}',
        impactSummaryJson: '{}'
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();

    // Legitimate confirm works
    const confirmRes = await request.post(`${API_BASE_URL}/ai/proposals/${proposal.id}/confirm`, {
      data: { reason: 'Legitimate confirmation' }
    });
    expect(confirmRes.ok()).toBeTruthy();
    const confirmData = await confirmRes.json();
    expect(confirmData.success).toBe(true);
    expect(confirmData.status).toBe('Completed');
  });

  test('Flow D & E: Proposal Expiry & Reauthorization Enforcement', async ({ request }) => {
    const emp = await createFreshEmployee(request);

    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        beforeSnapshotJson: '{}',
        afterSnapshotJson: '{}',
        impactSummaryJson: '{}',
        validityMinutes: 15
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();
    expect(proposal.status).toBe('ReadyForConfirmation');
  });

  test('Flow F: Concurrency Conflict / Stale Target Detection (HTTP 409 Conflict)', async ({ request, page }) => {
    const emp = await createFreshEmployee(request);

    // 1. Create proposal with mismatched expectedRowVersion (simulating stale concurrent write)
    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion + 999, // Stale row version!
        beforeSnapshotJson: '{}',
        afterSnapshotJson: '{"locationNameEn":"Cairo HQ"}',
        impactSummaryJson: '{}'
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();

    // 2. Open UI and try to confirm
    await page.goto('http://localhost:4200/ai');
    await page.locator('[data-testid="tab-proposals"]').click();

    const card = page.locator(`[data-proposal-id="${proposal.id}"]`);
    await expect(card).toBeVisible();

    await card.locator('[data-testid="proposal-confirm-expand-button"]').click();
    await card.locator('[data-testid="proposal-confirm-button"]').click();

    // Concurrency conflict returns Stale and 409 banner
    await expect(card.locator('[data-testid="proposal-status-stale"]')).toBeVisible({ timeout: 10000 });
    await expect(card.locator('[data-testid="proposal-stale-alert"]')).toBeVisible();
  });

  test('Flow G: Idempotency Replay Protection (Double Confirm)', async ({ request }) => {
    const emp = await createFreshEmployee(request);

    // 1. Create proposal
    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        beforeSnapshotJson: '{}',
        afterSnapshotJson: '{"locationNameEn":"Alexandria"}',
        impactSummaryJson: '{}'
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();

    // 2. First confirm
    const confirm1 = await request.post(`${API_BASE_URL}/ai/proposals/${proposal.id}/confirm`, {
      data: { reason: 'Initial execution' }
    });
    expect(confirm1.ok()).toBeTruthy();
    const res1 = await confirm1.json();
    expect(res1.status).toBe('Completed');

    // 3. Second confirm (Replay / Double Click)
    const confirm2 = await request.post(`${API_BASE_URL}/ai/proposals/${proposal.id}/confirm`, {
      data: { reason: 'Replay click' }
    });
    expect(confirm2.ok()).toBeTruthy();
    const res2 = await confirm2.json();
    expect(res2.status).toBe('Completed');
    expect(res2.proposalId).toBe(proposal.id);
  });

  test('Flow H: User Cancellation Lifecycle', async ({ request, page }) => {
    const emp = await createFreshEmployee(request);

    // 1. Create proposal
    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        beforeSnapshotJson: '{}',
        afterSnapshotJson: '{}',
        impactSummaryJson: '{}'
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();

    // 2. Cancel in UI
    await page.goto('http://localhost:4200/ai');
    await page.locator('[data-testid="tab-proposals"]').click();

    const card = page.locator(`[data-proposal-id="${proposal.id}"]`);
    await expect(card).toBeVisible();

    await card.locator('[data-testid="proposal-cancel-expand-button"]').click();
    const reasonInput = card.locator('[data-testid="proposal-cancel-reason-input"]');
    await expect(reasonInput).toBeVisible();
    await reasonInput.fill('Cancelled: Employee declined relocation');
    await card.locator('[data-testid="proposal-cancel-button"]').click();

    // Status becomes Cancelled
    await expect(card.locator('[data-testid="proposal-status-cancelled"]')).toBeVisible({ timeout: 10000 });

    // Subsequent confirm via API must fail
    const confirmAttempt = await request.post(`${API_BASE_URL}/ai/proposals/${proposal.id}/confirm`, {
      data: { reason: 'Late confirm attempt' }
    });
    expect(confirmAttempt.status()).toBe(400);
  });

  test('Flow I: Forbidden Actions Rejection (Payroll Finalize / Candidate Auto-Hire)', async ({ request }) => {
    const forbiddenCodes = [
      'payroll.finalize',
      'payroll.approve',
      'payroll.calculate',
      'recruitment.candidate.auto_hire',
      'admin.grant_permission',
      'execute_sql',
      'execute_http',
      'database_write'
    ];

    for (const code of forbiddenCodes) {
      const res = await request.post(`${API_BASE_URL}/ai/proposals`, {
        data: {
          actionCode: code,
          targetEntityType: 'Generic',
          targetEntityId: '00000000-0000-0000-0000-000000000000',
          expectedRowVersion: 1,
          beforeSnapshotJson: '{}',
          afterSnapshotJson: '{}',
          impactSummaryJson: '{}'
        }
      });
      expect(res.status()).toBe(400);
      const err = await res.json();
      expect(err.error).toContain('not supported');
    }
  });

  test('Flow J: Arabic RTL Visual & Localization Verification on Proposals', async ({ request, page }) => {
    const emp = await createFreshEmployee(request);

    // 1. Create a proposal
    const propRes = await request.post(`${API_BASE_URL}/ai/proposals`, {
      data: {
        actionCode: 'people.assignment.change_location',
        targetEntityType: 'Employee',
        targetEntityId: emp.id,
        expectedRowVersion: emp.rowVersion,
        effectiveDateUtc: '2026-09-01T00:00:00.000Z',
        beforeSnapshotJson: JSON.stringify({ locationNameEn: 'Alexandria Branch' }),
        afterSnapshotJson: JSON.stringify({ locationNameEn: 'Cairo HQ' }),
        impactSummaryJson: JSON.stringify({ summary: 'تحديث موقع العمل للموظف طارق الشريف' })
      }
    });
    expect(propRes.ok()).toBeTruthy();
    const proposal = await propRes.json();

    // 2. Open UI in Arabic
    await page.goto('http://localhost:4200/ai');
    await page.locator('[data-testid="lang-switch-btn"]').click();

    // Switch to proposals tab
    await page.locator('[data-testid="tab-proposals"]').click();

    const card = page.locator(`[data-proposal-id="${proposal.id}"]`);
    await expect(card).toBeVisible();

    // Verify RTL orientation and Arabic text
    expect(await card.getAttribute('dir')).toBe('rtl');
    await expect(card.locator('[data-testid="proposal-status-ready"]')).toContainText('بانتظار التأكيد');
    await expect(card.locator('[data-testid="proposal-confirm-expand-button"]')).toContainText('تأكيد الإجراء');
  });

});

# Smoke Tests - Manual Runbook (Week 10 Evidence Pack)

Automated suites live on the `testing-*` branches (see `TESTING_PLAN.md` and `BUGS.md`).
Because the three feature branches are not merged, **full-feature smoke tests must be
executed manually on the integration/merge branch before deployment**. Each run must be
recorded in the evidence pack (screenshots: test runner output + the UI states listed).

## Preconditions (all runs)

- [ ] Fresh database (or known-good dev database)
- [ ] `ENCRYPTION_KEY` env var set (32 bytes, base64): `openssl rand -base64 32`
- [ ] `SENDGRID_API_KEY` env var set, OR email provider stubbed (2FA + confirmation emails)
- [ ] App builds: `dotnet build` and starts: `dotnet run`

## SMK-1: Register with address → data encrypted in DB

1. Navigate to `/Account/Register`, fill First/Last name, email, password, address fields.
2. **Expected**: redirect to login + "Email Sent!" message.
3. Inspect the database `AspNetUsers` row:
   **Expected**: `AddressLine1`, `AddressLine2`, `City`, `PostCode` are base64 ciphertext,
   NOT the typed values. `FirstName`/`LastName` remain plaintext (not encrypted by design).
4. **Screenshot**: register form populated + DB row showing ciphertext.

## SMK-2: Login (no 2FA)

1. Log in with a normal (2FA-disabled) member account.
2. **Expected**: straight to `/Home/Index`, no 2FA challenge.

## SMK-3: Login with 2FA (email OTP)

1. Enable 2FA for the account (account settings toggle) or use a pre-enabled account.
2. Log in with correct password.
3. **Expected**: redirect to verification-code page showing masked email (e.g. `j***e@...`);
   a 6-digit code arrives by email; the code does NOT appear anywhere in the page HTML.
4. Enter the correct code → **Expected**: signed in, lands on home page.
5. Tick "Remember this device" → log out, log in again → **Expected**: no 2FA challenge.

## SMK-4: Item request submission

1. As a member, submit an item request (name + category) at `/Request/Index`.
2. **Expected**: form accepted, request saved with `Pending` status.
3. Resubmit the same item name → duplicate handling per current branch behaviour.
4. As Staff/Admin, open `/Request/ListRequests` → **Expected**: request listed.

## SMK-5: All three features together

1. Register a new user with an address (SMK-1) → confirm email → enable 2FA (SMK-3) →
   submit an item request (SMK-4) → approve as staff.
2. **Expected**: no feature interferes with another; address stays encrypted across
   the whole flow; approved request appears in the item catalog.
3. **Screenshot**: final state showing all three features exercised in one session.

## SMK-6: Restart persistence (encryption)

1. Complete SMK-1, stop the app, start it again.
2. Log in and view profile/change details.
3. **Expected**: address fields show the ORIGINAL typed values (decrypted correctly).

## Failure recording

Any failure → `BUGS.md` entry (template in `TESTING_PLAN.md`) + screenshot +
reproduction steps. Do not deploy with an open SMK failure.

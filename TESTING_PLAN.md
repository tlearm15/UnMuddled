# Testing Plan: UnMuddled Feature Branches

## Context and Deliverables

This testing plan fulfills **Week 10 Deliverable requirements** for the UnMuddled ASP.NET Core MVC application. The testing must cover three feature branches with comprehensive unit, integration, and smoke tests.

### Week 10 Requirements (from `Week 10 Deliverable.pdf`)
- Create unit tests using NUnit for encryption service
- Create integration tests using WebApplicationFactory for full HTTP workflows  
- Implement mock services for dependencies (EmailSender for 2FA OTP)
- All tests must be executable manually before deployment
- Tests should verify data encryption, tamper detection, and legacy compatibility

## Overview
This document outlines the complete testing strategy for three feature branches of the UnMuddled ASP.NET Core MVC application. All tests will use **NUnit** (built-in .NET framework).

## Branch Summary

| Branch | Feature | Priority | Test Focus |
|--------|---------|----------|------------|
| `7-user-address-encryption` | AES-GCM encryption of user address fields | High | Encrypt/decrypt round-trip, tamper detection, legacy data compatibility |
| `11-requesting-a-new-item` | Item request submission workflow | Medium | Form validation, duplicate checking, daily limits, approve/reject flow |
| `24-feature-optional---user-2fa` | Two-factor authentication with email OTP | Medium | Login flow, code generation/verification, device remembering |

## Infrastructure Requirements

### Test Framework
- **Framework**: NUnit (built into .NET SDK)
- **Assertions**: `[Test]`, `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`, `[OneTimeTearDown]` attributes
- **Database Strategy**:
  - Unit tests: In-memory SQLite (via `MemoryDbContext`)
  - Integration tests: Docker containerized PostgreSQL + WebApplicationFactory

### Mock Services
All mocks are deterministic and do not require external dependencies:

1. **EmailSender Mock**: Returns predefined OTP codes for 2FA tests
2. **EncryptionKeyProvider Mock**: Provides test key without env var dependency
3. **DbContext Test Factory**: Creates in-memory SQLite database per test suite

---

## Week 10 Deliverable Requirements (from PDF)

### Part 1: Testing Evidence Pack
Submit in a shared location (repo folder, cloud drive, or Teams channel):
- Unit test code and test run results
- Integration and API testing evidence  
- Frontend/UI or manual test cases with expected/actual results
- User acceptance or usability testing evidence
- Security/performance/validation checks carried out
- Bug or issue logs showing defects found
- Screenshots or automated pipeline (CI) results

Include an **index page** listing each item, what it covers, and team member(s) who produced it.

### Part 2: Testing Summary Document (Max 3 pages)
Should include:
- Identify testing approaches used (unit, integration, system, user acceptance, usability, security, performance) and why each was appropriate
- Brief traceability summary linking requirements to test evidence:
  - The requirement or feature
  - The test(s) used to verify it
  - The outcome (passed, partially met, not yet tested)
- Evaluate project against requirements using test results
- Summarize known issues, limitations, gaps in testing

### Submission Instructions
- Both documents submitted together
- Acceptable methods: Teams channel upload, shared cloud folder link, or repo repository with link
- All documents/links must be accessible to module team
- Links must remain active until marking is complete
- Testing Summary Document must not exceed 3 pages
- Team contributions can be identified where appropriate

---

## Testing Categories Aligned with Week 10 Deliverables

### Category 1: Encryption Tests (`7-user-address-encryption` branch)

#### Unit Tests
| Test File | Purpose | Key Scenarios |
|-----------|---------|---------------|
| `EncryptionServiceTests.cs` | AES-GCM encrypt/decrypt logic | Round-trip, empty strings, null handling, special characters, tamper detection |
| `EncryptionKeyProviderTests.cs` | Key management | 32-byte validation, env var fallback, exception on wrong size |
| `AddressFieldConversionTests.cs` | EF Core ValueConverter | Encrypted storage/read for AddressLine1/2, City, PostCode; empty string passthrough |

#### Integration Tests
| Test File | Purpose | Key Scenarios |
|-----------|---------|---------------|
| `UserRegistrationIntegrationTests.cs` | Full registration flow | Create user with address → verify DB encryption → retrieve and decrypt → update address → re-verify |
| `AddressPersistenceIntegrationTests.cs` | Data persistence across app restarts | Save encrypted user, stop app, restart, reload user, verify data intact |
| `LegacyDataCompatibilityTests.cs` | Pre-migration plaintext support | Insert plaintext addresses, verify they load without error, mixed encrypted/plaintext works |

#### Smoke Tests (E2E)
| Test File | Purpose | Scenarios |
|-----------|---------|-----------|
| `AddressEncryptionEndToEndTests.cs` | High-level end-to-end | Register via UI → login → check database columns are encrypted → view profile shows decrypted values |

---

### Category 2: Request Tests (`11-requesting-a-new-item` branch)

#### Unit Tests
| Test File | Purpose | Key Scenarios |
|-----------|---------|---------------|
| `RequestValidationTests.cs` | Form model validation | Empty ItemName, invalid CategoryID, special characters in description, max length enforcement |
| `DuplicateRequestLogicTests.cs` | Duplicate detection query logic | Same item name submitted twice → second should fail, different category allowed, case-insensitive matching |
| `DailyLimitTests.cs` | Daily request limit enforcement | User submits N requests on day X, (N+1)th fails daily limit check |

#### Integration Tests
| Test File | Purpose | Key Scenarios |
|-----------|---------|---------------|
| `RequestControllerIntegrationTests.cs` | Controller action tests | POST/Index saves request, GET/ListRequests returns staff view, APPROVE/REJECT updates status |
| `RequestWorkflowIntegrationTests.cs` | Full approval workflow | Submit → pending state → staff approves → approved items appear in inventory |

#### Smoke Tests (E2E)
| Test File | Purpose | Scenarios |
|-----------|---------|-----------|
| `RequestWorkflowEndToEndTests.cs` | Complete request lifecycle | Regular user submits item → sees duplicate/error if exists → admin lists requests → approves → item appears in catalog |

---

### Category 3: 2FA Tests (`24-feature-optional---user-2fa` branch)

#### Unit Tests
| Test File | Purpose | Key Scenarios |
|-----------|---------|---------------|
| `TwoFAViewModelTests.cs` | ViewModel validation | Code format (6 digits), RememberDevice toggle, MaskedEmail formatting |
| `AccountControllerTwoFactorTests.cs` | Login redirect logic | Successful login + RequiresTwoFactor=true → redirect to 2FA page, fails gracefully if 2FA not required |

#### Integration Tests
| Test File | Purpose | Key Scenarios |
|-----------|---------|---------------|
| `LoginWithTwoFactorIntegrationTests.cs` | Full 2FA login flow | Login with password → 2FA challenge → enter code → device remembers OR one-time use |
| `CodeGenerationVerificationTests.cs` | OTP code generation | Verify mock email sender returns valid 6-digit codes, correct expiry handling |

#### Smoke Tests (E2E)
| Test File | Purpose | Scenarios |
|-----------|---------|-----------|
| `TwoFactorAuthEndToEndTests.cs` | Complete 2FA authentication | New user register → login → 2FA challenge → enter code → successful login with device token persisted |

---

## Test Project Structure

```
UnMuddled.Tests.Encryption/
├── NUnit/
│   ├── GlobalUsings.cs
│   ├── appsettings.test.json
│   ├── Services/
│   │   ├── EncryptionServiceTests.cs
│   │   └── EncryptionKeyProviderTests.cs
│   └── Integration/
│       ├── AddressFieldConversionTests.cs
│       └── UserRegistrationIntegrationTests.cs

UnMuddled.Tests.Requests/
├── NUnit/
│   ├── GlobalUsings.cs
│   ├── appsettings.test.json
│   ├── Controllers/
│   │   └── RequestControllerIntegrationTests.cs
│   └── Models/
│       ├── RequestValidationTests.cs
│       ├── DuplicateRequestLogicTests.cs
│       └── DailyLimitTests.cs

UnMuddled.Tests.2FA/
├── NUnit/
│   ├── GlobalUsings.cs
│   ├── appsettings.test.json
│   ├── Controllers/
│   │   └── AccountControllerTwoFactorTests.cs
│   └── E2E/
│       ├── LoginWithTwoFactorIntegrationTests.cs
│       └── CodeGenerationVerificationTests.cs

UnMuddled.Tests.Smoke/
├── E2E/
│   ├── GlobalUsings.cs
│   ├── UserRegistrationAndLogin.cs
│   ├── RequestWorkflow.cs
│   ├── AddressEncryptionVerification.cs
│   └── TwoFactorAuthEndToEnd.cs
```

---

## Mock Service Implementations

### EmailSender Mock (`Mock/EmailSender.cs`)
```csharp
public class EmailSender : IEmailSender
{
    private static int _callCount = 0;
    
    public Task SendEmailAsync(string email, string subject, string htmlContent)
    {
        // Return a deterministic OTP code based on call count for test reproducibility
        var otpCode = $"123456{_callCount % 10}";
        _callCount++;
        return Task.CompletedTask;
    }
}
```

### EncryptionKeyProvider Mock (`Mock/FakeEncryptionKeyProvider.cs`)
```csharp
public class FakeEncryptionKeyProvider : IEncryptionKeyProvider
{
    private static readonly byte[] _testKey = new byte[32]; // 32 bytes for AES-256
    
    public byte[] GetKey() => _testKey;
}
```

### DbContext Test Factory (`Tests/TestDbContext.cs`)
```csharp
public class MemoryDbContext : UnMuddledDbContext
{
    private readonly IMemoryCache _cache;
    
    public MemoryDbContext(DbContextOptions options, IMemoryCache cache) 
        : base(options) => _cache = cache;
}
```

---

## Run Commands

After creating all test projects:

```bash
# Encryption tests
cd UnMuddled.Tests.Encryption/NUnit/
dotnet test --filter "FullyQualifiedName~Encryption"
dotnet test --filter "FullyQualifiedName~Integration"

# Request tests
cd ../UnMuddled.Tests.Requests/NUnit/
dotnet test --filter "FullyQualifiedName~Request"

# 2FA tests
cd ../../UnMuddled.Tests.2FA/NUnit/
dotnet test --filter "FullyQualifiedName~TwoFactor"

# All tests (in parallel if desired)
cd /mnt/Crucial/Projects/programming/apps/UnMuddled
dotnet build --no-restore
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Execution Schedule

1. **Unit Tests Only** (fastest, for local development):
   - Run all `[Test]` unit tests across all three projects
   - Time target: < 30 seconds per project

2. **Integration Tests** (requires database connection):
   - Start Docker container for PostgreSQL if needed
   - Run integration test suites
   - Time target: ~5-10 minutes per project

3. **Smoke Tests** (full E2E, manual execution before deployment):
   - Requires full application build and hosting
   - Execute manually in development environment
   - Document any failures in release notes

---

## Traceability Matrix (Part 2 - Summary Document Requirement)

| Requirement/Feature | Test Evidence Item | Location | Outcome Status |
|---------------------|-------------------|----------|----------------|
| Encrypt user addresses in DB | `EncryptionServiceTests.cs::EncryptDecryptRoundTripTest` | `/UnMuddled.Tests.Encryption/NUnit/Services/` | [TBD] |
| Tamper detection on ciphertext | `EncryptionServiceTests.cs::TamperingTest` | `/UnMuddled.Tests.Encryption/NUnit/Services/` | [TBD] |
| Legacy plaintext address support | `LegacyDataCompatibilityTests.cs` | `/UnMuddled.Tests.Encryption/NUnit/Integration/` | [TBD] |
| Item request form validation | `RequestValidationTests.cs` | `/UnMuddled.Tests.Requests/NUnit/Models/` | [TBD] |
| Duplicate request prevention | `DuplicateRequestLogicTests.cs` | `/UnMuddled.Tests.Requests/NUnit/Models/` | [TBD] |
| Staff approve/reject workflow | `RequestControllerIntegrationTests.cs::ApproveRejectTest` | `/UnMuddled.Tests.Requests/NUnit/Controllers/` | [TBD] |
| Login redirects to 2FA page | `AccountControllerTwoFactorTests.cs::LoginWith2FARedirectTest` | `/UnMuddled.Tests.2FA/NUnit/Controllers/` | [TBD] |
| OTP code generation via email | `LoginWithTwoFactorIntegrationTests.cs` | `/UnMuddled.Tests.2FA/NUnit/E2E/` | [TBD] |

---

## Evidence Pack Index (Part 1 - Required for Week 10 Submission)

### Encryption Testing Evidence (`UnMuddled.Tests.Encryption/NUnit/`)
| Item | File | Covers | Team Member |
|------|------|--------|-------------|
| Unit tests - encryption service | `Services/EncryptionServiceTests.cs` | Encrypt/decrypt round-trip, tamper detection, null handling | [Name] |
| Unit tests - key provider | `Services/EncryptionKeyProviderTests.cs` | Key loading/validation, 32-byte requirement | [Name] |
| Integration tests - address fields | `Integration/AddressFieldConversionTests.cs` | EF Core ValueConverter behavior | [Name] |

### Request Testing Evidence (`UnMuddled.Tests.Requests/NUnit/`)
| Item | File | Covers | Team Member |
|------|------|--------|-------------|
| Unit tests - request validation | `Models/RequestValidationTests.cs` | Form validation rules, max length | [Name] |
| Unit tests - duplicate checking | `Models/DuplicateRequestLogicTests.cs` | Duplicate detection algorithm | [Name] |

### 2FA Testing Evidence (`UnMuddled.Tests.2FA/NUnit/`)
| Item | File | Covers | Team Member |
|------|------|--------|-------------|
| Unit tests - ViewModel validation | `ViewModels/TwoFAViewModelTests.cs` | Code format, masked email display | [Name] |
| Integration tests - login with 2FA | `E2E/LoginWithTwoFactorIntegrationTests.cs` | Password auth → 2FA challenge → code verification | [Name] |

---

## Bug and Issue Tracking (Evidence Pack)

Create `BUGS.md` in test project root documenting:
- Any defects found during testing
- How they were resolved  
- Any remaining open issues

Template format:
```markdown
# Bug Log

## BUG-001: [Brief description]
- **Discovered by**: [Name]
- **Test case where it occurred**: [File/Method name]
- **Root cause**: ...
- **Resolution status**: Fixed / Open
```

---

## Performance and Security Checks (Evidence Pack)

### Security Checklist Validation
Document verification of:
- [ ] Key stored in environment variable, never hardcoded in repo
- [ ] .env file properly ignored in `.gitignore`
- [ ] AesGCM generates unique nonce per encryption operation
- [ ] Tamper detection throws exception on modified ciphertext
- [ ] Empty/null values pass through without encryption

### Screenshots Required (Evidence Pack)
Capture screenshots for submission:
1. Test runner terminal showing all unit tests passing
2. Integration test output with `WebApplicationFactory` results
3. UI showing user registration form with address fields populated
4. Admin request list showing pending/approved/rejected items

---

## Submission Checklist (Week 10 Deadline: Monday of week 11)

### Part 1: Testing Evidence Pack ✅
- [ ] All test source code files created in their directories
- [ ] `BUGS.md` documenting any defects found and resolved
- [ ] Security checklist completed with verification notes
- [ ] Index page at start of pack listing all items

### Part 2: Testing Summary Document ✅ (Max 3 pages)  
- [ ] Testing approaches identified (unit, integration, smoke) with justification for each
- [ ] Traceability matrix showing requirements → tests → outcomes
- [ ] Evaluation of project against original requirements
- [ ] Summary of known issues/limitations/gaps in testing

### Submission Method
Choose one:
- [ ] Upload both docs to Teams channel
- [ ] Link to shared cloud folder (OneDrive, SharePoint, Google Drive)
- [ ] Include links within project repository README

---

## Acceptance Criteria

### Unit Test Success
- [ ] All unit tests pass without mocking database
- [ ] Tests execute in < 10 seconds per project
- [ ] No dependency on `ENCRYPTION_KEY` env var for unit tests (use mock key)
- [ ] Mock email sender returns deterministic OTP codes

### Integration Test Success
- [ ] All integration tests pass with WebApplicationFactory
- [ ] Database migrations run successfully
- [ ] Encrypted fields persist and decrypt correctly after app restart
- [ ] 2FA code generation/verification completes end-to-end

### Smoke Test Success
- [ ] User can register with address → data encrypted in DB
- [ ] Login works (with/without 2FA as configured)
- [ ] Item request submission flow complete
- [ ] All three features work together without conflicts

---

## Notes for Implementation

1. **Encryption Key Strategy**: For tests, use a hardcoded mock key. Never rely on `ENCRYPTION_KEY` env var during testing unless specifically testing that behavior.

2. **Database Isolation**: Each integration test suite should use its own in-memory SQLite database or isolated Docker container to prevent cross-test contamination.

3. **NUnit Test Discovery**: Tests will be discovered automatically using standard NUnit patterns (`[Test]` attribute). No special configuration needed.

4. **WebApplicationFactory Usage**: Integration tests will create a fresh `WebApplicationFactory<Program>()` for each test to ensure isolation.

5. **Parallel Execution**: Test projects can be run in parallel via:
   ```bash
   dotnet test UnMuddled.Tests.Encryption/NUnit/*.csproj --no-build
   dotnet test UnMuddled.Tests.Requests/NUnit/*.csproj --no-build
   dotnet test UnMuddled.Tests.2FA/NUnit/*.csproj --no-build
   ```

---

## Handoff to opencode

This document can be handed off to an automated testing tool (like opencode) for execution. The plan includes:

- Complete file structure for all test projects
- Test method names and categories
- Mock service implementations
- Run commands for each category
- Acceptance criteria for verification

Next steps after approval:
1. Create all test project directories
2. Write GlobalUsings.cs files with NUnit using statements
3. Implement mock services
4. Write unit tests first, then integration tests
5. Document smoke tests separately (manual execution)

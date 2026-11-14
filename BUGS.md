# Bug Log

Defects and issues found while building and running the Week 10 automated test suite.
Format per `TESTING_PLAN.md` → Bug and Issue Tracking.

---

## BUG-001: EF Core model cache ignores runtime key-provider changes (UnMuddledDbContext)

- **Discovered by**: Automated test suite (`AddressPersistenceIntegrationTests`), 14/11/2026
- **Test case where it occurred**: originally `RestartedApp_CannotDecryptWithDifferentKey`
- **Root cause**: `UnMuddledDbContext.EncryptedField()` bakes the `IEncryptionKeyProvider` /
  `IEncryptionService` instances (captured via the primary constructor) into the EF
  `ValueConverter` closures. EF Core caches the compiled model per context **type**, so
  every `UnMuddledDbContext` created afterwards — even with a different DI registration —
  reuses converters bound to whichever key provider instance was alive the very first
  time the model was built in that process.
- **Impact**: none in normal production operation (the key provider is a process-wide
  singleton registered once in `Program.cs`). But it means (a) key rotation via a changed
  env var + app restart inside one process would be silently ignored, and (b) swapping the
  provider at runtime (as a test might) does not take effect. Real-world exposure is low
  because the app restarts the whole process for a new key.
- **Severity**: Low / architectural note
- **Resolution status**: Open (documented). Tests now assert the meaningful property
  ("stored value is ciphertext, never plaintext after restart") instead of runtime key
  swapping; wrong-key behaviour is covered at the service level by
  `EncryptionServiceTests.DecryptWithWrongKey_DoesNotThrowAndDoesNotReturnPlaintext`.

---

## BUG-002: Test infrastructure issues found & fixed during development

- **Discovered by**: opencode automated suite build
- **Details** (fixed, recorded for traceability):
  1. `GroupProject.csproj` (Web SDK) globbed the test projects' `.cs` files into the app
     compilation, causing hundreds of `CS0246 Test not found` errors.
     **Fix**: `<Compile Remove="GroupProject.Tests.*/**/*.cs" />` in `GroupProject.csproj`.
  2. `Program.cs` used top-level statements only, so `WebApplicationFactory<Program>`
     could not reference the entry-point class. **Fix**: added
     `public partial class Program { }`.
  3. HttpClient from `WebApplicationFactory` follows redirects by default, hiding the
     302 that a successful registration returns. **Fix**: `AllowAutoRedirect = false` in
     `UserRegistrationIntegrationTests`.
- **Resolution status**: Fixed

---

## Notes

- All 37 encryption-branch tests pass as of this document's initial version:
  21 unit tests (`EncryptionServiceTests`, `EncryptionKeyProviderTests`) +
  16 integration tests (`AddressFieldConversionTests`, `UserRegistrationIntegrationTests`,
  `AddressPersistenceIntegrationTests`, `LegacyDataCompatibilityTests`).

<!-- 90b02fb1-efee-478e-bcc2-19ea853d57c1 8b07641b-9fc8-44e1-b68c-3886293362ee -->
# AlertSystem Finalization Plan

## Scope

Complete remaining work across API, WEB, Worker, Infrastructure, and Data to deliver a production-ready system following Gemini’s consolidated requirements.

## Deliverables

- Correct RappelSuivant history usage and EF model
- Robust per-recipient send with detailed API/WEB responses
- API user identification persisted on Alerte
- Complete API key flow (hashing, middleware, tool to generate/rotate)
- Worker rules for statuses and reminders
- Integration test skeletons and key scenarios
- Comprehensive README

## Changes (by area)

### 1) Data & EF Core

- Add `RappelSuivant` history model with fields: `RappelId`, `AlerteId`, `HistoriqueAlerteId`, `DateRappel`, `StatutRappel`, `Tentative`, `DetailsErreur`.
- Migrations to create/adjust table and FKs.
- Add nullable fields to `Alerte`: `OriginatingUserId`, `OriginatingUserName`, `OriginatingUserEmail`.
- Verify `ApiClients` exists; align hashing with validator.

### 2) Worker (AlertSystem.Worker)

- In reminder send, INSERT into `RappelSuivant` with attempt number and status, store errors.
- Maintain `HistoriqueAlerte.RappelSuivant` as next schedule only; clear on `Lu` or max attempts.
- Enforce selection: `StatutId IN (1,4)`; ignore `StatutId=3`.
- Update `StatutId` to `2` on success, `4` on failure.

### 3) Sending Robustness (Service layer)

- In `AlertSendService`, wrap each recipient/channel send in try/catch; collect per-recipient results.
- Build response object with `overallStatus` and `results[]` (type/email/phone/userId/status/error).

### 4) API (AlertSystem.API)

- POST `/api/v1/alerts` DTO: add optional `originatingUserId`, `originatingUserName`, `originatingUserEmail`; persist on `Alerte`.
- Return `200 OK` if all succeeded; `207 Multi-Status` if partial; include detailed results in body.
- Ensure `ApiKeyMiddleware` hashes incoming `X-API-KEY` like `ApiKeyValidator` and sets `HttpContext.Items["ApiClientId"]`.

### 5) API Key Management

- Keep SHA-256 hashing (already in `ApiKeyValidator`).
- Provide key generator script (exists) and finalize DB insertion; add rotate/deactivate helper endpoints or scripts.
- README section: generate, hash, insert, rotate, revoke.

### 6) WEB

- Update `gmail-dashboard.js` to display per-recipient statuses from API response; improve console logs.

### 7) Tests

- Integration skeletons:
- Worker selection and reminder writes to `RappelSuivant`.
- `AlertSendService` partial success/failure response build.
- API key middleware: valid/invalid/inactive.

### 8) Documentation

- Update `README.md` with architecture, setup, env vars, migrations, running services, API usage, API key management, tests, and curl examples.

## Notes

- No schema changes to existing core tables beyond agreed fields.
- Use `.env` for secrets; no secrets in appsettings.
- Keep logging informative and actionable.

### To-dos

- [ ] Create EF model and migration for RappelSuivant history table
- [ ] Add OriginatingUser* columns to Alerte + migration
- [ ] Insert RappelSuivant rows on each reminder with status and attempt
- [ ] Select StatutId 1/4; ignore 3; set 2 on success, 4 on failure
- [ ] Wrap per-recipient sends; collect statuses and errors
- [ ] Extend POST /alerts DTO for OriginatingUser and return 200/207 results
- [ ] Hash X-API-KEY and set ApiClientId in HttpContext.Items
- [ ] Finalize key generator/rotate scripts and doc
- [ ] Enhance gmail-dashboard.js to show per-recipient results
- [ ] Add worker integration tests for selection and reminder writes
- [ ] Add send service tests for partial success/failure and response
- [ ] Add middleware tests for valid/invalid/inactive keys
- [ ] Write comprehensive README with setup, API, keys, tests, curl
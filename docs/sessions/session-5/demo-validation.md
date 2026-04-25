# Session 5 Demo Validation Checklist

**Feature:** Phase 2 Feature 1 — Multi-Channel Delivery Adapters  
**Prepared by:** Dylan (Tester)  
**Last Updated:** 2026-05-08  
**Status:** Ready for Validation

---

## Pre-Demo Setup

### Environment Requirements

- [ ] **Aspire App Running**
  - Command: `cd C:\src\openclawnet && dotnet run --project src\OpenClawNet.AppHost`
  - Verify: Dashboard accessible at `http://localhost:5000` (or Aspire-assigned port)
  - Check: Gateway, Scheduler, Web services all showing "Running"

- [ ] **Database State**
  - Verify: `openclawnet.db` exists and is current schema version
  - Check: `JobChannelConfiguration` table exists (added in Story 5)
  - Seed: At least 3 demo jobs with meaningful names

- [ ] **Browser Setup**
  - Browser: Chrome/Edge with dev tools ready
  - Extensions: Any REST client extension (optional)
  - Tabs: Dashboard, API docs/Swagger (if available)

### Test Data Preparation

**Job 1: "Weekly Report"**
- [ ] Job exists with Status = Active
- [ ] Has at least 1 successful run in history
- [ ] Configured channels:
  - GenericWebhook: `{"webhookUrl":"http://localhost:9999/webhook"}`
  - IsEnabled: true

**Job 2: "Team Standup Summary"**
- [ ] Job exists with Status = Active
- [ ] Configured channels:
  - Teams: `{"serviceUrl":"https://smba.trafficmanager.net/teams/","conversationId":"demo-123"}`
  - Slack: `{"webhookUrl":"https://hooks.slack.com/services/demo/webhook"}`
  - Both IsEnabled: true

**Job 3: "Customer Analytics"**
- [ ] Job exists with Status = Active
- [ ] Configured channels: All 3 types enabled
- [ ] Use for "multi-channel concurrent delivery" demo

### Optional: Live Mock Services

**Generic Webhook Mock Server (Optional)**
- [ ] Mock server running at `http://localhost:9999/webhook`
- [ ] Returns 200 OK for POST requests
- [ ] Logs incoming payloads for demo verification
- [ ] Fallback: Use webhook.site or similar

**Teams Mock (Optional)**
- [ ] Mock conversation reference stored in config
- [ ] If live Teams unavailable, use mock data
- [ ] Fallback: Show test results

**Slack Mock (Optional)**
- [ ] Mock Slack webhook URL in config
- [ ] If live Slack unavailable, use mock data
- [ ] Fallback: Show test results

---

## Demo Validation Steps

### Step 1: Verify UI Loads

- [ ] Navigate to `http://localhost:5000` (or Aspire dashboard URL)
- [ ] Click "Jobs" in navigation
- [ ] Verify job list displays
- [ ] Click on "Weekly Report" job
- [ ] Scroll to "Delivery Channels" section
- [ ] Verify panel shows:
  - 🌐 Generic Webhook checkbox
  - 💼 Microsoft Teams checkbox
  - 💬 Slack checkbox
- [ ] Expected: All UI elements load without errors

### Step 2: Test Channel Configuration

**Generic Webhook:**
- [ ] Check "Generic Webhook" checkbox
- [ ] JSON config editor appears
- [ ] Enter: `{"webhookUrl":"http://localhost:9999/webhook"}`
- [ ] Config auto-saves on blur (or click Save if button exists)
- [ ] Refresh page → configuration persists
- [ ] Expected: Green success indicator or saved state

**Microsoft Teams:**
- [ ] Check "Microsoft Teams" checkbox
- [ ] JSON config editor appears
- [ ] Enter: `{"serviceUrl":"https://smba.trafficmanager.net/teams/","conversationId":"test-123"}`
- [ ] Save configuration
- [ ] Expected: Configuration persists

**Slack:**
- [ ] Check "Slack" checkbox
- [ ] JSON config editor appears
- [ ] Enter: `{"webhookUrl":"https://hooks.slack.com/services/test/webhook"}`
- [ ] Save configuration
- [ ] Expected: Configuration persists

### Step 3: API Validation

**GET all channels for a job:**
```http
GET http://localhost:5000/api/jobs/{jobId}/channels
```
- [ ] Returns 200 OK
- [ ] Response contains array of channel configs
- [ ] Each config has: id, jobId, channelType, channelConfig, isEnabled, createdAt, updatedAt
- [ ] Expected response:
  ```json
  [
    {
      "id": "guid",
      "jobId": "guid",
      "channelType": "GenericWebhook",
      "channelConfig": "{\"webhookUrl\":\"...\"}",
      "isEnabled": true,
      "createdAt": "2026-05-08T...",
      "updatedAt": "2026-05-08T..."
    }
  ]
  ```

**GET single channel:**
```http
GET http://localhost:5000/api/jobs/{jobId}/channels/GenericWebhook
```
- [ ] Returns 200 OK
- [ ] Response contains single channel config
- [ ] Expected: Matches specific channel details

**PUT (update) channel:**
```http
PUT http://localhost:5000/api/jobs/{jobId}/channels/Teams
Content-Type: application/json

{
  "channelConfig": "{\"serviceUrl\":\"...\",\"conversationId\":\"updated\"}",
  "isEnabled": false
}
```
- [ ] Returns 200 OK
- [ ] Response shows updated config with new `updatedAt` timestamp
- [ ] Refresh UI → shows disabled state

**DELETE channel:**
```http
DELETE http://localhost:5000/api/jobs/{jobId}/channels/Slack
```
- [ ] Returns 204 No Content
- [ ] GET channels → deleted channel not in list
- [ ] Refresh UI → checkbox unchecked

### Step 4: Job Execution (Configuration Layer Only)

**Note:** Full delivery testing requires Stories 1-8 backend implementation. This validation focuses on configuration persistence.

- [ ] Navigate to job with configured channels
- [ ] Click "Run Now" (or equivalent execute button)
- [ ] Job starts executing
- [ ] Job completes successfully (Status: Success)
- [ ] Verify: Channel configurations still present after job run
- [ ] Future: Verify `AdapterDeliveryLog` entries created

### Step 5: Error Handling Validation

**Invalid Channel Type:**
```http
PUT http://localhost:5000/api/jobs/{jobId}/channels/InvalidChannel
Content-Type: application/json

{
  "channelConfig": "{}",
  "isEnabled": true
}
```
- [ ] Returns 400 Bad Request
- [ ] Error message: "Unsupported channel type 'InvalidChannel'"
- [ ] Lists supported channels: GenericWebhook, Teams, Slack

**Invalid JSON in Config:**
```http
PUT http://localhost:5000/api/jobs/{jobId}/channels/GenericWebhook
Content-Type: application/json

{
  "channelConfig": "not-valid-json{",
  "isEnabled": true
}
```
- [ ] Returns 400 Bad Request
- [ ] Error message: "Invalid JSON in ChannelConfig"

**Non-Existent Job:**
```http
GET http://localhost:5000/api/jobs/00000000-0000-0000-0000-000000000000/channels
```
- [ ] Returns 404 Not Found
- [ ] Error message: "Job not found"

---

## Test Results Recording

### Successful Scenarios

| Test Case | Expected Outcome | Actual Outcome | Status | Notes |
|-----------|------------------|----------------|--------|-------|
| UI loads with channel panel | Panel visible with 3 checkboxes | | ✅ ❌ | |
| Configure single channel (webhook) | Config persists, shows in UI | | ✅ ❌ | |
| Configure multiple channels (3) | All 3 persist, show in UI | | ✅ ❌ | |
| Update existing configuration | Updated config persists | | ✅ ❌ | |
| Delete channel configuration | Config removed, UI updates | | ✅ ❌ | |
| API GET all channels | Returns array of configs | | ✅ ❌ | |
| API GET single channel | Returns specific config | | ✅ ❌ | |
| API PUT update channel | Updates config, returns 200 | | ✅ ❌ | |
| API DELETE channel | Removes config, returns 204 | | ✅ ❌ | |

### Error Scenarios

| Test Case | Expected Outcome | Actual Outcome | Status | Notes |
|-----------|------------------|----------------|--------|-------|
| Invalid channel type | 400 Bad Request with error | | ✅ ❌ | |
| Invalid JSON config | 400 Bad Request with error | | ✅ ❌ | |
| Non-existent job | 404 Not Found | | ✅ ❌ | |

---

## Screenshots & Evidence

### Required Screenshots

1. **Channel Configuration Panel:**
   - [ ] Screenshot: UI showing all 3 channel checkboxes
   - [ ] Screenshot: JSON config editor for each channel type
   - [ ] Screenshot: Saved configuration with success indicator

2. **API Responses:**
   - [ ] Screenshot: GET channels response (full array)
   - [ ] Screenshot: GET single channel response
   - [ ] Screenshot: PUT update response
   - [ ] Screenshot: Error response for invalid channel type

3. **Job Execution:**
   - [ ] Screenshot: Job detail page showing configured channels
   - [ ] Screenshot: Job running with channels configured
   - [ ] Screenshot: Job completed successfully

### Video Recording (Optional)

- [ ] Record full demo walkthrough (8-10 minutes)
- [ ] Capture: UI navigation, API calls, job execution
- [ ] Upload to: [Team shared drive / documentation repo]
- [ ] Useful for: Async review, training, documentation

---

## Fallback Scenarios

### If Live UI Unavailable

- [ ] Show pre-recorded video of UI interaction
- [ ] Use screenshots from previous validation run
- [ ] Walk through API calls with Postman/curl
- [ ] Narrate: "Here's what the UI looks like in production"

### If Live API Unavailable

- [ ] Show test results from `MultiChannelDeliveryE2ETests`
- [ ] Display test code showing expected behavior
- [ ] Use mock data to demonstrate concepts

### If Database Empty

- [ ] Create test jobs on-the-fly during demo
- [ ] Use seed script to populate demo data
- [ ] Show entity definitions and schema

---

## Post-Demo Validation

After demo presentation:

- [ ] Verify all test jobs still in database
- [ ] Check for any errors in Aspire logs
- [ ] Review channel configurations still valid
- [ ] Capture final state screenshots
- [ ] Document any issues encountered
- [ ] Note audience questions/feedback

---

## Known Limitations

**Current Scope (Story 5 + Story 9):**
- ✅ Channel configuration UI functional
- ✅ Channel configurations persist to database
- ✅ API endpoints for CRUD operations
- ⏳ Delivery service not yet implemented (Stories 1, 2, 4, 6, 7, 8 pending)
- ⏳ Adapter factory not yet implemented
- ⏳ Actual webhook/Teams/Slack delivery not yet functional
- ⏳ `AdapterDeliveryLog` audit trail not yet queryable

**For Demo:**
- Focus on configuration layer (UI + API)
- Explain delivery will happen when backend lands
- Show test code as specification for future implementation
- Emphasize architecture and fire-and-forget design

---

## Success Criteria Summary

Demo is considered successful if:

1. ✅ UI loads and displays channel configuration panel
2. ✅ All 3 channel types (webhook, Teams, Slack) configurable
3. ✅ Configurations persist and survive page refresh
4. ✅ API endpoints return correct data
5. ✅ Error validation works (invalid channel type, invalid JSON)
6. ✅ Multi-channel configuration demonstrated (3 channels on 1 job)
7. ✅ Audience understands fire-and-forget architecture
8. ✅ Questions answered confidently

---

**Validation Owner:** Dylan (Tester)  
**Demo Date:** Session 5  
**Last Validated:** [Date to be filled during validation run]  
**Sign-Off:** [Name + Date after successful validation]

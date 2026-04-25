# Manual Test Guide: Multi-Channel Delivery Adapters

**Feature:** Phase 2 Feature 1 — Multi-Channel Delivery Adapters  
**Prepared by:** Dylan (Tester)  
**Last Updated:** 2026-05-08  
**Audience:** QA Testers, Backend Developers, DevOps

---

## Purpose

This guide helps testers manually verify multi-channel delivery functionality without requiring live Teams/Slack services. It covers:

1. Testing with mock HTTP responses (no external dependencies)
2. Configuring real Slack/Teams for live testing
3. Known limitations and setup requirements
4. Troubleshooting common issues

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Testing with Mocks (No External Services)](#testing-with-mocks)
3. [Testing with Real Services](#testing-with-real-services)
4. [Test Scenarios](#test-scenarios)
5. [Known Limitations](#known-limitations)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Software Requirements

- .NET 10 SDK or later
- Aspire workload installed (`dotnet workload install aspire`)
- SQLite (bundled with .NET)
- Git (for cloning repo)
- Postman, curl, or similar HTTP client (optional)

### Clone & Build

```bash
git clone https://github.com/elbruno/openclawnet.git
cd openclawnet
dotnet restore
dotnet build
```

### Run Application

```bash
cd src/OpenClawNet.AppHost
dotnet run
```

**Expected:** Aspire dashboard opens at `http://localhost:5000` (or assigned port)

---

## Testing with Mocks

This section covers testing **without live external services** using mock HTTP responses.

### Option 1: In-Memory Integration Tests

**Best for:** Automated testing, CI/CD pipelines

The `MultiChannelDeliveryE2ETests` class uses in-memory database and doesn't require external services.

**Run tests:**
```bash
cd tests/OpenClawNet.IntegrationTests
dotnet test --filter "FullyQualifiedName~MultiChannelDeliveryE2ETests"
```

**Expected output:**
```
Passed! - 8 tests passed in 2.1s
```

**What's tested:**
- Channel configuration CRUD operations
- Validation (invalid channel type, invalid JSON)
- Multi-channel scenarios (3 channels configured)
- Enable/disable state persistence

**What's NOT tested (requires Stories 1-8):**
- Actual HTTP delivery to webhooks
- Teams bot framework integration
- Slack webhook posting
- Audit trail (`AdapterDeliveryLog`) creation

---

### Option 2: Local Mock Webhook Server

**Best for:** Manual testing, UI validation

Set up a simple HTTP server to receive webhook POSTs:

#### Using Python (Simple HTTP Server)

**1. Create mock server:**
```python
# mock_webhook_server.py
from http.server import HTTPServer, BaseHTTPRequestHandler
import json

class WebhookHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        body = self.rfile.read(content_length)
        
        print(f"\n📥 Webhook received:")
        print(f"Headers: {dict(self.headers)}")
        print(f"Body: {body.decode('utf-8')}\n")
        
        # Respond with 200 OK
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps({"status": "received"}).encode())

    def log_message(self, format, *args):
        # Suppress default logging
        pass

if __name__ == '__main__':
    server = HTTPServer(('localhost', 9999), WebhookHandler)
    print("🚀 Mock webhook server running at http://localhost:9999/webhook")
    print("Press Ctrl+C to stop\n")
    server.serve_forever()
```

**2. Run server:**
```bash
python mock_webhook_server.py
```

**3. Configure job with mock URL:**
- Navigate to job detail page
- Enable "Generic Webhook"
- Set config: `{"webhookUrl":"http://localhost:9999/webhook"}`
- Save configuration

**4. Execute job:**
- Click "Run Now"
- Watch mock server terminal for incoming request
- Expected output:
  ```
  📥 Webhook received:
  Headers: {'Content-Type': 'application/json', ...}
  Body: {"jobId":"...","jobName":"...","artifactId":"...","content":"..."}
  ```

#### Using Node.js (Express)

**1. Install Express:**
```bash
npm install -g express body-parser
```

**2. Create server:**
```javascript
// mock-webhook.js
const express = require('express');
const bodyParser = require('body-parser');

const app = express();
app.use(bodyParser.json());

app.post('/webhook', (req, res) => {
  console.log('\n📥 Webhook received:');
  console.log('Body:', JSON.stringify(req.body, null, 2), '\n');
  res.status(200).json({ status: 'received' });
});

app.listen(9999, () => {
  console.log('🚀 Mock webhook server running at http://localhost:9999/webhook');
});
```

**3. Run server:**
```bash
node mock-webhook.js
```

---

### Option 3: webhook.site (Online Mock)

**Best for:** Quick testing, no local setup

**1. Go to:** https://webhook.site

**2. Copy your unique URL** (e.g., `https://webhook.site/abc123`)

**3. Configure job:**
- Set webhook URL: `{"webhookUrl":"https://webhook.site/abc123"}`

**4. Execute job:**
- Run job
- Check webhook.site page for incoming request
- Expected: POST request with job/artifact data

**Note:** webhook.site is a public service; avoid sending sensitive data.

---

## Testing with Real Services

This section covers testing **with live Teams and Slack** for full integration validation.

### Configure Slack

**Prerequisites:**
- Slack workspace (free tier OK)
- Admin access to create incoming webhooks

**Steps:**

**1. Create Incoming Webhook:**
- Go to https://api.slack.com/apps
- Click "Create New App" → "From scratch"
- Name: "OpenClawNet Delivery Test"
- Select workspace
- Click "Incoming Webhooks" → Toggle "Activate Incoming Webhooks" → "Add New Webhook to Workspace"
- Select channel (e.g., #test-notifications)
- Copy webhook URL (e.g., `https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXX`)

**2. Configure job in OpenClawNet:**
- Enable "Slack" channel
- Set config:
  ```json
  {"webhookUrl":"https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXX"}
  ```

**3. Execute job:**
- Run job
- Check Slack channel
- Expected: Message posted with job name and artifact content

**4. Verify message format:**
- Message should include:
  - Job name
  - Artifact type
  - Content (truncated if large)
  - Timestamp

**Troubleshooting:**
- If message doesn't appear: Check webhook URL is correct, channel selected, webhook still active
- If 404 error: Webhook URL expired or revoked, create new webhook

---

### Configure Microsoft Teams

**Prerequisites:**
- Microsoft 365 account with Teams access
- Teams bot registered (for proactive messaging)
- Conversation reference stored (obtained from bot interaction)

**Steps:**

**1. Register Teams Bot (One-Time Setup):**

This requires Azure Bot Service setup. For testing, use mock conversation reference:

**Mock Teams Config:**
```json
{
  "serviceUrl": "https://smba.trafficmanager.net/teams/",
  "conversationId": "mock-conversation-id-for-testing"
}
```

**Note:** Full Teams integration requires:
- Azure Bot Service registration
- Bot Framework SDK integration
- Conversation reference capture from incoming message
- Microsoft App ID and App Secret

For MVP testing, focus on:
- Configuration persistence (channel config saved)
- API validation (config structure correct)
- UI interaction (Teams checkbox + config editor)

**2. Configure job:**
- Enable "Microsoft Teams"
- Set mock config:
  ```json
  {
    "serviceUrl": "https://smba.trafficmanager.net/teams/",
    "conversationId": "19:meeting_test@thread.v2"
  }
  ```

**3. Execute job (Mock Mode):**
- Run job
- Verify: Configuration queried correctly
- Future: Actual message delivery when adapter implemented

**Live Teams Testing (Future):**

When `TeamsDeliveryAdapter` is implemented (Story 7):

1. Install OpenClawNet bot in Teams
2. Send a message to bot: "Hello"
3. Bot captures conversation reference and stores it
4. Configure job with stored conversation reference
5. Execute job
6. Verify: Proactive message appears in Teams conversation

---

## Test Scenarios

### Scenario 1: Single Channel (Webhook)

**Goal:** Verify basic channel configuration and persistence

1. Create new job: "Webhook Test"
2. Enable Generic Webhook
3. Config: `{"webhookUrl":"http://localhost:9999/webhook"}`
4. Save
5. Refresh page → Verify config persists
6. Query API: `GET /api/jobs/{jobId}/channels`
7. Expected: Array with 1 GenericWebhook entry
8. Execute job (with mock server running)
9. Expected: Mock server receives POST request

---

### Scenario 2: Multiple Channels (All 3)

**Goal:** Verify multi-channel configuration

1. Create new job: "Multi-Channel Test"
2. Enable all 3 channels:
   - Generic Webhook: `{"webhookUrl":"http://localhost:9999/webhook"}`
   - Teams: `{"serviceUrl":"...","conversationId":"..."}`
   - Slack: `{"webhookUrl":"https://hooks.slack.com/..."}`
3. Save
4. Refresh page → All 3 checkboxes checked, configs visible
5. Query API → Returns array with 3 entries
6. Execute job
7. Expected (when adapters implemented):
   - Mock webhook receives request
   - Slack channel receives message
   - Teams conversation receives message

---

### Scenario 3: Partial Configuration (Enabled/Disabled)

**Goal:** Verify enabled/disabled state behavior

1. Create job with 2 channels:
   - Generic Webhook: Enabled=true
   - Teams: Enabled=false
2. Save
3. Execute job
4. Expected (when adapters implemented):
   - Only webhook delivery attempted
   - Teams not attempted (disabled)
   - Audit log shows 1 delivery attempt (webhook only)

---

### Scenario 4: Update Existing Configuration

**Goal:** Verify configuration updates

1. Configure job with webhook: `{"webhookUrl":"http://localhost:9999/webhook"}`
2. Save
3. Update webhook URL: `{"webhookUrl":"http://localhost:9999/webhook-v2"}`
4. Save
5. Query API → Verify `updatedAt` timestamp changed
6. Execute job
7. Expected: Delivery uses new URL

---

### Scenario 5: Delete Configuration

**Goal:** Verify configuration removal

1. Configure job with webhook
2. Save
3. Delete: `DELETE /api/jobs/{jobId}/channels/GenericWebhook`
4. Refresh page → Webhook checkbox unchecked
5. Query API → Array is empty
6. Execute job
7. Expected: No delivery attempted

---

### Scenario 6: Invalid Configurations

**Goal:** Verify validation error handling

**Invalid Channel Type:**
1. Try: `PUT /api/jobs/{jobId}/channels/EmailChannel`
2. Expected: 400 Bad Request, error message lists supported channels

**Invalid JSON:**
1. Try: Config = `"not-json{"`
2. Expected: 400 Bad Request, "Invalid JSON" error

**Missing Job:**
1. Try: `GET /api/jobs/00000000-0000-0000-0000-000000000000/channels`
2. Expected: 404 Not Found, "Job not found"

---

## Known Limitations

### Current Scope (Story 5 + Story 9)

**✅ Implemented:**
- Channel configuration UI (checkboxes + JSON editors)
- API endpoints for CRUD operations
- Validation (channel type, JSON format)
- Configuration persistence to database
- E2E integration tests for configuration layer

**⏳ Not Yet Implemented (Stories 1-8 Pending):**
- Adapter factory (`IChannelDeliveryAdapterFactory`)
- Generic Webhook adapter (`GenericWebhookDeliveryAdapter`)
- Teams adapter (`TeamsDeliveryAdapter`)
- Slack adapter (`SlackDeliveryAdapter`)
- Delivery service (`IChannelDeliveryService`)
- Job executor integration (call delivery service after job completion)
- Audit trail entity (`AdapterDeliveryLog`)
- Audit trail query endpoints

### Testing Limitations

**Configuration Testing:** ✅ Fully testable now
- UI interaction
- API CRUD operations
- Validation logic
- Persistence

**Delivery Testing:** ⏳ Blocked until Stories 1-8 complete
- Actual HTTP requests to webhooks
- Teams bot framework integration
- Slack webhook posting
- Audit trail logging
- Fire-and-forget behavior
- Error handling during delivery

---

## Troubleshooting

### Issue: Channel panel doesn't appear in UI

**Possible Causes:**
- Blazor component not registered
- Endpoint not mapped in Program.cs
- Database missing `JobChannelConfiguration` table

**Solutions:**
1. Verify `JobChannelConfigEndpoints.MapJobChannelConfigEndpoints()` in `Program.cs`
2. Check `OpenClawDbContext` has `JobChannelConfigurations` DbSet
3. Clear browser cache, hard refresh (Ctrl+Shift+R)
4. Check browser console for JS errors

---

### Issue: API returns 403 Forbidden

**Cause:** Endpoints are loopback-only for security

**Solution:**
- Use `http://localhost:5000` not `http://127.0.0.1` or external IP
- Send requests from same machine where app is running
- Future: Add authentication for remote access

---

### Issue: Configuration not persisting

**Possible Causes:**
- Database write failing
- Validation error not displayed
- Browser not sending PUT request

**Solutions:**
1. Check browser network tab: Verify PUT request sent (200 OK response)
2. Check Aspire logs: Look for EF Core errors
3. Verify job exists: `GET /api/jobs/{jobId}` returns 200
4. Check database directly: Query `JobChannelConfigurations` table

---

### Issue: Mock webhook server not receiving requests

**Possible Causes:**
- Adapter not implemented yet (Stories 1-8 pending)
- Delivery service not wired into job executor
- Mock server not running
- Wrong port or URL in config

**Solutions:**
1. Verify mock server running: `curl http://localhost:9999/webhook`
2. Check webhook URL in config exactly matches server address
3. **Current limitation:** Delivery logic not implemented (Story 6)
   - Configuration saves successfully
   - Job completes successfully
   - But delivery not attempted (expected until Stories 1-8 land)

---

### Issue: Slack webhook returns 404

**Possible Causes:**
- Webhook URL expired
- Webhook revoked by workspace admin
- Wrong URL copied

**Solutions:**
1. Verify webhook still active: `curl -X POST [webhook-url] -d '{"text":"test"}'`
2. Create new incoming webhook in Slack app settings
3. Update job config with new URL

---

### Issue: Teams message not delivered

**Current Status:** Teams adapter not yet implemented (Story 7)

**When Story 7 lands:**
1. Verify conversation reference is valid
2. Check Azure Bot Service is running
3. Verify App ID and App Secret configured
4. Check Teams bot installed in target conversation

---

### Issue: Job fails when channel enabled

**Expected Behavior:** Job should **never fail** due to delivery issues (fire-and-forget design)

**If job fails:**
1. Check job executor logic: Delivery should be `Task.Run` without await
2. Verify delivery errors caught and logged (not thrown)
3. Check `AdapterDeliveryLog` for error details

**Current Status:** Job executor integration pending (Story 6)

---

## Manual Test Checklist

Use this checklist for comprehensive manual validation:

### Configuration Layer (Available Now)

- [ ] UI: Channel panel loads on job detail page
- [ ] UI: All 3 channel types show checkboxes with icons
- [ ] UI: JSON editor appears when channel enabled
- [ ] UI: JSON editor hides when channel disabled
- [ ] UI: Config auto-saves (or Save button works)
- [ ] UI: Success indicator shows after save
- [ ] UI: Refresh page → config persists
- [ ] API: GET /api/jobs/{jobId}/channels returns configs
- [ ] API: GET /api/jobs/{jobId}/channels/{type} returns single config
- [ ] API: PUT updates config and returns 200
- [ ] API: DELETE removes config and returns 204
- [ ] Validation: Invalid channel type → 400 error
- [ ] Validation: Invalid JSON → 400 error
- [ ] Validation: Missing job → 404 error

### Delivery Layer (Requires Stories 1-8)

- [ ] Mock webhook: Server receives POST request
- [ ] Slack: Message appears in channel
- [ ] Teams: Message appears in conversation
- [ ] Audit trail: `AdapterDeliveryLog` entries created
- [ ] Fire-and-forget: Job completes even if delivery fails
- [ ] Error logging: Failed delivery logs error message
- [ ] Concurrent delivery: All channels attempted simultaneously
- [ ] Disabled channel: Not attempted for delivery

---

## Feedback & Issues

If you encounter issues not covered in this guide:

1. **Check Aspire logs:** Dashboard → Service → Logs tab
2. **Check browser console:** F12 → Console tab
3. **Check database:** Query `JobChannelConfigurations` table
4. **Report issue:** Include:
   - Steps to reproduce
   - Expected vs actual behavior
   - Logs/screenshots
   - Environment (OS, .NET version, browser)

**Contact:** Dylan (Tester) or open GitHub issue

---

## Next Steps

After manual validation:

1. **Run automated tests:**
   ```bash
   dotnet test --filter "FullyQualifiedName~MultiChannelDeliveryE2ETests"
   ```

2. **Record test results:** Fill out `demo-validation.md` checklist

3. **Capture screenshots:** UI with configured channels, API responses

4. **Document issues:** Any bugs or unexpected behavior

5. **Await Stories 1-8:** Backend implementation for full delivery testing

---

**Guide Owner:** Dylan (Tester)  
**Last Updated:** 2026-05-08  
**Next Review:** After Stories 1-8 implementation

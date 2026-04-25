# Session 5 Demo Script: Multi-Channel Delivery Adapters

**Feature:** Phase 2 Feature 1 — Multi-Channel Delivery Adapters  
**Demo Date:** Session 5 Presentation  
**Prepared by:** Dylan (Tester)  
**Duration:** 8-10 minutes  
**Status:** Ready for Demo

---

## Demo Overview

This demo showcases OpenClawNet's new multi-channel delivery capability, allowing job artifacts to be automatically delivered to external channels (Teams, Slack, Generic Webhook) after job completion. The system implements a fire-and-forget pattern where delivery failures don't affect job success.

**Key Features Demonstrated:**
- Per-job channel configuration via UI
- Support for 3 channel types: Generic Webhook, Teams, Slack
- Fire-and-forget delivery (job succeeds even if delivery fails)
- Audit trail logging for all delivery attempts
- Visual channel management with emoji icons

---

## Prerequisites & Setup

### Before the Demo

1. **Test Job Creation:**
   ```bash
   cd C:\src\openclawnet
   dotnet run --project src\OpenClawNet.AppHost
   ```

2. **Pre-configure Demo Jobs:**
   - Job 1: "Weekly Report" with Generic Webhook configured
   - Job 2: "Team Standup Summary" with Teams + Slack configured
   - Job 3: "Customer Analytics" with all 3 channels configured

3. **Mock Endpoints (Optional):**
   - If demoing live delivery, set up test webhook at `http://localhost:9999/webhook`
   - For Teams: Use mock conversation reference
   - For Slack: Use mock webhook URL

4. **Database State:**
   - Ensure test database has sample jobs configured
   - Verify `JobChannelConfiguration` table has demo data

### Fallback Plan

If live services unavailable:
- Show pre-configured channel UI screenshots
- Display test results from `MultiChannelDeliveryE2ETests`
- Walk through audit log entries from previous test run
- Narrate: "Here's what the multi-channel delivery looks like in production"

---

## Demo Flow

### Part 1: Introduction (1 minute)

**Script:**
> "Today I'm demonstrating OpenClawNet's new multi-channel delivery feature. Previously, job artifacts stayed in the database. Now, admins can configure jobs to automatically deliver results to Teams channels, Slack workspaces, or generic webhooks — perfect for integrating with monitoring systems, notification hubs, or custom applications."

**Show:** OpenClawNet dashboard homepage with Jobs section

---

### Part 2: Channel Configuration UI (2-3 minutes)

**Steps:**

1. **Navigate to Job Detail Page**
   - Click "Jobs" → Select "Weekly Report" job
   - Scroll to "Delivery Channels" section (between metadata and run history)

2. **Show Channel Panel**
   - Point out 3 channel types with emoji icons:
     - 🌐 Generic Webhook
     - 💼 Microsoft Teams
     - 💬 Slack

3. **Configure Generic Webhook**
   - Check the "Generic Webhook" checkbox
   - Show JSON config editor appearing
   - Enter webhook config:
     ```json
     {"webhookUrl":"http://localhost:9999/webhook"}
     ```
   - Click "Save" (or config auto-saves on blur)
   - Show success indicator

4. **Configure Multiple Channels**
   - Navigate to "Team Standup Summary" job
   - Enable Teams + Slack
   - Show Teams config:
     ```json
     {
       "serviceUrl":"https://smba.trafficmanager.net/teams/",
       "conversationId":"demo-conversation-123"
     }
     ```
   - Show Slack config:
     ```json
     {"webhookUrl":"https://hooks.slack.com/services/demo/webhook"}
     ```
   - Emphasize: "Each job can have its own channel routing"

**Script:**
> "The UI makes it simple to configure delivery channels per job. Admins select which channels to enable, provide JSON configuration for each, and the system handles the rest. Notice the auto-save on blur — no need to click multiple save buttons."

---

### Part 3: Job Execution & Delivery (3-4 minutes)

**Steps:**

1. **Execute a Configured Job**
   - Navigate to "Weekly Report" job (webhook configured)
   - Click "Run Now"
   - Show job starting (Status: Running)

2. **Monitor Job Completion**
   - Wait for job to complete (~10-15 seconds for demo)
   - Show job status changes to "Success"
   - Emphasize: "Job marked complete immediately, delivery happens in background"

3. **Show Channel Configuration Query**
   - Open browser dev tools or Postman
   - Query: `GET /api/jobs/{jobId}/channels`
   - Show response with configured channels:
     ```json
     [
       {
         "id": "...",
         "jobId": "...",
         "channelType": "GenericWebhook",
         "channelConfig": "{\"webhookUrl\":\"http://localhost:9999/webhook\"}",
         "isEnabled": true,
         "createdAt": "2026-05-08T10:00:00Z",
         "updatedAt": "2026-05-08T10:00:00Z"
       }
     ]
     ```

4. **Demonstrate Fire-and-Forget (Optional)**
   - If time permits, configure a job with invalid webhook URL
   - Execute job → Show job still succeeds
   - Explain: "Delivery failures don't affect job success — critical for reliability"

**Script:**
> "When a job completes, the delivery service queries configured channels and fires delivery requests in the background. The job immediately marks as 'Success' — we don't wait for external services. This fire-and-forget pattern ensures job execution isn't blocked by slow or failing delivery endpoints."

---

### Part 4: Audit Trail (1-2 minutes)

**Steps:**

1. **Show Configuration Persistence**
   - Query: `GET /api/jobs/{jobId}/channels/GenericWebhook`
   - Show single channel details with timestamps

2. **Explain Audit Trail Design** (Future)
   - Mention: `AdapterDeliveryLog` entity (Story 3) tracks all delivery attempts
   - Fields: JobId, JobRunId, AdapterName, Success, ErrorMessage, ExternalId, DeliveredAt
   - Show example log entry:
     ```json
     {
       "id": "...",
       "jobId": "...",
       "jobRunId": "...",
       "adapterName": "GenericWebhook",
       "success": true,
       "errorMessage": null,
       "externalId": "200",
       "deliveredAt": "2026-05-08T10:01:23Z"
     }
     ```

3. **Query Example** (Future)
   - `GET /api/jobs/{jobId}/runs/{runId}/delivery-log`
   - Show all delivery attempts for a run

**Script:**
> "Every delivery attempt is logged to the audit trail, including successes, failures, and error messages. This gives admins full visibility into what happened with each delivery. If a webhook is down, we log the error but the job still completes successfully."

---

### Part 5: Multi-Channel Example (1-2 minutes)

**Steps:**

1. **Show Job with 3 Channels**
   - Navigate to "Customer Analytics" job
   - Show all 3 channels enabled in UI

2. **Execute Job**
   - Click "Run Now"
   - Show job completing

3. **Emphasize Concurrent Delivery** (Future)
   - "All 3 channels are attempted simultaneously"
   - "If Teams is slow, webhook and Slack aren't affected"
   - "Each delivery has its own log entry"

**Script:**
> "For critical jobs, you can configure multiple channels for redundancy. If Slack is down but Teams is up, you still get notifications. Each channel operates independently — failures in one don't affect others."

---

## Demo Validation Checklist

Before starting demo, verify:

- [ ] Aspire app running (`dotnet run --project src\OpenClawNet.AppHost`)
- [ ] Test jobs exist in database with configured channels
- [ ] Gateway API responding (`GET /api/jobs` returns 200)
- [ ] Channel config panel loads in UI (no 404 errors)
- [ ] Browser dev tools ready for API calls
- [ ] Fallback slides/screenshots ready if live demo fails

---

## Expected Outcomes

### Success Criteria

1. ✅ UI shows channel selection panel with 3 channel types
2. ✅ Channels can be enabled/disabled via checkboxes
3. ✅ JSON config editors appear when channels enabled
4. ✅ Configurations persist and survive page refresh
5. ✅ Multiple channels can be configured per job
6. ✅ API returns channel configurations correctly
7. ✅ Job execution completes regardless of delivery status

### Failure Scenarios to Demonstrate

1. **Invalid Webhook URL:** Job succeeds, error logged (future: show in audit trail)
2. **Disabled Channel:** Not attempted for delivery
3. **Missing Config:** Validation prevents saving

---

## Technical Notes

### Architecture Highlights

- **Data Model:** `JobChannelConfiguration` entity (1-to-many with ScheduledJob)
- **Endpoints:** `/api/jobs/{jobId}/channels` (GET/PUT/DELETE)
- **Adapters:** `IChannelDeliveryAdapter` interface with 3 implementations
- **Factory:** `IChannelDeliveryAdapterFactory` resolves adapters by name
- **Service:** `IChannelDeliveryService` orchestrates fire-and-forget delivery
- **Integration:** `JobExecutor` calls delivery service after job completion

### Fire-and-Forget Implementation

```csharp
// In JobExecutor after job completion
var configs = await GetEnabledChannelConfigs(jobId);
_ = Task.Run(async () =>
{
    foreach (var config in configs)
    {
        var adapter = await _adapterFactory.GetAdapterAsync(config.ChannelType);
        var result = await adapter.DeliverAsync(jobId, jobName, artifactId, ...);
        await LogDeliveryResult(jobId, jobRunId, config.ChannelType, result);
    }
}, cancellationToken);
```

Key: Don't await the Task.Run → job completes immediately

---

## Q&A Preparation

### Expected Questions

**Q: What happens if a webhook is slow (e.g., 30 seconds)?**  
A: Job completes immediately. Delivery happens in background. Slow webhooks don't block job execution.

**Q: Can we add email as a channel?**  
A: Yes! The adapter interface is generic. Add `EmailDeliveryAdapter` implementing `IChannelDeliveryAdapter`, register in factory, add UI checkbox.

**Q: How do we authenticate with Teams/Slack?**  
A: MVP uses webhook URLs (no auth). Teams adapter uses stored conversation references from bot framework. Future: OAuth tokens for user-specific delivery.

**Q: What if I want to retry failed deliveries?**  
A: Current design is single-attempt fire-and-forget. Future enhancement: retry queue with exponential backoff.

**Q: Can users (non-admins) configure channels?**  
A: Current MVP: admin-only (loopback-only endpoints). Future: role-based permissions for channel config.

---

## Troubleshooting

### Issue: Channel config panel doesn't appear
**Solution:** Verify `JobChannelConfigEndpoints` registered in `Program.cs`:
```csharp
app.MapJobChannelConfigEndpoints();
```

### Issue: 403 Forbidden on API calls
**Solution:** Endpoints are loopback-only for security. Use `localhost` not `127.0.0.1` or external IP.

### Issue: JSON validation fails
**Solution:** Verify config is valid JSON. Use `JsonSerializer.Deserialize<JsonElement>(config)` for validation.

### Issue: Job doesn't execute
**Solution:** Check job status (must be Active), verify trigger type (Manual for demo), ensure scheduler service running.

---

## Post-Demo Notes

After demo, capture:
- Screenshots of UI with configured channels
- API responses from channel queries
- Audit log entries (when available)
- Video recording for async review
- Audience questions and feedback

---

## Next Steps

Post-Session 5:
1. **Stories 1-8 Backend:** Implement adapter factory, webhook adapter, Teams adapter, Slack adapter, delivery service
2. **Story 9 Testing:** Run full E2E tests with real adapters
3. **Audit Trail:** Query `AdapterDeliveryLog` entries in demo
4. **Live Channels:** Demo with real Teams channel + Slack workspace
5. **Performance:** Measure delivery latency, add monitoring

---

**Demo Owner:** Dylan (Tester)  
**Last Updated:** 2026-05-08  
**Next Review:** After Session 5 delivery

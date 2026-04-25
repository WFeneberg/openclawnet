using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenClawNet.Storage;
using OpenClawNet.Storage.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OpenClawNet.IntegrationTests;

/// <summary>
/// E2E integration tests for multi-channel delivery adapters (Phase 2 Feature 1, Story 9).
/// Tests verify that job channel configurations are persisted and can be queried correctly.
/// 
/// NOTE: These tests focus on the configuration layer. Actual adapter delivery logic
/// (Stories 1-8: factory, webhook, Teams, Slack, delivery service) will be tested 
/// when backend implementation lands.
/// </summary>
public sealed class MultiChannelDeliveryE2ETests : IClassFixture<GatewayWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly GatewayWebAppFactory _factory;

    public MultiChannelDeliveryE2ETests(GatewayWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Test Case 1: Create job → Configure 1 channel (webhook) → Verify configuration persisted
    /// </summary>
    [Fact]
    public async Task SingleChannel_Webhook_ConfigurationPersisted()
    {
        // Arrange: Create a test job
        var jobId = await CreateTestJobAsync("single-webhook-job");

        // Act: Configure webhook channel
        var webhookConfig = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { webhookUrl = "http://localhost:9999/webhook" }),
            IsEnabled = true
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/jobs/{jobId}/channels/GenericWebhook",
            webhookConfig);

        // Assert: Configuration created successfully
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var savedConfig = await response.Content.ReadFromJsonAsync<JobChannelConfigDto>();
        savedConfig.Should().NotBeNull();
        savedConfig!.JobId.Should().Be(jobId);
        savedConfig.ChannelType.Should().Be("GenericWebhook");
        savedConfig.IsEnabled.Should().BeTrue();
        savedConfig.ChannelConfig.Should().Contain("webhookUrl");

        // Verify: Query configuration back
        var getResponse = await _client.GetAsync($"/api/jobs/{jobId}/channels");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var configs = await getResponse.Content.ReadFromJsonAsync<List<JobChannelConfigDto>>();
        configs.Should().NotBeNull();
        configs!.Should().HaveCount(1);
        configs[0].ChannelType.Should().Be("GenericWebhook");
    }

    /// <summary>
    /// Test Case 2: Create job → Configure 3 channels (webhook, Teams, Slack) → Verify all 3 persisted
    /// </summary>
    [Fact]
    public async Task MultipleChannels_AllThreeTypes_AllConfigured()
    {
        // Arrange: Create a test job
        var jobId = await CreateTestJobAsync("multi-channel-job");

        // Act: Configure 3 channels
        var channels = new[]
        {
            new
            {
                Type = "GenericWebhook",
                Config = JsonSerializer.Serialize(new { webhookUrl = "http://localhost:9999/webhook" }),
                Enabled = true
            },
            new
            {
                Type = "Teams",
                Config = JsonSerializer.Serialize(new
                {
                    serviceUrl = "https://smba.trafficmanager.net/teams/",
                    conversationId = "mock-conversation-123"
                }),
                Enabled = true
            },
            new
            {
                Type = "Slack",
                Config = JsonSerializer.Serialize(new { webhookUrl = "https://hooks.slack.com/services/mock/webhook" }),
                Enabled = true
            }
        };

        foreach (var channel in channels)
        {
            var response = await _client.PutAsJsonAsync(
                $"/api/jobs/{jobId}/channels/{channel.Type}",
                new { ChannelConfig = channel.Config, IsEnabled = channel.Enabled });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert: All 3 channels configured
        var getResponse = await _client.GetAsync($"/api/jobs/{jobId}/channels");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var configs = await getResponse.Content.ReadFromJsonAsync<List<JobChannelConfigDto>>();
        configs.Should().NotBeNull();
        configs!.Should().HaveCount(3);
        configs.Should().Contain(c => c.ChannelType == "GenericWebhook" && c.IsEnabled);
        configs.Should().Contain(c => c.ChannelType == "Teams" && c.IsEnabled);
        configs.Should().Contain(c => c.ChannelType == "Slack" && c.IsEnabled);

        // Verify: Each channel config contains expected fields
        var webhookConfig = configs.First(c => c.ChannelType == "GenericWebhook");
        webhookConfig.ChannelConfig.Should().Contain("webhookUrl");

        var teamsConfig = configs.First(c => c.ChannelType == "Teams");
        teamsConfig.ChannelConfig.Should().Contain("serviceUrl");
        teamsConfig.ChannelConfig.Should().Contain("conversationId");

        var slackConfig = configs.First(c => c.ChannelType == "Slack");
        slackConfig.ChannelConfig.Should().Contain("webhookUrl");
    }

    /// <summary>
    /// Test Case 3: Delivery failure scenario — invalid webhook URL → Verify configuration still valid
    /// (Note: Actual delivery logic testing will happen when backend services land)
    /// </summary>
    [Fact]
    public async Task InvalidWebhookUrl_ConfigurationStillValid()
    {
        // Arrange: Create a test job with invalid webhook URL
        var jobId = await CreateTestJobAsync("invalid-webhook-job");

        // Act: Configure channel with invalid URL (this is still valid JSON)
        var webhookConfig = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { webhookUrl = "not-a-valid-url" }),
            IsEnabled = true
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/jobs/{jobId}/channels/GenericWebhook",
            webhookConfig);

        // Assert: Configuration accepted (validation of URL format happens at delivery time, not config time)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var savedConfig = await response.Content.ReadFromJsonAsync<JobChannelConfigDto>();
        savedConfig.Should().NotBeNull();
        savedConfig!.IsEnabled.Should().BeTrue();
        savedConfig.ChannelConfig.Should().Contain("not-a-valid-url");

        // Future: When delivery service exists, verify that delivery fails but job succeeds
        // and error is logged to AdapterDeliveryLog with Success=false
    }

    /// <summary>
    /// Test Case 4: Partial failure — 1 channel enabled, 1 disabled → Verify both persisted with correct status
    /// </summary>
    [Fact]
    public async Task PartialConfiguration_OneEnabledOneDisabled_BothPersisted()
    {
        // Arrange: Create a test job
        var jobId = await CreateTestJobAsync("partial-config-job");

        // Act: Configure 2 channels, one enabled, one disabled
        var enabledChannel = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { webhookUrl = "http://localhost:9999/webhook" }),
            IsEnabled = true
        };

        var disabledChannel = new
        {
            ChannelConfig = JsonSerializer.Serialize(new
            {
                serviceUrl = "https://smba.trafficmanager.net/teams/",
                conversationId = "mock-conversation-456"
            }),
            IsEnabled = false
        };

        await _client.PutAsJsonAsync($"/api/jobs/{jobId}/channels/GenericWebhook", enabledChannel);
        await _client.PutAsJsonAsync($"/api/jobs/{jobId}/channels/Teams", disabledChannel);

        // Assert: Both configurations exist with correct enabled status
        var getResponse = await _client.GetAsync($"/api/jobs/{jobId}/channels");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var configs = await getResponse.Content.ReadFromJsonAsync<List<JobChannelConfigDto>>();
        configs.Should().NotBeNull();
        configs!.Should().HaveCount(2);

        var webhook = configs.First(c => c.ChannelType == "GenericWebhook");
        webhook.IsEnabled.Should().BeTrue();

        var teams = configs.First(c => c.ChannelType == "Teams");
        teams.IsEnabled.Should().BeFalse();

        // Future: When delivery service exists, verify that only enabled channels are attempted
    }

    /// <summary>
    /// Test Case 5: Update existing configuration — change enabled status and config
    /// </summary>
    [Fact]
    public async Task UpdateExistingConfiguration_EnabledStatusAndConfigChanged()
    {
        // Arrange: Create job and initial configuration
        var jobId = await CreateTestJobAsync("update-config-job");

        var initialConfig = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { webhookUrl = "http://localhost:9999/webhook-v1" }),
            IsEnabled = false
        };

        await _client.PutAsJsonAsync($"/api/jobs/{jobId}/channels/GenericWebhook", initialConfig);

        // Act: Update configuration
        var updatedConfig = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { webhookUrl = "http://localhost:9999/webhook-v2" }),
            IsEnabled = true
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/jobs/{jobId}/channels/GenericWebhook",
            updatedConfig);

        // Assert: Configuration updated
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var savedConfig = await response.Content.ReadFromJsonAsync<JobChannelConfigDto>();
        savedConfig.Should().NotBeNull();
        savedConfig!.IsEnabled.Should().BeTrue();
        savedConfig.ChannelConfig.Should().Contain("webhook-v2");
        savedConfig.UpdatedAt.Should().BeAfter(savedConfig.CreatedAt);
    }

    /// <summary>
    /// Test Case 6: Delete channel configuration
    /// </summary>
    [Fact]
    public async Task DeleteChannelConfiguration_ConfigRemoved()
    {
        // Arrange: Create job with configuration
        var jobId = await CreateTestJobAsync("delete-config-job");

        var config = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { webhookUrl = "http://localhost:9999/webhook" }),
            IsEnabled = true
        };

        await _client.PutAsJsonAsync($"/api/jobs/{jobId}/channels/GenericWebhook", config);

        // Act: Delete configuration
        var deleteResponse = await _client.DeleteAsync($"/api/jobs/{jobId}/channels/GenericWebhook");

        // Assert: Configuration deleted
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify: Configuration no longer exists
        var getResponse = await _client.GetAsync($"/api/jobs/{jobId}/channels");
        var configs = await getResponse.Content.ReadFromJsonAsync<List<JobChannelConfigDto>>();
        configs.Should().NotBeNull();
        configs!.Should().BeEmpty();
    }

    /// <summary>
    /// Test Case 7: Invalid channel type — verify validation
    /// </summary>
    [Fact]
    public async Task InvalidChannelType_ReturnsValidationError()
    {
        // Arrange: Create a test job
        var jobId = await CreateTestJobAsync("invalid-channel-job");

        // Act: Try to configure unsupported channel type
        var config = new
        {
            ChannelConfig = JsonSerializer.Serialize(new { someConfig = "value" }),
            IsEnabled = true
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/jobs/{jobId}/channels/UnsupportedChannel",
            config);

        // Assert: Returns BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Unsupported channel type");
    }

    /// <summary>
    /// Test Case 8: Invalid JSON in ChannelConfig — verify validation
    /// </summary>
    [Fact]
    public async Task InvalidJsonConfig_ReturnsValidationError()
    {
        // Arrange: Create a test job
        var jobId = await CreateTestJobAsync("invalid-json-job");

        // Act: Try to configure with invalid JSON
        var config = new
        {
            ChannelConfig = "not-valid-json{",
            IsEnabled = true
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/jobs/{jobId}/channels/GenericWebhook",
            config);

        // Assert: Returns BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Invalid JSON");
    }

    // Helper methods

    private async Task<Guid> CreateTestJobAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OpenClawDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var job = new ScheduledJob
        {
            Id = Guid.NewGuid(),
            Name = name,
            Prompt = "Test prompt for multi-channel delivery",
            Status = JobStatus.Active,
            TriggerType = TriggerType.Manual,
            AgentProfileName = null,
            CreatedAt = DateTime.UtcNow
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        return job.Id;
    }
}

// DTO for deserialization (matches Gateway DTO)
public record JobChannelConfigDto(
    Guid Id,
    Guid JobId,
    string ChannelType,
    string? ChannelConfig,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);

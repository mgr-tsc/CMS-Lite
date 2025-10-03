using System.Net;
using System.Text;
using System.Text.Json;
using CmsLiteTests.Support;
using Xunit;

namespace CmsLiteTests;

public class RateLimitingTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task AuthEndpoint_ExceedsRateLimit_Returns429()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        // Make requests to exceed the rate limit (auth limit is configured as 10 in production, but higher in dev)
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 15; i++) // Reduced for faster test execution
        {
            var request = new
            {
                Email = "nonexistent@test.com",
                Password = "wrongpassword"
            };
            var requestJson = JsonSerializer.Serialize(request);

            tasks.Add(client.PostAsync("/api/auth/login", new StringContent(requestJson, Encoding.UTF8, "application/json")));
        }

        var responses = await Task.WhenAll(tasks);

        // Should have at least some 429 responses due to rate limiting
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponses > 0, "Expected some requests to be rate limited");

        // Check for rate limit headers in the 429 responses
        var rateLimitedResponse = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        if (rateLimitedResponse != null)
        {
            Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Policy"), "Should contain rate limit policy header");
            Assert.True(rateLimitedResponse.Headers.Contains("Retry-After"), "Should contain retry-after header");
        }
    }

    [Fact]
    public async Task ContentReadEndpoint_WithAuthentication_RespectsRateLimit()
    {
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        // Create some content first
        var content = new { title = "Test Content" };
        var contentJson = JsonSerializer.Serialize(content);
        await client.PutAsync($"/api/v1/{factory.TestTenant}/test-content", new StringContent(contentJson, Encoding.UTF8, "application/json"));

        // Make many requests to the content read endpoint
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 15; i++) // Reduced for faster test execution
        {
            tasks.Add(client.GetAsync($"/api/v1/{factory.TestTenant}/test-content"));
        }

        var responses = await Task.WhenAll(tasks);

        // Should have some 429 responses due to rate limiting
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponses > 0, "Expected some content read requests to be rate limited");
    }

    [Fact]
    public async Task ContentWriteEndpoint_WithAuthentication_RespectsRateLimit()
    {
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        // Make many write requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 15; i++) // Reduced for faster test execution
        {
            var content = new { title = $"Test Content {i}" };
            var contentJson = JsonSerializer.Serialize(content);
            tasks.Add(client.PutAsync($"/api/v1/{factory.TestTenant}/test-content-{i}", new StringContent(contentJson, Encoding.UTF8, "application/json")));
        }

        var responses = await Task.WhenAll(tasks);

        // Should have some 429 responses due to rate limiting
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponses > 0, "Expected some content write requests to be rate limited");
    }

    [Fact]
    public async Task BulkOperations_RespectsRateLimit()
    {
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        // Create content to delete
        var content = new { title = "Test Content for Bulk Delete" };
        var contentJson = JsonSerializer.Serialize(content);
        await client.PutAsync($"/api/v1/{factory.TestTenant}/bulk-test", new StringContent(contentJson, Encoding.UTF8, "application/json"));

        // Make many bulk delete requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 15; i++) // Reduced for faster test execution
        {
            var deleteRequest = new { Resources = new[] { "bulk-test" } };
            var deleteRequestJson = JsonSerializer.Serialize(deleteRequest);
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/{factory.TestTenant}/bulk-delete")
            {
                Content = new StringContent(deleteRequestJson, Encoding.UTF8, "application/json")
            };
            tasks.Add(client.SendAsync(request));
        }

        var responses = await Task.WhenAll(tasks);

        // Should have some 429 responses due to rate limiting
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponses > 0, "Expected some bulk operation requests to be rate limited");
    }

    [Fact]
    public async Task RateLimit_Headers_AreCorrectlySet()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        // Make enough requests to trigger rate limiting
        HttpResponseMessage? rateLimitedResponse = null;
        for (int i = 0; i < 15; i++)
        {
            var request = new { Email = "test@test.com", Password = "wrongpassword" };
            var requestJson = JsonSerializer.Serialize(request);
            var response = await client.PostAsync("/api/auth/login", new StringContent(requestJson, Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        Assert.NotNull(rateLimitedResponse);

        // Check headers are present
        Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Policy"));
        Assert.True(rateLimitedResponse.Headers.Contains("Retry-After"));

        // Check header values
        var policyHeader = rateLimitedResponse.Headers.GetValues("X-RateLimit-Policy").FirstOrDefault();
        var retryAfterHeader = rateLimitedResponse.Headers.GetValues("Retry-After").FirstOrDefault();

        Assert.Equal("auth", policyHeader);
        Assert.Equal("60", retryAfterHeader);
    }
}
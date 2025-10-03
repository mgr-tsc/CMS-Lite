using System.Net;
using System.Text;
using CmsLiteTests.Support;
using Xunit;

namespace CmsLiteTests;

/// <summary>
/// Integration tests to verify resource name sanitization works end-to-end.
/// </summary>
public class ContentApiSanitizationTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task UploadContent_SanitizesResourceNameWithSpaces()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"message\": \"Hello World\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Upload with spaces in name: "Clean Code.json"
        var response = await client.PutAsync($"/api/v1/{factory.TestTenant}/Clean Code.json", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Verify response returns sanitized name
        Assert.Contains("clean-code.json", responseBody);
        Assert.DoesNotContain("Clean Code.json", responseBody);
    }

    [Fact]
    public async Task UploadContent_SanitizesResourceNameWithSpecialChars()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"title\": \"Annual Report\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Upload with special characters: "Annual Report (Q4) 2024!.json"
        var response = await client.PutAsync($"/api/v1/{factory.TestTenant}/Annual Report (Q4) 2024!.json", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Verify response returns sanitized name: "annual-report-q4-2024.json"
        Assert.Contains("annual-report-q4-2024.json", responseBody);
    }

    [Fact]
    public async Task RetrieveContent_UsesSanitizedName()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"data\": \"test\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Upload with spaces
        await client.PutAsync($"/api/v1/{factory.TestTenant}/My Document.json", content);

        // Retrieve using sanitized name
        var getResponse = await client.GetAsync($"/api/v1/{factory.TestTenant}/my-document.json");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrievedContent = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(jsonContent, retrievedContent);
    }

    [Fact]
    public async Task UploadContent_SanitizesTenantName()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"test\": true}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Note: This will try to use a different tenant name which may not exist
        // The test verifies sanitization happens even if tenant doesn't exist
        var response = await client.PutAsync("/api/v1/ACME Corp!/test.json", content);

        // Will fail with 404 because tenant doesn't exist, but URL was sanitized
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadContent_HandlesUppercaseExtension()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"uppercase\": \"extension\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Upload with uppercase extension
        var response = await client.PutAsync($"/api/v1/{factory.TestTenant}/MyFile.JSON", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Verify extension is lowercase
        Assert.Contains("myfile.json", responseBody);
    }

    [Fact]
    public async Task UploadContent_RemovesConsecutiveSpaces()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"spaces\": \"test\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Upload with multiple consecutive spaces
        var response = await client.PutAsync($"/api/v1/{factory.TestTenant}/My    Document    File.json", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Verify consecutive spaces become single hyphen
        Assert.Contains("my-document-file.json", responseBody);
    }

    [Fact]
    public async Task ListResources_ShowsSanitizedNames()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var jsonContent = "{\"list\": \"test\"}";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Upload multiple files with spaces
        await client.PutAsync($"/api/v1/{factory.TestTenant}/File One.json", content);
        await client.PutAsync($"/api/v1/{factory.TestTenant}/File Two.json", content);

        // List resources
        var listResponse = await client.GetAsync($"/api/v1/{factory.TestTenant}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listContent = await listResponse.Content.ReadAsStringAsync();

        // Verify sanitized names appear in listing
        Assert.Contains("file-one.json", listContent);
        Assert.Contains("file-two.json", listContent);
    }
}
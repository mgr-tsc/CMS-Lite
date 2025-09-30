using System.Net;
using System.Text;
using CmsLiteTests.Support;
using Xunit;

namespace CmsLiteTests;

public class ContentApiPdfTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    // Helper method to create a valid PDF file (minimal PDF structure)
    private static byte[] CreateValidPdf(string content = "Test PDF content")
    {
        var pdfHeader = "%PDF-1.4\n";
        var pdfBody = $"1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 4 0 R >>\nendobj\n4 0 obj\n<< /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n";
        var pdfTrailer = "xref\n0 5\n0000000000 65535 f\n0000000009 00000 n\n0000000056 00000 n\n0000000115 00000 n\n0000000184 00000 n\ntrailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n284\n%%EOF\n";
        return Encoding.UTF8.GetBytes(pdfHeader + pdfBody + pdfTrailer);
    }

    [Fact]
    public async Task UploadPdf_ValidPdf_ReturnsCreated()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var pdfBytes = CreateValidPdf("This is a test PDF document.");
        var response = await client.PutAsync($"/v1/{factory.TestTenant}/test-document.pdf",
            new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("test-document.pdf", content);
        Assert.Contains("version", content);
    }

    [Fact]
    public async Task UploadPdf_InvalidPdfBytes_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create invalid PDF (doesn't start with %PDF-)
        var invalidPdfBytes = Encoding.UTF8.GetBytes("This is not a PDF file");
        var response = await client.PutAsync($"/v1/{factory.TestTenant}/invalid-document.pdf",
            new ByteArrayContent(invalidPdfBytes) { Headers = { ContentType = new("application/pdf") } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid pdf content format", content);
    }

    [Fact]
    public async Task UploadPdf_MissingContentType_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var pdfBytes = CreateValidPdf();
        var content = new ByteArrayContent(pdfBytes);
        content.Headers.ContentType = null; // No Content-Type header

        var response = await client.PutAsync($"/v1/{factory.TestTenant}/no-content-type.pdf", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Content-Type header is required", responseContent);
    }

    [Fact]
    public async Task GetPdf_ExistingPdf_ReturnsPdfContent()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Upload PDF first
        var pdfBytes = CreateValidPdf("Retrieve me!");
        await client.PutAsync($"/v1/{factory.TestTenant}/retrieve-test.pdf",
            new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } });

        // Retrieve PDF
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/retrieve-test.pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var retrievedBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfBytes, retrievedBytes);
    }

    [Fact]
    public async Task GetPdf_NonExistentPdf_ReturnsNotFound()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/nonexistent.pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HeadPdf_ExistingPdf_ReturnsMetadata()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Upload PDF first
        var pdfBytes = CreateValidPdf("Metadata test");
        await client.PutAsync($"/v1/{factory.TestTenant}/metadata-test.pdf",
            new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } });

        // Get metadata with HEAD
        var request = new HttpRequestMessage(HttpMethod.Head, $"/v1/{factory.TestTenant}/metadata-test.pdf");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Content-Type can be in response.Content.Headers or response.Headers
        var contentType = response.Content.Headers.ContentType?.MediaType ??
                         response.Headers.GetValues("Content-Type").FirstOrDefault();
        Assert.Equal("application/pdf", contentType);

        Assert.True(response.Content.Headers.ContentLength > 0);

        // ETag can be in response.Headers.ETag or response.Headers["ETag"]
        var hasETag = response.Headers.ETag != null || response.Headers.Contains("ETag");
        Assert.True(hasETag, "Response should contain ETag header");
    }

    [Fact]
    public async Task UploadPdf_MultipleVersions_CreatesVersionHistory()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Upload version 1
        var pdfV1 = CreateValidPdf("Version 1 content");
        var response1 = await client.PutAsync($"/v1/{factory.TestTenant}/versioned.pdf",
            new ByteArrayContent(pdfV1) { Headers = { ContentType = new("application/pdf") } });
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Upload version 2
        var pdfV2 = CreateValidPdf("Version 2 content - updated");
        var response2 = await client.PutAsync($"/v1/{factory.TestTenant}/versioned.pdf",
            new ByteArrayContent(pdfV2) { Headers = { ContentType = new("application/pdf") } });
        Assert.True(response2.StatusCode == HttpStatusCode.OK || response2.StatusCode == HttpStatusCode.Created);

        // Get latest version (should be version 2)
        var getResponse = await client.GetAsync($"/v1/{factory.TestTenant}/versioned.pdf");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedBytes = await getResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfV2, retrievedBytes);

        // Get version 1 explicitly
        var getV1Response = await client.GetAsync($"/v1/{factory.TestTenant}/versioned.pdf?version=1");
        Assert.Equal(HttpStatusCode.OK, getV1Response.StatusCode);
        var retrievedV1Bytes = await getV1Response.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfV1, retrievedV1Bytes);
    }

    [Fact]
    public async Task DeletePdf_ExistingPdf_ReturnsSoftDeleted()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Upload PDF
        var pdfBytes = CreateValidPdf("Delete me");
        await client.PutAsync($"/v1/{factory.TestTenant}/to-delete.pdf",
            new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } });

        // Delete PDF (soft delete)
        var deleteResponse = await client.DeleteAsync($"/v1/{factory.TestTenant}/to-delete.pdf");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Try to retrieve deleted PDF
        var getResponse = await client.GetAsync($"/v1/{factory.TestTenant}/to-delete.pdf");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ListResources_IncludesPdfFiles()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Upload a PDF
        var pdfBytes = CreateValidPdf("List test");
        await client.PutAsync($"/v1/{factory.TestTenant}/list-test.pdf",
            new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } });

        // List resources
        var listResponse = await client.GetAsync($"/v1/{factory.TestTenant}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var content = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("list-test.pdf", content);
    }

    [Fact]
    public async Task UploadPdf_EmptyBody_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var emptyContent = new ByteArrayContent(Array.Empty<byte>());
        emptyContent.Headers.ContentType = new("application/pdf");

        var response = await client.PutAsync($"/v1/{factory.TestTenant}/empty.pdf", emptyContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Empty body", content);
    }

    [Fact]
    public async Task UploadPdf_WithoutAuthentication_ReturnsUnauthorized()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        // No authentication token

        var pdfBytes = CreateValidPdf();
        var response = await client.PutAsync($"/v1/{factory.TestTenant}/unauthorized.pdf",
            new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
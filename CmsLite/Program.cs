using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using CmsLite.Database.Repositories;
using CmsLite.Database;
using CmsLite.Authentication;
using CmsLite.Content;
using CmsLite.Helpers.RequestMappers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var storageConnectionString = configuration["AzureStorage:ConnectionString"] ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");
var containerName = configuration["AzureStorage:Container"] ?? "cms";
var dbPath = configuration["Database:Path"] ?? "cmslite.db";
// Add database services
builder.Services.AddDbContext<CmsLiteDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});
// Add blob storage services
builder.Services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton<IBlobRepo, BlobRepo>();
builder.AddCmsLiteAuthentication();
var app = builder.Build();

// Health endpoint
app.MapGet("/health", (IHostEnvironment env) =>
{
    var envValue = env.IsDevelopment() ? "dev" : "prod";
    return Results.Ok(new { env = envValue });
}).WithDescription("Check the health of the application.").WithTags("Health");

// Signup endpoint (only in development)
app.MapPost("/attach-user", async (SignUpRequest request, IUserRepo userRepo, ITenantRepo tenantRepo) =>
{
    var tenant = await tenantRepo.GetTenantByIdAsync(request.TenantId);
    if (tenant == null)
    {
        return Results.BadRequest($"Tenant with ID {request.TenantId} does not exist.");
    }
    var existingUser = await userRepo.GetUserByEmailAsync(request.Email);
    if (existingUser != null)
    {
        return Results.Conflict("A user with this email already exists.");
    }
    var newUser = new DbSet.User
    {
        Id = Guid.NewGuid().ToString(),
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        PasswordHash = CmsLite.Helpers.Utilities.HashPassword(request.Password),
        TenantId = request.TenantId,
        IsActive = true
    };
    try
    {
        await userRepo.CreateUserAsync(newUser);
    }
    catch
    {
        return Results.Problem("An error occurred while attaching the user.");
    }
    return Results.Ok(new
    {
        message = "User attached successfully",
        email = request.Email,
        firstName = request.FirstName,
        lastName = request.LastName,
        tenantId = request.TenantId
    });
}).RequireAuthorization().WithDescription("Attach a new user to an existing tenant.").WithTags("SignUp");

app.MapPost("/create-tenant", async (CreateTenantRequest request, ITenantRepo tenantRepo, IUserRepo userRepo, CmsLiteDbContext dbContext) =>
{
    var existingTenant = await tenantRepo.GetTenantByNameAsync(request.Name);
    if (existingTenant != null)
    {
        return Results.Conflict("A tenant with this name already exists.");
    }
    using var transaction = await dbContext.Database.BeginTransactionAsync();
    try
    {
        var newTenant = new DbSet.Tenant
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description
        };
        var newUser = new DbSet.User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.OwnerEmail,
            FirstName = request.OwnerFirstName,
            LastName = request.OwnerLastName,
            PasswordHash = CmsLite.Helpers.Utilities.HashPassword(request.OwnerPassword),
            TenantId = newTenant.Id,
            IsActive = true
        };
        await tenantRepo.CreateTenantAsync(newTenant);
        await userRepo.CreateUserAsync(newUser);
        await transaction.CommitAsync();
        return Results.Ok(new
        {
            message = "Tenant and owner user created successfully",
            tenantId = newTenant.Id,
            ownerUserId = newUser.Id
        });
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}).WithDescription("Create a new tenant along with its owner user.").WithTags("SignUp");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var scopedServices = scope.ServiceProvider;
    var db = scopedServices.GetRequiredService<CmsLiteDbContext>();
    _ = scopedServices.GetRequiredService<IBlobRepo>();
    db.Database.EnsureCreated();
}

app.UseCmsLiteAuthentication();
app.MapAuthenticationEndpoints();
app.MapContentEndpoints();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHttpsRedirection();
}
app.Run();
public partial class Program { }
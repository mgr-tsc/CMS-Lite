# CMS-Lite (JSON Content Store)

A lightweight, multi-tenant JSON content management system with **JWT Authentication** built with **.NET 8** and **React 18**. Provides versioned content storage using **Azure Blob Storage** with **SQLite** metadata indexing, designed for small companies to manage dynamic content.

## 🚀 Features

### 🔐 Authentication & Security
- **JWT Authentication**: Secure token-based authentication with refresh capability
- **Token Rotation**: Enhanced security with session-aware token refresh
- **Tenant Isolation**: Users can only access their tenant's content
- **Session Management**: Active session tracking with revocation support
- **Password Security**: Secure password hashing and verification

### 📁 Content Management
- **Multi-tenant**: Content scoped by tenant with authentication-based isolation
- **Directory Structure**: Hierarchical organization with 5-level nesting (0-4)
- **Versioned**: All content changes create new versions (append-only)
- **Optimistic Concurrency**: ETag-based conflict resolution
- **Soft Deletes**: Resources marked deleted, not physically removed
- **Pagination**: Cursor-based for efficient large dataset handling
- **Root Directory Auto-Creation**: Automatic root directory per tenant

### 🎨 Modern Frontend
- **React 18 Application**: Modern responsive web interface
- **FluentUI Components**: Microsoft's design system integration
- **TypeScript**: Full type safety across the application
- **Authentication Context**: Complete auth state management

---

## 🐳 Running Locally with Docker Compose

**Start the complete development environment:**

```sh
docker compose -f docker/docker-compose.dev.yaml up --build
```

**Access the services:**
- **React Web App:** [http://localhost:9090](http://localhost:9090) (Main UI with Authentication)
- **API:** [http://localhost:8080](http://localhost:8080) (REST API with JWT)
- **Azurite Storage:** [http://localhost:10000](http://localhost:10000) (Azure Storage Emulator)

**Health check:**
```sh
curl http://localhost:8080/health
```

**Stop services:**
```sh
docker compose -f docker/docker-compose.dev.yaml down
```

---

## 🔑 Authentication Flow

### Demo Credentials
```
Email: admin@email.com
Password: abc
```

### API Authentication
```bash
# Login
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@email.com", "password": "abc"}'

# Response includes JWT token
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": "user-id",
    "email": "admin@email.com",
    "tenant": { "id": "acme" }
  }
}

# Use token for authenticated requests
curl -X GET http://localhost:8080/v1/acme/config \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Refresh token (with rotation)
curl -X POST http://localhost:8080/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"token": "YOUR_CURRENT_TOKEN"}'
```

---

## 📡 API Endpoints

### 🔐 Authentication (Public)
- **POST /auth/login** → User authentication with email/password
- **POST /auth/logout** → Revoke current session
- **GET /auth/me** → Get current user information
- **POST /auth/refresh** → Refresh authentication token (with rotation)

### 👥 User & Tenant Management (Public)
- **POST /create-tenant** → Create new tenant with owner user

### 👥 User & Tenant Management (Authenticated)
- **POST /attach-user** → Attach user to existing tenant

### 📁 Content Management (Authenticated)
- **PUT /v1/{tenant}/{resource}** → Create/update JSON content (auto-versioned)
  - *Optional header*: `X-Directory-Id: {directoryId}` (if not provided, uses root directory)
- **GET /v1/{tenant}/{resource}** → Retrieve content (latest or specific version)
- **HEAD /v1/{tenant}/{resource}** → Get metadata without content body
- **DELETE /v1/{tenant}/{resource}** → Soft delete content
- **GET /v1/{tenant}** → List tenant resources (with filtering/pagination)
- **GET /v1/{tenant}/{resource}/versions** → List all versions of resource

### 📂 Directory Management (Authenticated)
- **GET /v1/{tenant}/directories** → List directory tree for tenant
- **POST /v1/{tenant}/directories** → Create new directory with optional parent
- **GET /v1/{tenant}/directories/{id}** → Get directory details and metadata
- **GET /v1/{tenant}/directories/{id}/contents** → Get content items in directory

**Features:**
- **Hierarchical Structure**: Support for up to 5 nesting levels (0-4)
- **Root Directory**: Automatic creation per tenant
- **Tenant Isolation**: Secure directory access control
- **Content Organization**: Directory-based content assignment
- **Read-Only Operations**: View and create directories (update/delete coming later)

### 🏥 System
- **GET /health** → Service health check

All content endpoints require **Bearer token authentication** and enforce **tenant isolation**.

**Security Note**: Directory structure is not exposed in URLs for security. Directory assignment uses secure header-based approach.

---

## 📮 Postman API Testing

### Complete Authentication & Content Workflow

#### 1. **Authentication Flow**

**Login** (POST /auth/login):
```bash
POST http://localhost:8080/auth/login
Content-Type: application/json

{
  "email": "admin@email.com",
  "password": "abc"
}

# Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T12:30:00Z",
  "user": {
    "id": "test-user-id",
    "email": "admin@email.com",
    "firstName": "Test",
    "lastName": "User",
    "tenant": {
      "id": "acme",
      "name": "acme"
    }
  }
}
```

**Get Current User** (GET /auth/me):
```bash
GET http://localhost:8080/auth/me
Authorization: Bearer {your-jwt-token}
```

#### 2. **Content Management with Directory Support**

**Create Content in Root Directory** (PUT /v1/{tenant}/{resource}):
```bash
PUT http://localhost:8080/v1/acme/config
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "appName": "My Application",
  "version": "1.0.0",
  "settings": {
    "theme": "dark",
    "language": "en"
  }
}

# Response (201 Created):
{
  "tenant": "acme",
  "resource": "config",
  "version": 1,
  "etag": "etag-abc123...",
  "sha256": "sha256hash...",
  "size": 128
}
```

**Create Content in Specific Directory** (PUT /v1/{tenant}/{resource}):
```bash
PUT http://localhost:8080/v1/acme/homepage
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
X-Directory-Id: {directory-id-from-your-setup}

{
  "title": "Welcome to Our Site",
  "content": "This is the homepage content",
  "published": true
}
```

**Retrieve Content** (GET /v1/{tenant}/{resource}):
```bash
GET http://localhost:8080/v1/acme/config
Authorization: Bearer {your-jwt-token}

# Response includes content + ETag header
```

**Get Content Metadata** (HEAD /v1/{tenant}/{resource}):
```bash
HEAD http://localhost:8080/v1/acme/config
Authorization: Bearer {your-jwt-token}

# Response: Headers only (ETag, Content-Length, Content-Type)
```

**List Tenant Resources** (GET /v1/{tenant}):
```bash
GET http://localhost:8080/v1/acme
Authorization: Bearer {your-jwt-token}

# With filtering:
GET http://localhost:8080/v1/acme?prefix=home&limit=10

# Response:
{
  "items": [
    {
      "id": 1,
      "tenantId": "acme",
      "resource": "config",
      "latestVersion": 1,
      "etag": "etag-abc123...",
      "byteSize": 128,
      "sha256": "sha256hash...",
      "updatedAtUtc": "2024-01-01T12:00:00Z"
    }
  ],
  "nextCursor": null
}
```

**Update Content with Optimistic Concurrency** (PUT /v1/{tenant}/{resource}):
```bash
PUT http://localhost:8080/v1/acme/config
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
If-Match: {etag-from-previous-response}

{
  "appName": "My Updated Application",
  "version": "2.0.0"
}
```

**Get Content Versions** (GET /v1/{tenant}/{resource}/versions):
```bash
GET http://localhost:8080/v1/acme/config/versions
Authorization: Bearer {your-jwt-token}

# Response:
[
  {
    "version": 2,
    "etag": "etag-def456...",
    "sha256": "newhash...",
    "byteSize": 145,
    "createdAtUtc": "2024-01-01T12:05:00Z"
  },
  {
    "version": 1,
    "etag": "etag-abc123...",
    "sha256": "oldhash...",
    "byteSize": 128,
    "createdAtUtc": "2024-01-01T12:00:00Z"
  }
]
```

**Get Specific Version** (GET /v1/{tenant}/{resource}?version={version}):
```bash
GET http://localhost:8080/v1/acme/config?version=1
Authorization: Bearer {your-jwt-token}
```

**Soft Delete Content** (DELETE /v1/{tenant}/{resource}):
```bash
DELETE http://localhost:8080/v1/acme/config
Authorization: Bearer {your-jwt-token}

# Response: 204 No Content
```

#### 3. **Directory Management Examples**

**List Directory Tree** (GET /v1/{tenant}/directories):
```bash
GET http://localhost:8080/v1/acme/directories
Authorization: Bearer {your-jwt-token}

# Response:
{
  "directories": [
    {
      "id": "root-dir-id",
      "name": "Root",
      "level": 0,
      "parentId": null,
      "isRoot": true,
      "contentCount": 2,
      "createdAtUtc": "2024-01-01T12:00:00Z"
    },
    {
      "id": "docs-dir-id",
      "name": "Documents",
      "level": 1,
      "parentId": "root-dir-id",
      "isRoot": false,
      "contentCount": 5,
      "createdAtUtc": "2024-01-01T12:30:00Z"
    }
  ],
  "totalCount": 2
}
```

**Create New Directory** (POST /v1/{tenant}/directories):
```bash
POST http://localhost:8080/v1/acme/directories
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "name": "Projects",
  "parentId": "root-dir-id"
}

# Response (201 Created):
{
  "id": "new-projects-dir-id",
  "name": "Projects",
  "level": 1,
  "parentId": "root-dir-id",
  "isRoot": false,
  "createdAtUtc": "2024-01-01T12:45:00Z"
}
```

**Get Directory Details** (GET /v1/{tenant}/directories/{id}):
```bash
GET http://localhost:8080/v1/acme/directories/docs-dir-id
Authorization: Bearer {your-jwt-token}

# Response:
{
  "id": "docs-dir-id",
  "name": "Documents",
  "level": 1,
  "parentId": "root-dir-id",
  "isRoot": false,
  "isActive": true,
  "contentCount": 5,
  "subDirectoryCount": 2,
  "createdAtUtc": "2024-01-01T12:30:00Z",
  "updatedAtUtc": "2024-01-01T12:30:00Z"
}
```


**Get Directory Contents** (GET /v1/{tenant}/directories/{id}/contents):
```bash
GET http://localhost:8080/v1/acme/directories/docs-dir-id/contents
Authorization: Bearer {your-jwt-token}

# Response:
{
  "directory": {
    "id": "docs-dir-id",
    "name": "Documentation",
    "level": 1,
    "parentId": "root-dir-id",
    "isRoot": false
  },
  "contentItems": [
    {
      "id": 1,
      "resource": "api-docs",
      "latestVersion": 3,
      "contentType": "application/json",
      "byteSize": 2048,
      "etag": "etag-xyz789...",
      "createdAtUtc": "2024-01-01T12:35:00Z",
      "updatedAtUtc": "2024-01-01T12:50:00Z"
    }
  ],
  "nextCursor": null,
  "totalCount": 1
}
```


#### 4. **Directory Security Testing**

**Test Invalid Directory ID**:
```bash
PUT http://localhost:8080/v1/acme/test-content
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
X-Directory-Id: invalid-directory-id

{
  "test": "content"
}

# Expected: 400 Bad Request
# Response: "Invalid directory ID: invalid-directory-id"
```

**Test Cross-Tenant Directory Access**:
```bash
PUT http://localhost:8080/v1/acme/test-content
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
X-Directory-Id: {directory-id-from-different-tenant}

{
  "test": "content"
}

# Expected: 400 Bad Request
# Response: "Invalid directory ID: {directory-id}"
```


**Test Directory Nesting Limit**:
```bash
# After creating 5 levels (0,1,2,3,4), attempt to create 6th level
POST http://localhost:8080/v1/acme/directories
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "name": "Level5_ShouldFail",
  "parentId": "level-4-directory-id"
}

# Expected: 400 Bad Request
# Response: "Maximum directory nesting level (5) exceeded. Cannot create subdirectory."
```

#### 5. **Error Scenarios**

**Unauthorized Request**:
```bash
GET http://localhost:8080/v1/acme/config
# No Authorization header

# Expected: 401 Unauthorized
```

**Invalid Tenant**:
```bash
GET http://localhost:8080/v1/nonexistent-tenant/config
Authorization: Bearer {your-jwt-token}

# Expected: 400 Bad Request
# Response: "Tenant 'nonexistent-tenant' not found"
```

**Optimistic Concurrency Conflict**:
```bash
PUT http://localhost:8080/v1/acme/config
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
If-Match: wrong-etag

{
  "updated": "content"
}

# Expected: 412 Precondition Failed
```

#### 6. **Logout & Token Management**

**Logout** (POST /auth/logout):
```bash
POST http://localhost:8080/auth/logout
Authorization: Bearer {your-jwt-token}

# Response: 200 OK
# Token is now invalidated
```

**Refresh Token** (POST /auth/refresh):
```bash
POST http://localhost:8080/auth/refresh
Content-Type: application/json

{
  "token": "{your-current-jwt-token}"
}

# Response: New token with rotated session ID
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T13:00:00Z"
}
```

### Postman Environment Variables
Set up these variables for easy testing:
- `baseUrl`: `http://localhost:8080`
- `jwtToken`: `{set-from-login-response}`
- `tenant`: `acme`
- `etag`: `{set-from-content-responses}`

---

## 🏗️ Architecture Overview

### Technology Stack
- **Backend**: .NET 8 ASP.NET Core Minimal API
- **Authentication**: JWT Bearer tokens with session management
- **Database**: SQLite with Entity Framework Core
- **Storage**: Azure Blob Storage (Azurite for development)
- **Frontend**: React 18 + TypeScript + FluentUI + Vite
- **Testing**: xUnit with comprehensive integration tests
- **DevOps**: Docker Compose development environment

### Database Schema
- **Tenant**: Multi-tenant organization entities
- **User**: User accounts with tenant association
- **UserSession**: JWT session tracking for revocation
- **Directory**: Hierarchical folder structure with 5-level nesting limit
- **ContentItem**: Latest version metadata per tenant/resource with directory assignment
- **ContentVersion**: Complete version history (append-only)

### Security Architecture
- **Authentication Middleware**: JWT validation on all content endpoints
- **Tenant Validation Middleware**: Ensures users can only access their tenant's data
- **Token Rotation**: New session ID generated on each refresh for enhanced security
- **Session Tracking**: Active session management with database persistence

---

## 🏗️ Project Structure

```
CMS-Lite/
├── CmsLite/                         # .NET 8 Web API with Authentication
│   ├── Program.cs                   # Minimal API setup with auth integration
│   ├── Authentication/              # JWT authentication system
│   │   ├── AuthenticationEndpoints.cs     # Auth API endpoints
│   │   ├── CmsLiteAuthenticationService.cs # Core auth logic
│   │   ├── TenantValidationMiddleware.cs   # Tenant isolation
│   │   └── AuthenticationServiceRegistration.cs # DI setup
│   ├── Content/                     # Content management endpoints
│   │   └── ContentEndpoints.cs      # Content API with authorization
│   ├── Database/                    # EF Core context & repositories
│   │   ├── CmsLiteDbContext.cs      # Full schema with relationships
│   │   └── Repositories/            # User, UserSession, Blob repos
│   ├── Middlewares/                 # Custom middleware
│   └── Helpers/                     # Utilities and validation
├── CmsLiteTests/                    # Comprehensive test suite
│   ├── ContentApiTests.cs           # Content CRUD with auth
│   └── Support/                     # Test infrastructure
├── cms-lite-web-client/             # React 18 + TypeScript + FluentUI
│   ├── src/                         # 20+ TypeScript components
│   │   ├── contexts/AuthContext.tsx # Authentication state management
│   │   ├── types/auth.ts            # TypeScript auth types
│   │   └── components/              # UI components with FluentUI
│   ├── package.json                 # React Router, FluentUI dependencies
│   └── vite.config.ts               # Vite build configuration
├── docker/                          # Development environment
│   ├── docker-compose.dev.yaml     # API + React + Azurite
│   └── Dockerfile.web               # React app containerization
└── *.md                             # Comprehensive documentation
```

---

## ⚙️ Configuration

### JWT Configuration (appsettings.Development.json)
```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;...",
    "Container": "cms-dev"
  },
  "Database": {
    "Path": "cmslite-dev.db"
  },
  "Jwt": {
    "Key": "your-development-jwt-secret-key-32-chars-min",
    "Issuer": "CmsLite",
    "Audience": "CmsLite-Users",
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  }
}
```

### Environment Variables (Production)
- `JWT_SECRET_KEY` - JWT signing key (32+ characters)
- `AzureStorage:ConnectionString` - Azure Storage connection string
- `AzureStorage:Container` - Container name (default: "cms")
- `Database:Path` - SQLite database path (default: "cmslite.db")

---

## 🛠️ Local Development

### Prerequisites
- .NET 8 SDK
- Node.js 18+ (for React frontend)
- Docker (for Azurite emulator)

### API Development

**For Local Development (with HTTPS):**
```bash
dotnet restore
dotnet run --project CmsLite          # API at http://localhost:8080 + https://localhost:5050
dotnet test                           # Run comprehensive test suite
```

**Environment Configuration:**
- **Local Development**: Uses `Local` environment → `appsettings.Local.json` (with HTTPS support)
- **Docker Development**: Uses `Development` environment → `appsettings.Development.json` (HTTP only)

**⚠️ Local Development Requirements:**
When running locally (without Docker), you need Azurite emulator running:
```bash
# Install Azurite globally (one-time setup)
npm install -g azurite

# Start Azurite emulator (required for local development)
azurite --silent --location ./azurite-data --debug ./azurite-debug.log

# Azurite will run on:
# - Blob service: http://127.0.0.1:10000
# - Queue service: http://127.0.0.1:10001
# - Table service: http://127.0.0.1:10002
```

The `appsettings.Local.json` is configured to use `UseDevelopmentStorage=true` which connects to these default Azurite ports.

### Frontend Development
```bash
cd cms-lite-web-client
npm install
npm run dev                           # React app at :5173
npm run build                         # Production build
npm run lint                          # ESLint + TypeScript checking
```

### Full Stack Development
```bash
# Recommended: Use Docker Compose for full environment
docker compose -f docker/docker-compose.dev.yaml up --build
```

---

## 🧪 Testing

```bash
# Run all tests (API + Authentication)
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage Includes
- ✅ Authentication flow (login/logout/refresh/token rotation)
- ✅ Content CRUD operations with tenant isolation
- ✅ Directory structure and hierarchical organization
- ✅ Directory nesting validation (5-level limit)
- ✅ Root directory auto-creation and protection
- ✅ Compensation pattern for blob/database consistency
- ✅ JWT middleware and authorization
- ✅ Version management and rollback
- ✅ Optimistic concurrency control
- ✅ Error handling and edge cases
- ✅ Multi-tenant data isolation

---

## 📋 Current Status

### ✅ Completed Features
- **🔐 Complete Authentication System**: JWT with session management and token rotation
- **🏢 Multi-tenant Content API**: All CRUD operations with tenant isolation
- **📂 Directory Management**: Hierarchical organization with 5-level nesting and security
- **⚖️ Transactional Consistency**: Compensation pattern for blob/database operations
- **🛡️ Enhanced Security**: Directory validation, tenant isolation, root protection
- **🎨 React Frontend**: Responsive UI with authentication integration
- **🗄️ Database Schema**: Optimized relationships with directory support
- **🛡️ Security Middleware**: Authentication and tenant validation
- **🧪 Comprehensive Testing**: 36 tests with directory API and compensation coverage
- **🐳 Docker Environment**: Full development containerization
- **📚 OpenAPI Documentation**: Swagger integration for API endpoints

### 🚧 Current Development Focus
- **Frontend-Backend Integration**: Connecting React UI to authentication endpoints
- **Content Management Interface**: JSON content editing in React
- **User Management**: Admin interface for user creation
- **Error Handling**: Improved user feedback and error states

### 📅 Roadmap
- **👥 User Role Management**: Role-based permissions system
- **📊 Audit Logging**: Track all content and authentication events
- **⚡ Performance**: Caching and optimization
- **🚀 Production Deploy**: Azure/AWS deployment configuration
- **📈 Monitoring**: Application insights and health checks

---

## 🔒 Security Features

### Authentication & Authorization
- **JWT Tokens**: Short-lived (30 minutes) with refresh capability
- **Token Rotation**: New session ID on each refresh for enhanced security
- **Session Revocation**: Database-tracked sessions for immediate invalidation
- **Password Security**: SHA-256 hashing (upgrade to bcrypt recommended)

### Tenant Isolation
- **Database-level**: Foreign key constraints ensure data separation
- **API-level**: Middleware validates tenant access on every request
- **JWT Claims**: User's tenant embedded in token for fast validation

### Production Considerations
- **HTTPS Only**: All traffic encrypted in production
- **Secret Management**: JWT keys via environment variables
- **CORS**: Configured for frontend domain
- **Rate Limiting**: Planned for authentication endpoints

---

## 📚 Documentation

- **Comprehensive development guide** → Detailed API documentation and examples
- **Authentication implementation** → JWT system and security details
- **Database schema design** → Entity relationships and architecture decisions
- **📖 OpenAPI/Swagger** → Interactive API documentation at `/swagger` (development)

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Implement changes with tests
4. Ensure all tests pass: `dotnet test`
5. Run linting: `npm run lint` (for frontend changes)
6. Submit a pull request

### Development Commands
```bash
# Backend
dotnet build                          # Build API
dotnet test                           # Run tests
dotnet watch run --project CmsLite    # Hot reload

# Frontend
npm run dev                           # Development server
npm run build                         # Production build
npm run lint                          # Code quality

# Docker
docker compose -f docker/docker-compose.dev.yaml logs -f  # View logs
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🚀 Getting Started Summary

1. **Clone repository**: `git clone <repo-url>`
2. **Start environment**: `docker compose -f docker/docker-compose.dev.yaml up --build`
3. **Access web app**: [http://localhost:9090](http://localhost:9090)
4. **Login with demo**: `admin@email.com` / `abc`
5. **Explore API**: [http://localhost:8080/swagger](http://localhost:8080/swagger)

**Ready for full-stack development with authentication! 🎯**
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
- **Versioned**: All content changes create new versions (append-only)
- **Optimistic Concurrency**: ETag-based conflict resolution
- **Soft Deletes**: Resources marked deleted, not physically removed
- **Pagination**: Cursor-based for efficient large dataset handling

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
- **POST /attach-user** → Attach user to existing tenant

### 📁 Content Management (Authenticated)
- **PUT /v1/{tenant}/{resource}** → Create/update JSON content (auto-versioned)
- **GET /v1/{tenant}/{resource}** → Retrieve content (latest or specific version)
- **HEAD /v1/{tenant}/{resource}** → Get metadata without content body
- **DELETE /v1/{tenant}/{resource}** → Soft delete content
- **GET /v1/{tenant}** → List tenant resources (with filtering/pagination)
- **GET /v1/{tenant}/{resource}/versions** → List all versions of resource

### 🏥 System
- **GET /health** → Service health check

All content endpoints require **Bearer token authentication** and enforce **tenant isolation**.

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
- **ContentItem**: Latest version metadata per tenant/resource
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
```bash
dotnet restore
dotnet run --project CmsLite          # API with authentication at :5000
dotnet test                           # Run comprehensive test suite
```

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
- **🎨 React Frontend**: Responsive UI with authentication integration
- **🗄️ Database Schema**: Optimized relationships with proper foreign keys
- **🛡️ Security Middleware**: Authentication and tenant validation
- **🧪 Comprehensive Testing**: Unit and integration test coverage
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

- **🏗️ WARP.md** → Comprehensive development guide with API documentation
- **🤖 CLAUDE.md** → AI assistant guidance and project context
- **🔐 AUTH_API_BRAINSTORM.md** → Authentication implementation details
- **🗄️ AUTH_DATABASE_BRAINSTORM.md** → Database schema design decisions
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
# CMS-Lite (JSON Content Store)

A lightweight CMS-style service for storing **tenant-scoped JSON content** in **Azure Blob Storage**, with **SQLite** metadata indexing, exposed via a **.NET 8 Minimal API**, and fully containerized.

Think of it as a dead-simple **content registry**:  
- Each tenant has its own namespace (`/{tenant}/{resource}/v{n}.json`)  
- All writes are **versioned** (append-only)  
- Metadata is indexed locally in SQLite for fast listing  
- Reads always serve JSON directly from blob storage  
- Interaction is **API-only** (no UI yet)  
- Authentication/authorization can be layered in later

---
## 🐳 Running Locally with Docker Compose

To run the app and its dependencies locally using Docker Compose:

1. **Build and start the services:**

   ```sh
   docker compose -f docker/docker-compose.dev.yaml up --build
   ```

2. **Access the API:**

   The API will be available at [http://localhost:8080](http://localhost:8080).

3. **Health check:**

   You can verify the API is running by visiting [http://localhost:8080/health](http://localhost:8080/health) in your browser or with:

   ```sh
   curl http://localhost:8080/health
   ```

4. **Stopping the services:**

   Press `Ctrl+C` in the terminal, or run:

   ```sh
   docker compose -f docker/docker-compose.dev.yaml down
   ```

---

## 📂 Storage Model

- **Container:** `cms`
- **Blob key convention:**  
- **SQLite schema:**
- `ContentItem`: tracks latest version + metadata per resource  
- `ContentVersion`: append-only version history  

---

## 🚀 Features

- **PUT /v1/{tenant}/{resource}** → upload JSON, auto-versioned  
- **GET /v1/{tenant}/{resource}** → fetch JSON (latest or ?version=n)  
- **HEAD /v1/{tenant}/{resource}** → metadata only  
- **GET /v1/{tenant}** → list resources for tenant  
- **DELETE /v1/{tenant}/{resource}** → soft delete  
- **GET /v1/{tenant}/{resource}/versions** → list versions  

✅ Optimistic concurrency with `If-Match: <ETag>`  
✅ SHA-256 integrity hashes  
✅ Soft delete (keeps history)  
✅ Cursor-based pagination for lists  

---

## 🛠️ Project Layout
---

## ⚙️ Configuration

`appsettings.json`

```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "Container": "cms"
  },
  "Database": {
    "Path": "cms.sqlite"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}


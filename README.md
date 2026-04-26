# SmartJobTracker

A full-stack job application tracker built with ASP.NET Core 8 and React 18, featuring JWT authentication, AI-powered job search, and multi-source external job aggregation.

---

## Tech Stack

**Backend:** ASP.NET Core 8 Web API, Entity Framework Core 8, SQL Server Express, BCrypt.Net, JWT Bearer Auth, iText7 (PDF parsing)

**Frontend:** React 18, React Router v6, Axios, Tailwind CSS

---

## Features

- **User Authentication** — Register and login with JWT (72-hour tokens, BCrypt password hashing). All data is scoped per user.
- **Job Application Tracking** — Add, update, and manage your job applications with status tracking.
- **Saved Jobs** — Bookmark jobs to apply to later.
- **External Job Search** — Aggregate live listings from Adzuna, JSearch (LinkedIn / Indeed / Glassdoor), NodeFlair, and Careers@Gov.
- **AI Job Search** — AI-powered job matching with visa sponsorship scoring and resume tailoring suggestions.
- **Resume Upload** — Upload a PDF resume to auto-populate your profile.
- **Dashboard** — Overview of application stats and recent activity.

---

## Project Structure

```
SmartJobTracker/
├── SmartJobTracker.API/          # ASP.NET Core 8 Web API
│   ├── Controllers/              # API endpoints
│   ├── DTOs/                     # Request/response models
│   ├── Entities/                 # EF Core entity models
│   ├── Services/                 # Business logic (AI search, external jobs, resume)
│   ├── appsettings.json          # Config (DB connection string, JWT settings)
│   └── Program.cs                # App bootstrap, middleware, EF migrations
│
└── smart-job-tracker-ui/         # React 18 frontend
    └── src/
        ├── Pages/                # Login, Register, Dashboard, AppliedJobs, etc.
        ├── components/           # Layout, Sidebar, JobCard, modals
        ├── contexts/             # AuthContext (JWT state)
        ├── services/             # api.js (Axios + interceptors), authService.js
        └── App.js                # Routes + ProtectedRoute wrapper
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server Express (or any SQL Server instance)
- Node.js 18+

### Backend Setup

1. Update the connection string in `SmartJobTracker.API/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=SmartJobTrackerDB;Trusted_Connection=True;"
   }
   ```

2. Add your API keys to `appsettings.json`:
   ```json
   "ExternalJobApis": {
     "AdzunaAppId": "YOUR_ADZUNA_APP_ID",
     "AdzunaApiKey": "YOUR_ADZUNA_API_KEY",
     "JSearchApiKey": "YOUR_JSEARCH_API_KEY"
   }
   ```

3. Run the API (tables are auto-created on first start):
   ```bash
   cd SmartJobTracker.API
   dotnet run
   ```

   Swagger UI: `https://localhost:7xxx/swagger`

### Frontend Setup

```bash
cd smart-job-tracker-ui
npm install
npm start
```

The app runs at `http://localhost:3000` and proxies API calls to the backend.

---

## Authentication

All API endpoints (except `/api/auth/register` and `/api/auth/login`) require a Bearer token:

```
Authorization: Bearer <jwt_token>
```

The React frontend automatically attaches the token via an Axios request interceptor and redirects to `/login` on 401 responses.

---

## External Job Sources

| Source | Coverage |
|---|---|
| Adzuna | Global |
| JSearch (RapidAPI) | LinkedIn, Indeed, Glassdoor |
| NodeFlair | Singapore tech jobs |
| Careers@Gov | Singapore government jobs |

---

## Environment Variables (never commit these)

| Key | Description |
|---|---|
| `Jwt:Key` | JWT signing secret |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `ExternalJobApis:AdzunaApiKey` | Adzuna API key |
| `ExternalJobApis:JSearchApiKey` | JSearch (RapidAPI) key |

---

## License

MIT

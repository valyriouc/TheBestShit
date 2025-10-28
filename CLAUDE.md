# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Top5 Resources Platform - A community-driven platform for curating and ranking the top 5 resources per topic using confidence-based voting algorithms. Resources are organized into Categories → Sections → Resources with upvote/downvote mechanisms and score calculations.

## Tech Stack

- **Backend**: ASP.NET Core 8.0 Web API
  - Entity Framework Core with SQLite (TopFive.db)
  - ASP.NET Core Identity for authentication (JWT-based)
  - Default route pattern: `/[controller]/[action]`
- **Frontend**: Vue 3 with Composition API
  - Vite dev server (port 5173)
  - Vue Router for navigation
  - Pinia for state management

## Development Commands

### Backend (ASP.NET Core)
```bash
# Run backend API (from iteration1 directory or solution root)
dotnet run --project iteration1

# Build backend
dotnet build

# Run tests
dotnet test

# Backend runs on http://localhost:5190 by default
```

### Frontend (Vue/Vite)
```bash
# Navigate to client directory first
cd thebestclient

# Install dependencies
npm install

# Run dev server (port 5173)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run unit tests (Vitest)
npm run test:unit
```

## Architecture

### Data Model Hierarchy
```
Category (owner, sections[])
  └─ Section (owner, resources[], publicEdit flag)
      └─ Resource (owner, votes, score)
          └─ Vote (user, direction: bool)
```

### Backend Structure

- **Models/** - Entity definitions (Resource.cs contains all entities)
  - `Resource`: Community-submitted resource with URL, votes, and confidence score
  - `Category`: Top-level grouping with name, description, sections
  - `Section`: Mid-level container for related resources
  - `Vote`: User voting record (Direction: true=upvote, false=downvote)
  - `TopFiveUser`: Extends IdentityUser with Trust property

- **Controllers/** - API endpoints
  - `ResourceController`: CRUD for resources, `/Resource/five` for top 5 query
  - `CategoryController`: Category management
  - `SectionController`: Section management
  - `VoteController`: Voting operations
  - `UserController`: User operations
  - All inherit from `AppBaseController` which provides `GetCurrentUserAsync()`

- **Voting/** - Ranking algorithms
  - `ConfidenceRankingAlgorithm`: Wilson score confidence interval (80% confidence)
  - `HotRankingAlgorithm`: Reddit-style hot ranking with time decay
  - Currently using confidence algorithm with 20-vote minimum threshold

- **Response/** - API response wrappers (`AppResponseInfo<T>`)
- **Paging/** - Pagination utilities

### Frontend Structure

- **views/** - Page components
  - Home, Login, Register, Profile
  - Categories (browse), Top5 (view category sections)
  - editing/CreateCategory, editing/CreateSection

- **stores/** - Pinia state management
  - `user.js`: Authentication and user state

- **api/** - API client utilities
  - `apiHelper.js`: `ApiHelper` class with `authorizedFetch()` method
  - Base URL: `http://localhost:5190`
  - Handles 401 redirects to login automatically

- **router/** - Vue Router configuration
  - Routes in `index.js`

## Key Implementation Details

### Authentication Flow
- Backend uses ASP.NET Core Identity with JWT tokens
- Identity endpoints mapped via `app.MapIdentityApi<TopFiveUser>()`
- Frontend stores token in Pinia store (user.js)
- `ApiHelper.authorizedFetch()` includes token and handles 401 errors
- NoOpEmailSender used in development (no actual emails sent)

### CORS Configuration
In development mode only, CORS is enabled for Vue dev server:
- Allowed origin: `http://localhost:5173`
- Credentials allowed

### Scoring System
Resources require minimum 20 total votes to receive non-zero score. Score calculated using Wilson score confidence interval (80% confidence level) - gives lower bound of confidence interval for proportion of upvotes. This prevents new items with few votes from dominating rankings.

### Database
- SQLite database: `TopFive.db` in iteration1 directory
- EnsureCreated() called on context initialization (creates DB if missing)
- No formal migrations - database schema created automatically

### Public Editing
Categories and Sections have `PublicEdit` boolean flag (currently defaults to false). This determines whether non-owners can add content. Note: Comment in Resource.cs asks "what if another user wants to add a section or resource to a category or section of someone else" - this is an open design question.

## Common Patterns

### API Response Pattern
Controllers return `AppResponseInfo<T>` wrapper with HttpStatusCode, message, and data:
```csharp
return Ok(new AppResponseInfo<ResourceRequest>(
    HttpStatusCode.OK,
    "Resource created successfully.",
    request));
```

### Authorization
- Most endpoints require authentication (inherited from AppBaseController)
- Use `[AllowAnonymous]` attribute for public endpoints (e.g., `/Resource/five`)
- Use `GetCurrentUserAsync()` from base controller to get authenticated user

### Frontend API Calls
Always use `ApiHelper.authorizedFetch()` for authenticated requests:
```javascript
const response = await ApiHelper.authorizedFetch('/endpoint', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
});
```

## Testing framework 
For mocking the actual database we use the `DbContextOptions<ApplicationDbContext>` with an actual SQLite database
Sample:
```csharp
  SqliteConnection connection = new SqliteConnection("Filename=:memory:"); 
  connection.Open();
        
        // Setup in-memory database with unique name per test instance
    DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite(connection)
      .ConfigureWarnings(x => x.Default(Microsoft.EntityFrameworkCore.WarningBehavior.Ignore))
      .Options;
        
```
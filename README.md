# schedule-ders

ASP.NET Core app for managing courses, SI sessions, SI requests, and SI leaders.

## Stack
- ASP.NET Core MVC + Razor Pages
- Entity Framework Core
- PostgreSQL locally and on Railway
- ASP.NET Core Identity

## UI / Theme Notes
The current UI uses a shared CSS token setup with light and dark theme support.

Theme files:
- `schedule-ders/wwwroot/css/site.css`
- `schedule-ders/wwwroot/css/base.css`
- `schedule-ders/wwwroot/css/components.css`
- `schedule-ders/wwwroot/css/theme.css`

Primary light-theme tokens live in `schedule-ders/wwwroot/css/theme.css` under `:root`.
Current core colors:
- Accent blue: `#0067C5`
- Accent green: `#007F3E`
- Ink: `#002E57`

If you are making visual changes, start in `theme.css` for colors and `components.css` for shared component styling.

## Clone And Run

### 1. Prerequisites
- .NET SDK 10
- PostgreSQL running locally

Default local database settings used by the app:

```text
Host=localhost;Port=5432;Database=ScheduleDB;Username=postgres;Password=postgres;SSL Mode=Disable
```

### 2. Create the local database
If PostgreSQL is installed but the app database does not exist yet:

```powershell
createdb -h localhost -p 5432 -U postgres ScheduleDB
```

If `createdb` is not on your PATH, use the PostgreSQL install location, for example:

```powershell
& 'C:\Program Files\PostgreSQL\17\bin\createdb.exe' -h localhost -p 5432 -U postgres ScheduleDB
```

### 3. From the repo root
Run these commands from:

```text
C:\Users\<your-user>\source\repos\schedule-ders
```

```powershell
dotnet restore
dotnet build schedule-ders/schedule-ders.csproj -t:Compile
dotnet ef database update --project schedule-ders --context ScheduleContext
dotnet run --project schedule-ders
```

### 4. If you are already inside the app folder
If your terminal is already in:

```text
...\schedule-ders\schedule-ders
```

use:

```powershell
dotnet restore
dotnet build -t:Compile
dotnet ef database update --context ScheduleContext
dotnet run
```

## Optional Local Admin Login
Demo users are only seeded in `Development` and only when a demo password is configured.

From the repo root:

```powershell
dotnet user-secrets set "Seed:DemoUserPassword" "Admin123!" --project schedule-ders
dotnet run --project schedule-ders
```

Or from inside `schedule-ders/schedule-ders`:

```powershell
dotnet user-secrets set "Seed:DemoUserPassword" "Admin123!"
dotnet run
```

Local demo logins:
- Admin: `admin@email.com`
- Professor: `professor@email.com`
- Student: `student@email.com`

Example password:
- `Admin123!`

## Railway Deploy Notes
Production is intended to use Railway PostgreSQL.

Connection string resolution order:
1. `DATABASE_URL`
2. `ConnectionStrings:DefaultConnection`

Recommended Railway setup:
1. Create a Railway project.
2. Add a PostgreSQL service.
3. Add a web service from this repo.
4. Set the app service `DATABASE_URL`.
5. Optionally set `Seed__DemoUserPassword` for non-production demo data scenarios.

## Migrations
Old SQL Server migrations remain in `schedule-ders/Migrations` for reference only and are excluded from compilation.

Current PostgreSQL migrations live in:
- `schedule-ders/PostgresMigrations`

To add a new PostgreSQL migration:

```powershell
dotnet ef migrations add <MigrationName> --project schedule-ders --context ScheduleContext --output-dir PostgresMigrations
```

Then apply it:

```powershell
dotnet ef database update --project schedule-ders --context ScheduleContext
```

## Troubleshooting

### PostgreSQL connection refused on `localhost:5432`
That usually means PostgreSQL is not installed or its Windows service is not running.

### `--project schedule-ders` says the path does not exist
You are probably already inside the `schedule-ders/schedule-ders` folder. In that case use `dotnet run` directly or point `--project` to `.\schedule-ders.csproj`.

### App starts but no admin account exists
Set `Seed:DemoUserPassword` with `dotnet user-secrets set ...` and start the app again.

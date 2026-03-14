# schedule-ders Quick Setup

## Local Run with PostgreSQL
The app now uses PostgreSQL via `ConnectionStrings:DefaultConnection`. You can override that locally with user secrets or use Railway's `DATABASE_URL` in production.

```powershell
dotnet restore
dotnet build schedule-ders/schedule-ders.csproj -t:Compile
dotnet ef database update --project schedule-ders --context ScheduleContext
dotnet run --project schedule-ders
```

Default local connection string in `appsettings.json`:

```text
Host=localhost;Port=5432;Database=ScheduleDB;Username=postgres;Password=postgres;SSL Mode=Disable;Trust Server Certificate=true
```

## Railway Deploy
Recommended Railway setup:
1. Create a new Railway project.
2. Add a PostgreSQL service.
3. Add a web service from this repo.
4. Set the app service's `DATABASE_URL` variable to the PostgreSQL connection string or Railway reference.
5. Optionally set `Seed__DemoUserPassword` if you want demo accounts in development-like environments.

The app accepts connection settings in this order:
1. `DATABASE_URL`
2. `ConnectionStrings:DefaultConnection`

## Migrations
The old SQL Server migrations are still in `schedule-ders/Migrations` for reference, but they are excluded from compilation. Generate PostgreSQL migrations into a new folder like this:

```powershell
dotnet ef migrations add InitialPostgres --project schedule-ders --context ScheduleContext --output-dir PostgresMigrations
dotnet ef database update --project schedule-ders --context ScheduleContext
```

## Optional Demo Accounts
Demo users are only seeded in Development and only when a password is provided.

```powershell
dotnet user-secrets set "Seed:DemoUserPassword" "<strong-password>" --project schedule-ders
```

Demo emails:
- `admin@scheduleders.app`
- `professor@scheduleders.app`
- `student@scheduleders.app`

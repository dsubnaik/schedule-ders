# schedule-ders Quick Setup

## Local Run (default)
The app uses LocalDB by default via `ConnectionStrings:DefaultConnection`.

```powershell
dotnet restore
dotnet build schedule-ders/schedule-ders.csproj -t:Compile
dotnet ef database update --project schedule-ders
dotnet run --project schedule-ders
```

## Shared Azure SQL Setup
The app will use SQL connection strings in this order:
1. `AZURE_SQL_CONNECTIONSTRING` (environment variable)
2. `ConnectionStrings:AzureSqlConnection`
3. `ConnectionStrings:DefaultConnection` (LocalDB fallback)

Recommended: use user secrets (do not commit credentials).

```powershell
dotnet user-secrets init --project schedule-ders
dotnet user-secrets set "ConnectionStrings:AzureSqlConnection" "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" --project schedule-ders
dotnet ef database update --project schedule-ders
```

## Optional Demo Accounts (development only)
Demo users are only seeded in Development and only when a password is provided.

```powershell
dotnet user-secrets set "Seed:DemoUserPassword" "<strong-password>" --project schedule-ders
```

Demo emails:
- `admin@scheduleders.app`
- `professor@scheduleders.app`
- `student@scheduleders.app`

If `Seed:DemoUserPassword` is not set, you can still register/login normally.

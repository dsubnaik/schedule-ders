# Schedule-Ders

Schedule-Ders is a web application for managing Supplemental Instruction (SI) scheduling, course support requests, SI leader assignments, and public student schedule lookup.

The application gives administrators one place to maintain courses, sessions, semesters, SI leaders, and request status updates. Professors can submit and track SI support requests, while students can quickly search available SI sessions without needing an account.

## Key Features

- Public SI schedule search by course, professor, day, time, semester, and location
- Student favorite courses for faster access after login
- Professor SI request submission and request tracking
- Admin request review workflow with status updates and notes
- Course, semester, session, and SI leader management
- SI leader candidate tracking on professor requests
- PDF and CSV exports for requests, sessions, and SI leader records
- Role-based dashboards for Admin, Professor, and Student users
- Email notification support through SMTP or Resend
- Built-in help page with role-specific user guidance

## User Roles

### Students

Students can view the public SI schedule, search for their courses, and see session days, times, locations, professors, and assigned SI leaders. Logged-in students can save favorite courses.

### Professors

Professors can submit SI requests for their courses, include request notes, suggest potential SI leader candidates, and track each request as it moves through the review process.

### Administrators

Administrators can manage courses, semesters, sessions, SI leaders, and incoming SI requests. They can update request statuses, assign leaders, maintain course/session data, and export operational records.

## Technology Stack

- ASP.NET Core MVC and Razor Pages
- ASP.NET Core Identity with role-based access
- Entity Framework Core
- PostgreSQL
- Bootstrap, custom CSS, and Razor views
- Swagger in local development

## Production Notes

The app is designed to run with PostgreSQL and supports Railway-style deployment.

Connection string resolution order:

1. `DATABASE_URL`
2. `ConnectionStrings:DefaultConnection`

On startup, the application automatically applies pending Entity Framework migrations.

## Configuration

Required production configuration depends on the deployment environment, but the main settings are:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Notifications": {
    "AdminRecipient": "",
    "ProfessorStatusRecipientOverride": ""
  },
  "Email": {
    "Smtp": {
      "FromAddress": "",
      "FromName": "Schedule DERS",
      "Host": "",
      "Port": 587,
      "Username": "",
      "Password": "",
      "EnableSsl": true
    },
    "Resend": {
      "ApiKey": "",
      "FromAddress": "",
      "FromName": "Schedule DERS"
    }
  }
}
```

If `Email:Resend:ApiKey` is configured, the app uses Resend for email delivery. Otherwise, it falls back to SMTP.

## Local Development

### Prerequisites

- .NET SDK 10
- PostgreSQL
- Entity Framework Core tools

Default local database connection:

```text
Host=localhost;Port=5432;Database=ScheduleDB;Username=postgres;Password=postgres;SSL Mode=Disable
```

Create the local database if it does not already exist:

```powershell
createdb -h localhost -p 5432 -U postgres ScheduleDB
```

If `createdb` is not on your PATH, use the PostgreSQL install path, for example:

```powershell
& 'C:\Program Files\PostgreSQL\17\bin\createdb.exe' -h localhost -p 5432 -U postgres ScheduleDB
```

From the repository root:

```powershell
dotnet restore
dotnet build schedule-ders/schedule-ders.csproj -t:Compile
dotnet ef database update --project schedule-ders --context ScheduleContext
dotnet run --project schedule-ders
```

If your terminal is already inside `schedule-ders/schedule-ders`, use:

```powershell
dotnet restore
dotnet build -t:Compile
dotnet ef database update --context ScheduleContext
dotnet run
```

## Demo Users

Demo users are seeded only in `Development`, or when `Seed:EnableDemoUsers` is enabled, and only when a demo password is configured.

From the repository root:

```powershell
dotnet user-secrets set "Seed:DemoUserPassword" "Password1!" --project schedule-ders
dotnet run --project schedule-ders
```

Demo login emails:

- Admin: `admin@email.com`
- Professor: `professor@email.com`
- Student: `student@email.com`

Use the password configured in `Seed:DemoUserPassword`.

## Deployment Checklist

- Provision a PostgreSQL database
- Configure `DATABASE_URL` or `ConnectionStrings:DefaultConnection`
- Configure email delivery through Resend or SMTP
- Set notification recipient values
- Confirm production users and roles
- Verify migrations apply successfully on startup
- Confirm the public SI schedule, professor request flow, admin request workflow, and exports

## Database Migrations

Current PostgreSQL migrations live in:

```text
schedule-ders/PostgresMigrations
```

To add a new migration:

```powershell
dotnet ef migrations add <MigrationName> --project schedule-ders --context ScheduleContext --output-dir PostgresMigrations
```

To apply migrations manually:

```powershell
dotnet ef database update --project schedule-ders --context ScheduleContext
```

Older SQL Server migrations remain in `schedule-ders/Migrations` for reference and are excluded from compilation.

## Support Notes

- The public schedule is available without login.
- Account registration uses ASP.NET Core Identity and requires confirmed accounts.
- Admin, Professor, and Student access is controlled through Identity roles.
- Request body and form sizes are limited for safer production operation.
- Forwarded headers are enabled for Railway and other reverse-proxy hosting environments.


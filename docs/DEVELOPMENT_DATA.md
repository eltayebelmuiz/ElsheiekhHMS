# Development test data

The repository has an explicit, idempotent development seeder for manual HMS
testing. It is implemented in
`ElsheiekhHMS.Infrastructure/Development/DevelopmentDataSeeder.cs` and is
invoked from Web startup only when all of these conditions hold:

- the ASP.NET Core environment is `Development`;
- `DevelopmentSeed:Enabled` is `true` in User Secrets; and
- the resolved SQL Server database name is exactly `ElsheiekhHMS_Dev`.

The seeder checks that all existing migrations are applied, never migrates the
database, never deletes existing rows, and skips records identified by stable
development markers. Identity users are created through `UserManager` and roles
through the existing role seeder. Patient and queue codes use the existing
allocators; appointment-linked queue entries use the accepted arrival workflow.
No Encounter or clinical records are created.

Set development-only passwords outside the repository:

```powershell
dotnet user-secrets set DevelopmentSeed:Enabled true --project ElsheiekhHMS.Web
dotnet user-secrets set DevelopmentSeed:Users:SystemAdministrator:Password '<strong password>' --project ElsheiekhHMS.Web
dotnet user-secrets set DevelopmentSeed:Users:Administrator:Password '<strong password>' --project ElsheiekhHMS.Web
dotnet user-secrets set DevelopmentSeed:Users:Receptionist:Password '<strong password>' --project ElsheiekhHMS.Web
dotnet user-secrets set DevelopmentSeed:Users:Provider:Password '<strong password>' --project ElsheiekhHMS.Web
dotnet user-secrets set DevelopmentSeed:Users:Patient:Password '<strong password>' --project ElsheiekhHMS.Web
```

Run the Web project in Development to execute the seed once, then leave the
flag enabled for safe repeatability or disable it for ordinary local starts.
The current development login accounts are:

| Role | Username | Password source | State |
| --- | --- | --- | --- |
| SystemAdministrator | `sysadmin.dev@elsheiekh.local` | User Secrets | Active |
| Administrator | `admin.dev@elsheiekh.local` | User Secrets | Active |
| Receptionist | `reception.dev@elsheiekh.local` | User Secrets | Active |
| Provider | `provider.dev@elsheiekh.local` | User Secrets | Active |
| Patient | `patient.dev@elsheiekh.local` | User Secrets | Active |

`tools/dev-data-inspection.sql` contains read-only count, status, identity,
and clinical-table checks. Run it only against `ElsheiekhHMS_Dev`; it does not
write data and must not be used for Identity creation or domain allocation.

Manual browser checks remain required for login, dashboard, patient search and
pagination, departments, appointments, queue operations, and role boundaries.

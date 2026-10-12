## Database Setup

### Technology
- PostgreSQL 16
- Entity Framework Core 9
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
- Code-First Migrations

### Database Configuration

The project uses PostgreSQL as its database and Entity Framework Core migrations to manage the database schema.

Configure the connection string using .NET User Secrets in the API project. Do not commit database credentials to source control.

```powershell
dotnet user-secrets set "ConnectionStrings:KBSConnection" "Host=localhost;Port=5432;Database=KBS_DB;Username=postgres;Password=YOUR_PASSWORD" --project .\backend\KBS.API
```

Replace `YOUR_PASSWORD` with your local PostgreSQL password.

### Apply Database Migrations

Run the following command from the repository root:

```powershell
dotnet ef database update --project .\backend\KBS.DAL --startup-project .\backend\KBS.API
```

To create a new migration after changing the entity models or database configuration:

```powershell
dotnet ef migrations add MigrationName --project .\backend\KBS.DAL --startup-project .\backend\KBS.API --output-dir Migrations
```

Replace `MigrationName` with a descriptive migration name.

### Current Database Schema

The initial schema includes the following entities:

- Products
- Categories
- Brands
- Inventories
- Roles
- Users
- Orders
- OrderDetails

EF Core also maintains the `__EFMigrationsHistory` table to track applied migrations.

### Seed Data

The `SeedRolesAndCategories` migration inserts the following default data:

**Roles**
- Admin
- Customer

**Categories**
- Membrane
- Mechanical
- Optical
- Hall Effect
- TMR
- Low-profile
s
### Current Migrations

- `InitialCreate` — creates the initial database schema and relationships.
- `SeedRolesAndCategories` — inserts the default roles and product categories.

For a new development environment, configure the connection string and apply the migrations. Existing databases should be updated through migrations rather than recreated manually.
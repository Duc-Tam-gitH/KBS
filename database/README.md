# KBS PostgreSQL Database

Raw SQL is the source of truth for the KBS88 application schema. The existing
EF Core migration history table is retained, but EF Core should scaffold from
this database rather than create a competing schema.

## Execution order

Run the scripts from this directory:

```powershell
psql -h localhost -U postgres -d postgres -f 01_create_database.sql
psql -h localhost -U postgres -d KBS88 -f 02_tables.sql
psql -h localhost -U postgres -d KBS88 -f 03_constraints.sql
psql -h localhost -U postgres -d KBS88 -f 04_seed_data.sql
psql -h localhost -U postgres -d KBS88 -f 05_views.sql
psql -h localhost -U postgres -d KBS88 -f 06_procedures.sql
psql -h localhost -U postgres -d KBS88 -f 07_demo_procedures.sql
```

All scripts are designed to be re-run. The demo script always rolls back.

## Script contents

- `01_create_database.sql`: conditionally creates KBS88 with UTF-8 and the requested locale.
- `02_tables.sql`: creates Categories, Brands, Products, Inventories, Roles, Users, Orders, and OrderDetails.
- `03_constraints.sql`: adds keys, foreign keys, checks, and lookup indexes.
- `04_seed_data.sql`: adds realistic Vietnamese keyboard-market seed data.
- `05_views.sql`: creates product, order, revenue, stock, and brand reports.
- `06_procedures.sql`: creates procedures, reporting functions, and triggers.
- `07_demo_procedures.sql`: demonstrates the database API without persisting changes.

## Database objects

Views: `vw_ProductFull`, `vw_OrderSummary`, `vw_RevenueByDay`,
`vw_RevenueByMonth`, `vw_TopSellingProducts`, `vw_LowStockProducts`,
and `vw_ProductByBrand`.

Procedures: `sp_CreateOrder` creates and prices an order while decrementing
stock; `sp_UpdateOrderStatus` enforces valid lifecycle transitions;
`sp_UpdateStock` prevents negative stock; `sp_RegisterUser` validates
roles and duplicate accounts; `sp_CancelOrder` cancels with a notice log.

Functions: `fn_GetRevenueByDay`, `fn_GetTopSellingProducts`,
`fn_GetLowStockProducts`, `fn_GetMonthlyRevenue`, and
`fn_GetCustomerOrderHistory`.

Triggers: `trg_products_auto_inventory` creates stock rows,
`trg_inventories_updated_at` stamps inventory updates,
`trg_orders_validate_status` protects lifecycle transitions, and
`trg_orderdetails_stock_check` rejects overselling.

## Example calls

```sql
CALL "sp_UpdateStock"(1, 5);
CALL "sp_UpdateOrderStatus"(1, 'CONFIRMED');
SELECT * FROM "fn_GetRevenueByDay"(current_date - 30, current_date);
SELECT * FROM "fn_GetLowStockProducts"(10);
```

## EF Core scaffolding

```powershell
dotnet ef dbcontext scaffold "Host=localhost;Database=KBS88;Username=postgres;Password=***" Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models --context-dir Data --context KbsDbContext --project KBS.DAL --startup-project KBS.API --force
```

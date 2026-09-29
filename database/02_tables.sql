\set ON_ERROR_STOP on

-- Raw SQL owns the KBS88 application schema. Mixed-case identifiers are quoted
-- so the names remain compatible with EF Core scaffolding.
CREATE TABLE IF NOT EXISTS "Categories" (
    "Id" bigserial,
    "Name" varchar(100) NOT NULL,
    "Description" text,
    "CreatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "Brands" (
    "Id" bigserial,
    "Name" varchar(100) NOT NULL,
    "Country" varchar(50),
    "Description" text,
    "CreatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "Products" (
    "Id" bigserial,
    "Name" varchar(255) NOT NULL,
    "Price" numeric(12,2) NOT NULL,
    "OriginalPrice" numeric(12,2),
    "Description" text,
    "CategoryId" bigint NOT NULL,
    "BrandId" bigint NOT NULL,
    "SwitchType" varchar(50),
    "Layout" varchar(20),
    "ConnectionType" varchar(30),
    "Keycap" varchar(50),
    "Color" varchar(50),
    "IsHotSwap" boolean NOT NULL DEFAULT false,
    "IsRGB" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "IsActive" boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS "Inventories" (
    "Id" bigserial,
    "ProductId" bigint NOT NULL,
    "StockQuantity" integer NOT NULL DEFAULT 0,
    "UpdatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" bigserial,
    "Name" varchar(50) NOT NULL,
    "Description" text
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" bigserial,
    "Username" varchar(50) NOT NULL,
    "Email" varchar(255) NOT NULL,
    "PasswordHash" varchar(255) NOT NULL,
    "FullName" varchar(100),
    "PhoneNumber" varchar(20),
    "RoleId" bigint NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "IsActive" boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" bigserial,
    "UserId" bigint NOT NULL,
    "CustomerName" varchar(100) NOT NULL,
    "PhoneNumber" varchar(20) NOT NULL,
    "ShippingAddress" text NOT NULL,
    "TotalPrice" numeric(12,2) NOT NULL,
    "PaymentMethod" varchar(20) NOT NULL DEFAULT 'COD',
    "PaymentStatus" varchar(20) NOT NULL DEFAULT 'PENDING',
    "OrderStatus" varchar(20) NOT NULL DEFAULT 'PENDING',
    "CreatedAt" timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "OrderDetails" (
    "Id" bigserial,
    "OrderId" bigint NOT NULL,
    "ProductId" bigint NOT NULL,
    "UnitPrice" numeric(12,2) NOT NULL,
    "Quantity" integer NOT NULL
);

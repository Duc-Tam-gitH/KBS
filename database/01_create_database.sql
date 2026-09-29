-- Run this script while connected to the maintenance database (usually postgres).
-- It uses only PostgreSQL server SQL and is safe in pgAdmin, DBeaver, and VS Code.
-- PostgreSQL does not support CREATE DATABASE IF NOT EXISTS, and CREATE DATABASE
-- cannot run inside a DO block or transaction. If this query reports missing,
-- execute the documented CREATE DATABASE command separately as an administrator.
SELECT CASE
    WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = 'KBS88')
        THEN 'KBS88 already exists.'
    ELSE 'KBS88 is missing. Run: CREATE DATABASE "KBS88" WITH ENCODING ''UTF8'' TEMPLATE template0 LC_COLLATE ''en_US.UTF-8'' LC_CTYPE ''en_US.UTF-8'';'
END AS "DatabaseStatus";

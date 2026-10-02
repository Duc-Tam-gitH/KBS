\set ON_ERROR_STOP on

-- Run this script while connected to the maintenance database (usually postgres).
-- PostgreSQL cannot parameterize CREATE DATABASE, so psql executes the statement
-- only when KBS88 is absent. The requested locale must exist on the host.
-- FIX: Dùng collation C để tránh phụ thuộc locale của hệ điều hành.
SELECT 'CREATE DATABASE "KBS88" WITH ENCODING ''UTF8'' TEMPLATE template0 LC_COLLATE ''C'' LC_CTYPE ''C'''
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'KBS88')
\gexec

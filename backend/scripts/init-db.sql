-- Create the application database and user for the backend (PostgreSQL).
-- Run this as a PostgreSQL superuser (e.g. postgres) once per environment.
--
-- Usage (from repo root or backend/):
--   psql -h localhost -p 5432 -U postgres -f backend/scripts/init-db.sql
-- Or use the wrapper: backend/scripts/init-db.sh (reads backend/.env and substitutes password).

-- Create role (user) with login and password (idempotent).
-- __APP_PASSWORD__ is replaced by init-db.sh from POSTGRES_CONNECTION_STRING in backend/.env
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'mockhealthsystem_user') THEN
    EXECUTE 'CREATE ROLE mockhealthsystem_user WITH LOGIN PASSWORD ' || quote_literal('__APP_PASSWORD__');
  END IF;
END
$$;

-- Create database with the app user as owner (idempotent when run via psql \gexec).
SELECT 'CREATE DATABASE mockhealthsystem_db OWNER mockhealthsystem_user'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'mockhealthsystem_db')\gexec

-- Connect to the app database and grant schema rights (required on PostgreSQL 15+).
\c mockhealthsystem_db
GRANT ALL ON SCHEMA public TO mockhealthsystem_user;
GRANT CREATE ON SCHEMA public TO mockhealthsystem_user;

-- Grant the app user full rights on all current and future objects in public.
-- Run as PostgreSQL superuser (e.g. postgres) when the app user gets "permission denied"
-- (e.g. after migrations were run as postgres). __APP_USER__ is replaced by grant-app-user.sh.
--
-- Usage: ./scripts/grant-app-user.sh

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO "__APP_USER__";
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO "__APP_USER__";
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO "__APP_USER__";
GRANT ALL PRIVILEGES ON SCHEMA public TO "__APP_USER__";

-- So future objects created by postgres (or current user) are also usable by the app user
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO "__APP_USER__";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO "__APP_USER__";

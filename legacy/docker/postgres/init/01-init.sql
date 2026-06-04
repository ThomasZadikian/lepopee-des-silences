-- Create application users
CREATE USER rpg_app WITH PASSWORD 'AppPass123!';
CREATE USER rpg_reader WITH PASSWORD 'ReaderPass123!';

-- Grant privileges
GRANT CONNECT ON DATABASE rpg_esi07_dev TO rpg_app, rpg_reader;

\c rpg_esi07_dev

-- Grant schema usage
GRANT USAGE ON SCHEMA public TO rpg_app, rpg_reader;

-- Grant table privileges (applied to future tables)
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT ON TABLES TO rpg_reader;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO rpg_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO rpg_app;
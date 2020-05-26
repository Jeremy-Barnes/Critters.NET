Download https://www.postgresql.org/download/windows/ -> https://www.enterprisedb.com/downloads/postgres-postgresql-downloads -> 12.3 Windows x86-64 (or 32)
Run exe
install dir = C:\Program Files\PostgreSQL\12
Install all components
Set postgres's data directory to C:\Program Files\PostgreSQL\12\data
username: postgres password: pgadminlocal (using placeholder passwords consistently here)
port 5432


open pgadmin4
Set Master Password for pgAdmin --- pgGodModeLocal


go to Services in windows and turn postgresql-x64-12 to manual

go to your install directory (C:\Program Files\PostgreSQL\12\data) and open postgresql.conf and set max_prepared_transactions = 1 (its commented out and 0 by default)

Open PGAdmin 4

In the left hand pane open Servers > PostgreSQL 12 > Databases > right click on db 'postgres' and go to Query Tool
Execute: 
	CREATE DATABASE "CrittersDB"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    CONNECTION LIMIT = -1;

In the left hand pane open Servers > PostgreSQL 12 > Databases > right click on 'CrittersDB' and go to Query Tool

Run these commands: 

ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL PRIVILEGES ON SEQUENCES TO PUBLIC;

CREATE ROLE "LocalApp" WITH
	LOGIN
	NOSUPERUSER
	NOCREATEDB
	NOCREATEROLE
	INHERIT
	NOREPLICATION
	CONNECTION LIMIT -1
	PASSWORD 'localapplicationpassword';

	
	
	
And then run all sql in the Critters.NET\SQL folder in date-order

Download https://www.postgresql.org/download/windows/ -> https://www.enterprisedb.com/downloads/postgres-postgresql-downloads -> 11.5 Windows x86-64 (or 32)
Run exe
install dir = D:\Program Files\PostgreSQL\11
Install all components
Set postgres's data directory to D:\Program Files\PostgreSQL\11\data
username: postgres password: pgadminlocal (using placeholder passwords consistently here)
port 5432


open pgadmin4
Set Master Password for pgAdmin --- pgGodModeLocal


go to Services in windows and turn postgresql-x64-11 to manual

Open PGAdmin 4

In the left hand pane open Servers > PostgreSQL 11 > right click on 'postgres' and go to Query Tool
Execute: 
	CREATE DATABASE "CrittersDB"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    CONNECTION LIMIT = -1;

In the left hand pane open Servers > PostgreSQL 11 > right click on 'CrittersDB' and go to Query Tool

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
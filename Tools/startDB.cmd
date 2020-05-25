@echo off
echo Start Postgres
start "" "C:\Program Files\PostgreSQL\12\bin\pg_ctl.exe"  -D "C:\Program Files\PostgreSQL\12\data" -w "start"

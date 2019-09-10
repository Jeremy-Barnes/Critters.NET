@echo off
echo Start Postgres
start "" "D:\Program Files\PostgreSQL\11\bin\pg_ctl.exe"  -D "D:\Program Files\PostgreSQL\11\data" -w "start"

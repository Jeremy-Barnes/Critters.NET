@echo off 
echo Preparing to build
cd ../"CritterServer"
echo Clean everything
dotnet clean
echo Cleaned, now build! 
dotnet run
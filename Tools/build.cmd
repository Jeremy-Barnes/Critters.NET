@echo off 
echo Preparing to build
cd ../"Critters.NET Server"
echo Clean everything
dotnet clean
echo Cleaned, now build! 
dotnet run
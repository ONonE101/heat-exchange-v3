@echo off
cd /d "%~dp0"
echo Open http://localhost:5183 after the application starts.
dotnet run --project HeatExchange.Web --urls http://localhost:5183
pause

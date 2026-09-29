@echo off
dotnet build --configuration Tools
dotnet run --project Content.Vagrant.Server --configuration Tools
pause

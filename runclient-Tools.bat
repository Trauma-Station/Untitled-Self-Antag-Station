@echo off
dotnet build --configuration Tools
dotnet run --project Content.Vagrant.Client --configuration Tools

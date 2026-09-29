#!/usr/bin/env bash
dotnet build --configuration Tools
dotnet run --project Content.Vagrant.Server --configuration Tools
read -p "Press enter to continue"

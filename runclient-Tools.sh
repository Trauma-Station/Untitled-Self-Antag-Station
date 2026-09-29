#!/usr/bin/env bash
dotnet build --configuration Tools
dotnet run --project Content.Vagrant.Client --configuration Tools
read -p "Press enter to continue"

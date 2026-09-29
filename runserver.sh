#!/usr/bin/env bash
dotnet build
dotnet run --project Content.Vagrant.Server
read -p "Press enter to continue"

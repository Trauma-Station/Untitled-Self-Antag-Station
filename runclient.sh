#!/usr/bin/env bash
dotnet build
dotnet run --project Content.Vagrant.Client
read -p "Press enter to continue"

#!/usr/bin/env -S just --justfile

export PATH := env_var('HOME') + "/.dotnet:" + env_var('PATH')

# Afficher les commandes disponibles
default:
    @just --list

# Restaurer les packages NuGet
install:
    dotnet restore BattleTank.sln

# Compiler le projet C#
build:
    dotnet build BattleTank.sln

# Ouvrir l'éditeur Godot
dev:
    godot --editor

# Lancer le serveur en mode headless
run:
    godot --headless -- --server

# Exécuter les tests NUnit
test:
    dotnet test src/Tests/BattleTank.Tests.csproj

# Exécuter les tests avec couverture
test-cover:
    dotnet test src/Tests/BattleTank.Tests.csproj --collect:"XPlat Code Coverage"

# Scanner les vulnérabilités
trivy:
    trivy fs .

# Installer les dépendances pre-commit
precommit_dependencies:
    pip install pre-commit

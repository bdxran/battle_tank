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

# Lancer le serveur avec un mot de passe admin (ex: just run-admin admin)
run-admin password='':
    godot --headless -- --server --admin-password={{ password }}

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

# Exporter le serveur Linux headless
export-server:
    mkdir -p exports/server
    godot --headless --export-release "Linux Server" exports/server/server.x86_64

# Exporter le client Linux
export-client-linux:
    mkdir -p exports/linux
    godot --headless --export-release "Linux" exports/linux/BattleTank.x86_64

# Exporter le client Windows
export-client-windows:
    mkdir -p exports/windows
    godot --headless --export-release "Windows Desktop" exports/windows/BattleTank.exe

# Exporter le client macOS
export-client-macos:
    mkdir -p exports/macos
    godot --headless --export-release "macOS" exports/macos/BattleTank.zip

# Exporter tous les clients
export-client: export-client-linux export-client-windows export-client-macos

# Construire l'image Docker du serveur
docker-build:
    docker compose -f docker/docker-compose.yml build

# Démarrer le serveur Docker
docker-run:
    docker compose -f docker/docker-compose.yml up -d

# Arrêter le serveur Docker
docker-stop:
    docker compose -f docker/docker-compose.yml down

# Suivre les logs du serveur
docker-logs:
    docker compose -f docker/docker-compose.yml logs -f server

# Suivre uniquement les métriques
docker-metrics:
    docker compose -f docker/docker-compose.yml logs -f server | grep '\[metrics\]'

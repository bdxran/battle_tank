# Dockerfile — battle-tank
# Build multi-stage : export Godot headless Linux

ARG GODOT_VERSION=4.6

# ---------------------------------------------------------------------------
# Étape 1 — Export du serveur Godot
# ---------------------------------------------------------------------------
FROM ghcr.io/godotengine/godot:${GODOT_VERSION}-linux AS builder

WORKDIR /app
COPY . .

# Exporter le projet en mode serveur Linux headless
RUN mkdir -p /build && \
    godot --headless --export-release "Linux Server" /build/server.x86_64

# ---------------------------------------------------------------------------
# Étape 2 — Image de production minimale (inclut le runtime .NET 8)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim

WORKDIR /game

COPY --from=builder /build/server.x86_64 ./server
RUN chmod +x ./server

# Utilisateur non-root
RUN groupadd -r gameserver && useradd -r -g gameserver gameserver
USER gameserver

EXPOSE 4242/udp

CMD ["./server", "--headless", "--", "--server"]

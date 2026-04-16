# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed
- Zone BR masquée dans les modes sans Battle Royale (Training, Deathmatch, Teams, CaptureZone) — `ZoneNode` se cache si `GameStateFull.Mode != BattleRoyale`
- Zone BR invisible au spawn en BR : délai d'activation de 15s avant apparition (`ZoneActivationDelay`) — évite les dégâts immédiats au spawn
- `ZoneController.GetSnapshot()` retourne `Radius = 0` pendant le délai d'activation pour signaler l'état inactif au client

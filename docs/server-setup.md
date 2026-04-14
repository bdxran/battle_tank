# Héberger un serveur Battle Tank

Ce guide s'adresse à quelqu'un qui veut lancer un serveur pour jouer avec des amis.

Télécharger `server-linux.tar.gz` depuis la [dernière release](../../releases/latest).

---

## Prérequis système

Sur Ubuntu/Debian :
```bash
sudo apt install libgl1 libx11-6 libudev1
```

---

## Lancer le serveur

```bash
tar xzf server-linux.tar.gz
chmod +x server.x86_64
./server.x86_64 --headless -- --server
```

### Variables d'environnement

```bash
SERVER_PORT=4242 MAX_PLAYERS_PER_ROOM=8 ./server.x86_64 --headless -- --server
```

| Variable | Défaut | Description |
|----------|--------|-------------|
| `SERVER_PORT` | `4242` | Port UDP |
| `TICK_RATE` | `20` | Ticks par seconde |
| `MAX_PLAYERS_PER_ROOM` | `8` | Joueurs max par partie |

---

## Lancer en arrière-plan

### Avec nohup

```bash
nohup ./server.x86_64 --headless -- --server > server.log 2>&1 &
echo "PID: $!"
```

### Comme service systemd (recommandé sur VPS)

Créer `/etc/systemd/system/battle-tank.service` :

```ini
[Unit]
Description=Battle Tank Server
After=network.target

[Service]
Type=simple
User=gameserver
WorkingDirectory=/opt/battle-tank
ExecStart=/opt/battle-tank/server.x86_64 --headless -- --server
Restart=on-failure
Environment=SERVER_PORT=4242
Environment=MAX_PLAYERS_PER_ROOM=8

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now battle-tank
sudo journalctl -u battle-tank -f
```

---

## Configuration réseau

Ouvrir le port UDP 4242 dans le firewall :

```bash
# ufw
sudo ufw allow 4242/udp

# iptables
sudo iptables -A INPUT -p udp --dport 4242 -j ACCEPT
```

Sur un VPS/cloud (AWS, Hetzner, etc.), ouvrir également le port dans le groupe de sécurité / règles de pare-feu de l'hébergeur.

### Communiquer l'adresse aux joueurs

Les joueurs entrent l'**IP publique** du serveur et le port `4242` dans le client.

```bash
# Connaître son IP publique
curl -s ifconfig.me
```

---

## Vérifier que le serveur tourne

Au démarrage, les logs doivent afficher :

```
[server] Listening on port 4242
[server] Waiting for players...
```

Si le serveur ne démarre pas :
- Port déjà utilisé : `ss -ulnp | grep 4242`
- Prérequis manquants : relancer `sudo apt install libgl1 libx11-6 libudev1`
- Permissions : `chmod +x server.x86_64`

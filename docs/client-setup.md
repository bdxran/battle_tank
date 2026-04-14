# Installer et lancer le client Battle Tank

Télécharger le client correspondant à votre système depuis la [dernière release](../../releases/latest).

---

## Windows

1. Télécharger `client-windows.zip`
2. Extraire le zip n'importe où
3. Double-cliquer sur `BattleTank.exe`

> **Windows SmartScreen** peut afficher un avertissement "Windows a protégé votre PC" car l'exécutable n'est pas signé.
> Cliquer sur **"Informations complémentaires"** puis **"Exécuter quand même"**.

---

## Linux

### Installation

```bash
tar xzf client-linux.tar.gz
chmod +x BattleTank.x86_64
./BattleTank.x86_64
```

### Prérequis

Sur Ubuntu/Debian, si le jeu ne se lance pas :

```bash
sudo apt install libgl1 libx11-6 libudev1
```

### Problème de rendu (erreur Vulkan)

Si le jeu affiche une erreur liée à Vulkan ou ne démarre pas sur un GPU ancien :

```bash
./BattleTank.x86_64 --rendering-driver opengl3
```

---

## macOS

> **Important :** le build macOS est compilé pour **Apple Silicon (arm64)** uniquement.
> Il ne fonctionne pas sur les Mac Intel.

### Installation

```bash
# Extraire le zip
unzip client-macos.zip

# Supprimer la quarantaine imposée par macOS sur les apps non signées
xattr -cr BattleTank.app

# Lancer
open BattleTank.app
```

### Alternative sans terminal

La première fois uniquement :
1. **Clic droit** sur `BattleTank.app`
2. Choisir **"Ouvrir"**
3. Cliquer **"Ouvrir quand même"** dans la boîte de dialogue

Les fois suivantes, un double-clic suffit.

### Pourquoi cette manipulation ?

macOS Gatekeeper bloque par défaut les applications qui ne sont pas signées avec un certificat Apple Developer. Battle Tank étant un projet open-source sans certificat payant, cette étape est nécessaire une seule fois.

---

## Se connecter à un serveur

Au lancement, le client demande l'adresse IP et le port du serveur.

- **Jouer en local (même réseau)** : entrer l'IP locale du serveur (ex: `192.168.1.42`)
- **Jouer via internet** : entrer l'IP publique du serveur
- **Port par défaut** : `4242`

Pour héberger votre propre serveur, voir [server-setup.md](server-setup.md).

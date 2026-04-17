# INF1009 — Projet de réseaux

Simulation d'un service de réseau en mode connecté, côté appelant, en C#.

---

## Ce que fait le programme

Le programme simule les échanges entre deux entités :

- **ET** (`TransportEntity`) — la couche transport, qui lit les demandes dans `Slec.txt` et interagit avec ER via des primitives de service.
- **ER** (`NetworkEntity`) — la couche réseau, qui gère les connexions, construit les paquets et utilise le service de liaison simulé.

Les trois phases du protocole sont implémentées : établissement de connexion, transfert de données et libération. Les paquets produits (appel, communication établie, données, libération, ACK) respectent les formats binaires définis dans le sujet.

---

## Comment lancer la simulation

```
dotnet run
```

Les fichiers d'entrée et de sortie se trouvent dans le dossier `Data/`.

---

## Fichiers de simulation

| Fichier | Rôle |
|---|---|
| `Slec.txt` | Jeu d'essai lu par ET (commandes CONNECT / DATA / DISCONNECT) |
| `Secr.txt` | Résultats écrits par ET (issue de chaque connexion) |
| `Lecr.txt` | Paquets émis par ER vers la couche liaison (format binaire) |
| `Llec.txt` | Réponses simulées reçues de la couche liaison |

Les adresses source et destination sont définies dans `Slec.txt` pour garantir la reproductibilité des résultats. Elles ont été choisies pour couvrir les cas définis au §3.5 du sujet (multiples de 13, 15, 19 et 27).

---

## Scénarios testés

Le jeu d'essai couvre tous les cas décrits dans le sujet :

1. Connexion normale, données courtes, libération
2. Message long nécessitant une segmentation (3 paquets, bit M)
3. Message de 128 octets exactement (cas limite, pas de segmentation)
4. Refus du fournisseur (src multiple de 27)
5. Absence de réponse du distant — timeout (src multiple de 19)
6. Refus du distant (src multiple de 13)
7. ACK négatif suivi d'une réémission réussie
8. Absence d'ACK sur les données (src multiple de 15) — abandon après réémission
9. Deux connexions ouvertes simultanément

---

## Structure du projet

```
INF1009/
├── Program.cs
├── Models/
│   ├── ConnectionContext.cs
│   ├── Primitive.cs
│   └── Packet.cs
└── Services/
    ├── TransportEntity.cs
    ├── NetworkEntity.cs
    ├── LinkServiceSimulator.cs
    ├── SegmentationService.cs
    └── FileService.cs
```

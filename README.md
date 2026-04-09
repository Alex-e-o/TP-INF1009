# TP-INF1009 (Réseaux 1)

## État du projet

Ce projet simule, en C#, un service de réseau en mode connecté à partir du point de vue du processus appelant.  
La base de l’architecture est déjà définie :

- `Program.cs` sert de point d’entrée.
- `Models` contient les objets de données.
- `Services` contient la logique métier.
- `Data` contient les fichiers d’entrée et de sortie.

Le but maintenant est de terminer l’implémentation de chaque partie en respectant la logique du sujet.

---

## Ce qu’il reste à faire

### Priorité 1 : terminer le cœur de la simulation
- Finaliser la logique de `TransportEntity`.
- Finaliser la logique de `NetworkEntity`.
- Valider le comportement de `LinkServiceSimulator`.
- Vérifier le fonctionnement de `SegmentationService`.
- S’assurer que `Program.cs` lance correctement toute la chaîne.

### Priorité 2 : solidifier les entrées et sorties
- Définir clairement le format du fichier `Slec.txt`.
- Produire des résultats lisibles dans `Secr.txt`.
- Écrire correctement les paquets simulés dans `Lecr.txt`.
- Écrire correctement les réponses simulées dans `Llec.txt`.

### Priorité 3 : tester et corriger
- Tester l’établissement de connexion.
- Tester le transfert de données.
- Tester la libération de connexion.
- Tester les cas d’échec :
  - refus distant,
  - refus fournisseur,
  - absence de réponse,
  - acquittement négatif.
- Corriger les états de connexion si besoin.

---

## Structure du projet

```text
ProjetReseaux/
├── Program.cs
├── Models/
│   ├── ConnectionContext.cs
│   ├── Primitive.cs
│   └── Packet.cs
├── Services/
│   ├── TransportEntity.cs
│   ├── NetworkEntity.cs
│   ├── LinkServiceSimulator.cs
│   ├── FileService.cs
│   └── SegmentationService.cs
├── Data/
│   ├── Slec.txt
│   ├── Secr.txt
│   ├── Lecr.txt
│   └── Llec.txt
└── README.md
```

---

## Répartition du travail par fichier

## `Program.cs`

### Rôle
Point d’entrée du programme.

### Ce qu’il reste à faire
- Initialiser les chemins des fichiers.
- Créer `FileService`, `SegmentationService`, `LinkServiceSimulator`, `NetworkEntity` et `TransportEntity`.
- Vider les fichiers de sortie au démarrage.
- Lancer la simulation.
- Gérer les erreurs de base.

### Ce fichier ne doit pas contenir
- La logique des connexions.
- La segmentation.
- La simulation de liaison.
- Le traitement des primitives.

---

## `Models/ConnectionContext.cs`

### Rôle
Représente une connexion active ou en cours d’établissement.

### Ce qu’il reste à faire
- Garder l’identifiant de l’extrémité de connexion.
- Garder le numéro de connexion réseau.
- Garder les adresses source et destination.
- Garder l’état de la connexion.
- Garder les numéros de séquence si nécessaire.

### Utilité
Ce modèle sert à suivre l’état d’une communication du début à la fin.

---

## `Models/Primitive.cs`

### Rôle
Représente une primitive échangée entre ET et ER.

### Ce qu’il reste à faire
- Définir les types de primitives.
- Permettre le transport des adresses source et destination.
- Permettre le transport d’un identifiant de connexion.
- Permettre le transport de données.
- Permettre le transport d’une raison de refus ou de libération.

### Utilité
Ce modèle sert de message logique entre les deux entités.

---

## `Models/Packet.cs`

### Rôle
Représente un paquet réseau utilisé par ER.

### Ce qu’il reste à faire
- Définir les types de paquets.
- Inclure le numéro de connexion.
- Inclure les adresses source et destination.
- Inclure `ps` et `pr`.
- Inclure le bit `M`.
- Inclure les données utiles.
- Inclure une raison si le paquet est une libération.

### Utilité
Ce modèle sert à simuler les NPDU du protocole réseau.

---

## `Services/FileService.cs`

### Rôle
Centralise tout l’accès aux fichiers.

### Ce qu’il reste à faire
- Lire `Slec.txt`.
- Écrire dans `Secr.txt`.
- Écrire dans `Lecr.txt`.
- Écrire dans `Llec.txt`.
- Vider les fichiers de sortie au besoin.

### Utilité
Éviter de dupliquer le code de lecture/écriture partout.

---

## `Services/SegmentationService.cs`

### Rôle
Découpe les données lorsque le message dépasse 128 octets.

### Ce qu’il reste à faire
- Couper les messages en segments.
- Construire les paquets de données correspondants.
- Gérer le bit `M`.
- Faire avancer les numéros de séquence modulo 8.

### Point important
Cette partie doit être cohérente avec le format des paquets de données du sujet.

---

## `Services/LinkServiceSimulator.cs`

### Rôle
Simule la couche liaison.

### Ce qu’il reste à faire
- Écrire les paquets envoyés dans `Lecr.txt`.
- Générer une réponse aléatoire ou une absence de réponse.
- Appliquer les règles du sujet :
  - pas de réponse si adresse source multiple de 19,
  - refus distant si adresse source multiple de 13,
  - pas d’acquittement si adresse source multiple de 15,
  - acquittement négatif selon le tirage aléatoire.
- Écrire les réponses simulées dans `Llec.txt`.

### Utilité
Cette classe remplace le vrai réseau dans la simulation.

---

## `Services/NetworkEntity.cs`

### Rôle
Contient la logique de l’entité réseau `ER`.

### Ce qu’il reste à faire
- Recevoir les primitives venant de `TransportEntity`.
- Traiter `NCONNECT.req`.
- Décider si la connexion est acceptée ou refusée.
- Créer un numéro de connexion.
- Construire le paquet d’appel.
- Appeler le service de liaison.
- Traiter le transfert de données.
- Gérer les acquittements et la réémission unique.
- Gérer la libération de connexion.

### Utilité
C’est le cœur fonctionnel du projet.

---

## `Services/TransportEntity.cs`

### Rôle
Contient la logique de l’entité transport `ET`.

### Ce qu’il reste à faire
- Lire les demandes dans `Slec.txt`.
- Convertir chaque ligne en primitive.
- Envoyer la primitive à `NetworkEntity`.
- Maintenir la table des connexions.
- Écrire les résultats dans `Secr.txt`.
- Gérer la fin d’une communication.

### Utilité
Cette classe représente l’utilisateur du service de réseau.

---

## Ordre conseillé pour finir

1. Stabiliser le format de `Slec.txt`.
2. Finaliser `FileService`.
3. Finaliser `ConnectionContext`, `Primitive` et `Packet`.
4. Terminer `LinkServiceSimulator`.
5. Terminer `SegmentationService`.
6. Terminer `NetworkEntity`.
7. Terminer `TransportEntity`.
8. Valider `Program.cs`.
9. Tester tous les scénarios.
10. Corriger les cas limites.

---

## Validation minimale attendue

Le projet sera considéré comme fonctionnel lorsque :

- une connexion peut être demandée,
- la connexion peut être acceptée ou refusée,
- un transfert de données peut être effectué,
- la segmentation fonctionne si nécessaire,
- les acquittements sont gérés,
- une réémission unique est possible,
- la libération de connexion fonctionne,
- les fichiers `Secr`, `Lecr` et `Llec` sont bien remplis.

---

## Organisation

- `Models` = données.
- `Services` = logique.
- `Program.cs` = orchestration.
- `Data` = fichiers de simulation.
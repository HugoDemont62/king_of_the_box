<!-- Badges ajoutés en haut du fichier -->
<p align="center">
  <img src="https://img.shields.io/badge/status-Development%20in%20progress-orange" alt="status" />
  <img src="https://img.shields.io/badge/WIP-Yes-yellow" alt="wip" />
  <img src="https://img.shields.io/badge/language-C%23-blue" alt="language" />
  <img src="https://img.shields.io/badge/platform-s%26box-9cf" alt="platform" />
  <a href="./LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="license" /></a>
</p>

# King of the Box

Jeu expérimental pour s&box — capture de zones multijoueur.

Statut : Development in progress 🚧

Ce dépôt contient un prototype de mode de jeu « King of the Box ». Le projet est en cours de développement : des systèmes de base (gestion des équipes, zones de capture, HUD) existent, mais de nombreuses fonctionnalités et polissages restent à implémenter.

Racine du dépôt
- Code/ : code C# du jeu (logique serveur/client, composants, UI Razor).
- Assets/ : scènes, prefabs et terrains utilisés par le jeu.
- Editor/ : outils et extensions d'éditeur spécifiques au projet.
- Libraries/, Localization/, ProjectSettings/ : dépendances, traductions et réglages.

Prérequis
- s&box (accès et installation selon les conditions Facepunch).
- .NET SDK compatible (selon la version utilisée par s&box).
- Visual Studio / JetBrains Rider (recommandé)

Développement local (rapide)
1. Ouvrir la solution `king_of_the_box.slnx` dans votre IDE.
2. Restaurer/charger les dépendances si demandé.
3. Compiler le projet `Code` et `Editor`.
4. Lancer s&box et charger la scène principale (si applicable) ou exécuter en mode debug depuis l'IDE.

Conseils
- Tester les changements côté serveur et client (certains composants utilisent `OnFixedUpdate`, `OnUpdate` et le système de *sync*).
- Respecter les types d'équipes (enum `KobTeam`) et évitez d'exposer des constantes magiques depuis le code de gameplay.

Contribution
Les contributions sont les bienvenues :
- Ouvrir une issue pour discuter des grosses modifications.
- Préparer des branches par fonctionnalité (feature/nom)
- Faire des commits petits et ciblés + messages clairs.

TODO / Prochaines étapes (exemples)
- Ajouter des tests unitaires pour les managers et logique de capture.
- Polir le HUD et l'expérience de capture (sons, FX, transitions).
- Ajouter des options de configuration serveur (points par seconde, rayon, etc.).

Contact
- Auteur / Responsable du dépôt : voir le commit history.

Licence
- Ce projet est distribué sous la licence [MIT](./LICENSE). Voir le fichier `LICENSE` à la racine du dépôt.

---

Fichier créé automatiquement : README généré pour aider le développement en cours. Modifiez-le selon vos besoins.

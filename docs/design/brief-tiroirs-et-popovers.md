# Brief Claude Design — tiroirs, popovers et overlays d'exploration (contenu réel)

> La maquette `Tiroirs et popovers.dc.html` reçue est un gabarit visuel de démonstration (bascule
> entre listes plates), pas encore une refonte du contenu réel — les 4 composants les plus riches
> (Besace, Influences, Équipe, Journal) sont aujourd'hui bien plus complets que ce que la démo
> montre. Ce document décrit le contenu et le comportement **réels**, exhaustifs, pour que la
> prochaine passe Claude Design parte du vrai périmètre plutôt que de la version simplifiée.
>
> Palette/typo : `direction-visuelle-palais-respire.md` (void/panel/ink/mint/danger, Newsreader/IBM
> Plex). Modale unique déjà actée pour les popups bloquants — les tiroirs, eux, restent un
> traitement plus léger (glissement latéral, pas d'assombrissement total de la carte derrière),
> distinct de la superposition de nœuds (`brief-superposition-noeuds.md`).

---

## Neuf éléments, trois familles de traitement

| Famille | Éléments | Position |
|---|---|---|
| **Tiroirs latéraux** (un seul ouvert à la fois, glissement) | Besace, Influences, Équipe | Besace/Influences/Équipe : ancrés à droite (Équipe est en fait ancré à **gauche** aujourd'hui — à trancher, voir note) |
| **Modale centrée** | Journal, Diptyque de décision | Centre écran, fond assombri |
| **Chrome persistant** (toujours à l'écran, pas de bascule) | Ruban de statut (bas), Micro-menu d'équipe (bas-gauche), Overlay Elise (bas-gauche, conditionnel), Popup de réputation (haut-droite, auto-disparition) | Fixe |

Note de position : dans le code actuel, `InventoryDrawer`/`LawsPopover` sont ancrés à droite,
`PartyDrawer` est ancré à **gauche** — un seul clic en dehors ferme celui qui est ouvert
(mutuellement exclusifs). À trancher avec Claude Design : garder cette asymétrie gauche/droite, ou
tout unifier d'un seul côté maintenant que le style change.

---

## 1. Besace (tiroir — objets de la run en cours)

Grille de 2 colonnes de cellules (nom, quantité ×N si &gt;1, rareté colorée) ; un clic ouvre une
fiche détail sous la grille avec :
- Nom, badges (quantité, rareté)
- Type (Grimoire / Potion / etc.)
- Description
- **Effet** le cas échéant, libellé selon le type (`+X Vitalité`, `+X Garde`, `+X Mana`,
  `+X Charge`, `+X Garde (prochain combat)`, `+X% Vitalité et Mana`, `Fragment narratif`)
- **Contrat tactique** si l'objet a une portée (portée N, forme croix/losange/carte entière, ligne
  de vue requise ou non) — utile pour les objets utilisables en combat
- **Valeur** en Éclats du Palais / Éclats de Him'Lit si l'objet a un coût marchand
- Actions contextuelles, exclusives selon le type d'objet :
  - **Grimoire** : sélecteur du personnage lecteur + avertissement si ça remplace un sort dans
    l'emplacement temporaire III + bouton « Apprendre »
  - Objet à pages (`readablePages`) : bouton « Lire » → ouvre un lecteur de livre plein cadre
    (`BookReader`, feuilletage page à page)
  - Objet utilisable (`isUsable`) : bouton « Utiliser »
  - Sinon : mention « Cet objet ne peut pas être utilisé actuellement. »
- Compteur de capacité en en-tête (`N / capacité`), passe en rouge/alerte à saturation.

## 2. Influences (tiroir — lois et malédictions actives, + climat de salle)

En tête, un panneau de climat de salle (`RoomClimatePanel`) quand pertinent. Deux sections :

- **Lois du Palais** (compteur en en-tête) : par loi — nom, version, chip de domaine (combat/gold ;
  mémoire-récit/frost ; loi-édit/gold), chip de rareté, chip de polarité, description. Si la loi
  est `law.portes-ouvertes` : liste des salles à venir révélées. Bouton « Révoquer (Déni
  permanent) » si le joueur possède cet objet permanent, désactivé pendant le temps de recharge.
- **Malédictions** (compteur en en-tête) : par malédiction — badge de sévérité, nom, badge
  « Consommée » si applicable (ligne atténuée), description, durée.
- État vide : icône + « Aucune influence active. »

## 3. Équipe (tiroir — état vivant de la run, distinct de la fiche de personnage permanente)

**Ne pas confondre avec l'écran Équipe fusionné (fiche permanente/hors-run)** — celui-ci montre
l'état **de la run en cours**, uniquement pendant l'exploration (masqué en combat, où les
portraits de combat couvrent déjà ce rôle). Sections :

- **Alliés** : par membre — nom, badge KO si défait / badge « Allié » si non actif, ligne de
  stats (Vitalité actuelle/max coloré par seuil, Garde si &gt;0, Mana avec infobulle, Charge si
  &gt;0 avec infobulle « jauge tactique limitée à 5 », Mouvement), barre de vie fine, liste des
  sorts (avec pastille « Temp. I/II/III » pour les sorts temporaires), **3 emplacements
  temporaires** affichés explicitement (remplis ou « Libre »).
- **Calice infini** (si l'objet permanent est possédé) : bouton d'utilisation, désactivé en
  recharge.
- **Modificateurs actifs** : libellé + durée + source (Loi du Palais / Malédiction / Objet /
  Événement).
- **Malédictions actives** : sévérité + nom + description.
- **Lois du Palais** (résumé compact, juste domaine + nom — le détail complet vit dans Influences).
- **Objets de run** : rareté + nom + quantité + description.
- État vide : « Aucune donnée d'équipe disponible. »

## 4. Journal (« Le Carnet de bord » — modale centrée)

Pagination **par salle visitée** (une page = une salle), pas une simple liste chronologique plate :
- En-tête de page : « Salle N — nom de salle » entre deux filets avec losanges.
- Frise verticale (`timeline`) : un point par entrée journalisée dans cette salle, relié par un
  trait continu, texte en italique dans un encart.
- Pagination : boutons « ‹ Salle précédente » / « Salle suivante › » + statut « Page X / Y ».
- Aperçu global : une rangée de points cliquables, un par salle, le point actif agrandi.
- Ouvre automatiquement sur la dernière page (la salle la plus récente).
- État vide : citation façon PNJ (« Les pages sont encore vierges… »).

## 5. Ruban de statut (bas d'écran, persistant, repliable)

Replié par défaut en un petit onglet (☰) pour ne pas encombrer la carte — cliquer déplie le ruban
complet :
- **Info** (gauche) : chip « Salle N », chip type de salle, badge de climat de salle (cliquable →
  ouvre Influences), compteur de lois actives (cliquable), compteur de malédictions actives
  (cliquable), compteur de modificateurs actifs (cliquable).
- **Actions** (droite) : Équipe (ouvre le tiroir Équipe), La Besace, Carnet de bord (désactivé sans
  l'objet permanent requis), Quitter la salle (si pas à un point sûr), Sauvegarder (si à un point
  sûr), Abandonner (si à un point sûr), bouton de repli.

## 6. Micro-menu d'équipe (bas-gauche, persistant, actif même en combat)

Dock d'icônes toujours affiché : État de l'équipe (raccourci vers le tiroir Équipe, masqué en
combat), puis Équipe / Statistiques / Grimoire / Équipement / Besace — chacune ouvre en **modale
plein cadre par-dessus le plateau** (`PageOverlayModal`) plutôt que de naviguer hors de l'écran.
**C'est exactement le point d'entrée qui accueillera l'écran Équipe fusionné à onglets une fois
ce chantier fait** — un seul bouton pourrait suffire à terme au lieu de 5. En développement
uniquement : un bouton DevTools distinct visuellement (teinte d'alerte, jamais confondu avec les
boutons de gestion de personnage).

## 7. Overlay Elise (bas-gauche, conditionnel)

Cadre discret à coins ornés, n'apparaît que lorsqu'un commentaire contextuel existe (ex. après la
résolution d'un événement) : nom « Elise » en tête, puis le texte. Ne bloque jamais le clic
(`pointer-events: none`).

## 8. Popup d'effet de réputation (haut-droite, pile de pastilles auto-disparition)

Quand une action affecte la réputation d'un PNJ, une pastille apparaît (montant signé + nom du
PNJ), colorée selon gain/perte, et disparaît d'elle-même après ~1,8s. Plusieurs pastilles peuvent
s'empiler si plusieurs effets arrivent d'affilée.

## 9. Diptyque de décision (modale centrée, confirmation binaire)

Déjà un composant générique réutilisable (`DecisionDiptych`) — utilisé aujourd'hui pour
« Abandonner la run ? », mais prévu pour toute confirmation binaire du jeu. Trois zones
horizontales : option d'annulation (gauche), titre + description (centre), option de confirmation
(droite, teinte d'alerte si l'action est destructrice). C'est le même diptyque que celui déjà vu
dans `Le Seuil.dc.html` pour « Abandonner la traversée ? » — cohérence déjà confirmée entre les
deux écrans.

---

## Points ouverts pour Claude Design

- Asymétrie gauche/droite des tiroirs (voir note en tête de document) — à trancher.
- La Besace existe aujourd'hui sous **trois formes différentes** avec des périmètres qui se
  recoupent : ce tiroir (objets de run, pendant l'exploration), la page `/besace` (identique en
  contenu), et le futur onglet Besace de l'écran Équipe fusionné. Il faudra un seul gabarit visuel
  réutilisé aux trois endroits plutôt que trois designs distincts.
- Le tiroir Équipe (état de run) et l'écran Équipe fusionné (fiche permanente) partagent un nom
  mais pas un contenu — à nommer différemment dans l'UI si la confusion s'avère gênante en test.

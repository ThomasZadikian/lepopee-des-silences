# Brief Claude Design — univers graphique, UI et UX (hors combat/bestiaire/salles)

> Document de transmission, au même titre que `brief-direction-artistique-combat.md` (créatures et
> sorts) et `brief-design-par-salle.md` (les 27 salles). Ceux-là couvrent le **plateau** ; celui-ci
> couvre **l'interface elle-même** — les 11 écrans du jeu, leur cohérence entre eux, et ce qui doit
> rendre chacun reconnaissable sans lire son titre.
>
> Contexte : le jeu entre en accès bêta. Le plateau de combat et l'exploration sont la référence
> déjà posée ; le reste de l'interface doit la rejoindre en qualité et en personnalité. Deux écrans
> sont explicitement hors périmètre pour l'instant (animations de sorts, boss, événements, quêtes —
> prévus plus tard dans la bêta) et un existant est à supprimer (l'écran de transition entre
> pièces).

---

## 0. Ce qui est déjà tranché — à ne pas re-décider

Le jeu a déjà une identité forte, posée dans le code et dans les deux briefs précédents. Ce
document part de là, il ne repart pas de zéro.

**Le ton** (déjà écrit pour le bestiaire, vrai pour toute l'interface) : *rien n'est franchement
monstrueux, tout est légèrement faux. L'horreur vient de la politesse, de la répétition et du
protocole — jamais du gore.* Un menu trop poli, un bouton qui répond avec une formule d'huissier,
une confirmation qui prend la forme d'un tampon officiel : c'est la même veine que le majordome
sans visage qui dit *« Vos pieds, je vous prie. »*

**La palette** : sombre, violacée, faiblement saturée (`--void`, `--bg`, `--panel`, `--ink` en
oklch). Toute couleur vive est un événement, jamais un décor — règle déjà énoncée pour le combat,
à faire respecter à l'identique dans les menus. Le moteur expose déjà **quatre humeurs**
(`data-mood`) : la nuit violette par défaut, `mauve`, `sap` (vert-de-gris froid) et `palais`
(cuivre, ambre, sang, cristal — la halle du jeu vivant, déjà posée dans `PalaceAtmosphere.vue`).
Aujourd'hui, **tous les écrans hors combat tournent en `palais`**, sans exception. C'est un choix
qui fonctionne comme socle (voir §4), mais qui aplatit un peu les écrans entre eux faute d'être
combiné à autre chose.

**La typographie** : `Playfair Display` (serif, la voix du Palais — noms propres, titres de salle,
noms de sort) / `Inter` (l'interface fonctionnelle — boutons, libellés, corps de texte) /
`JetBrains Mono` (les nombres bruts — statistiques, dégâts, tout ce qui doit se lire comme une
mesure plutôt que comme une phrase). Cette hiérarchie est déjà appliquée par endroits ; elle doit
devenir une règle stricte partout, y compris dans les écrans encore génériques.

**Les sept registres émotionnels** (Effroi ✶, Déni ◇, Mélancolie ❍, Rupture ⟡, Mémoire ◈, Silence
○, Folie ✳) ont chacun un glyphe et une couleur déjà câblés côté moteur (`EmotionalTypeBadge`).
C'est un langage iconographique tout fait, sous-exploité hors combat — voir §2.

**L'ornement existant** : un simple filet horizontal avec un losange central (`RuleOrnament`,
variantes or/givre) sert déjà de séparateur sur plusieurs pages. C'est peu, mais c'est cohérent —
à enrichir plutôt qu'à remplacer.

---

## 1. L'univers graphique, au-delà du plateau

Le Palais n'est pas un décor médiéval-fantastique classique : c'est une **machine baroque et
vivante**, à mi-chemin entre un palais Belle Époque et l'intérieur d'une horlogerie à vapeur.
`PalaceAtmosphere.vue` le dit déjà en code : rouages en fond, un cœur de chair qui bat au bas de
l'écran, de la vapeur, des gouttières qui laissent couler l'or et la braise, une faille de magma,
un cristal fracturé. Ce n'est pas une illustration de fond parmi d'autres — **c'est l'identité
graphique du jeu**, et elle doit infuser l'interface, pas seulement l'apparaître derrière elle.

Proposition de lecture pour la production graphique : **chaque panneau d'interface est lui-même
une petite salle du Palais.** Un cadre n'est pas un `<div>` avec une bordure, c'est un morceau de
boiserie, de marbre veiné ou de parchemin, choisi selon ce que l'écran raconte — exactement comme
chaque salle a son matériau (tapis bordeaux du Hall, cristal de la caverne, os et cendre des
Enfers). Un bouton n'est pas un rectangle cliquable, c'est un **objet du Palais** qu'on actionne :
un cachet qu'on appose, une clef qu'on tourne, un cordon de sonnette qu'on tire — au choix, mais
jamais un bouton anonyme de jeu vidéo générique.

Vocabulaire de matières à tenir sur l'ensemble des écrans, en écho direct au bestiaire déjà écrit :
- **Or et laiton** (`--gold*`) — la mémoire, l'archive, ce qui a de la valeur. Cadres de
  personnages, listes de sorts appris, tout ce qui est acquis et permanent.
- **Marbre et pierre pâle** (proche `--frost`) — l'autorité du Palais, sa froideur cérémonielle.
  Confirmations, écrans de choix engageants (nouvelle run, abandon).
- **Parchemin et encre** (`--gold` pâle + noir dense) — l'écrit, le journal, le compte-rendu.
  Tutoriel, Manifestations (dossier de menaces), Statuts (glossaire).
- **Cuir et tissu usé** — le Porteur lui-même, sa besace. Réservé à Besace et, plus généralement, à
  tout ce qui appartient concrètement au joueur plutôt qu'au Palais.
- **Cristal et lumière interne** — réservé aux écrans qui montrent un état vivant qui évolue
  (progression, réputation) plutôt qu'une simple liste statique.

---

## 2. Grammaire transversale — ce qui doit être identique partout

Ce sont les règles qu'un joueur doit apprendre une fois et retrouver sur les 11 écrans, sans
exception, sous peine de casser la lisibilité que le combat a déjà construite.

### 2.1 Couleur = information, jamais décoration
Règle déjà énoncée pour le bestiaire, à appliquer à la lettre dans les menus : un écran « calme »
(Équipe, Statistiques, Grimoire en simple consultation) reste presque monochrome. Une couleur
n'apparaît que pour porter un sens précis — un glyphe de registre émotionnel, un état de jauge
(voir Réputation), une alerte. Si une page a besoin de « plus de vie », la réponse est le mouvement
ou la matière (voir §2.4), jamais d'ajouter des teintes supplémentaires.

### 2.2 Les glyphes de registre, langage transversal
`EmotionalTypeBadge` existe déjà pour le combat ; ces sept glyphes doivent devenir la façon dont
**tout le jeu** classe une menace ou un contenu par nature émotionnelle, pas seulement l'écran de
combat : dans Manifestations (chaque boss porte déjà son registre), dans Statuts (grouper les
altérations par registre plutôt que par simple mécanique DoT/Buff/Debuff serait plus cohérent avec
le reste), dans Grimoire (un sort du registre Silence devrait porter le glyphe ○ à côté de son
nom, exactement comme un ennemi du même registre).

### 2.3 Une hiérarchie de cadre selon le camp
Le joueur doit apprendre à lire un cadre avant de lire le texte qu'il contient : liseré or = allié
/ acquis / positif, liseré `--blood` sourd = menace / perte / négatif, liseré `--frost` = neutre /
informatif. Cette règle existe déjà en pointillé (le combat l'applique), elle doit devenir
systématique dans Équipe, Statistiques, Grimoire, Équipement, Besace.

### 2.4 Le mouvement du Palais : vivant, mais fatigué
Aucune animation d'interface ne doit avoir de rebond ni de ressort (« spring easing »). Le battement
de cœur de `PalaceAtmosphere` (`cubic-bezier(0.5, 0, 0.5, 1)`, lent, presque poussif) est la
signature de mouvement du jeu entier : tout ce qui bouge dans l'UI — apparition d'un panneau,
ouverture d'un menu, validation d'un choix — doit respirer sur ce même tempo, jamais claquer comme
une UI de jeu mobile. Un menu qui s'ouvrait d'un coup sec serait le premier signe visuel de rupture
avec le reste du jeu.

### 2.5 Dosage de l'atmosphère complète
`PalaceAtmosphere` (rouages + cœur + vapeur + gouttières + cristal + grain) est un morceau
spectaculaire, à réserver aux écrans qu'on ne visite qu'occasionnellement et où l'on veut marquer
un moment (Seuil, Réputation en fin de run). Sur les écrans consultés en boucle pendant une run
(Grimoire, Équipement, Besace, Statistiques), une version **allégée** est nécessaire — grain et
vignette seuls, sans le cœur ni les rouages en continu — sous peine de fatigue visuelle et de
distraction sur des écrans où le joueur doit lire des chiffres vite.

### 2.6 Un langage de fenêtre unique
La fenêtre marchand (déjà refondue) et les popups de sélection de nœud en exploration ont chacune
leur propre chrome de modale aujourd'hui. Avant de personnaliser chaque écran, il faut arrêter
**un seul** cadre de modale/popup — bordure, coin, façon dont elle s'ouvre — et le réutiliser
partout où une fenêtre secondaire s'ouvre par-dessus un écran (confirmation d'abandon de run sur le
Seuil, détail d'un objet en Besace, etc.).

---

## 3. Écran par écran — ce qui rend chacun reconnaissable

Onze écrans existent aujourd'hui. Chacun doit avoir une idée directrice propre — pas seulement une
palette différente, une **raison d'être visuelle** liée à ce qu'il fait dans le jeu.

### Le Seuil — accueil (`/`) — À REFONDRE EN PRIORITÉ

C'est la toute première image du jeu, et c'est aujourd'hui l'écran le plus générique de tous alors
que le nom lui-même appelle une mise en scène évidente : **un seuil**, littéralement. Le code porte
déjà les tokens `--seuil-arch-wash` et `--seuil-arch-shadow` — une arche est déjà pressentie, à
pousser beaucoup plus loin qu'un simple lavis de fond.

Proposition : le joueur se tient devant la porte du Palais, pas devant un menu. Une arche
monumentale, sculptée, occupe le centre de l'écran. Les deux choix (reprendre une run / en
commencer une) ne sont pas des boutons empilés dans un panneau : ce sont **deux battants**, ou deux
inscriptions gravées de part et d'autre de l'arche, qu'on pousse plutôt qu'on ne clique. Si une run
est reprenable, sa vignette (date, progression) apparaît comme une **plaque commémorative** posée
au pied de l'arche plutôt que comme une carte de menu standard. C'est l'écran qui mérite le
traitement `PalaceAtmosphere` complet et le plus habité (cœur battant compris) — c'est la première
respiration du Palais que le joueur perçoit.

### Run — exploration et combat (`/run/:runId`)

Déjà la référence du jeu (tilecraft, panneau de combat, jetons). Ce document ne le retouche pas —
il fixe le niveau que les dix autres écrans doivent rejoindre.

### Manifestations — dossier des boss (`/manifestations`)

Ce n'est pas un menu, c'est une **archive consultée**. Le registre visuel le plus proche dans le
bestiaire est celui des Copistes (§3.2 du brief combat) : parchemin, cire rouge, encre noire. Un
boss non-affronté devrait porter un sceau non rompu ou une page partiellement voilée plutôt que
d'être simplement listé — l'idée que ce dossier **se remplit** au fil de la progression du joueur,
comme le parchemin du Copiste Aveugle se remplit en direct pendant un combat.

### Statuts — glossaire des altérations (`/statuts`)

À rattacher littéralement au **livre qui s'écrit seul**, décrit dans le canon du Palier (*« un
immense livre s'écrivant seul »*). Chaque entrée du glossaire est une page de ce livre plutôt
qu'une fiche technique. Grouper par registre émotionnel (§2.2) en plus du regroupement mécanique
actuel (par tick / par tour / instantané) donnerait à cet écran une deuxième lecture cohérente avec
le reste du jeu.

### Réputation — bilan de fin de run (`/reputation/:runId`)

L'écran du **jugement**. Les trois états déjà codés (Latent, Tendu, Rompu → sap, or, sang) sont
une jauge de verdict, pas une simple étiquette de statut : traiter chaque PNJ comme un dossier
scellé qu'on ouvre, avec un solde qui penche visuellement (une balance, un sceau qui se brise sur
« Rompu ») plutôt qu'un badge coloré. C'est, avec le Seuil, l'écran qui doit le plus porter
l'atmosphère complète — c'est la fin d'un chapitre, il doit peser.

### Tutoriel — règles du jeu (`/tutoriel`)

Déjà sectionné par couleur (or = combat, givre = génération de salles, sève = profondeur/risque) —
bon réflexe, à cadrer explicitement comme **les notes de l'Architecte** plutôt qu'une page d'aide
générique : plans, croquis, annotations à la main, en écho direct au personnage de Thomas (« un
carnet de bord toujours sur lui », « des lignes de plan d'architecte » sur les avant-bras). Une
règle de jeu présentée comme une marge annotée d'un plan de construction est plus cohérente avec le
Palais qu'une liste à puces neutre.

### Équipe — vue d'ensemble du groupe (`/equipe`)

Le seul écran où les six compagnons coexistent visuellement. Traiter comme une **galerie de
portraits** — un manteau de cheminée du Palais plutôt qu'une grille de fiches. Chaque personnage
garde, même en miniature, le détail signature déjà écrit pour lui dans le brief combat : la besace
du Porteur, le carnet de Thomas, la peluche de Mina qui traîne au sol, les mains toujours ouvertes
de Mané. Ce sont des cadres au sens propre — chacun avec le matériau de son personnage (pierre et
or pour Thomas, rideau gris-violet pour Elise, cuir sombre pour John).

### Statistiques — fiche individuelle détaillée (`/statistiques`)

Le pendant zoomé d'Équipe : un **dossier personnel** qu'on sort de la galerie. Même logique de
matériau par personnage, mais cette fois avec les nombres en avant (`JetBrains Mono`, discipline
déjà posée en §0). C'est l'écran le plus « instrumenté » du lot — il peut se permettre d'être plus
sobre et plus dense, à condition de garder le cadre-personnage identifiable au premier coup d'œil
entre Équipe et Statistiques.

### Grimoire — sorts appris (`/grimoire`)

Le nom appelle déjà le traitement : un **livre qu'on feuillette**, pas une liste. Grouper les sorts
par registre magique (Flamme froide, Encre, Silence, Liturgique…) tel que déjà défini en §4 du
brief combat, plutôt que par personnage — cela apprend au joueur la même grammaire visuelle que le
bestiaire et les effets de sort en combat, renforçant la cohérence globale plutôt que de créer un
troisième système de classement.

### Équipement — armes et objets équipés (`/equipement`)

**L'armoire**, ordonnée, presque cérémonielle : silhouette d'abord, détail ensuite, en écho direct
à la règle « la silhouette prime sur le détail » du bestiaire. Une arme équipée devrait se voir
posée sur son personnage (silhouette-paperdoll) plutôt que listée sous forme d'icône seule — c'est
l'écran qui doit le plus ressembler à une penderie de maison de maître, rangée, avec chaque pièce à
sa place.

### Besace — inventaire de run (`/besace`)

**L'exact opposé d'Équipement**, et c'est volontaire : c'est l'objet signature du Porteur
lui-même (*« Une besace en évidence — l'inventaire est un système central »*, *« il compte ce qu'il
a »*). Là où Équipement est une armoire rangée, Besace doit se sentir **portée, en vrac, à même le
sac** — objets qui se bousculent, pas une grille propre. C'est le seul écran du jeu qui appartient
au joueur plus qu'au Palais ; il devrait être visuellement le plus chaud et le moins cérémoniel des
onze, avec le cuir usé en matière dominante plutôt que l'or ou le marbre.

### Écran de transition entre pièces — À SUPPRIMER

Retiré du parcours, ne demande aucune direction graphique. Sa suppression simplifie la boucle
d'exploration ; rien à concevoir ici.

### Animations de sorts, boss, événements, quêtes — HORS PÉRIMÈTRE (bêta ultérieure)

Les animations de sorts et les boss ont déjà leur brief complet (`brief-direction-artistique-
combat.md`, §2 et §4) — aucune direction nouvelle à produire ici, seulement à l'implémenter le
moment venu. Les événements et les quêtes n'ont pas encore d'écran : quand ils arriveront,
recommandation forte de **ne pas inventer un nouveau langage visuel pour eux**. Un événement se
déclenche dans une salle : il doit hériter de l'identité de cette salle (`brief-design-par-salle
.md`) plutôt que d'ouvrir une fenêtre neutre. Une quête est un objet écrit : elle relève plutôt du
registre Encre/Copiste déjà défini, cohérent avec Manifestations et Statuts.

---

## 4. Points de cohérence à trancher avant production

- **Un seul cadre de modale/popup** (§2.6) — à définir avant de personnaliser quoi que ce soit
  d'autre, sous peine de onze écrans cohérents individuellement mais qui ne se répondent pas quand
  une fenêtre s'ouvre par-dessus.
- **L'humeur `palais` comme socle commun, pas comme seule option** — aujourd'hui les onze écrans
  hors combat tournent tous en `data-mood="palais"`. Recommandation : garder `palais` comme humeur
  par défaut de toute l'interface (c'est ce qui fait tenir le jeu ensemble), mais envisager `mauve`
  ou `sap` comme accents ponctuels et volontaires — par exemple Réputation en `mauve` pour un
  bilan plus froid et jugeant, ou Statuts en `sap` pour le rattacher visuellement aux altérations
  « végétales »/organiques — plutôt que de multiplier les humeurs sans raison narrative.
- **Le glyphe de registre comme langage transversal** (§2.2) — décision à prendre une fois, pas
  écran par écran : soit il devient systématique partout où un registre émotionnel s'applique
  (Manifestations, Statuts, Grimoire), soit il reste un outil de combat seulement. Ce document
  recommande la première option.

---

## 5. Note d'usage

Comme les deux briefs précédents : les citations en italique issues du catalogue de jeu sont
canon. Le reste — matériaux, cadrage, hiérarchie de cadre, propositions écran par écran — est une
direction ouverte à itération. Pour transmettre à Claude Design, ce document se lit en
complément direct de `brief-direction-artistique-combat.md` (personnages, bestiaire, sorts) et de
`brief-design-par-salle.md` (les 27 salles) — les trois ensemble couvrent tout ce que le joueur
verra à la sortie de la bêta.

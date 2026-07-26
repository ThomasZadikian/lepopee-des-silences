# Brief de direction artistique — Combattants & sorts

> Destiné à la production graphique. Recense **6 alliés**, **6 boss**, **41 ennemis** et
> **138 sorts**, avec pour chacun une description de ce à quoi il doit ressembler.
>
> Les descriptions narratives proviennent du catalogue de jeu (`CatalogSeedRunner.cs`) —
> elles sont canon. Ce qui est ajouté ici : silhouette, palette, détail signature et
> intention d'animation.

---

## 0. Cadre général

### Ton

Le Palais est un lieu bâti par l'imagination d'un enfant, reconstruit par un Architecte,
et gouverné par Him'Lit. Rien n'y est franchement monstrueux : tout y est **légèrement
faux**. Un majordome trop courbé, un promeneur qui salue toujours au même angle, une
infirmière dont on ne voit pas les yeux. L'horreur vient de la politesse, de la répétition
et du protocole — pas du gore.

**Règle directrice** : chaque créature doit pouvoir être décrite en une phrase qui commence
normalement et se termine mal.

### Palette de référence (tokens du jeu, format oklch)

| Rôle | Token | Valeur |
|---|---|---|
| Fond profond | `--void` | `oklch(0.208 0.044 272)` |
| Fond / panneau | `--bg` / `--panel` | `oklch(0.258 0.050 270)` / `oklch(0.302 0.054 268)` |
| Encre claire (texte, os) | `--ink` | `oklch(0.948 0.018 280)` |
| Or (mémoire, Palais) | `--gold` | `oklch(0.862 0.098 86)` |
| Braise (vitalité) | `--ember` | `oklch(0.700 0.150 45)` |
| Givre (flamme froide) | `--frost` | `oklch(0.846 0.100 276)` |
| Sang | `--blood` | `oklch(0.812 0.110 13)` |
| Sève (végétal) | `--sap` | `oklch(0.840 0.092 162)` |

L'ensemble est **sombre, violacé, faiblement saturé**. Toute couleur vive est un événement :
elle signale une menace, une magie ou une émotion, jamais un décor.

### Registres émotionnels

Sept registres structurent le bestiaire. Chacun a déjà son glyphe et sa couleur dans le
moteur — à respecter strictement, c'est la clé de lecture du joueur en combat.

| Registre | Glyphe | Couleur | Sensation |
|---|---|---|---|
| **Effroi** | ✶ | `oklch(0.62 0.20 18)` | rouge sourd, terreur figée |
| **Déni** | ◇ | `oklch(0.78 0.13 78)` | ambre clinique, propreté fausse |
| **Mélancolie** | ❍ | `oklch(0.70 0.11 248)` | bleu délavé, lenteur |
| **Rupture** | ⟡ | `oklch(0.66 0.19 38)` | orange forge, cassure |
| **Mémoire** | ◈ | `oklch(0.84 0.11 86)` | or poussiéreux, archive |
| **Silence** | ○ | `oklch(0.80 0.02 272)` | gris-violet, absence |
| **Folie** | ✳ | `oklch(0.64 0.22 340)` | magenta, décrochage |

### Contraintes techniques

- Les combattants sont des **jetons sur une grille tactique 3D** (TresJS/Three.js), vus en
  vue trois-quarts plongeante. La silhouette doit être lisible **à petite taille**, de dos
  comme de face.
- **La silhouette prime sur le détail.** Un joueur doit distinguer un Guard d'un Skirmisher
  d'un seul coup d'œil, sans lire le nom.
- Le terrain est procédural et comporte de l'élévation : prévoir que les jetons soient
  **posés au sol**, sans ombre portée peinte en dur (l'ombre est calculée par la scène).
- Les sorts sont des **effets superposés à la grille**, pas des cinématiques : durée courte,
  lecture immédiate, pas d'occlusion du terrain.

### Grammaire des silhouettes par rôle

| Rôle | Lecture visuelle attendue |
|---|---|
| **Guard** | large, bas, symétrique — occupe la case entière |
| **Bruiser** | haut, épaules lourdes, asymétrie marquée |
| **Skirmisher** | étroit, penché en avant, appui sur un pied |
| **Support** | vertical, statique, un objet tenu devant soi |
| **Disruptor** | contour instable, mal défini, flotte |
| **Swarm** | petit, jamais seul — se dessine par groupes de 3+ |

---

## 1. Alliés — 6 personnages

Ce sont les seules figures que le joueur regarde pendant des heures. Elles doivent être
**chaleureuses au milieu d'un décor hostile** : plus de contraste, des tissus qui bougent,
des visages lisibles. Là où le bestiaire est figé, les alliés respirent.

> Équipe de **4 maximum** en combat. Prévoir que les six coexistent visuellement dans
> n'importe quelle combinaison de quatre.

### Le Porteur — personnage joueur

Le protagoniste, celui qui traverse le Palais. Sa fiche n'est pas verrouillée par le
catalogue : c'est la page la plus libre du brief.

**Silhouette** — humaine, adulte, sans armure lourde ; un voyageur plutôt qu'un guerrier.
Une besace en évidence (l'inventaire est un système central). Vêtements de marche usés,
superposés, réparés.
**Palette** — `--ink` et bruns neutres, une seule note chaude (`--ember`) sur un accessoire
signature.
**Détail signature** — il porte quelque chose qui n'est pas à lui et qu'il n'a pas encore
rendu.
**Animation** — repos : il vérifie sa besace. C'est un personnage qui compte ce qu'il a.

### Elise — l'accompagnatrice

Aussi ancienne que l'Enfant, elle connaît tout du Palais. Le silence et l'apathie d'Elise
ne sont qu'une façade : au fond d'elle brille un espoir aussi grand que le Palais.
Rôle mécanique : soutien et soin (`Baiser d'Elise`, `Larme d'Elise`, `Se taire`).

**Silhouette** — féminine, droite, immobile ; la plus statique de l'équipe. Robe longue
sans ornement qui tombe droit, comme un rideau.
**Palette** — `--ink` désaturé, gris-violet du registre **Silence**. Une seule zone chaude,
au niveau du cœur, visible uniquement quand elle soigne.
**Détail signature** — elle ne regarde jamais tout à fait la caméra. Bouche fermée en
permanence : son pouvoir, c'est « Se taire ».
**Animation** — repos : parfaitement immobile, seule l'étoffe bouge. Soin : elle se penche
et pose les lèvres, sans un mot.

### Thomas — la première projection de l'Architecte

Calme, équilibré, conscient de ce qu'il est. Celle des projections qui ressemble le plus à
l'Architecte. Rôle mécanique : tank (110 PV, Défense 12, garde 8) — `Fondations`, `Rempart`.

**Silhouette** — la plus large et la plus stable de l'équipe. Épaules carrées, position
d'appui, pieds écartés. Il occupe visuellement la case entière.
**Palette** — `--gold-deep` et pierre. Il est bâti dans les matériaux du Palais lui-même.
**Détail signature** — un carnet de bord toujours sur lui (objet canon du jeu). Ses
avant-bras portent des lignes de plan d'architecte, comme des veines réglées.
**Animation** — repos : il regarde autour de lui, évalue la pièce. Garde : il pose un pied
en avant et le sol se raffermit sous l'allié protégé.

### Mané — l'intelligence émotionnelle

Très émotive et très impulsive, mais d'une intelligence émotionnelle redoutable — elle
comprend vite ceux qui l'entourent. Rôle mécanique : DPS rapide (Attaque 15) —
`Impulsivité`, `Caresse de Mané`.

**Silhouette** — la plus mobile. Étroite, en mouvement même à l'arrêt, cheveux et vêtements
en retard d'un temps sur le corps.
**Palette** — `--blood` adouci, tons chauds. La seule alliée franchement colorée.
**Détail signature** — les mains toujours ouvertes, jamais en poing, même quand elle frappe.
**Animation** — repos : elle change d'appui sans arrêt, regarde ses coéquipiers plutôt que
l'ennemi. Attaque : elle part avant d'avoir décidé.

### Mina — l'enfant du Palais

Une petite fille née dans le Palais, la seconde de ses habitants. Ses parents restent
inconnus ; elle les cherche. Rôle mécanique : fragile (65 PV, Attaque 5) — soutien et bonus
de réputation.

**Silhouette** — nettement plus petite que les autres. Elle doit être immédiatement lisible
comme « l'enfant » du groupe, même à distance.
**Palette** — pastels délavés sur fond sombre. Elle est la seule tache claire du champ de
bataille.
**Détail signature** — sa peluche (objet canon : « Peluche de Mina »), tenue par un bras,
qui traîne au sol quand elle marche.
**Animation** — repos : elle regarde ailleurs, vers les portes, vers les couloirs. Elle
cherche quelqu'un. En combat : elle se place derrière quelqu'un.

### John — l'ancien voleur

Un ancien voleur qui, en pillant d'anciennes ruines, a fini par traverser la faille du
Palais. Il y a survécu. Rôle mécanique : offensif agile (90 PV, Attaque 13) —
`Vol à la tire`, `La liberté retrouvée`.

**Silhouette** — penchée en avant, mains près du corps, jamais de face. Il se présente
toujours de trois-quarts, prêt à partir.
**Palette** — cuirs sombres, `--void` et `--ink-4`. Le moins visible de l'équipe, ce qui est
le propos.
**Détail signature** — il porte des objets qui ne viennent pas du Palais : butin de ruines
d'un autre monde, seule preuve qu'un dehors existe.
**Animation** — repos : il évalue les sorties de la salle. Attaque : un geste vif, presque
escamoté, appris dans les ruines.

---

## 2. Boss — 6 figures

Un boss doit être reconnaissable **à la silhouette seule, en un quart de seconde**. Chacun
occupe plusieurs cases visuellement, même si la mécanique le place sur une seule.

### Le Grand Cardinal — `Antechamber`, `Threshold`

*Le grand cardinal du Palais.* Premier boss rencontré (difficulté 2, 90 PV). Lance
`Prière` et `Flamme froide`.

**Silhouette** — verticale, immense, en robe cardinalice ; une colonne de tissu. Plus haut
que large, chapeau ecclésiastique exagéré qui prolonge la verticale.
**Palette** — rouge cardinal désaturé vers `--blood-dim`, doublure `--gold`. La flamme
froide qu'il invoque est `--frost` : le contraste chaud/froid est son identité.
**Détail signature** — sous la robe, aucun corps. Le tissu tient debout tout seul. Ses mains
gantées émergent de manches trop longues.
**Animation** — il ne marche pas, il glisse. Sa robe ne suit pas ses mouvements avec le bon
délai.

### L'Impératrice — la Vipère — `Rupture`, `Memory`

*L'impératrice du Palais.* Difficulté 3, 140 PV, Vitesse 14 — le boss rapide.

**Silhouette** — féminine, allongée, sinueuse. Aucun angle droit : tout est courbe et
enroulement. Traîne qui s'enroule au sol comme un corps de serpent.
**Palette** — violacé profond sur `--void`, écailles à reflets `--gold`.
**Détail signature** — elle ne cligne jamais des yeux. Une couronne fine, portée légèrement
de travers.
**Animation** — mouvements de reptile : longues immobilités, puis détente brutale. Sa
vitesse doit être visible dans son idle.

### L'Homoncule — le Vieillard — `Rupture`, `Silence`

*Le roi, l'Homoncule, bien des noms lui furent donnés.* Difficulté 3, 160 PV, Défense 27,
Vitesse 8 — le plus lent et le plus dur. Lance `Transmutation`.

**Silhouette** — voûtée, massive, disproportionnée : membres trop courts sur un torse trop
grand. Il donne l'impression d'un être inachevé, ce qu'il est.
**Palette** — nacré, soufré, bleu-violet de flamme froide (`--frost-deep` mêlé d'ambre
sale). Chair qui n'a jamais été finie.
**Détail signature** — sa peau porte les traces de sa fabrication : coutures, marques de
moule, zones non lissées. **Le feu, le vrai, est sa seule terreur** — prévoir une réaction
visuelle de recul face aux sorts de flamme séraphine.
**Animation** — lent, presque doux, jusqu'à ce qu'il hurle. Le hurlement doit être une
rupture d'animation totale.

### Le Pape Louis XVII — `Antechamber`, `Fear`

*Le pape.* Difficulté 4, 200 PV, Défense 36 — le mur. Lance `Brume`.

**Silhouette** — la plus large. Assis ou trônant plutôt que debout, chape immense étalée qui
couvre le sol autour de lui. Une masse, pas une figure.
**Palette** — `--gold` terni, blanc cassé jaunissant, orfroi lourd.
**Détail signature** — la tiare est trop grande et repose sur ses épaules, pas sur sa tête.
Il regarde depuis dessous.
**Animation** — quasi immobile. Seule la brume qu'il exhale bouge. Quand il frappe, seul un
bras se déplace.

### Him'Lit — `Final` — boss final

*Le Seigneur du Palais. Il ne dirige que le Palais, mais d'une main de fer — arrogant,
cynique, d'une élégance absolue.* Difficulté 5, 280 PV, Attaque 32, Défense 48. Le seul boss
de danger 100.

**Silhouette** — élégante, mince, absolument verticale. L'exact inverse de l'Homoncule : là
où tout le Palais est bancal, **lui est parfait**. C'est ce qui le rend terrifiant.
**Palette** — noir profond `--void`, ligne `--gold` pure, aucun désordre. Une seule couleur
d'accent, tenue.
**Détail signature** — une élégance sans faute : rien ne dépasse, rien n'est usé, rien n'est
réparé. Il est le seul être du Palais qui ne soit pas abîmé.
**Animation** — il bouge peu et toujours à propos. Aucun geste parasite. Quand il agit, le
décor réagit avant lui.

### L'Impératrice de la Falaise — mini-boss, `Legendary`

*Une silhouette féminine démesurée émergeant à mi-corps de la mer violacée, couronnée d'une
structure qui évoque à la fois un diadème et une cage thoracique renversée. Sa robe est la
mer — littéralement : les vagues sont son ourlet, et la marée suit ses humeurs.*

**Silhouette** — buste seul, gigantesque, sortant de l'eau. Pas de jambes : la mer commence
là où le corps s'arrête. Elle doit dépasser du cadre.
**Palette** — mer violacée (`--void` saturé), couronne d'os pâle `--ink`, écume `--frost`.
**Détail signature** — la couronne-cage thoracique, lue comme diadème de loin et comme
squelette de près. Le niveau de l'eau monte avec `Marée montante` : la falaise se rétrécit
littéralement autour du joueur.
**Animation** — respiration lente et marine. La marée suit ses humeurs — le niveau de l'eau
est son indicateur d'état.

---

## 3. Ennemis — 41 créatures, 16 familles

Chaque famille partage une **grammaire visuelle commune** : un matériau, une couleur, un
principe de déformation. Un joueur doit reconnaître la famille avant de reconnaître
l'individu.

---

### 3.1 Les Veilleurs du Seuil — registre **Silence** ○

*Grammaire de famille* : la livrée de maison, le service, la révérence. **Aucun n'a de
visage.** Matériau : tissu lourd, marbre, argent terni. Ils ne vous attaquent pas : ils vous
corrigent.

**Veilleur du Tapis** — *Guard, Common*
> Une silhouette de majordome sans visage, penchée en permanence vers le sol, comme figée
> dans une révérence qui n'a jamais eu le droit de se relever. Ses mains gantées lissent
> inlassablement un pan de tapis bordeaux qui le suit où qu'il aille, cousu à ses chevilles.
> « Vos pieds. Je vous prie. »

**Silhouette** — pliée à 90°, jamais redressée ; large et basse, elle barre le passage.
**Palette** — bordeaux profond du tapis, livrée noire, gants blancs (seule valeur claire).
**Détail** — la couture tapis/chevilles doit être explicite : il est attaché à son devoir.
**Animation** — il lisse le tapis en boucle, même en combat.

**Porteur de Plateau** — *Support, Common*
> Un torse en livrée, sans jambes, flottant à hauteur exacte de service. Sur son plateau
> d'argent : trois tasses. La première fume, la deuxième est vide, la troisième est
> retournée. Personne n'a jamais bu la troisième. « Thé ? Eau ? Attention ? »

**Silhouette** — buste seul, coupé net à la taille, flottant à hauteur constante.
**Palette** — livrée sombre, argent du plateau, vapeur `--ink` de la première tasse.
**Détail** — les trois tasses sont l'élément narratif : fumante / vide / retournée. Toujours
lisibles.
**Animation** — hauteur rigoureusement stable, quoi qu'il arrive. Le plateau ne penche jamais.

**Écho de Politesse** — *Disruptor, Common*
> Une brume en forme de courbette. On la distingue à peine dans les couloirs distordus : un
> pli dans l'air qui s'incline sur votre passage et ne se redresse que dans votre dos.
> « Après vous. Non — après vous. »

**Silhouette** — quasi absente : une distorsion, un pli. Le plus difficile à cadrer, et
c'est le propos.
**Palette** — transparence pure, léger décalage chromatique gris-violet.
**Détail** — la courbette n'existe que dans la déformation de ce qu'il y a derrière.
**Animation** — s'incline face au joueur, se redresse uniquement hors de son champ.

**Sentinelle du Seuil** — *Bruiser, Elite*
> Un pilier de marbre du Hall — l'un des quatre — descendu de son socle. Des veines
> bleu-violet parcourent sa pierre : la Flamme froide dort dedans. Il marche lentement, et le
> sol s'essuie tout seul devant ses pas. « Le seuil a été souillé. Cela ne se pardonne pas. »

**Silhouette** — colonne. Strictement cylindrique, sans bras évidents, plus haute que tout.
**Palette** — marbre pâle veiné `--frost` lumineux. La flamme froide pulse sous la pierre.
**Détail** — le sol se nettoie tout seul devant elle : effet de traînée inversée.
**Animation** — lenteur totale, pas de transition. Elle est à un endroit, puis à un autre.

---

### 3.2 Les Copistes — registre **Mémoire** ◈

*Grammaire de famille* : le papier, l'encre, l'acte d'écrire. Ils enregistrent le combat
pendant qu'il a lieu. Matériau : parchemin, cire, encre noire, cuir.

**Copiste Aveugle** — *Disruptor, Common*
> Un scribe voûté dont les orbites sont scellées de cire à cacheter. Ses doigts, terminés par
> des plumes, courent sur un parchemin déroulé à même l'air. Il recopie tout ce qui se passe
> dans la pièce — les gestes, les cris, les silences — en temps réel. « Je n'ai pas besoin de
> voir. Le texte se souvient pour moi. »

**Silhouette** — voûtée sur un parchemin flottant qui prolonge sa silhouette à l'horizontale.
**Palette** — parchemin `--gold` pâle, cire rouge `--blood` aux orbites, encre noire.
**Détail** — les sceaux de cire sur les yeux, et les doigts-plumes. Le parchemin se remplit
en direct pendant le combat.
**Animation** — sa main écrit **ce que le joueur vient de faire**, avec une frame de retard.

**Encrier Vivant** — *Support, Common*
> Une masse d'encre noire contenue dans un corps de verre fêlé, à peu près humanoïde. Elle
> laisse derrière elle des flaques qui forment des mots — toujours les mêmes : les premières
> pièces du Palais, décrites à l'infini. « Il ne faut jamais, jamais manquer d'encre. »

**Silhouette** — humanoïde approximative, contour en verre, contenu liquide et mobile.
**Palette** — verre translucide sur encre absolument noire — le point le plus sombre du jeu.
**Détail** — les fêlures du verre, et les flaques-mots laissées au sol (traînée persistante
sur la grille).
**Animation** — le niveau d'encre baisse quand elle recharge un allié.

**Page Inachevée** — *Disruptor, Uncommon*
> Une feuille immense, déchirée à mi-hauteur, qui flotte verticalement. Le texte qu'elle
> porte s'interrompt en plein mot. Ceux qui la lisent trop longtemps sentent leur propre
> pensée s'interrompre au même endroit, encore et encore. « La phrase s'arrête ici. Vous
> aussi. »

**Silhouette** — plan vertical, presque 2D. Vue de profil, elle disparaît presque.
**Palette** — blanc papier sur fond sombre : le contraste le plus violent de la famille.
**Détail** — la déchirure horizontale à mi-hauteur, et le texte coupé net en plein mot.
**Animation** — flotte sans poids ; se retourne pour éviter d'être vue de profil.

**Le Relieur** — *Bruiser, Rare*
> Un artisan massif au tablier de cuir, dont les bras se terminent en aiguilles courbes
> enfilées de nerf. Il ne relie pas des livres : il relie des instants entre eux, cousant la
> douleur d'hier à celle de demain pour qu'aucune ne puisse finir. « Rien ne se termine tant
> que je n'ai pas cousu la dernière page. »

**Silhouette** — la plus massive de la famille. Épaules d'artisan, tablier lourd, bras
allongés terminés en aiguilles.
**Palette** — cuir brun, acier des aiguilles, fil rouge sombre `--blood`.
**Détail** — les aiguilles courbes enfilées de nerf, et le fil qui relie physiquement deux
adversaires quand il lance `Reliure de chair`.
**Animation** — geste de couture ample et régulier, comme un métier à tisser.

---

### 3.3 Les Squelettes de Souvenirs — registre **Mémoire** ◈

*Grammaire de famille* : l'os gris cendre, la gravure illisible, l'objet incongru. Ce sont
des souvenirs que personne n'a jamais racontés. Matériau : os, cendre, braise.

**Squelette de Souvenir** — *Skirmisher, Common*
> Un squelette gris cendre dont les os portent des gravures illisibles — les restes d'un
> moment que personne n'a jamais raconté. Il tient parfois un objet incongru : une tasse, un
> jouet, une clef. L'objet est le seul indice de ce qu'il fut. « ... » (il n'a jamais été
> raconté ; il n'a pas de voix)

**Silhouette** — squelette humain classique, mais **toujours tenant un objet du quotidien**.
L'objet fait la variation : prévoir 4–5 déclinaisons (tasse, jouet, clef, chaussure, lettre).
**Palette** — gris cendre mat, sans blanc. Les gravures captent une lueur `--gold` faible.
**Détail** — les gravures doivent être illisibles mais manifestement volontaires.
**Animation** — il ne lâche jamais son objet, même en attaquant. Aucun son, jamais.

**Porteur de Cendre** — *Support, Uncommon*
> Une silhouette encapuchonnée courbée sous une hotte débordant de cendre et d'ossements.
> Elle traverse la Calamité en ramassant ce qui reste des souvenirs morts, et les rallume un
> à un, comme des braises. « Je me souviens d'eux. C'est mon fardeau, et ma monnaie. »

**Silhouette** — courbée sous une charge qui la dépasse en hauteur. La hotte est plus grande
que le porteur.
**Palette** — cendre grise, braises `--ember` dans la hotte — la seule chaleur de la famille.
**Détail** — les braises se rallument une à une quand il soigne.
**Animation** — démarche lourde, il se penche pour ramasser ce qui tombe des autres.

**Chœur Muet** — *Disruptor, Rare*
> Trois cages thoraciques fusionnées en un seul buste, surmontées de trois crânes aux
> mâchoires grandes ouvertes. Aucun son n'en sort — mais l'air vibre, et le silence qui règne
> autour d'eux pèse physiquement sur les épaules. « Ils chantent. Vous ne l'entendrez jamais.
> C'est ça, le supplice. »

**Silhouette** — triple. Trois crânes en éventail sur un buste unique — silhouette la plus
identifiable du bestiaire.
**Palette** — os gris, intérieur des mâchoires en noir absolu.
**Détail** — les trois mâchoires grandes ouvertes en permanence, jamais fermées.
**Animation** — **aucun son**. La vibration est rendue par une distorsion de l'air autour du
buste, et par le silence forcé de la bande-son.

---

### 3.4 Les Chimères des Plaines — registre **Effroi** ✶

*Grammaire de famille* : l'animal composite, le camouflage pastoral, l'immobilité de
chasse. De loin, un troupeau. De près, non. Matériau : pelage, os, herbe haute.

**Chimère Affamée** — *Skirmisher, Common*
> Un prédateur composite — corps de cervidé, mâchoire de brochet, pattes trop nombreuses et
> repliées sous le ventre. Immobile dans les hautes herbes, elle est indiscernable des
> animaux paisibles de la plaine. Jusqu'à ce que quelque chose saigne. « Elle ne rugit pas.
> Elle compte vos battements de cœur. »

**Silhouette** — cervidé de loin, faux de près. Les pattes surnuméraires repliées sous le
ventre ne se voient qu'au mouvement.
**Palette** — fauve et herbe sèche, camouflage réel. `--blood` uniquement dans la gueule.
**Détail** — la mâchoire de brochet sur un corps de cerf : l'erreur d'assemblage doit sauter
aux yeux dès qu'elle ouvre la bouche.
**Animation** — immobilité totale, puis détente. Aucun cri : elle attend que ça saigne.

**Berger d'Ordres** — *Support, Uncommon*
> Une haute figure pastorale au visage effacé, appuyée sur une houlette faite d'une règle
> d'architecte démesurément allongée. Il ne parle pas aux chimères : il leur montre, et elles
> comprennent. Ses gestes ont la précision d'un plan. « Le troupeau ne demande qu'une chose.
> Je la lui accorde. »

**Silhouette** — verticale et haute, appuyée sur une hampe qui la dépasse largement.
**Palette** — bure claire délavée, règle en bois et laiton `--gold`.
**Détail** — la houlette est une **règle d'architecte** : graduations visibles. Lien direct
avec l'Architecte du Palais.
**Animation** — il désigne. Ses gestes sont géométriques, jamais organiques.

**Agneau Inversé** — *Disruptor, Uncommon*
> De loin : un agneau paisible, blanc, broutant. De près : la laine pousse vers l'intérieur,
> et ce qui remplit le corps n'est pas de la chair. C'est du silence comprimé, prêt à se
> détendre d'un coup. « Il broutait. Vous avez cligné des yeux. Il vous regarde. »

**Silhouette** — agneau parfaitement ordinaire. Aucune déformation externe : tout est à
l'intérieur.
**Palette** — blanc laine pur — le plus clair du bestiaire, donc le plus inquiétant sur
`--void`.
**Détail** — la laine pousse **vers l'intérieur**. Visible seulement aux ouvertures (bouche,
yeux, oreilles), où l'on devine un vide comprimé.
**Animation** — broute paisiblement. **Change d'orientation entre deux frames**, sans
transition, quand le joueur ne le regarde pas.

---

### 3.5 Les Créations du Forgeron — registre **Rupture** ⟡

*Grammaire de famille* : le métal mal assemblé, la chaleur résiduelle, le geste répété sans
but. Ce sont des ratés d'atelier qui continuent de travailler. Matériau : fonte, plaques,
laitier incandescent.

**Création Instable** — *Bruiser, Common*
> Un assemblage humanoïde de plaques mal jointes, dont une jambe est plus courte que l'autre
> et dont le torse s'ouvre par intermittence sur un foyer qui n'aurait jamais dû rester
> allumé. Elle se redresse sans cesse, compulsivement, comme pour prouver quelque chose à un
> marteau absent. « Elle se tient debout. Presque. C'est le presque qui fait mal. »

**Silhouette** — humanoïde bancale, asymétrie de jambes visible même à l'arrêt. Elle penche.
**Palette** — fer gris, joints mal soudés, foyer `--ember` intermittent dans le torse.
**Détail** — le torse s'ouvre et se referme par intermittence, révélant le feu intérieur.
**Animation** — elle se redresse en boucle, compulsivement, et retombe.

**Marteau Vivant** — *Bruiser, Uncommon*
> Un marteau de forge de deux mètres, animé, dont le manche s'est tordu en colonne
> vertébrale. Il frappe le sol en rythme, continuellement — le rythme exact du Forgeron au
> travail. Quand il frappe autre chose que le sol, ça hurle. C'est lui, le hurlement.
> « Les marteaux qui hurlent. C'est de lui qu'on parle. »

**Silhouette** — outil, pas créature. Une masse en haut, un manche-colonne vertébrale en bas.
Aucun membre.
**Palette** — acier sombre, manche osseux `--ink-4`.
**Détail** — le manche est **anatomiquement une colonne vertébrale** : vertèbres lisibles.
**Animation** — frappe le sol en rythme **constant**, y compris hors de son tour. C'est un
métronome. Le hurlement vient de lui, pas de la cible.

**Sentinelle de Fonte** — *Support, Uncommon*
> Une statue de fonte grossière, assise en tailleur au milieu des piliers de fer, qui murmure
> la litanie alchimique du Forgeron. Elle ne se lève jamais. Ses mains, posées sur ses
> genoux, rougissent quand elle transmute — et le métal de ses alliés rougit avec.
> « Plomb, or, mercure, soufre, sel. Elle récite. C'est tout ce qu'on lui a laissé. »

**Silhouette** — assise en tailleur, immobile, base large et triangulaire. **Ne se lève
jamais** — à modéliser assise, définitivement.
**Palette** — fonte noire mate ; les mains chauffent au `--ember` pendant la transmutation.
**Détail** — les mains posées à plat sur les genoux, qui rougissent par la paume.
**Animation** — lèvres qui murmurent en boucle les cinq mots. Aucun déplacement.

**Scorie Rampante** — *Skirmisher, Common*
> Une flaque de laitier incandescent, à demi solidifiée, qui se traîne en laissant des traces
> vitrifiées. Par moments, une forme s'ébauche dans sa masse — une main, un profil — puis
> retombe. Elle n'a jamais eu de forme finale. Elle les essaie toutes. « Ce que la forge
> recrache. Ça rampe. Ça brûle. Ça se souvient d'avoir été un projet. »

**Silhouette** — informe, basse, étalée au sol. La seule créature horizontale de la famille.
**Palette** — noir croûté en surface, `--ember` vif dans les fissures.
**Détail** — des formes s'ébauchent puis retombent dans la masse : une main, un profil.
Jamais deux fois la même.
**Animation** — reptation lente, traînée vitrifiée persistante sur les cases traversées.

---

### 3.6 Les Blouses Blanches — registre **Déni** ◇

*Grammaire de famille* : le blanc amidonné, la propreté excessive, le vocabulaire de soin
retourné en menace. Matériau : coton empesé, émail, métal chromé. **Personne n'a d'yeux
visibles.**

**Infirmière du Déni** — *Disruptor, Uncommon*
> Une silhouette amidonnée, impeccable, dont la coiffe descend trop bas pour qu'on voie les
> yeux. Elle pousse un chariot dont les fioles sont toutes étiquetées du même mot, illisible.
> Sa voix est celle de Margot — en plus douce, ce qui est pire. « Vous n'avez pas mal.
> Regardez le dossier : nulle part il n'est écrit que vous avez mal. »

**Silhouette** — verticale et nette, élargie par le chariot qu'elle pousse.
**Palette** — blanc pur amidonné sur `--void`, liseré ambre `--gold-dim` du registre Déni.
**Détail** — la coiffe qui masque les yeux ; les fioles toutes identiques, même étiquette
illisible.
**Animation** — démarche silencieuse et régulière. Le chariot ne grince jamais.

**Souvenir Alité** — *Skirmisher, Common*
> Un lit d'hôpital qui se déplace seul, draps tendus sur une forme humaine qui respire.
> Personne n'est dessous. La forme respire quand même. Sur la table de chevet, des fleurs
> fanées se refont une jeunesse quand on les regarde. « Il attend une visite. Vous ferez
> l'affaire. »

**Silhouette** — mobilier, pas créature. Un lit sur roulettes, drap tendu formant une bosse
humaine.
**Palette** — blanc drap, métal chromé, fleurs `--sap` fanées sur la table de chevet.
**Détail** — le drap **se soulève et retombe** : quelque chose respire. Les fleurs
rajeunissent quand la caméra les fixe.
**Animation** — se déplace seul, roulettes qui tournent, respiration continue sous le drap.

**Régisseur des Couloirs Blancs** — *Support, Rare*
> Un fonctionnaire immense au dos droit, dont le trousseau de clefs pend jusqu'au sol. Chaque
> clef ouvre une porte qui n'existe plus. Il arpente les couloirs blancs en vérifiant des
> serrures absentes, et l'ordre qu'il maintient est si total que l'air lui-même circule en
> file indienne. « Les visites sont terminées. Elles l'ont toujours été. »

**Silhouette** — la plus haute de la famille, dos parfaitement droit, allongée par le
trousseau qui pend jusqu'au sol.
**Palette** — blouse blanche, laiton terni `--gold-deep` du trousseau.
**Détail** — le trousseau touche le sol : des dizaines de clefs, toutes différentes, toutes
inutiles.
**Animation** — il vérifie des serrures qui ne sont pas là, sur des murs nus.

---

### 3.7 Les Pénitents de la Montagne — registre **Effroi** ✶

*Grammaire de famille* : la bure, l'ascension, la prière contrainte. Ils montent depuis
toujours. Matériau : laine grossière, or liturgique, os.

**Pèlerin Sans Visage** — *Skirmisher, Common*
> Une silhouette en robe de bure, courbée par la pente, dont la capuche s'ouvre sur une
> surface lisse — pas effacée : usée, comme une pièce de monnaie trop manipulée. Il gravit la
> montagne en égrenant un chapelet dont chaque grain est une petite dent. « Il monte depuis
> si longtemps qu'il a usé son visage contre le vent. »

**Silhouette** — penchée vers l'avant à l'angle de la pente, même sur terrain plat.
**Palette** — bure brun-gris, aucun accent. Le plus terne du bestiaire.
**Détail** — le visage **usé, pas absent** : reliefs encore devinables, comme une monnaie
lisse. Le chapelet de dents.
**Animation** — il monte, toujours. Même à l'arrêt, il pousse contre un vent absent.

**Prieur Lituique** — *Support, Uncommon*
> Un officiant au dos trop droit pour la bure qu'il porte, dont la bouche est cousue de fil
> d'or — et qui prie quand même, par les pores, par les gestes, par les jointures de ses
> doigts qui craquent en rythme liturgique. Devant lui flotte un encensoir qui fume à
> l'envers : la fumée descend. « Elle restaure — mais nourrit ce qui rôde. Lui, il sait
> exactement ce qui rôde. »

**Silhouette** — verticale, raide, avec un encensoir flottant devant elle.
**Palette** — bure sombre, fil d'or `--gold` vif à la bouche, fumée `--frost` descendante.
**Détail** — la **bouche cousue de fil d'or**, et la fumée qui tombe au lieu de monter.
Inverser la physique de la fumée est la signature de la famille.
**Animation** — doigts qui craquent en rythme liturgique ; l'encensoir oscille lentement.

**Frayeur Exhumée** — *Bruiser, Rare*
> Le premier explorateur — ou ce que l'ouverture de sa chambre funéraire a réveillé de lui.
> Un corps momifié dans une posture de recul, bras levés devant un danger que personne
> d'autre ne voit, figé au centième de seconde de sa dernière terreur. Il projette cette
> terreur autour de lui comme une lampe projette la lumière. « Depuis la découverte de la
> chambre, les échos de la frayeur ne cessent de s'agiter. En voici la source. »

**Silhouette** — **posture de recul figée** : bras levés en protection, corps rejeté en
arrière. Il ne change jamais de pose, y compris en attaquant.
**Palette** — bandelettes ocre sec, `--blood` sombre aux articulations.
**Détail** — la terreur se propage comme une lumière : halo `--effroi` projeté au sol autour
de lui, avec ombres portées vers l'extérieur.
**Animation** — il se déplace **sans sortir de sa pose**. Un objet qu'on fait glisser, pas un
être qui marche.

---

### 3.8 Les Faux Habitants du Jardin — registre **Déni** ◇

*Grammaire de famille* : la vie de quartier parfaite, la boucle comportementale, l'entretien
excessif. Matériau : tissu du dimanche, végétal taillé, outils de jardin.

**Promeneur Figé** — *Skirmisher, Common*
> Un promeneur en habits du dimanche, sourire cordial, chapeau levé en salut perpétuel. Son
> bras ne redescend jamais complètement. Quand on le croise une deuxième fois, il salue
> exactement pareil — même angle, même sourire, même phrase, même virgule. « Belle journée,
> n'est-ce pas ? N'est-ce pas ? N'est-ce pas ? »

**Silhouette** — un bras levé en permanence : la lecture se fait sur cette asymétrie.
**Palette** — costume clair du dimanche, chapeau de paille. Chaleureux, donc dissonant.
**Détail** — le bras ne redescend **jamais complètement** : il repart avant l'horizontale.
**Animation** — boucle **strictement identique** à chaque rencontre. Même angle à la frame
près — la répétition parfaite est l'effet.

**Jardinier Sans Ombre** — *Disruptor, Uncommon*
> Un jardinier voûté sur ses massifs, sécateur en main, qui taille sans interruption des
> fleurs déjà parfaites. Le soleil du Palais l'éclaire de face, de dos, de partout — et il ne
> projette aucune ombre. C'est lui qui l'a coupée : elle faisait désordre. « Les fleurs sont
> merveilleuses parce que je coupe tout ce qui ne l'est pas. »

**Silhouette** — voûtée sur son ouvrage, sécateur toujours en main.
**Palette** — tablier de jardin, verts `--sap` des massifs, acier du sécateur.
**Détail** — **aucune ombre portée**, alors que tous les autres jetons en ont une. À traiter
comme une exception explicite du moteur de rendu.
**Animation** — il taille en continu des fleurs déjà parfaites.

---

### 3.9 Les Gardiens de Crystal — registre **Mémoire** ◈

*Grammaire de famille* : le cristal translucide, l'inclusion d'objets, la lumière interne.
Ce sont des archives vivantes. Matériau : quartz, lumière réfractée.

**Gardien Intemporel** — *Bruiser, Rare*
> Un colosse de crystal translucide dans lequel on distingue, en suspension, des objets
> d'époques impossibles : un marteau qui n'est pas celui du Forgeron, une craie qui n'est pas
> celle de l'Enfant, une plume qui n'est pas celle de l'Écrivain. Des prototypes. Ou des
> originaux. « Il gardait déjà. Il gardera encore. Le mot "toujours" a été inventé pour
> éviter de le décrire. »

**Silhouette** — colosse anguleux, taillé à facettes. Aucune courbe.
**Palette** — cristal translucide `--frost`, réfractions `--gold` sur les arêtes.
**Détail** — les **objets en suspension** à l'intérieur : marteau, craie, plume. Ce sont des
échos des trois créateurs du Palais (Forgeron, Enfant, Écrivain) — mais pas les leurs.
**Animation** — les objets internes dérivent lentement, indépendamment du corps.

**Éclat Éveillé** — *Skirmisher, Uncommon*
> Un cristal flottant de la taille d'un cœur, qui pulse d'une lumière interne au rythme d'un
> battement. Il n'a ni yeux ni bouche, mais tous ceux qui l'approchent jurent s'être sentis
> dévisagés — puis mémorisés. « Un joyau qui a fini par comprendre qu'on le regardait. »

**Silhouette** — minuscule, flottante, géométrique. Le plus petit jeton du bestiaire.
**Palette** — cristal clair, pulsation interne `--frost` vif.
**Détail** — la taille exacte d'un cœur, et le rythme de pulsation d'un battement cardiaque.
**Animation** — pulse en continu ; **s'oriente vers celui qui le regarde**, sans yeux.

---

### 3.10 Les Échos d'Émotions

*Grammaire de famille* : pas de corps, pas de matière. Une émotion pure figée dans l'air, en
forme du geste qui l'exprime. Aucun visage, aucun membre — seulement une déchirure colorée.

**Écho de Colère** — *Bruiser, Uncommon*
> Une déchirure rouge sombre dans l'air, en forme de geste interrompu — un poing levé qui
> n'est jamais retombé. Elle vibre d'une chaleur sèche et cherche, en permanence, quelque
> chose qui mérite d'éclater. « Ça n'a plus personne à défendre. Ça frappe quand même. »

**Silhouette** — un poing levé, abstrait, lisible comme geste et non comme corps.
**Palette** — rouge sombre `--blood` saturé sur `--void`, vibration de chaleur sèche.
**Animation** — tension continue qui monte sans jamais se résoudre.

**Écho de Peur** — *Disruptor, Uncommon*
> Un frémissement pâle qui n'est jamais tout à fait là où on le regarde. Il se déplace par
> saccades, longe les murs, et son contact donne l'exacte sensation d'une porte qu'on trouve
> fermée dans le noir. « Il guette une sortie qui n'existe plus. Vous êtes entre lui et elle. »

**Silhouette** — pâle, mal définie, décalée d'un ou deux pixels par rapport à sa position
réelle.
**Palette** — blanc-gris froid, quasi transparent.
**Animation** — déplacement **par saccades**, jamais fluide. Longe systématiquement les
obstacles de la grille plutôt que de les traverser.

**Écho de Tristesse** — *Support, Uncommon*
> Une lenteur visible — l'air lui-même semble plus épais autour de lui. Il a vaguement la
> forme d'une personne assise, même quand il se déplace. Ceux qui le traversent se souviennent
> soudain de tout ce qu'ils n'ont pas dit à temps. « Il ne pleure pas. Il constate, longtemps
> après tout le monde. »

**Silhouette** — forme assise, conservée **même en déplacement**.
**Palette** — bleu délavé `--frost-deep` du registre Mélancolie.
**Animation** — toutes ses animations tournent à vitesse réduite par rapport au reste de la
scène. L'air autour de lui est visiblement plus dense.

---

### 3.11 Familles canon (bestiaire historique)

Antérieures au chantier Bestiaire, sans registre émotionnel assigné. Elles couvrent les
salles `Threshold`, `Fear`, `Shadow`, `Rupture` et `Memory`.

#### Prédateurs — l'ombre qui chasse

**Voraces** — *Bruiser, Elite*
> Hautes d'un mètre quarante à trois mètres, elles dévorent les énergies. Intelligentes,
> elles chassent en meute — ou seules, quand l'énergie est assez alléchante.

**Silhouette** — variable en taille (de 1,40 m à 3 m) : prévoir **trois échelles du même
modèle**, ce qui justifie visuellement la meute. Bipède, allongée, prédatrice.
**Palette** — noir d'ombre, sans texture, avec une gueule plus claire.
**Détail** — l'intelligence doit se lire : elles se coordonnent, se placent, attendent.

**Lamiz** — *Swarm, Common*
> Une meute attirée par l'énergie « alléchante ». Là où l'une apparaît, les autres suivent.

**Silhouette** — petite, quadrupède, **jamais seule** : à concevoir directement comme un
groupe de 3 à 5 exemplaires.
**Palette** — ombre, avec des reflets huileux distincts par individu.
**Animation** — elles se déplacent en essaim, se suivent avec un décalage.

**Uguiro** — *Bruiser, Elite*
> Un monstre des profondeurs du Palais. Lent à se révéler, terrible une fois éveillé.

**Silhouette** — masse au repos, indistincte ; puis dépliement. Deux états visuels très
différents.
**Palette** — noirs profonds, humidité des profondeurs.
**Animation** — **le réveil est l'événement**. Au repos, on ne doit pas comprendre ce que
c'est.

**Le Fossoyeur pâle** — *Skirmisher, Common*
> Il creuse avant même que tu sois tombé. Rapide, silencieux, jamais las.

**Silhouette** — maigre, haute, avec un outil de creusement. Penchée sur son ouvrage.
**Palette** — pâleur cireuse, terre sombre.
**Détail** — il creuse **la tombe du joueur**, en cours de combat, sur une case adjacente.

#### Brume

**Ombres tentaculaires** — *Disruptor, Common*
> Dans la brume, elles s'étirent jusqu'aux toits. On murmure des rats grands comme des
> chiens, des serpents à pattes — mais ce ne sont que ses bras.

**Silhouette** — pas de corps central visible : seulement des extensions qui montent hors
cadre.
**Palette** — brume gris-violet, noir des membres.
**Détail** — les « rats géants » et « serpents à pattes » des rumeurs sont **ses bras** :
suggérer ces formes animales dans les extrémités.

#### Lituisme

**L'Œil du Visionnaire animé** — *Disruptor, Elite*
> Le symbole rampe sur les pavés au gré des flammes. Pupille en amande, violacée et jaune :
> il vous voit avant que vous ne le voyiez.

**Silhouette** — **motif au sol**, pas créature debout. Il rampe à plat sur les pavés.
**Palette** — violacé et jaune, exactement comme décrit — le contraste le plus saturé du jeu.
**Détail** — c'est un **symbole religieux animé**, donc une forme graphique nette, pas
organique. Pupille en amande.
**Animation** — se déplace au gré de l'éclairage. Il regarde toujours le joueur en premier.

#### Psyché

**La Goule** — *Drain, Elite*
> L'Anxiété personnifiée. Elle envahit, recouvre, étouffe — jusqu'au « Tais-toi » d'Elise
> qui, parfois, la fait reculer.

**Silhouette** — envahissante : elle déborde de sa case, s'étale sur les cases voisines.
**Palette** — gris étouffant, matière qui recouvre.
**Détail** — elle **recouvre** plutôt qu'elle ne frappe. Lien direct avec Elise : prévoir une
réaction de recul au sort `Se taire`.

#### Alchimie

**L'Homoncule** — *Bruiser, Elite* — (version standard ; voir aussi le boss § 2)
> Né d'une flamme froide bleu-violet, nacré et soufré. Lent, presque doux — jusqu'à ce qu'il
> hurle. Le feu, le vrai, est sa seule terreur.

**Silhouette** — voûtée, inachevée, membres mal proportionnés.
**Palette** — nacré, soufré, `--frost` bleu-violet.
**Détail** — vulnérabilité au feu à rendre visible : recul face à `Flamme Séraphine`.

**L'Enfant d'argile** — *Support, Common*
> Un essai raté de l'Homoncule, abandonné avant l'achèvement. Il soigne encore, par réflexe.

**Silhouette** — petite, enfantine, **incomplète** : une partie du corps n'a jamais été
modelée.
**Palette** — argile crue, terre grise non cuite.
**Détail** — les traces de doigts du modeleur sont encore visibles. Il soigne par réflexe,
sans comprendre.
**Animation** — geste de soin répété machinalement, même sans cible.

---

## 4. Sorts — 138 effets

Les sorts ne sont pas des cinématiques : ce sont des **effets courts superposés à la
grille**. Trois exigences absolues :

1. **Lisibilité de camp** — le joueur doit savoir en 200 ms si l'effet lui est favorable.
2. **Non-occlusion** — le terrain et les jetons restent visibles pendant l'effet.
3. **Distinction Physique / Magique** — le moteur expose déjà `category`. Le **Physique** est
   net, rapide, sans traînée (impact + poussière). Le **Magique** a une phase de montée, une
   couleur propre et une rémanence.

### Grammaire par type d'effet

| Type | Forme | Timing |
|---|---|---|
| `Damage` mono | trait ou impact ponctuel sur la cible | 200–300 ms |
| `Damage` zone (`AllEnemies`) | onde partant du lanceur | 400–600 ms |
| `Heal` | montée verticale douce depuis le sol | 400 ms |
| `Buff` | halo bref + icône persistante sous le jeton | 300 ms + persistant |
| `Debuff` | descente ou compression vers le jeton | 300 ms + persistant |
| `Drain` | trait **orienté** cible → lanceur | 500 ms |
| `Silence` | absence : les effets voisins se coupent | 200 ms |
| `DamageOverTime` | pulsation discrète à chaque tick | 150 ms / tick |

---

### 4.1 Registre Flamme froide — bleu-violet `--frost`

Ne brûle pas la peau mais la chair ; le givre transperce l'os. **Pas de lumière chaude,
jamais.** Cristallisation plutôt que combustion, fumée qui descend.

`Flamme froide` · `Égide` · `Corps de verre` · `Repli de papier` · `Socle` · `Facette` ·
`Prisme` · `Stase` · `Réfraction` · `Pulsation` · `Poing de crystal` · `Fonte` ·
`Reformation`

**Forme** — éclats géométriques, arêtes nettes, réfraction. Rien d'organique.
**Couleur** — `--frost` `oklch(0.846 0.100 276)`, cœur plus clair, contour violet profond.
**Signature** — la surface touchée **givre** une demi-seconde avant de se fissurer.

### 4.2 Registre Flamme véritable & Forge — orange `--ember`

Le feu, le vrai. La seule terreur de l'Homoncule. À l'exact opposé de la flamme froide :
chaleur, combustion, métal en fusion.

`Flamme Séraphine` · `Souffle de la forge` · `Frappe d'enclume` · `Coup de grâce du
forgeron` · `Scorie` · `Contact` · `Laitier ardent` · `Éclat vitrifié` · `Foyer ouvert` ·
`Cadence` · `Redressement` · `Coup de plaque` · `Transmutation` · `Litanie`

**Forme** — combustion pleine, projections de métal, braises retombantes.
**Couleur** — `--ember` `oklch(0.700 0.150 45)`, cœur blanc-jaune, fumée noire.
**Signature** — la cible **rougit de l'intérieur** avant l'impact. Sur `Transmutation`, une
séquence de cinq flashes : plomb, or, mercure, soufre, sel.

### 4.3 Registre Encre & Écriture — noir sur or `--gold`

Ce qui est écrit arrive. L'encre est le noir le plus dense du jeu.

`Dictée` · `Plume sèche` · `Encre vive` · `Recharge` · `Éclaboussure` · `Phrase inachevée` ·
`Marge blanche` · `Couture` · `Reliure de chair` · `Nœud final` · `Anagramme` ·
`Écriture continuelle` · `Lecture des silences` · `Névrose`

**Forme** — traits de plume, éclaboussures, texte qui se forme et se dissout.
**Couleur** — encre noire absolue sur parchemin `--gold` pâle.
**Signature** — chaque sort **écrit un mot** trop bref pour être lu. Sur `Reliure de chair`,
un fil rouge relie physiquement deux jetons et persiste tant que l'effet dure. Sur `Marge
blanche`, la cible s'efface partiellement.

### 4.4 Registre Os & Cendre — gris cendre

Ce qui n'a jamais été raconté. Aucune saturation, aucun éclat.

`Griffe d'os` · `Fragment gravé` · `Étreinte creuse` · `Effondrement` · `Braise mémorielle` ·
`Jet de cendre` · `Fardeau partagé` · `Berceuse inversée` · `Note tenue` · `Chapelet de
dents` · `Bâton de marche` · `Griffe de recul`

**Forme** — projections d'éclats d'os, nuages de cendre, effondrements.
**Couleur** — gris cendre mat, braises `--ember` très localisées.
**Signature** — la cendre **retombe lentement** et persiste au sol une seconde après l'effet.
Sur `Effondrement`, le lanceur se démembre volontairement puis se reforme.

### 4.5 Registre Silence & Protocole — gris-violet ○

L'absence rendue visible. Le plus difficile et le plus important : ces sorts se lisent par
**soustraction**.

`Se taire` · `Silence` · `Étouffement feutré` · `Seuil souillé` · `Verdict du seuil` ·
`Formule creuse` · `Courbette inversée` · `Chute de marbre` · `Pli du tapis` ·
`Service du thé` · `Tasse retournée` · `Étiquette` · `Silence partagé` · `Contemplation
infinie`

**Forme** — compression de l'espace, ondes qui **absorbent** au lieu d'émettre.
**Couleur** — gris-violet `oklch(0.80 0.02 272)`, très faible contraste.
**Signature** — pendant l'effet, **les autres animations de la scène se figent** et la
bande-son se coupe. Le silence est un événement audio autant que visuel.

### 4.6 Registre Clinique & Déni — ambre ◇

Le vocabulaire du soin retourné en agression. Propre, net, faussement rassurant.

`Placebo` · `Bordage` · `Injection blanche` · `Drap tendu` · `Sonnette` · `Visite` ·
`Tour de clef` · `Trousseau` · `Extinction des feux` · `Salut de chapeau` ·
`Conversation tranquille` · `Pas de promenade` · `Sifflotement`

**Forme** — lignes droites, angles nets, gestes de procédure. Aucune organicité.
**Couleur** — blanc clinique et ambre `oklch(0.78 0.13 78)`.
**Signature** — l'effet ressemble à un **soin** dans sa forme et fait des dégâts dans son
résultat. Sur `Placebo`, le prochain soin de la cible affiche des chiffres verts... qui
s'annulent.

### 4.7 Registre Liturgique & Enfers — or et rouge sombre

Prière contrainte, fleuve des enfers, âmes réclamées.

`Prière` · `Repentir` · `Encens inversé` · `Oraison cousue` · `Dernière prière` ·
`Posture finale` · `Larme des enfers` · `Symphonie des enfers` · `Déluge du Styx` ·
`Sursaut mémoriel`

**Forme** — fumée qui **descend** au lieu de monter, eaux noires qui montent du sol.
**Couleur** — `--gold` liturgique et `--blood` très sombre.
**Signature** — l'inversion de la gravité de la fumée est la marque de tout le registre. Sur
`Déluge du Styx`, l'eau noire monte depuis le bas de la grille et recouvre les cases.

### 4.8 Registre Végétal & Jardin — vert `--sap`

Entretien, taille, greffe. Le soin par la coupe.

`Sécateur` · `Émondage` · `Greffe` · `Paillage` · `Brout` · `Ration`

**Forme** — coupes nettes, pousses rapides, paillis qui recouvre.
**Couleur** — `--sap` `oklch(0.840 0.092 162)`, terre brune.
**Signature** — chaque sort **coupe puis fait repousser**. Le soin passe par une amputation.

### 4.9 Registre Bestial & Chasse — fauve

Prédation, désignation de proie, curée.

`Morsure composite` · `Bond de flanc` · `Curée` · `Guet` · `Désignation` · `Houlette` ·
`Regard fixe` · `Bêlement à l'envers` · `Détente`

**Forme** — trajectoires rapides, arcs de mâchoires, marquage au sol.
**Couleur** — fauve, `--blood` sur les impacts.
**Signature** — `Désignation` pose une **marque persistante** sur la cible, visible par tous
les jetons de la famille. Sur `Bêlement à l'envers`, le son rentre au lieu de sortir : onde
qui converge vers le lanceur.

### 4.10 Registre Émotion pure — couleur du registre

Pas de matière, pas de projectile : une émotion qui se propage.

`Éclat` · `Constat sec` · `Montée` · `Explosion` · `Frisson` · `Porte fermée` · `Saccade` ·
`Poids` · `Constat tardif` · `Frayeur organique` · `Regard infantile` · `Impulsivité` ·
`Plongée dans la folie`

**Forme** — déformation de l'air, ondes de chaleur ou de froid, décalage chromatique.
**Couleur** — celle du registre émotionnel concerné (§ 0).
**Signature** — aucune particule solide. L'effet passe uniquement par la **distorsion** de ce
qu'il y a derrière. Sur `Plongée dans la folie`, saturation magenta `oklch(0.64 0.22 340)` et
tremblement de l'image.

### 4.11 Registre Marin — Impératrice de la Falaise

`Marée montante` · `Lame de fond` · `Lame de fond (renforcée)`

**Forme** — montée du niveau d'eau sur la grille, vague qui balaie une ligne de cases.
**Couleur** — mer violacée, écume `--frost`.
**Signature** — `Marée montante` **réduit visiblement la surface jouable** : les cases
noyées deviennent inaccessibles. C'est le seul sort du jeu qui modifie le terrain.

### 4.12 Registre Construction & Palais — or `--gold`

Ce que l'Enfant a bâti continue de se construire. Le registre du Palais lui-même.

`Construction perpétuelle` · `Fondations` · `Rempart` · `Création` · `Veillée` ·
`Connaissance académique` · `Une destinée cruelle` · `Favorite de Elise`

**Forme** — structures qui s'élèvent depuis le sol, plans d'architecte, pierre qui s'assemble.
**Couleur** — `--gold` et pierre pâle.
**Signature** — la construction **continue tour après tour** : l'effet visuel doit croître à
chaque tour tant que le buff dure, sans se rejouer depuis zéro.

### 4.13 Registre Personnel — sorts d'alliés

Les seuls sorts intimes du jeu. Petits, doux, sans spectacle — leur retenue est ce qui les
distingue.

`Baiser d'Elise` · `Larme d'Elise` · `Caresse de Mané` · `Vol à la tire` ·
`La liberté retrouvée` · `Clairvoyance`

**Forme** — un seul geste, tenu. Pas d'explosion, pas d'onde.
**Couleur** — chaude et sourde, à l'échelle d'un seul jeton.
**Signature** — ces sorts **ne débordent jamais de la case**. Là où tout le reste du jeu
occupe l'espace, eux se replient. `Baiser d'Elise` : elle se penche, pose les lèvres, rien de
plus.

### 4.14 Registre Physique brut

Ce que tout ce qui a des poings ou des crocs sait faire. Aucune couleur, aucune magie.

`Frappe` · `Brume` · `Éclaboussure`

**Forme** — impact, poussière, recul du jeton touché.
**Couleur** — aucune. Blanc d'impact et poussière grise.
**Signature** — le **recul du jeton** est l'effet principal. `Brume` est l'exception du
groupe : brouillard non naturel qui se lève et réduit la lisibilité **pour les deux camps**.

---

## 5. Priorités de production

Si la production doit être étalée, cet ordre maximise ce qui est visible le plus tôt :

1. **Les 6 alliés** — présents à chaque seconde de jeu.
2. **Les 5 familles des premiers étages** — Veilleurs du Seuil, Copistes, Squelettes de
   Souvenirs, Blouses Blanches, Prédateurs.
3. **Les 3 premiers boss** — Grand Cardinal, Impératrice-Vipère, Homoncule-Vieillard.
4. **Les registres de sorts 4.1, 4.2, 4.5, 4.14** — ils couvrent à eux seuls la majorité des
   sorts rencontrés en début de partie.
5. **Le reste du bestiaire**, famille par famille.
6. **Him'Lit et l'Impératrice de la Falaise** — vus en fin de parcours, mais ce sont les deux
   pièces qui doivent être les plus abouties.

---

## 6. Notes à l'attention de la production

- **Les citations en italique sont canon** : elles proviennent du catalogue de jeu et ne
  doivent pas être réinterprétées. Ce qui les suit (silhouette, palette, détail, animation)
  est une proposition de direction, ouverte à discussion.
- **Le Porteur (§ 1) est la seule fiche non verrouillée** par le catalogue — c'est la page la
  plus libre, et sans doute celle qui mérite le plus d'allers-retours.
- **Trois PNJ n'ont pas encore de fiche complète** au catalogue (Nicholas, John, Le Tisseur).
  John apparaît ici parce qu'il est jouable et que son kit est défini ; les deux autres sont
  hors périmètre pour l'instant.
- Le bestiaire compte **23 PNJ non combattants** (Majordome, Hitomi, Forgeron, l'Enfant,
  l'Architecte, l'Écrivain, Erika, Iris, Ethan, Margot…) qui ne figurent pas dans ce brief :
  ils relèvent d'un lot « portraits de dialogue », distinct des jetons de combat.

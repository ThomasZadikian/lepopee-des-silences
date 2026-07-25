# Brief Claude Design — un design par salle du Palais

Document de transmission. Il contient (1) les 27 salles du Palais avec leur description canon et
leur place dans l'enchaînement, (2) la décision de cadrage à prendre avant de produire quoi que ce
soit, et (3) tous les autres éléments graphiques qui restent à définir.

Source de vérité : `services/catalog/.../CatalogSeedRunner.cs`, fonction `SeedPalaisWorldAsync`.
Les descriptions ci-dessous sont recopiées **verbatim** du seed — c'est le texte canon, pas une
reformulation.

---

## 1. La décision à prendre avant tout le reste

Il existe **deux vocabulaires de thème** dans le projet, et le moteur de rendu n'en connaît qu'un.

| | Vocabulaire A — « thème de rendu » | Vocabulaire B — « thème de salle » |
|---|---|---|
| Valeurs | 7 : `Threshold`, `Memory`, `Forest`, `Rupture`, `Silence`, `Antechamber`, `Final` | 12 : `Welcome`, `Memory`, `Silence`, `Feelings`, `Collapse`, `Confinement`, `Meditation`, `Peace`, `Fear`, `Madness`, `Terrify`, `Underground` |
| Vient de | `RoomType` (enum générique du moteur), via `RoomThemeResolver.cs` | Le catalogue, colonne `Theme` de chaque salle canon |
| Utilisé par | **Tout le rendu** : tuiles, fond, brume, particules (`tilecraft`) | **Rien, aujourd'hui** |

**Conséquence** : les 27 salles du Palais s'écrasent aujourd'hui sur 7 palettes génériques. Le Hall
d'entrée et le quatrième étage des Enfers peuvent parfaitement sortir avec le même sol.

Il faut donc choisir sur quoi la déclinaison visuelle s'indexe :

- **Option 1 — par salle (27 palettes).** Identité maximale. La clé `catalogRoomKey`
  (`"room.halldentree"`, `"room.enfer3"`, …) **arrive déjà côté client** dans `RoomDto`, donc
  c'est un branchement direct, sans travail backend. C'est la recommandation : le texte canon est
  assez distinct salle par salle pour que 27 palettes ne se ressemblent pas, et le grain narratif
  du jeu le justifie.
- **Option 2 — par thème de salle (12 palettes).** Moitié moins d'assets, identité déjà bien
  supérieure à aujourd'hui. Mais `Memory` couvre le Palier, le Labyrinthe et la Cellule du
  château, qui n'ont rien à voir visuellement.
- **Option 3 — statu quo (7 palettes).** À écarter : c'est l'état actuel, celui qui pose problème.

Le tableau ci-dessous donne les deux colonnes pour que la décision puisse être prise sur pièces.

---

## 2. Les 27 salles

Structure du Monde « Palais ». Salle d'entrée : **Hall d'entrée**. Profondeur = `MinDepth` de la
salle (la profondeur maximale est 9 partout, donc non répétée).

### Niveau 0 — l'entrée

| Salle | Clé | Thème salle | Rareté | Prof. | Mène à |
|---|---|---|---|---|---|
| **Hall d'entrée** | `room.halldentree` | Welcome | Epic | 0 | Palier, Couloirs, Pièce des émotions, Passage brisé, Pièce camisolée, Jardin, **Hôpital** |

> *« Depuis toujours le Palais a su accueillir ses invités. Couvert d'un grand tapis rouges et
> habillé de quatres merveilleux pilier de marbre, le Hall d'entrée du Palais n'est que la
> représentation de l'arrogance de son propriétaire. Une fois traversé, rares sont les personnes
> qui ont eu l'occasion de le revoir. »*

Note : l'Hôpital est une exception assumée — c'est une salle de profondeur 4 rattachée
directement au Hall.

### Niveau 1 — les deux artères

| Salle | Clé | Thème salle | Rareté | Prof. | Mène à |
|---|---|---|---|---|---|
| **Palier** | `room.palier` | Memory | Rare | 1 | Couloirs, Salle de méditation |
| **Couloirs** | `room.couloirs` | Silence | Common | 1 | *toutes les salles de niveau 1 et 2* |

> **Palier** — *« Situé juste après le hall d'entrée, le palier n'est accessible qu'à ceux qui
> auront su gravir les 8 marches qui séparent les deux pièces. 8 marches qui semblent une éternité
> pour ceux qui empruntent cette voie, se retrouvant finalement face à un immense livre s'écrivant
> seul. »*

> **Couloirs** — *« Distordue, parfois suintant, parfois envahi d'entités monstrueuses, les
> couloirs sont les chemins à suivre pour espérer pouvoir pénétrer dans une pièce. Le tapis
> bordeaux qui habille le sol n'est pas sans rappeler que vous êtes proche du Hall, sans pouvoir
> l'atteindre. »*

Les Couloirs sont le carrefour du Palais (mode `AllExceptListed`) : depuis eux, tout ce qui n'est
pas dans une chaîne stricte est atteignable. C'est la salle que le joueur reverra le plus souvent
— sa lisibilité et sa capacité à ne pas lasser comptent plus que celle de n'importe quelle autre.

### Niveau 2 — les culs-de-sac et les portes

| Salle | Clé | Thème salle | Rareté | Prof. | Mène à |
|---|---|---|---|---|---|
| **Pièce des émotions** | `room.feelings` | Feelings | Uncommon | 2 | *cul-de-sac → retour Hall* |
| **Passage brisé, vers la tortue** | `room.turtle` | Collapse | Epic | 2 | *cul-de-sac → retour Hall* |
| **Pièce camisolée** | `room.enfermement` | Confinement | Rare | 2 | *cul-de-sac → retour Hall* |
| **Salle de méditation** | `room.meditation` | Meditation | Uncommon | 2 | *cul-de-sac → retour Hall* |
| **Chambre 08** | `room.room08` | Peace | Common | 2 | *cul-de-sac → retour Hall* |
| **Chambre d'Elise** | `room.chambredelise` | Feelings | Epic | 2 | *cul-de-sac → retour Hall* |
| **Le jardin** | `room.jardin` | Peace | Common | 2 | **Le soleil** (ouvre une chaîne) |
| **La falaise** | `room.falaise` | Fear | Common | 2 | **Les enfers** (ouvre une chaîne) |
| **La montagne** | `room.montagne` | Meditation | Common | 2 | **Le temple** (ouvre une chaîne) |
| **Labyrinthe** | `room.labyrinthe` | Memory | Rare | 3 | La falaise, La faille |
| **L'hopital** | `room.hopital` | Madness | Rare | 4 | **La cellule de l'hôpital** (ouvre une chaîne) |
| **La faille** | `room.faille` | Silence | Common | 6 | *terminus* |

> **Pièce des émotions** — *« Autrefois une simple chambre accueillant les invités, l'Architecte a,
> lors de la seconde reconstruction du Palais, adapté cette pièce pour qu'elle n'accueille qu'un
> seul type d'invité : les émotions, et ceux, dans le maigre espoir qu'elles puissent se sentir
> chez elle dans le Palais. Aujourd'hui, cette pièce n'est plus remplie que par quelques échos
> d'émotions, ou quelques objets laissés ici et là par les anciens locataires. »*

> **Passage brisé, vers la tortue** — *« Dans des temps anciens, bien avant la seconde
> reconstruction, il semblait exister un lien entre le Palais et une autre entité tout aussi grande
> et imposante. Désormais brisé, le lien qui permettait autrefois aux habitants de chaque côté de
> se rejoindre n'est plus qu'une faille dans l'immensité du Palais. Peu sont les invités ayant pu
> contempler cette faille violacée. »*

> **Pièce camisolée** — *« Alors qu'il devenait fou, l'architecte a bâti un système de sécurité
> archaïque, dans l'urgence d'une mort proche. Bâtie de murs renforcés, une simple porte d'acier
> que seule Elise peut ouvrir de l'extérieur, cette pièce existe pour isoler tous ceux qui oseront
> y pénétrer. »*

> **Salle de méditation** — *« Située au sommet du Palais, côtoyant les cieux, cette pièce apaise
> les êtres qui y entrent. »*

> **Chambre 08** — *« Parmi l'infinité de pièces que contient le Palais, la chambre 08 a une
> histoire toute particulière, et une habitante tout aussi unique : Hitomi. Longuement maintenue
> close pour laisser le temps à cette femme de se soigner après les brûlures qu'elle a subies,
> cette pièce est aujourd'hui ouverte et y croiser Hitomi relève de la chance. »*

> **Chambre d'Elise** — *« En dehors des couloirs, loin de l'entrée du Palais, la chambre d'Elise
> date d'avant la construction du Palais. Bâtie au début dans le cœur de l'architecte, cette
> chambre ne servait qu'à contenir avidement une créature aussi belle qu'essentielle au
> fonctionnement de ce dernier. Lors de la seconde reconstruction, la décision du conseil fut prise
> de la libérer et de la laisser vivre librement. Même si Elise n'est presque jamais dans sa
> chambre, bien des évènements peuvent y survenir, et des créatures y apparaître. »*

> **Le jardin** — *« Entourant le Palais, le jardin ressemble à tout ce dont toute personne
> pourrait rêver : des fleurs merveilleuses, une ambiance calme et sereine et des habitants qui
> s'y promènent, sifflotant et discutant tranquillement. »*

> **La falaise** — *« Seul passage vers les enfers, cette falaise surplombe la mer violacée qui
> sépare le Palais des enfers. Malheureux sont ceux qui croiseront l'impératrice dans ce lieu. »*

> **La montagne** — *« Paysage de calme, de retraite et d'apaisement, les montagnes du Palais sont
> un lieu de repentance pour tous ceux qui souhaitent effectuer un pèlerinage en toute
> quiétude. »*

> **Labyrinthe** — *« Maintenu enfermé, protégé par les sinueux couloirs, le labyrinthe abrite le
> premier livre, celui qui a permis au Palais de devenir infini et d'écrire l'histoire des
> habitants d'origine. Y rentrer n'est pas le plus difficile, mais en sortir sans le fil d'Ariane
> relève du défi. »*

> **L'hopital** — *« Blanc, vide d'émotions, une odeur de produit ménager et uniquement habité de
> souvenirs et de regrets, l'hopital du Palais a longtemps accueilli les âmes errantes et les
> avatars mourants. Aujourd'hui, il existe encore même si y pénétrer n'est que peu enviable. »*

> **La faille** — *« Centre de l'univers du Palais, la faille est le point névralgique de toutes
> les dimensions que le Palais a su accueillir. Lors de la seconde reconstruction, une implosion a
> eu lieu dans le cœur du Palais. Lorsque l'architecte et le conservateur allèrent vérifier, le
> cœur avait disparu et une faille violacée le remplaçait. C'est à cet instant que fut créé
> l'aventurier, n'ayant pour seule mission que de pénétrer dans cette faille et explorer les
> différents univers qui s'offrent à lui. »*

### Les quatre chaînes strictes

Une chaîne stricte est un couloir à sens unique : chaque salle n'a qu'une seule suite possible, et
la dernière renvoie au Hall d'entrée. Visuellement, c'est là que la **progression** doit se lire —
une chaîne doit se dégrader, se réchauffer ou se resserrer d'un maillon à l'autre, sinon le joueur
ne sent pas qu'il s'enfonce.

#### Chaîne A — Les Enfers (entrée : La falaise)

| # | Salle | Clé | Thème salle | Prof. |
|---|---|---|---|---|
| 1 | La falaise | `room.falaise` | Fear | 2 |
| 2 | **Les enfers - La calamité** | `room.enfer1` | Silence | 3 |
| 3 | **Les enfers - la plaine** | `room.enfer2` | Madness | 3 |
| 4 | **Les enfers - la forge** | `room.enfer3` | Terrify | 3 |
| 5 | **Les enfers - Le chateau** | `room.enfer4` | Collapse | 3 |

> **La calamité** — *« La calamité, le premier étage des Enfers. Composé d'une terre dévastée,
> hantée par les squelettes des souvenirs morts, ce lieu est aussi cruel par son hostilité que par
> le silence pesant qui y règne. »*

> **La plaine** — *« Calme, silencieuse, habitée d'animaux et autres chimères, les plaines sont le
> reflet des créations de l'architecte. Mais le calme apparent laisse rapidement place à des ordres
> qui ne demandent qu'une seule chose : se nourrir. »*

> **La forge** — *« Des marteaux qui hurlent, une forge qui recrache de la fumée et des créations
> inachevées qui errent sans but sur les plaques d'acier et les piliers de fer qui décorent cet
> étage. Le forgeron guette, crée et rejette ses propres créations. »*

> **Le chateau** — *« Dernier étage connu des enfers, le chateau fut longtemps la résidence de
> l'Homoncule. Son sol, souillé par les milliers de soldats morts pour sauver l'enfant prisonnier,
> hurle encore de désespoir et de souffrance. »*

#### Chaîne B — Le Soleil (entrée : Le jardin)

| # | Salle | Clé | Thème salle | Prof. |
|---|---|---|---|---|
| 1 | Le jardin | `room.jardin` | Peace | 2 |
| 2 | **Le soleil** | `room.soleil` | Feelings | 4 |
| 3 | **Le chateau** | `room.chateau` | Peace | 4 |
| 4 | **Le chateau - La cellule** | `room.cellule` | Memory | 5 |

> **Le soleil** — *« Un astre cosmique et, en son centre, un chateau. Le soleil n'est, comme tout
> ce qui habite le Palais, qu'une simple pièce aux dimensions immenses. »*

> **Le chateau** — *« Autrefois situé dans le quatrième étage des enfers, le chateau se trouve
> désormais au centre du soleil, alimentant le plasma et le rayonnement de cet astre qui réchauffe
> le Palais. »*

> **La cellule** — *« Une pièce, une seule et unique pièce de ce chateau porte toute l'histoire du
> Palais. À l'intérieur, des jeux d'enfants, des coloriages et des dessins sur le mur, un simple
> lit et le souvenir d'un petit être qui créa la première version du Palais, bien avant que
> l'Architecte ne vienne imposer ses plans. »*

Attention au piège : il y a **deux « chateau »** dans le canon — `room.enfer4` (Les enfers - Le
chateau) et `room.chateau` (au centre du soleil). Le texte dit qu'il s'agit du *même bâtiment
déplacé*. Un rappel visuel entre les deux serait juste ; une confusion entre les deux ne le serait
pas.

#### Chaîne C — L'Hôpital (entrée : L'hopital, rattachée directement au Hall)

| # | Salle | Clé | Thème salle | Prof. |
|---|---|---|---|---|
| 1 | L'hopital | `room.hopital` | Madness | 4 |
| 2 | **L'hopital - la cellule** | `room.cellulehopital` | Madness | 5 |
| 3 | La faille | `room.faille` | Silence | 6 |

> **La cellule de l'hôpital** — *« Au même titre que la chambre du chateau a longtemps accueilli
> l'enfant, la cellule de l'hopital fut construite sur mesure pour l'Architecte, juste avant la
> seconde reconstruction du Palais. Plongé dans une folie sans nom, submergé par les émotions et
> les échos, il fut interné après avoir voulu détruire le livre situé dans le Labyrinthe. »*

C'est la seule chaîne qui ne retombe pas sur le Hall : elle se referme sur **La faille**.

#### Chaîne D — La Montagne (entrée : La montagne)

| # | Salle | Clé | Thème salle | Prof. |
|---|---|---|---|---|
| 1 | La montagne | `room.montagne` | Meditation | 2 |
| 2 | **La montagne - Le temple** | `room.templempontagne` | Silence | 2 |
| 3 | **La montagne - la chambre funéraire** | `room.chambrefunéraire` | Underground | 2 |
| 4 | **La montagne - Les sous-terrains** | `room.sousterrainmontagne` | Underground | 2 |
| 5 | **La montagne - La caverne de crystal** | `room.cavernedecrystal` | Collapse | 2 |

> **Le temple** — *« Contemplant les montagnes et les plaines, le temple des montagnes impressionne
> par sa structure Maya, sa taille déraisonnable et, surtout, ses pièces aux piliers ornés de
> joyaux et de gravures anciennes. »*

> **La chambre funéraire** — *« Au centre du temple, une vision d'horreur se réveille. La chambre
> funéraire du premier explorateur fut découverte lors de la première reconstruction du Palais, par
> un aventurier accompagné de Hitomi et, depuis, les échos de la frayeur ne cessent de s'agiter au
> sein de cette pièce. »*

> **Les sous-terrains** — *« Derrière la chambre funéraire, cachée par une porte qui ne s'ouvre que
> si l'on est digne des profondeurs, se trouve un long tunnel qui mène à une antichambre, qui mène
> à un lieu unique et magnifique : la chambre de crystal. »*

> **La caverne de crystal** — *« Pièce antique, datée de la construction du Palais, la caverne de
> crystal abrite bien plus que de simples joyaux resplendissants. Une magie ancestrale, des
> gardiens intemporels et, au milieu de tout cela, une sorte de vieille maison continuellement en
> feu. »*

Chaîne la plus longue et la plus « descendante » : extérieur apaisé → temple monumental → tombeau →
tunnel → merveille souterraine. C'est le meilleur terrain de démonstration pour la progression
visuelle d'une chaîne.

---

## 3. Ce qu'un « design de salle » doit fournir

Le moteur de tuiles (`tilecraft`, déjà en place) consomme un thème et en dérive tout. Une palette
de salle doit donc renseigner, au minimum, ces postes — ce sont ceux que le moteur sait déjà lire :

| Poste | Rôle | État actuel |
|---|---|---|
| **Sol** (4 niveaux d'élévation) | La tuile de base, déclinée par palier de hauteur | 7 variantes génériques |
| **Falaise / bord** | Le flanc d'une tuile en bord de salle ou au bord d'un trou | 7 variantes |
| **Obstacle** (3 silhouettes) | Les blocs infranchissables | 3 par thème, jamais par salle |
| **Fond de scène** | Ciel, source lumineuse, silhouettes de décor sur deux plans, brume, vignette | 7 variantes |
| **Particules d'ambiance** | 12-20 particules lentes | 7 variantes |
| **Voile de brouillard** | Couleur du voile sur l'inexploré | 7 variantes |

Les 7 variantes ci-dessus sont exactement le problème décrit en §1.

---

## 4. Les autres éléments graphiques à définir

Au-delà des salles, voici tout ce qui est soit incomplet, soit générique, soit absent.

### 4.1 Les décors d'événement — le manque le plus visible

Un nœud pose un « décor » sur sa case. Il n'en existe que **trois** aujourd'hui, pour **douze**
types de nœud :

| Décor existant | Types de nœud qui l'utilisent |
|---|---|
| `npc` (silhouette encapuchonnée) | Présence, Marchand, **et toute embuscade** |
| `campfire` (feu de camp animé) | Repos |
| `star` (éclat suspendu animé) | Objet, Souvenir, Loi |

Ce qui manque :

- **Marchand** partage la silhouette du PNJ — un étal, un ballot, quelque chose de commerçant.
- **Malédiction** n'a aucun décor.
- **Boss de salle / Confrontation finale** n'ont aucun décor : leur case a un halo, c'est tout.
  C'est l'objectif de la salle, il devrait se voir de loin.
- **Élite** et **Rare** n'ont rien non plus.
- L'embuscade emprunte la silhouette du PNJ faute de mieux — un décor propre lèverait
  l'ambiguïté (ou pas, si l'ambiguïté est voulue : c'est un choix de design à trancher).

Contrainte technique : un décor se peint sur une toile **128 × 320** avec une ancre au sol à
**0,7562** de la hauteur (une silhouette monte donc bien au-dessus de sa case). Le sol, lui, est en
**128 × 170**, ancre **0,5412**.

### 4.2 Le jeton du groupe

C'est le joueur, et c'est un disque lumineux générique. Il ne change jamais, quel que soit le
personnage, la salle ou l'état de la partie. Candidat évident à une vraie identité.

### 4.3 Les surbrillances de case

Quatre variantes existent dans le moteur : `move` (portée de déplacement), `cursor` (case sous la
souris), `path` (**tout juste câblé** : le trajet exact que le groupe empruntera), `attack`
(réservée au futur combat tactique, jamais utilisée). Elles sont aujourd'hui de simples losanges
teintés. La lisibilité de `move` **contre** `path` est un vrai enjeu : ce sont deux informations
différentes qui se superposent.

### 4.4 Les indices de danger

Trois marques peintes au sol signalent qu'un nœud se déclenche au contact : `tracks` (traces),
`glow` (lueur), `blight` (flétrissure). Une quatrième valeur existe — `none` — et c'est
**délibérément l'embuscade** : rien à peindre, indiscernable d'un sol normal. Ne pas « corriger »
ce vide.

### 4.5 Les caches

Deux états : `hint` (une dalle qui sonne creux, avant fouille) et `revealed` (l'alcôve ouverte).
Une animation de poussière (`drawRevealFx`) couvre les 500 ms de transition. Tout cela existe déjà
et fonctionne ; c'est la déclinaison par salle qui manque.

### 4.6 Le brouillard de guerre

Un voile sur tout le plateau, percé autour de chaque case déjà vue. Sa couleur suit le thème. Son
opacité vient d'être réduite pour laisser transparaître le fond. Une déclinaison par salle serait
naturelle — la brume d'un jardin et celle d'une forge ne sont pas la même matière.

---

## 5. Ce qu'il ne faut PAS refaire

Ces éléments sont livrés, validés en jeu et ne demandent rien :

- Le moteur `tilecraft` lui-même (projection isométrique, tri par peintre, mise en cache des
  sprites, gabarits de toile). Il est vendoré tel quel et ne doit pas être modifié.
- La géométrie : tuile 128 × 64 en projection 2:1, palier d'élévation de 20 px, 4 niveaux.
- Les effets animés déjà en place : flamme du feu de camp, scintillement de l'éclat, respiration du
  PNJ, poussière de fouille, particules d'ambiance.
- La grille de jeu elle-même : 10 × 8, formes non rectangulaires, obstacles, élévation 0-3.

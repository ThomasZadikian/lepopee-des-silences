# Prompt — Correction du lot Bestiaire avant intégration

Le lot `design_handoff_combat` est excellent sur la forme : la grammaire de silhouette tient,
les registres sont respectés, le moteur de combat est conforme à la SFD v2.0. **Un seul
problème le rend inintégrable en l'état**, et il est structurel.

## Le problème

Le roster peint et la base de données du jeu sont **deux listes disjointes**.

Le moteur ne fait apparaître en combat que les ennemis présents au catalogue
(`CatalogSeedRunner.cs`, 41 ennemis). Or :

| | |
|---|---|
| Figures ennemies peintes | 38 |
| **Correspondances avec un ennemi réel** | **15** |
| Ennemis réels **sans jeton** | **26** |
| Jetons peints **sans ennemi correspondant** | **29** |

Conséquence directe : **29 jetons ne s'afficheront jamais** — rien dans le jeu ne les invoque.
Et **26 ennemis que le joueur affronte aujourd'hui** n'ont aucune figure.

Ce n'est pas un problème de qualité graphique. C'est un problème d'identifiants : `ROSTER` ne
référence aucune clé du catalogue (zéro occurrence de `canon.enemy.`), et le roster a été
composé pour donner à chaque famille un jeu de rôles complet plutôt que pour couvrir les
créatures qui existent.

## Ce qu'il faut corriger

### 1. Les identifiants du roster doivent être les clés du catalogue

**Règle absolue** : chaque clé de `ROSTER` correspondant à un ennemi est **la clé catalogue
sans le préfixe `canon.enemy.`**.

C'est déjà le cas pour 15 figures — `veilleur-tapis`, `copiste-aveugle`, `porteur-plateau`,
`echo-politesse`, `sentinelle-seuil`, `encrier-vivant`, `page-inachevee`, `squelette-souvenir`,
`porteur-cendre`, `choeur-muet`, `infirmiere-deni`, `souvenir-alite`, `regisseur-blanc`,
`fossoyeur-pale`, `echo-colere`. **Ne pas y toucher, elles sont justes.**

Ajouter en outre un champ explicite sur chaque entrée ennemie, pour que le lien soit lisible
sans déduction :

```js
'veilleur-tapis': {
  catalogKey: 'canon.enemy.veilleur-tapis',   // ← NOUVEAU, obligatoire pour tout ennemi
  name: 'Veilleur du Tapis', side: 'enemy', …
}
```

Les alliés et le boss placeholder n'ont pas de `catalogKey` (ils ne viennent pas de cette
table) — mettre `catalogKey: null` explicitement.

### 2. Peindre les 26 ennemis manquants

Ils sont listés plus bas avec leur clé, leur rôle, leur rang, leur registre et leur
**description canon** — celle-ci fait foi et ne doit pas être réinterprétée.

Priorité haute (ils apparaissent dès les premiers étages) : `lamiz`, `ombres-tentaculaires`,
`fossoyeur-pale`, `promeneur-fige`, `pelerin-sans-visage`, `chimere-affamee`,
`creation-instable`, `scorie-rampante`, `enfant-argile`.

### 3. Décider du sort des 29 jetons orphelins

Ils sont peints et souvent réussis, mais ne correspondent à rien. Trois options, à trancher
figure par figure — **ne rien supprimer sans arbitrage** :

- **(a) Les conserver hors roster de combat**, dans un export séparé
  (`ROSTER_PROPOSITIONS`), comme propositions d'extension du bestiaire. Elles seront
  éventuellement ajoutées au catalogue plus tard, mais ne doivent pas polluer `ROSTER`, qui
  doit rester le miroir exact des ennemis jouables.
- **(b) Les remapper** sur un ennemi réel non couvert, quand la silhouette peut servir sans
  trahir la description canon. Exemple plausible : `noyau-de-brume` (drain, brume) pourrait
  couvrir `canon.enemy.goule-anxiete` (Drain, Psyché) — **à valider, la description canon
  prime**, et une Goule n'est pas une brume.
- **(c) Les retirer**, si la figure ne sert ni l'un ni l'autre.

Recommandation : **(a) par défaut**. Le travail est bon, il ne faut pas le jeter — mais
`ROSTER` doit cesser d'être un mélange de réel et d'hypothétique.

### 4. Ne pas régresser `tilecraft.js`

Le README dit « Copier tel quel. C'est la version à jour ». **C'est faux pour ce dépôt.**

La version en place sur la branche `T-RPG` porte un correctif documenté, ligne 1191 :

> `⚠ DIVERGENCE ASSUMÉE vis-à-vis du handoff — à reporter à chaque rafraîchissement.`

Le socle d'un obstacle suit **l'élévation réelle de sa case** au lieu d'être toujours cuit à
`TILE.MAX`. Sans ce correctif, tout obstacle posé au niveau 0 reçoit un piédestal de trois
paliers, et **tous les obstacles lisent comme des tours** — les silhouettes basses (éboulis,
rocher, tronc couché) ne peuvent plus jouer leur rôle de barrage au sol. Les signatures de
`bakeObstacle` et `obstacleSilhouette` ont également reçu un paramètre d'élévation.

Pour la prochaine livraison : **repartir de la version du dépôt**, pas de la vôtre, et y
ajouter la couche combat. Sinon chaque lot réintroduit le bug.

### 5. Corriger le décompte du README

Il annonce « 57 jetons, 7 alliés ». Le fichier en contient **45 entrées `ROSTER`, dont
6 alliés**. Aligner le texte sur le code.

## Ce qui ne change pas

Tout le reste est validé et ne doit pas bouger : la grammaire de silhouette par rôle, les sept
registres émotionnels et leurs couleurs, les quatre règles de peinture, les quatre formes de
zone, le moteur de combat et ses règles, les 12 sorts du vocabulaire, `boss-ombre` comme
placeholder. Les six boss réels feront l'objet d'un lot séparé, une fois leurs fiches et leurs
scripts fournis.

---

# Annexe A — Les 26 ennemis à peindre

Format : `clé catalogue` — Nom · *rôle, rang* · registre, puis la description canon.

#### Predateurs

- **`canon.enemy.voraces`** — Voraces · *Bruiser, Elite* · registre —
  > Hautes d'un mètre quarante à trois mètres, elles dévorent les énergies. Intelligentes, elles chassent en meute — ou seules, quand l'énergie est assez alléchante.

- **`canon.enemy.lamiz`** — Lamiz · *Swarm, Common* · registre —
  > Une meute attirée par l'énergie « alléchante ». Là où l'une apparaît, les autres suivent.

- **`canon.enemy.uguiro`** — Uguiro · *Bruiser, Elite* · registre —
  > Un monstre des profondeurs du Palais. Lent à se révéler, terrible une fois éveillé.

#### Brume

- **`canon.enemy.ombres-tentaculaires`** — Ombres tentaculaires · *Disruptor, Common* · registre —
  > Dans la brume, elles s'étirent jusqu'aux toits. On murmure des rats grands comme des chiens, des serpents à pattes — mais ce ne sont que ses bras.

#### Lituisme

- **`canon.enemy.oeil-du-visionnaire`** — L'Œil du Visionnaire animé · *Disruptor, Elite* · registre —
  > Le symbole rampe sur les pavés au gré des flammes. Pupille en amande, violacée et jaune : il vous voit avant que vous ne le voyiez.

#### Psyche

- **`canon.enemy.goule-anxiete`** — La Goule · *Drain, Elite* · registre —
  > L'Anxiété personnifiée. Elle envahit, recouvre, étouffe — jusqu'au « Tais-toi » d'Elise qui, parfois, la fait reculer.

#### Alchimie

- **`canon.enemy.homoncule`** — L'Homoncule · *Bruiser, Elite* · registre —
  > Né d'une flamme froide bleu-violet, nacré et soufré. Lent, presque doux — jusqu'à ce qu'il hurle. Le feu, le vrai, est sa seule terreur.

- **`canon.enemy.enfant-argile`** — L'Enfant d'argile · *Support, Common* · registre —
  > Un essai raté de l'Homoncule, abandonné avant l'achèvement. Il soigne encore, par réflexe.

#### Copistes

- **`canon.enemy.relieur`** — Le Relieur · *Bruiser, Rare* · registre Memoire
  > Un artisan massif au tablier de cuir, dont les bras se terminent en aiguilles courbes enfilées de nerf. Il ne relie pas des livres : il relie des instants entre eux, cousant la douleur d'hier à celle de demain pour qu'aucune ne puisse finir. « Rien ne se termine tant que je n'ai pas cousu la dernière page. »

#### Chimeres des Plaines

- **`canon.enemy.chimere-affamee`** — Chimère Affamée · *Skirmisher, Common* · registre Effroi
  > Un prédateur composite — corps de cervidé, mâchoire de brochet, pattes trop nombreuses et repliées sous le ventre. Immobile dans les hautes herbes, elle est indiscernable des animaux paisibles de la plaine. Jusqu'à ce que quelque chose saigne. « Elle ne rugit pas. Elle compte vos battements de cœur. »

- **`canon.enemy.berger-ordres`** — Berger d'Ordres · *Support, Uncommon* · registre Effroi
  > Une haute figure pastorale au visage effacé, appuyée sur une houlette faite d'une règle d'architecte démesurément allongée. Il ne parle pas aux chimères : il leur montre, et elles comprennent. Ses gestes ont la précision d'un plan. « Le troupeau ne demande qu'une chose. Je la lui accorde. »

- **`canon.enemy.agneau-inverse`** — Agneau Inversé · *Disruptor, Uncommon* · registre Effroi
  > De loin : un agneau paisible, blanc, broutant. De près : la laine pousse vers l'intérieur, et ce qui remplit le corps n'est pas de la chair. C'est du silence comprimé, prêt à se détendre d'un coup. « Il broutait. Vous avez cligné des yeux. Il vous regarde. »

#### Creations du Forgeron

- **`canon.enemy.creation-instable`** — Création Instable · *Bruiser, Common* · registre Rupture
  > Un assemblage humanoïde de plaques mal jointes, dont une jambe est plus courte que l'autre et dont le torse s'ouvre par intermittence sur un foyer qui n'aurait jamais dû rester allumé. Elle se redresse sans cesse, compulsivement, comme pour prouver quelque chose à un marteau absent. « Elle se tient debout. Presque. C'est le presque qui fait mal. »

- **`canon.enemy.marteau-vivant`** — Marteau Vivant · *Bruiser, Uncommon* · registre Rupture
  > Un marteau de forge de deux mètres, animé, dont le manche s'est tordu en colonne vertébrale. Il frappe le sol en rythme, continuellement — le rythme exact du Forgeron au travail. Quand il frappe autre chose que le sol, ça hurle. C'est lui, le hurlement. « Les marteaux qui hurlent. C'est de lui qu'on parle. »

- **`canon.enemy.sentinelle-fonte`** — Sentinelle de Fonte · *Support, Uncommon* · registre Rupture
  > Une statue de fonte grossière, assise en tailleur au milieu des piliers de fer, qui murmure la litanie alchimique du Forgeron. Elle ne se lève jamais. Ses mains, posées sur ses genoux, rougissent quand elle transmute — et le métal de ses alliés rougit avec. « Plomb, or, mercure, soufre, sel. Elle récite. C'est tout ce qu'on lui a laissé. »

- **`canon.enemy.scorie-rampante`** — Scorie Rampante · *Skirmisher, Common* · registre Rupture
  > Une flaque de laitier incandescent, à demi solidifiée, qui se traîne en laissant des traces vitrifiées. Par moments, une forme s'ébauche dans sa masse — une main, un profil — puis retombe. Elle n'a jamais eu de forme finale. Elle les essaie toutes. « Ce que la forge recrache. Ça rampe. Ça brûle. Ça se souvient d'avoir été un projet. »

#### Penitents de la Montagne

- **`canon.enemy.pelerin-sans-visage`** — Pèlerin Sans Visage · *Skirmisher, Common* · registre Effroi
  > Une silhouette en robe de bure, courbée par la pente, dont la capuche s'ouvre sur une surface lisse — pas effacée : usée, comme une pièce de monnaie trop manipulée. Il gravit la montagne en égrenant un chapelet dont chaque grain est une petite dent. « Il monte depuis si longtemps qu'il a usé son visage contre le vent. »

- **`canon.enemy.prieur-lituique`** — Prieur Lituique · *Support, Uncommon* · registre Effroi
  > Un officiant au dos trop droit pour la bure qu'il porte, dont la bouche est cousue de fil d'or — et qui prie quand même, par les pores, par les gestes, par les jointures de ses doigts qui craquent en rythme liturgique. Devant lui flotte un encensoir qui fume à l'envers : la fumée descend. « Elle restaure — mais nourrit ce qui rôde. Lui, il sait exactement ce qui rôde. »

- **`canon.enemy.frayeur-exhumee`** — Frayeur Exhumée · *Bruiser, Rare* · registre Effroi
  > Le premier explorateur — ou ce que l'ouverture de sa chambre funéraire a réveillé de lui. Un corps momifié dans une posture de recul, bras levés devant un danger que personne d'autre ne voit, figé au centième de seconde de sa dernière terreur. Il projette cette terreur autour de lui comme une lampe projette la lumière. « Depuis la découverte de la chambre, les échos de la frayeur ne cessent de s'agiter. En voici la source. »

#### Faux Habitants du Jardin

- **`canon.enemy.promeneur-fige`** — Promeneur Figé · *Skirmisher, Common* · registre Deni
  > Un promeneur en habits du dimanche, sourire cordial, chapeau levé en salut perpétuel. Son bras ne redescend jamais complètement. Quand on le croise une deuxième fois, il salue exactement pareil — même angle, même sourire, même phrase, même virgule. « Belle journée, n'est-ce pas ? N'est-ce pas ? N'est-ce pas ? »

- **`canon.enemy.jardinier-sans-ombre`** — Jardinier Sans Ombre · *Disruptor, Uncommon* · registre Deni
  > Un jardinier voûté sur ses massifs, sécateur en main, qui taille sans interruption des fleurs déjà parfaites. Le soleil du Palais l'éclaire de face, de dos, de partout — et il ne projette aucune ombre. C'est lui qui l'a coupée : elle faisait désordre. « Les fleurs sont merveilleuses parce que je coupe tout ce qui ne l'est pas. »

#### Gardiens de Crystal

- **`canon.enemy.gardien-intemporel`** — Gardien Intemporel · *Bruiser, Rare* · registre Memoire
  > Un colosse de crystal translucide dans lequel on distingue, en suspension, des objets d'époques impossibles : un marteau qui n'est pas celui du Forgeron, une craie qui n'est pas celle de l'Enfant, une plume qui n'est pas celle de l'Écrivain. Des prototypes. Ou des originaux. « Il gardait déjà. Il gardera encore. Le mot “toujours” a été inventé pour éviter de le décrire. »

- **`canon.enemy.eclat-eveille`** — Éclat Éveillé · *Skirmisher, Uncommon* · registre Memoire
  > Un cristal flottant de la taille d'un cœur, qui pulse d'une lumière interne au rythme d'un battement. Il n'a ni yeux ni bouche, mais tous ceux qui l'approchent jurent s'être sentis dévisagés — puis mémorisés. « Un joyau qui a fini par comprendre qu'on le regardait. »

#### Echos d'Emotions

- **`canon.enemy.echo-peur`** — Écho de Peur · *Disruptor, Uncommon* · registre —
  > Un frémissement pâle qui n'est jamais tout à fait là où on le regarde. Il se déplace par saccades, longe les murs, et son contact donne l'exacte sensation d'une porte qu'on trouve fermée dans le noir. « Il guette une sortie qui n'existe plus. Vous êtes entre lui et elle. »

- **`canon.enemy.echo-tristesse`** — Écho de Tristesse · *Support, Uncommon* · registre —
  > Une lenteur visible — l'air lui-même semble plus épais autour de lui. Il a vaguement la forme d'une personne assise, même quand il se déplace. Ceux qui le traversent se souviennent soudain de tout ce qu'ils n'ont pas dit à temps. « Il ne pleure pas. Il constate, longtemps après tout le monde. »

#### Imperatrice de la Falaise

- **`canon.enemy.imperatrice`** — L'Impératrice · *Bruiser, Legendary* · registre —
  > Une silhouette féminine démesurée émergeant à mi-corps de la mer violacée, couronnée d'une structure qui évoque à la fois un diadème et une cage thoracique renversée. Sa robe est la mer — littéralement : les vagues sont son ourlet, et la marée suit ses humeurs. « Malheureux sont ceux qui croiseront l'impératrice dans ce lieu. »


---

# Annexe B — Les 29 jetons orphelins

À sortir de `ROSTER` (option (a) par défaut) ou à remapper (option (b)).

- **alchimie** — `alambic-marcheur` Alambic Marcheur (support), `homoncule-verre` Homoncule de Verre (skirmisher), `creuset-vivant` Creuset Vivant (bruiser)
- **brume** — `voile-marcheur` Voile Marcheur (disruptor), `main-de-brume` Mains de Brume (swarm), `noyau-de-brume` Noyau de Brume (drain)
- **chimeres** — `chimere-cornue` Chimère Cornue (guard), `levraut-double` Levraut Double (swarm), `grand-cerf-faux` Le Grand Cerf Faux (bruiser)
- **crystal** — `gardien-facette` Gardien à Facettes (guard), `eclat-errant` Éclat Errant (swarm), `prisme-sentinelle` Prisme Sentinelle (bruiser)
- **echos** — `echo-chagrin` Écho de Chagrin (disruptor), `echo-joie-fausse` Écho de Joie Fausse (skirmisher)
- **forgeron** — `automate-soufflet` Automate à Soufflet (guard), `enclume-marchante` Enclume Marchante (bruiser), `clou-vivant` Clou Vivant (skirmisher)
- **jardin** — `buisson-taille` Buisson Taillé (guard), `jardinier-cire` Jardinier de Cire (support), `epouvantail-poli` Épouvantail Poli (disruptor)
- **lituisme** — `officiant-lituique` Officiant Lituique (support), `porte-encens` Porte-Encens (disruptor), `lecteur-de-nom` Lecteur de Nom (bruiser)
- **penitents** — `penitent-agenouille` Pénitent Agenouillé (guard), `porte-chaine` Porte-Chaîne (bruiser), `cierge-marcheur` Cierge Marcheur (support)
- **psyche** — `miroir-porteur` Miroir Porteur (guard), `reflet-inverse` Reflet Inverse (skirmisher), `pensee-parasite` Pensées Parasites (swarm)

`boss-ombre` n'est pas concerné : c'est un placeholder assumé, à conserver tel quel.

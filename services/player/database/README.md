# Player database — baseline V1.0.0

`player-db-v1.0.0.sql` est le script canonique de création d'une base Player neuve pour la version 1.0.0.

Il est généré depuis l'unique migration EF Core `InitialV1_0_0`. L'historique de migrations antérieur à la V1 a volontairement été fusionné : aucune base de production pré-V1 n'est supportée ni attendue.

## Règles de déploiement

- V1.0.0 démarre sur une base vide et applique `InitialV1_0_0` ou le script SQL généré équivalent.
- Les bases locales et de test créées avant cette baseline doivent être supprimées puis recréées ; elles ne doivent pas être mises à niveau avec l'ancien historique.
- `PlayerDbContextModelSnapshot` et `player-db-v1.0.0.sql` doivent rester cohérents avec `InitialV1_0_0`.
- `dotnet ef migrations has-pending-model-changes` doit rester sans différence avant livraison.
- Après la livraison V1.0.0, toute modification de schéma devra utiliser une nouvelle migration incrémentale normale. La baseline V1 ne devra plus être réécrite.

Le script SQL est un artefact dérivé du modèle EF : la migration EF reste la source de vérité du schéma applicatif.

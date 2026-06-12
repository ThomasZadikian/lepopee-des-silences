# Game Engine Observability

## Objectif

Observabilité minimale du Game Engine Service pour rendre les erreurs plus diagnosticables.

## X-Correlation-Id

Le Game Engine supporte le header `X-Correlation-Id` sur toutes les requêtes.

### Comportement

- Si le header `X-Correlation-Id` est présent dans la requête, la valeur est réutilisée.
- Si le header est absent, un GUID est généré automatiquement.
- Le header `X-Correlation-Id` est retourné dans la réponse.
- Les logs sont enrichis avec `CorrelationId` via un scope de logging.

### Exemple

```http
GET /api/v2/runs/{runId}
X-Correlation-Id: my-trace-123
```

Réponse :

```http
HTTP/1.1 200 OK
X-Correlation-Id: my-trace-123
```

### Logs

Les logs incluent le `CorrelationId` dans le scope :

```
[12:00:00 INF] Processing request CorrelationId=my-trace-123
[12:00:01 ERR] Unhandled exception occurred. CorrelationId=my-trace-123
```

### Middleware pipeline

Ordre actuel dans `Program.cs` :

1. `ExceptionHandlingMiddleware` — capture les erreurs
2. `CorrelationIdMiddleware` — génère/enrichit le correlation ID
3. `UseSwagger()` / `UseSwaggerUI()`
4. `UseHttpsRedirection()`
5. `UseCors()`
6. `MapControllers()`

Le correlation ID est disponible même en cas d'erreur car le middleware est branché après le exception handling.

## Limites actuelles

- Pas d'OpenTelemetry
- Pas de distributed tracing
- Pas de métriques Prometheus
- Pas de monitoring externe
- Pas de dashboard

## Non-objectifs

- Pas d'ajout d'Identity/Gateway
- Pas de service-to-service authentication
- Pas de RabbitMQ
- Pas de monitoring externe

## Prochaines étapes

- Ajouter OpenTelemetry pour le distributed tracing
- Ajouter des métriques Prometheus
- Ajouter un dashboard Grafana
- Ajouter service-to-service authentication

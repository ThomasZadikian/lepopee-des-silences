# RPG_ESI07 - v0.8.2.2

[![Backend CI](https://github.com/ThomasZadikian/rpg_esi07/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/ThomasZadikian/rpg_esi07/actions/workflows/backend-ci.yml)
[![Frontend CI](https://github.com/ThomasZadikian/rpg_esi07/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/ThomasZadikian/rpg_esi07/actions/workflows/frontend-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Vue.js](https://img.shields.io/badge/Vue.js-3.x-4FC08D)](https://vuejs.org/)
[![Unity](https://img.shields.io/badge/Unity-6.x-000000)](https://unity.com/)
[![Coverage](https://img.shields.io/badge/coverage-87%25-brightgreen)]()

**Application web** : https://rpgesi07.up.railway.app/dashboard

Projet RNCP 36286 — Expert en Informatique et Systèmes d'Information  
Spécialisation : **Cybersécurité**

---

## Description

Architecture distribuée sécurisée combinant :

- **Client Unity 6** (Windows) — Jeu RPG 2D tour par tour avec système ATB
- **API REST** ASP.NET Core 10 — Backend Clean Architecture, sécurisé
- **Portail Vue.js 3** — Interface web de gestion du compte joueur
- **PostgreSQL 16** — Base de données relationnelle (Docker)

---

## Stack Technique

### Backend
- .NET 10 / ASP.NET Core
- Clean Architecture (Domain, Application, Infrastructure, API)
- CQRS + MediatR
- Entity Framework Core 10 + PostgreSQL 16
- JWT Authentication + Argon2id + TOTP MFA
- FluentValidation, AutoMapper, Serilog
- Rate Limiting, CORS, RGPD (Articles 15, 17, 20)
- Tests unitaires XUnit/Moq/FluentAssertions (87% coverage)
- SonarCloud + CodeQL

### Frontend
- Vue.js 3 + TypeScript
- Vuetify 3
- Vite
- Pinia
- Axios

### Client Jeu (Unity)
- Unity 6 (6000.3.x)
- Universal Render Pipeline 2D
- Système ATB (Active Time Battle)
- File d'initiative sur 6 tours
- Boss scripté avec phases
- Synchronisation temps réel avec l'API

### Infrastructure
- Docker + docker-compose
- Railway (production)
- GitHub Actions (CI/CD)
- pgAdmin

---

## Démarrage rapide

### Prérequis
- Docker Desktop
- Unity 6 (via Unity Hub)
- Git

### 1. Cloner le repo
```bash
git clone https://github.com/ThomasZadikian/rpg_esi07.git
cd rpg_esi07
```

### 2. Configurer les variables d'environnement
```bash
cp .env.example .env
# Remplir les valeurs dans .env
```

### 3. Lancer l'infrastructure complète
```bash
docker compose up --build
```

- **API** : http://localhost:5009
- **Frontend** : http://localhost:5173
- **pgAdmin** : http://localhost:5050

### 4. Client Unity
- Ouvrir Unity Hub
- Ajouter le projet `./unity-client`
- Lancer depuis la scène Bootstrap

---

## Variables d'environnement

Voir `.env.example` pour la liste complète des variables requises.

Les secrets ne sont jamais commités — ils sont injectés via `.env` en local et via les variables Railway en production.

---

## Tests

```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm run test
```

---

## Architecture

```
rpg_esi07/
├── backend/                  ← API ASP.NET Core
│   ├── RPG_ESI07.API/
│   ├── RPG_ESI07.Application/
│   ├── RPG_ESI07.Domain/
│   ├── RPG_ESI07.Infrastructure/
│   └── RPG_ESI07.Tests/
├── frontend/                 ← Vue.js 3
├── unity-client/             ← Client Unity 6
├── docker/                   ← Configuration Docker
├── .github/workflows/        ← CI/CD GitHub Actions
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## Sécurité

- JWT + Argon2id + TOTP MFA
- RBAC (Player/Admin)
- Protection IDOR
- Rate Limiting
- RGPD Articles 15, 17, 20
- SonarCloud + CodeQL sur chaque PR
- Secrets gérés via variables d'environnement

---

## Statut

| Composant | Statut |
|-----------|--------|
| Backend API | ✅ Production |
| Frontend Vue | ✅ Production |
| Client Unity | ✅ V1 fonctionnelle |
| CI/CD | ✅ Opérationnel |
| Déploiement Railway | ✅ En ligne |

---

## Auteur

**Thomas Zadikian**  
Étudiant ESI 07 — Projet RNCP 36286 — 2026

---

## Licence

Ce projet est sous licence [MIT](LICENSE).
```
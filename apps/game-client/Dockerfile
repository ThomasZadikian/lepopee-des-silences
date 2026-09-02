# syntax=docker/dockerfile:1.7
FROM node:24.19.0-alpine3.23 AS build
WORKDIR /app
COPY apps/game-client/package.json apps/game-client/package-lock.json ./
RUN npm ci --ignore-scripts
COPY apps/game-client/ ./
ARG VITE_GAME_ENGINE_API_URL=/game-engine
ARG VITE_PLAYER_API_URL=/player
ENV VITE_GAME_ENGINE_API_URL=$VITE_GAME_ENGINE_API_URL
ENV VITE_PLAYER_API_URL=$VITE_PLAYER_API_URL
RUN npm run build

FROM caddy:2.10.2-alpine AS runtime
COPY apps/game-client/Caddyfile /etc/caddy/Caddyfile
COPY --from=build --chown=caddy:caddy /app/dist /srv
RUN chown -R caddy:caddy /config /data
USER caddy
EXPOSE 8080

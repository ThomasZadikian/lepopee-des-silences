FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm install
COPY . .
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:alpine AS runtime
# Copie des fichiers statiques générés (Vue.js/Vite)
COPY --from=build /app/dist /usr/share/nginx/html

# Utilisation du répertoire templates pour l'injection de variables
COPY nginx.conf /etc/nginx/templates/default.conf.template

EXPOSE 80

# La commande par défaut de l'image nginx:alpine gère déjà envsubst 
# si le fichier est dans /etc/nginx/templates/
CMD ["nginx", "-g", "daemon off;"]
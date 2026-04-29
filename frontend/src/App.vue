<template>
  <!-- RouterView affiche le composant correspondant à l'URL courante -->
  <v-app>
    <RouterView />
  </v-app>
</template>

<script setup lang="ts">
import { RouterView } from "vue-router";
</script>

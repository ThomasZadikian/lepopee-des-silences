import "@mdi/font/css/materialdesignicons.css";
import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import "vuetify/dist/vuetify.min.css";
import { aliases, mdi } from "vuetify/iconsets/mdi";

export default createVuetify({
  components,
  directives,
  icons: { defaultSet: "mdi", aliases, sets: { mdi } },
  theme: {
    defaultTheme: "rpgLight",
    themes: {
      rpgLight: {
        dark: false,
        colors: {
          background: "#F5F0E8",
          surface: "#EDE8DE",
          "surface-variant": "#E5E0D6",
          "on-surface-variant": "#4A4A4A",
          primary: "#1A1A1A",
          secondary: "#2D4A8A",
          accent: "#8B4513",
          error: "#C0392B",
          warning: "#D68910",
          info: "#2D4A8A",
          success: "#1E8449",
        },
      },
    },
  },
  defaults: {
    VCard: { rounded: "0", elevation: 0 },
    VBtn: { rounded: "0", elevation: 0 },
    VTextField: { variant: "outlined", density: "compact", rounded: "0" },
    VChip: { rounded: "0" },
  },
});

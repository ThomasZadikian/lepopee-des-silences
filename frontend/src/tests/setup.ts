import { config } from "@vue/test-utils"
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

const vuetify = createVuetify({ components, directives })

config.global.plugins = [vuetify]

config.global.stubs = {
  "v-container": { template: "<div><slot /></div>" },
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
  "v-card": { template: "<div><slot /></div>" },
  "v-card-title": { template: "<div><slot /></div>" },
  "v-card-subtitle": { template: "<div><slot /></div>" },
  "v-card-text": { template: "<div><slot /></div>" },
  "v-card-actions": { template: "<div><slot /></div>" },
  "v-text-field": {
    template: "<div><label>{{ label }}</label><input /></div>",
    props: ["label"],
  },
  "v-btn": { template: "<button><slot /></button>" },
  "v-alert": { template: "<div><slot /></div>" },
  "v-app": { template: "<div><slot /></div>" },
  "v-chip": { template: "<span><slot /></span>" },
  "v-icon": { template: "<span />" },
  "v-dialog": { template: "<div><slot /></div>" },
  "v-spacer": { template: "<span />" },
  "v-divider": { template: "<hr />" },
  "v-list": { template: "<div><slot /></div>" },
  "v-list-item": { template: "<div><slot /></div>" },
  "v-progress-circular": { template: "<div class='v-progress-circular' />" },
  "v-navigation-drawer": { template: "<div><slot /></div>" },
  "v-app-bar": { template: "<div><slot /></div>" },
  "v-app-bar-title": { template: "<div><slot /></div>" },
  "v-main": { template: "<div><slot /></div>" },
}
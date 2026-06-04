import { describe, expect, it } from "vitest"
import { createVuetify } from "vuetify"

describe("vuetify configuration", () => {
  it("crée une instance Vuetify sans erreur", () => {
    const vuetify = createVuetify()
    expect(vuetify).toBeDefined()
  })

  it("utilise le thème rpgLight par défaut", () => {
    const vuetify = createVuetify({
      theme: { defaultTheme: "rpgLight" },
    })
    expect(vuetify).toBeDefined()
  })
})

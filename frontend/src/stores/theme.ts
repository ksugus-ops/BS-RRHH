import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export type ColorScheme = 'dark' | 'light'

const STORAGE_KEY = 'hria.theme'
/** El modo claro es el predeterminado de la aplicación. */
const DEFAULT_SCHEME: ColorScheme = 'light'

function readStored(): ColorScheme {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return saved === 'light' || saved === 'dark' ? saved : DEFAULT_SCHEME
  } catch {
    // localStorage puede no estar disponible (modo privado, políticas del navegador).
    return DEFAULT_SCHEME
  }
}

/**
 * Aplica el esquema al documento.
 *
 * `.hria-dark` en <html> es el selector que usan a la vez las variables CSS
 * de `style.css` y el tema de PrimeVue (`darkModeSelector` en main.ts), de modo
 * que ambos cambian con una sola clase.
 */
export function applyColorScheme(scheme: ColorScheme) {
  const root = document.documentElement
  root.classList.toggle('hria-dark', scheme === 'dark')
  root.style.colorScheme = scheme
}

export const useThemeStore = defineStore('theme', () => {
  const scheme = ref<ColorScheme>(readStored())
  const isDark = computed(() => scheme.value === 'dark')

  function set(next: ColorScheme) {
    scheme.value = next
    applyColorScheme(next)
    try {
      localStorage.setItem(STORAGE_KEY, next)
    } catch {
      // Si no se puede persistir, el cambio sigue siendo válido en esta sesión.
    }
  }

  function toggle() {
    set(isDark.value ? 'light' : 'dark')
  }

  return { scheme, isDark, set, toggle }
})

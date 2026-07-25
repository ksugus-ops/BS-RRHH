import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import 'primeicons/primeicons.css'

import './style.css'
import App from './App.vue'
import { router } from './router'
import { HriaTheme } from './theme'
import { setTokenProvider, setUnauthorizedHandler } from '@/shared/http/client'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore, applyColorScheme } from '@/stores/theme'

async function bootstrap() {
  const app = createApp(App)

  const pinia = createPinia()
  app.use(pinia)
  app.use(PrimeVue, {
    // La clase .hria-dark en <html> conmuta a la vez el tema de PrimeVue
    // y las variables CSS propias de style.css.
    theme: { preset: HriaTheme, options: { darkModeSelector: '.hria-dark' } },
  })
  app.use(ToastService)
  app.use(ConfirmationService)

  // Aplica el esquema de color guardado (oscuro por defecto).
  applyColorScheme(useThemeStore().scheme)

  // Conecta el cliente HTTP con el store de auth (token + gestión de 401).
  const auth = useAuthStore()
  setTokenProvider(() => auth.token)
  setUnauthorizedHandler(() => {
    auth.logout()
    if (router.currentRoute.value.name !== 'login') {
      router.push({ name: 'login' })
    }
  })

  // Restaura la sesión antes de montar el router.
  await auth.restore()

  app.use(router)
  app.mount('#app')
}

bootstrap()

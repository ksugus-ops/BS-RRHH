import { definePreset } from '@primevue/themes'
import Aura from '@primevue/themes/aura'

/**
 * Tema HRIA: acento verde-menta (emerald/teal) sobre un aspecto amable,
 * inspirado en paneles de RR. HH. modernos. Admite modo claro y oscuro;
 * el modo oscuro se activa con la clase `.hria-dark` en <html> (ver main.ts).
 */
export const HriaTheme = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#e9fbf4',
      100: '#c9f5e4',
      200: '#96ebcd',
      300: '#5fdcb2',
      400: '#33cc9a',
      500: '#16b98a',
      600: '#0d9973',
      700: '#0b7d5f',
      800: '#0c6350',
      900: '#0a5142',
      950: '#032c24',
    },
    colorScheme: {
      light: {
        primary: {
          color: '#16b98a',
          contrastColor: '#ffffff',
          hoverColor: '#0d9973',
          activeColor: '#0b7d5f',
        },
      },
      dark: {
        // Sobre fondo oscuro el acento se aclara para mantener el contraste,
        // y el texto sobre el acento pasa a ser oscuro.
        primary: {
          color: '#33cc9a',
          contrastColor: '#052e24',
          hoverColor: '#5fdcb2',
          activeColor: '#96ebcd',
        },
        surface: {
          0: '#ffffff',
          50: '#eef2f0',
          100: '#d6ded9',
          200: '#b3c0ba',
          300: '#8b9c95',
          400: '#63756e',
          500: '#455650',
          600: '#33423c',
          700: '#26312c',
          800: '#1b2723',
          900: '#15201c',
          950: '#0d1412',
        },
      },
    },
  },
})

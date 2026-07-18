import { defineConfig } from 'vite'

export default defineConfig({
  base: './', // itch.io 子路径部署必需
  build: { target: 'es2022' },
})

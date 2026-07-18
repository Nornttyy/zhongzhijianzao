import { cpSync, mkdirSync } from 'node:fs'
mkdirSync('public/assets', { recursive: true })
cpSync('assets/processed', 'public/assets', { recursive: true })
console.log('assets/processed -> public/assets 同步完成')

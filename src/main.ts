import { Application } from 'pixi.js'

const app = new Application()
await app.init({ resizeTo: window, background: 0x101612 })
document.body.appendChild(app.canvas)
console.log('森之低语 骨架启动')

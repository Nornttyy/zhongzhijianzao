import { chromium } from 'playwright'

const url = process.argv[2] ?? 'http://localhost:5173/'
const shot = process.argv[3] ?? '/tmp/claude-0/-workspace-senzhidiyu/d79c50ce-7689-4303-af9e-89ea7745d1f7/scratchpad/probe.png'
const browser = await chromium.launch({
  executablePath: '/root/.cache/ms-playwright/chromium_headless_shell-1223/chrome-linux/headless_shell',
})
const page = await browser.newPage({ viewport: { width: 1280, height: 720 } })

await page.addInitScript(() => {
  window.addEventListener('unhandledrejection', (e) => console.log('[UNHANDLED-REJECTION]', String(e.reason), e.reason?.stack ?? ''))
  window.addEventListener('error', (e) => console.log('[WINDOW-ERROR]', e.message, e.filename, e.lineno))
})
page.on('console', (m) => console.log(`[console.${m.type()}]`, m.text()))
page.on('pageerror', (e) => console.log('[pageerror]', e.message, '\n', e.stack?.split('\n').slice(0, 6).join('\n')))
page.on('requestfailed', (r) => console.log('[requestfailed]', r.url(), r.failure()?.errorText))

await page.goto(url, { waitUntil: 'networkidle' })
await page.waitForTimeout(3000)
const canvas = await page.evaluate(() => {
  const c = document.querySelector('canvas')
  return c ? { w: c.width, h: c.height } : null
})
console.log('[canvas]', JSON.stringify(canvas))
await page.screenshot({ path: shot })
console.log('[screenshot]', shot)
await browser.close()

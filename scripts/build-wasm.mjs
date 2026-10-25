import { execSync } from 'node:child_process'
import { cpSync, rmSync, mkdirSync, readdirSync } from 'node:fs'
import { join, resolve } from 'node:path'

const root = resolve(import.meta.dirname, '..')
const proj = join(root, 'core/PidLab.Wasm')

execSync('dotnet publish -c Release', { cwd: proj, stdio: 'inherit' })

function findDir(dir, name) {
  for (const e of readdirSync(dir, { withFileTypes: true })) {
    if (!e.isDirectory()) continue
    const p = join(dir, e.name)
    if (e.name === name) return p
    const r = findDir(p, name)
    if (r) return r
  }
}
const fw = findDir(join(proj, 'bin/Release'), '_framework')
if (!fw) throw new Error('_framework не знайдено після publish')

const dest = join(root, 'web/public/dotnet')
rmSync(dest, { recursive: true, force: true })
mkdirSync(dest, { recursive: true })
cpSync(fw, join(dest, '_framework'), { recursive: true })
console.log('OK:', fw, '→', dest)

import { existsSync, mkdirSync, readdirSync, writeFileSync } from 'fs'
import { build } from 'esbuild'

const scriptRoot = './build/Scripts/WebSharper/'
const projectBundleDir = `${scriptRoot}QuickFinCore/`

if (!existsSync(projectBundleDir)) {
  throw new Error(`WebSharper project bundle was not found: ${projectBundleDir}`)
}

mkdirSync(scriptRoot, { recursive: true })

for (const file of readdirSync(projectBundleDir)) {
  if (file.endsWith('.js')) {
    console.log('Bundling:', file)
    await build({
      entryPoints: [`${projectBundleDir}${file}`],
      bundle: true,
      minify: true,
      format: 'iife',
      outfile: `${scriptRoot}${file}`,
      globalName: 'wsbundle'
    })
  }
}

if (!existsSync(`${scriptRoot}all.css`)) {
  writeFileSync(`${scriptRoot}all.css`, '')
}

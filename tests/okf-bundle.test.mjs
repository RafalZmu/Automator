import assert from 'node:assert/strict'
import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import test from 'node:test'
import { fileURLToPath } from 'node:url'
import { buildBundle } from '../scripts/build-okf.mjs'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const bundleDir = join(root, 'docs', 'okf')
const text_of = (path) => readFileSync(path, 'utf8').replace(/\r\n/g, '\n')
const RESERVED = new Set(['index.md', 'log.md'])

function files(dir, base = dir) {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) =>
    entry.isDirectory() ? files(join(dir, entry.name), base) : [join(dir, entry.name).slice(base.length + 1).replace(/\\/g, '/')],
  )
}

test('docs/okf matches what scripts/build-okf.mjs generates', () => {
  const expected = buildBundle()
  for (const [path, content] of expected) {
    if (path === 'log.md') continue
    const target = join(bundleDir, path)
    assert.ok(existsSync(target), `${path} is missing; run npm run okf:build`)
    assert.equal(text_of(target), content, `${path} is stale; run npm run okf:build`)
  }
  const actual = files(bundleDir).filter((path) => path !== 'log.md')
  assert.deepEqual(actual.sort(), [...expected.keys()].filter((path) => path !== 'log.md').sort())
})

test('docs/okf is a conformant OKF v0.2 bundle', () => {
  const root_index = text_of(join(bundleDir, 'index.md'))
  assert.match(root_index, /^---\nokf_version: "0\.2"\n---\n/)
  for (const path of files(bundleDir)) {
    const text = text_of(join(bundleDir, path))
    const name = path.split('/').pop()
    if (RESERVED.has(name)) {
      if (path !== 'index.md') assert.ok(!text.startsWith('---'), `${path} must not carry frontmatter`)
      continue
    }
    const match = /^---\n([\s\S]*?)\n---\n/.exec(text)
    assert.ok(match, `${path} needs YAML frontmatter`)
    assert.match(match[1], /^type: ".+"$/m, `${path} needs a non-empty type`)
  }
})

test('index entries link to files that exist', () => {
  for (const path of files(bundleDir).filter((p) => p.endsWith('index.md'))) {
    const text = text_of(join(bundleDir, path))
    for (const [, target] of text.matchAll(/\]\((?!https?:)([^)#]+)\)/g)) {
      const resolved = target.startsWith('/') ? join(bundleDir, target) : join(bundleDir, dirname(path), target)
      assert.ok(existsSync(resolved), `${path} links to missing ${target}`)
    }
  }
})

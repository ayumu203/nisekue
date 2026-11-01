import assert from 'node:assert/strict'
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { dirname, join } from 'node:path'
import { describe, it } from 'node:test'
import { compareStructures } from '../src/structure.mjs'

async function makeTree(files) {
  const root = await mkdtemp(join(tmpdir(), 'masterdata-structure-'))
  for (const [path, content] of Object.entries(files)) {
    await mkdir(dirname(join(root, path)), { recursive: true })
    await writeFile(join(root, path), content, 'utf8')
  }
  return root
}

describe('compareStructures', () => {
  it('値が違ってもファイル構成とヘッダーが同じならずれなし', async () => {
    const fixture = await makeTree({ 'quest/enemies.csv': 'id,name\n1,a\n', 'move/move_master.csv': 'move_id\n1\n' })
    const actual = await makeTree({ 'quest/enemies.csv': 'id,name\n1,b\n2,c\n', 'move/move_master.csv': 'move_id\r\n9\r\n' })
    try {
      assert.deepEqual(await compareStructures(fixture, actual), [])
    } finally {
      await rm(fixture, { recursive: true, force: true })
      await rm(actual, { recursive: true, force: true })
    }
  })

  it('列の過不足・順番違い・ファイルの過不足を報告する', async () => {
    const fixture = await makeTree({
      'quest/enemies.csv': 'id,name,ai_type\n',
      'quest/stages.csv': 'id,name\n',
      'quest/only_fixture.csv': 'id\n',
    })
    const actual = await makeTree({
      'quest/enemies.csv': 'id,name\n',
      'quest/stages.csv': 'name,id\n',
      'quest/only_sheet.csv': 'id\n',
    })
    try {
      const problems = await compareStructures(fixture, actual)
      assert.equal(problems.length, 4)
      assert.ok(problems.some((p) => p.includes('only_fixture.csv') && p.includes('スプレッドシートにない')))
      assert.ok(problems.some((p) => p.includes('only_sheet.csv') && p.includes('テスト用 CSV にない')))
      assert.ok(problems.some((p) => p.includes('enemies.csv') && p.includes('足りない列: ai_type')))
      assert.ok(problems.some((p) => p.includes('stages.csv') && p.includes('列の順番が違う')))
    } finally {
      await rm(fixture, { recursive: true, force: true })
      await rm(actual, { recursive: true, force: true })
    }
  })
})

import assert from 'node:assert/strict'
import { mkdtemp, readFile, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { describe, it } from 'node:test'
import { assetUrl, releaseTag, writeBundle } from '../src/pull.mjs'

describe('assetUrl', () => {
  it('版から Release のタグとアセット URL を作る', () => {
    assert.equal(releaseTag('3'), 'masterdata-3')
    assert.equal(
      assetUrl('ayumu203/nisekue', '3'),
      'https://github.com/ayumu203/nisekue/releases/download/masterdata-3/masterdata.json',
    )
  })
})

describe('writeBundle', () => {
  it('スナップショットの CSV を出力先に書き出す', async () => {
    const out = await mkdtemp(join(tmpdir(), 'masterdata-'))
    try {
      const paths = await writeBundle({ files: { 'server/resources/quest/stages.csv': 'id,name\n1,a\n' } }, out)
      assert.deepEqual(paths, ['server/resources/quest/stages.csv'])
      assert.equal(await readFile(join(out, 'server/resources/quest/stages.csv'), 'utf8'), 'id,name\n1,a\n')
    } finally {
      await rm(out, { recursive: true, force: true })
    }
  })

  it('server/resources 配下の .csv 以外は書き出さない', async () => {
    const out = await mkdtemp(join(tmpdir(), 'masterdata-'))
    try {
      await assert.rejects(writeBundle({ files: { 'server/Program.cs': 'x' } }, out), /CSVパス/)
      await assert.rejects(writeBundle({ files: { 'server/resources/../../x.csv': 'x' } }, out), /CSVパス/)
      await assert.rejects(writeBundle({ files: {} }, out), /含まれていません/)
    } finally {
      await rm(out, { recursive: true, force: true })
    }
  })
})

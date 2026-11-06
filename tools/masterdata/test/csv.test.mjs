import assert from 'node:assert/strict'
import { describe, it } from 'node:test'
import { cellToString, parseIndex, tableToCsv } from '../src/csv.mjs'

describe('cellToString', () => {
  it('数値・真偽値・空セルを CSV 用の文字列にする', () => {
    assert.equal(cellToString(2147483647, 'A1'), '2147483647')
    assert.equal(cellToString(0.025, 'A1'), '0.025')
    assert.equal(cellToString(true, 'A1'), 'true')
    assert.equal(cellToString(null, 'A1'), '')
    assert.equal(cellToString(undefined, 'A1'), '')
  })

  it('テキストとして入っている値は表記をそのまま残す', () => {
    assert.equal(cellToString('0.30', 'A1'), '0.30')
    assert.equal(cellToString('false', 'A1'), 'false')
    assert.equal(cellToString(' ', 'A1'), ' ')
  })

  it('カンマ・改行・指数表記はエラーにする', () => {
    assert.throws(() => cellToString('a,b', 'tab!B2'), /tab!B2: セルにカンマ/)
    assert.throws(() => cellToString('a\nb', 'tab!B2'), /改行/)
    assert.throws(() => cellToString(1e21, 'tab!B2'), /指数表記/)
  })
})

describe('tableToCsv', () => {
  it('ヘッダーの列数に揃え、空行を捨て、LF と末尾改行で出力する', () => {
    const csv = tableToCsv(
      [
        ['id', 'name', 'rate', ''],
        [1, 'スライム', '1.30'],
        [],
        ['', '', ''],
        [2, 'ゴーレム', 0.5, ''],
      ],
      'enemies',
    )
    assert.equal(csv, 'id,name,rate\n1,スライム,1.30\n2,ゴーレム,0.5\n')
  })

  it('ヘッダーより右にデータがある行はセル位置付きでエラーにする', () => {
    assert.throws(() => tableToCsv([['id'], [1, 'x']], 'enemies'), /enemies!B2: ヘッダーより右/)
  })

  it('ヘッダーの空欄・重複はエラーにする', () => {
    assert.throws(() => tableToCsv([], 'enemies'), /ヘッダー行がありません/)
    assert.throws(() => tableToCsv([['id', '', 'name']], 'enemies'), /enemies!B1: ヘッダーが空/)
    assert.throws(() => tableToCsv([['id', 'id']], 'enemies'), /ヘッダーが重複/)
  })
})

describe('parseIndex', () => {
  it('空行を飛ばしてタブ名とパスを返す', () => {
    const entries = parseIndex([
      ['enemies', 'server/resources/quest/enemies.csv'],
      [],
      ['stages', 'server/resources/quest/stages.csv'],
    ])
    assert.deepEqual(entries, [
      { tab: 'enemies', path: 'server/resources/quest/enemies.csv' },
      { tab: 'stages', path: 'server/resources/quest/stages.csv' },
    ])
  })

  it('server/resources 配下の .csv 以外への書き込みは拒否する', () => {
    assert.throws(() => parseIndex([['x', 'server/Program.cs']]), /_index!B2/)
    assert.throws(() => parseIndex([['x', 'server/resources/../Program.csv']]), /_index!B2/)
    assert.throws(() => parseIndex([['x', '.github/workflows/ci.csv']]), /_index!B2/)
  })

  it('片方だけの行や重複はエラーにする', () => {
    assert.throws(() => parseIndex([['x', '']]), /_index!A2/)
    assert.throws(
      () =>
        parseIndex([
          ['x', 'server/resources/a.csv'],
          ['x', 'server/resources/b.csv'],
        ]),
      /タブ名が重複/,
    )
  })
})

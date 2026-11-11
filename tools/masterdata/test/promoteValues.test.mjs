import assert from 'node:assert/strict'
import { describe, it } from 'node:test'
import { diffTable, formatDiffSummary, hasDiff, toUserEnteredTable, toUserEnteredValue } from '../src/promoteValues.mjs'

describe('toUserEnteredValue', () => {
  it('文字列は先頭に \' を付けてテキストのまま残す', () => {
    assert.equal(toUserEnteredValue('0.30'), "'0.30")
    assert.equal(toUserEnteredValue('true'), "'true")
    assert.equal(toUserEnteredValue('slime'), "'slime")
    assert.equal(toUserEnteredValue("'quoted"), "''quoted")
  })

  it('数式・数値・真偽値・空はそのまま', () => {
    assert.equal(toUserEnteredValue('=A1*2'), '=A1*2')
    assert.equal(toUserEnteredValue(12), 12)
    assert.equal(toUserEnteredValue(0.5), 0.5)
    assert.equal(toUserEnteredValue(false), false)
    assert.equal(toUserEnteredValue(''), '')
    assert.equal(toUserEnteredValue(null), '')
  })

  it('表全体を変換する', () => {
    assert.deepEqual(toUserEnteredTable([['id', 'rate'], [1, '0.30'], null]), [["'id", "'rate"], [1, "'0.30"], []])
  })
})

describe('diffTable', () => {
  it('同じ内容なら差分なし（末尾の空セル・空行は無視）', () => {
    const diff = diffTable([['id', 'hp'], [1, 10]], [['id', 'hp', ''], [1, 10], []])
    assert.equal(hasDiff(diff), false)
  })

  it('変わった行・増えた行・減った行を数える', () => {
    const before = [['id', 'hp'], [1, 10], [2, 20], [3, 30]]
    const after = [['id', 'hp'], [1, 15], [2, 20]]
    assert.deepEqual(diffTable(before, after), { changed: 1, added: 0, removed: 1, changedRows: [2] })
    assert.deepEqual(diffTable(after, before), { changed: 1, added: 1, removed: 0, changedRows: [2] })
  })

  it('数値とテキストの違いも差分として扱う', () => {
    assert.equal(diffTable([[1]], [['1']]).changed, 1)
  })
})

describe('formatDiffSummary', () => {
  it('差分のあるタブだけ表にする', () => {
    const text = formatDiffSummary([
      { tab: 'jobs', diff: { changed: 2, added: 1, removed: 0, changedRows: [3, 5] } },
      { tab: 'items', diff: { changed: 0, added: 0, removed: 0, changedRows: [] } },
    ])
    assert.match(text, /\| jobs \| 2 \| 1 \| 0 \| 3, 5 \|/)
    assert.doesNotMatch(text, /items/)
  })

  it('差分が無ければその旨を返す', () => {
    assert.equal(formatDiffSummary([]), '差分はありません。\n')
  })
})

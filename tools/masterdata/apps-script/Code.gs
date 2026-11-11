/**
 * ステージング用スプレッドシートに「nisekue」メニューを追加し、
 * GitHub Actions のワークフローをスプレッドシートから起動する。
 *
 * このファイルはステージング用スプレッドシートの「拡張機能 → Apps Script」に貼り付けて使う。
 * リポジトリにあるのは控え。設定手順は同じフォルダの README.md を参照。
 *
 * スクリプト プロパティ（プロジェクトの設定 → スクリプト プロパティ）
 *   GITHUB_TOKEN            fine-grained personal access token（nisekue の Actions: Read and write のみ）
 *   STAGING_SPREADSHEET_ID  ステージング用スプレッドシートの ID。これと一致するスプレッドシートでのみ動く
 */

const REPO = 'ayumu203/nisekue'
const REF = 'main'
const WORKFLOWS = {
  deployStaging: {
    file: 'deploy-backend-dev.yml',
    label: 'ステージングに反映',
    confirm: 'ステージング用スプレッドシートの内容でステージングのバックエンドを再デプロイします。\n（整合性テストに失敗した場合はデプロイされません。反映まで数分かかります）',
  },
  promote: {
    file: 'promote-masterdata.yml',
    label: '本番へ昇格',
    confirm:
      'ステージング用スプレッドシートの内容で本番用スプレッドシートを上書きし、本番へデプロイします。\n' +
      '実行後、GitHub の prod Environment で承認するまで本番には反映されません。',
  },
}

function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('nisekue')
    .addItem(WORKFLOWS.deployStaging.label, 'deployStaging')
    .addItem('直近の反映結果を見る', 'showLatestStagingRun')
    .addSeparator()
    .addItem(WORKFLOWS.promote.label, 'promoteToProduction')
    .addItem('直近の昇格結果を見る', 'showLatestPromoteRun')
    .addToUi()
}

function deployStaging() {
  dispatchWithConfirm_(WORKFLOWS.deployStaging)
}

function promoteToProduction() {
  dispatchWithConfirm_(WORKFLOWS.promote)
}

function showLatestStagingRun() {
  showLatestRun_(WORKFLOWS.deployStaging)
}

function showLatestPromoteRun() {
  showLatestRun_(WORKFLOWS.promote)
}

function dispatchWithConfirm_(workflow) {
  const ui = SpreadsheetApp.getUi()
  if (!assertStagingSpreadsheet_()) return

  const answer = ui.alert(workflow.label, workflow.confirm, ui.ButtonSet.OK_CANCEL)
  if (answer !== ui.Button.OK) return

  const res = githubRequest_(`/repos/${REPO}/actions/workflows/${workflow.file}/dispatches`, {
    method: 'post',
    payload: JSON.stringify({ ref: REF }),
  })
  const code = res.getResponseCode()
  if (code === 204) {
    ui.alert(
      workflow.label,
      `起動しました。進み具合は「直近の${workflow === WORKFLOWS.promote ? '昇格' : '反映'}結果を見る」か、次の URL で確認できます。\n` +
        `https://github.com/${REPO}/actions/workflows/${workflow.file}`,
      ui.ButtonSet.OK,
    )
    return
  }
  ui.alert(workflow.label, describeError_(code, res.getContentText(), workflow), ui.ButtonSet.OK)
}

function showLatestRun_(workflow) {
  const ui = SpreadsheetApp.getUi()
  if (!assertStagingSpreadsheet_()) return

  const res = githubRequest_(`/repos/${REPO}/actions/workflows/${workflow.file}/runs?per_page=1`, { method: 'get' })
  const code = res.getResponseCode()
  if (code !== 200) {
    ui.alert(workflow.label, describeError_(code, res.getContentText(), workflow), ui.ButtonSet.OK)
    return
  }
  const run = (JSON.parse(res.getContentText()).workflow_runs || [])[0]
  if (!run) {
    ui.alert(workflow.label, 'まだ実行されていません。', ui.ButtonSet.OK)
    return
  }
  const started = Utilities.formatDate(new Date(run.created_at), 'Asia/Tokyo', 'yyyy-MM-dd HH:mm')
  ui.alert(
    workflow.label,
    `開始: ${started}\n状態: ${describeStatus_(run)}\n${run.html_url}`,
    ui.ButtonSet.OK,
  )
}

function describeStatus_(run) {
  if (run.status !== 'completed') {
    return run.status === 'waiting' ? '承認待ち（GitHub で承認してください）' : '実行中'
  }
  switch (run.conclusion) {
    case 'success':
      return '成功'
    case 'failure':
      return '失敗（整合性テストで止まった可能性があります。URL からログを確認してください）'
    case 'cancelled':
      return 'キャンセル'
    default:
      return run.conclusion
  }
}

function describeError_(code, body, workflow) {
  if (code === 401) return 'GITHUB_TOKEN が無効です。トークンの有効期限を確認し、作り直してください。'
  if (code === 403) return 'GITHUB_TOKEN に Actions の Read and write 権限がありません。'
  if (code === 404) return `ワークフロー ${workflow.file} が見つかりません（main にまだ無いか、トークンの対象リポジトリが違います）。`
  if (code === 422) return `ワークフローを起動できませんでした（${body}）。`
  return `GitHub API がエラーを返しました（${code}）: ${body}`
}

/** 設定したステージング用スプレッドシート以外（コピーした本番用など）では動かさない */
function assertStagingSpreadsheet_() {
  const props = PropertiesService.getScriptProperties()
  const stagingId = props.getProperty('STAGING_SPREADSHEET_ID')
  const ui = SpreadsheetApp.getUi()
  if (!props.getProperty('GITHUB_TOKEN') || !stagingId) {
    ui.alert('nisekue', 'スクリプト プロパティ GITHUB_TOKEN と STAGING_SPREADSHEET_ID を設定してください。', ui.ButtonSet.OK)
    return false
  }
  if (SpreadsheetApp.getActiveSpreadsheet().getId() !== stagingId) {
    ui.alert(
      'nisekue',
      'このスプレッドシートはステージング用として登録されていません。\n本番用スプレッドシートからは実行できません。',
      ui.ButtonSet.OK,
    )
    return false
  }
  return true
}

function githubRequest_(path, options) {
  const token = PropertiesService.getScriptProperties().getProperty('GITHUB_TOKEN')
  return UrlFetchApp.fetch(`https://api.github.com${path}`, {
    ...options,
    contentType: 'application/json',
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: 'application/vnd.github+json',
      'X-GitHub-Api-Version': '2022-11-28',
    },
    muteHttpExceptions: true,
  })
}

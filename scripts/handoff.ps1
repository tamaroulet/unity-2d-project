#Requires -Version 5.1
# SPDX-AI-Disclosure: ai-generated
<#
.SYNOPSIS
  受け渡しサイクル（docs/workflow/HANDOFF_PROTOCOL.md）を回す。

.DESCRIPTION
  人間は 1 行だけ言えばよい。Gemini がこのスクリプトを順に呼ぶ。

    scripts/handoff.ps1 start  -Request "ターン6で進めない"
      → docs/handoff/<日付>-<連番>/ を作り、01-request.md と
        02-context.md の実測部分（git / build / test）を自動生成する。
        「既に潰した仮説」「見立て」「判断してほしいこと」は Gemini が埋める。

    scripts/handoff.ps1 ask    -Dir docs/handoff/2026-09-04-01
      → 02-context.md を読ませて Claude に判断させ、03-instruction.md と
        確定指示書を発行させる。Claude に渡すのはファイルを指す 1 行だけで、
        本文は流し込まない（Claude 側が自分で読む。枠の節約になる）。

    scripts/handoff.ps1 review -Dir docs/handoff/2026-09-04-01
      → 04-result.md を読ませて合否を判定させ、05-review.md を書かせる。

  Claude の起動は必ず invoke_claude_safe.ps1 を経由する。
  使用率 85% 以上で物理的に遮断され、課金超過を防ぐ。
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('start', 'ask', 'review')]
    [string]$Action,

    [string]$Request,
    [string]$Dir,
    [string]$Model = 'sonnet'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$protocol = 'docs/workflow/HANDOFF_PROTOCOL.md'

function Invoke-Claude([string]$Prompt) {
    $safe = Join-Path $PSScriptRoot 'invoke_claude_safe.ps1'
    Write-Host "[handoff] Claude へ依頼:" -ForegroundColor Cyan
    Write-Host "  $Prompt"
    & powershell -ExecutionPolicy Bypass -NoProfile -File $safe -Prompt $Prompt -Model $Model
    if ($LASTEXITCODE -ne 0) {
        Write-Error "[handoff] Claude の起動に失敗（枠の遮断の可能性）。Gemini 側で進めるか、枠の回復を待つこと。"
    }
}

if ($Action -eq 'start') {
    if (-not $Request) { Write-Error '[handoff] start には -Request が必要。人間の原文をそのまま渡すこと。' }

    $date = Get-Date -Format 'yyyy-MM-dd'
    $n = 1
    while (Test-Path (Join-Path $root ("docs/handoff/{0}-{1:D2}" -f $date, $n))) { $n++ }
    $dirRel = "docs/handoff/{0}-{1:D2}" -f $date, $n
    $dirAbs = Join-Path $root $dirRel
    New-Item -ItemType Directory -Path $dirAbs -Force | Out-Null

    # 01: 人間の原文。要約も言い換えもしない
    Set-Content -Path (Join-Path $dirAbs '01-request.md') -Value $Request -Encoding utf8

    Write-Host '[handoff] 実測データを収集中...' -ForegroundColor Cyan
    Push-Location $root
    try {
        $gitStatus = (& git status --porcelain | Out-String).Trim()
        if (-not $gitStatus) { $gitStatus = '(出力なし・作業ツリーはクリーン)' }
        $gitLog = (& git log --oneline -5 | Out-String).Trim()
        # MSB3277（参照バージョン競合）は Unity 環境で常に出る雑音なので落とす。
        # 残すのは実際のエラーと集計行だけ。02-context.md を読む側の負担を減らす。
        $build = (& dotnet build Game/Game.sln -v q --nologo 2>&1 |
            Where-Object { $_ -notmatch 'MSB3277' } | Out-String).Trim()
    } finally { Pop-Location }

    $quoted = ($Request -split "`n" | ForEach-Object { "> $_" }) -join "`n"

    $ctx = @"
# 相談 $($date)-$('{0:D2}' -f $n)

## 人間の原文（逐語・改変禁止）
$quoted

## 実測データ

### git
``````
$ git status --porcelain
$gitStatus

$ git log --oneline -5
$gitLog
``````

### ビルド
``````
$ dotnet build Game/Game.sln -v q --nologo
$build
``````

### テスト
``````
（TODO: Gemini が Unity-MCP の run_tests か batchmode で取得し、生出力を貼る。
  どちらを使ったか明記すること）
``````

### エラー・ログ
``````
（TODO: Gemini が read_console 等で取得し、要約せず貼る）
``````

## 既に潰した仮説
- （TODO: 調べて違うと分かったことを書く。Claude に再調査させないため）

## 私（Gemini）の見立て
- 原因の候補: A =（1行）/ B =（1行）
- 推す案: （A か B か）。理由は（1〜2行）

## Claude に判断してほしいこと
（TODO: 選択形式で書く。開いた質問にしない）
"@

    Set-Content -Path (Join-Path $dirAbs '02-context.md') -Value $ctx -Encoding utf8

    Write-Host ''
    Write-Host "[handoff] 作成: $dirRel" -ForegroundColor Green
    Write-Host '  01-request.md  … 人間の原文（確定）'
    Write-Host '  02-context.md  … 実測部分は自動生成済み。TODO を埋めること'
    Write-Host ''
    Write-Host '次: TODO を埋めてから  scripts/handoff.ps1 ask -Dir ' -NoNewline
    Write-Host $dirRel -ForegroundColor Yellow
    exit 0
}

if (-not $Dir) { Write-Error "[handoff] $Action には -Dir が必要。" }
$dirRel = $Dir -replace '\\', '/' -replace '^.*?(docs/handoff/)', '$1'

if ($Action -eq 'ask') {
    $ctxPath = Join-Path $root (Join-Path $dirRel '02-context.md')
    if (-not (Test-Path $ctxPath)) { Write-Error "[handoff] 見つからない: $dirRel/02-context.md" }
    if ((Get-Content $ctxPath -Raw) -match 'TODO:') {
        Write-Error "[handoff] 02-context.md に TODO が残っている。埋めてから ask すること。"
    }

    Invoke-Claude "$dirRel/02-context.md を読み、$protocol に従って $dirRel/03-instruction.md と、次の確定指示書 docs/instructions/ を発行してください。判断の裏取りを 1 回だけ行ってから決めてください。"
    exit 0
}

if ($Action -eq 'review') {
    $resPath = Join-Path $root (Join-Path $dirRel '04-result.md')
    if (-not (Test-Path $resPath)) { Write-Error "[handoff] 見つからない: $dirRel/04-result.md" }

    Invoke-Claude "$dirRel/04-result.md を読み、$protocol に従って $dirRel/05-review.md を書いてください。合否（即時承認 / 修正後承認 / 差し戻し）と再試行回数を先頭に明記してください。差し戻しが 2 往復に達している場合は人間へ上げてください。"
    exit 0
}

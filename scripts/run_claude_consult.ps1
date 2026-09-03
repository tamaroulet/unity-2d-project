$prompt = Get-Content -Path (Join-Path $PSScriptRoot "claude_prompt.txt") -Raw -Encoding UTF8
& (Join-Path $PSScriptRoot "invoke_claude_safe.ps1") -Prompt $prompt


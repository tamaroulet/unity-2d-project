# SPDX-AI-Disclosure: ai-generated
# copy_webgl_to_docs.ps1
# WebGL ビルド成果物を docs/webgl/ にコピーし、GitHub Pages で公開可能な状態にする

$sourceDir = "c:\dev\unity-2d-project\Game\Builds\WebGL"
$destDir = "c:\dev\unity-2d-project\docs\webgl"

if (-not (Test-Path $sourceDir)) {
    Write-Error "WebGL build directory does not exist: $sourceDir"
    exit 1
}

if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

Copy-Item -Path "$sourceDir\*" -Destination $destDir -Recurse -Force
Write-Host "[SUCCESS] WebGL build artifacts copied from '$sourceDir' to '$destDir'."

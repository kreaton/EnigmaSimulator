# Renders tools\banner.html to images\banner.png (1920x1200) with headless Edge.
#   powershell -File tools\make-banner.ps1
$edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
$html = (Resolve-Path (Join-Path $PSScriptRoot "banner.html")).Path
$url = "file:///" + $html.Replace('\', '/')
$outDir = Join-Path $PSScriptRoot "..\images"
New-Item -ItemType Directory -Force $outDir | Out-Null
$png = Join-Path (Resolve-Path $outDir).Path "banner.png"
# A throwaway profile, so a running Edge does not interfere with the headless render.
$edgeProfile = Join-Path ([System.IO.Path]::GetTempPath()) ("enigma-banner-" + [guid]::NewGuid())

& $edge --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 `
    --window-size=1920,1200 --virtual-time-budget=3000 --user-data-dir="$edgeProfile" --screenshot="$png" $url 2>$null | Out-Null
Remove-Item -Recurse -Force $edgeProfile -ErrorAction SilentlyContinue
Write-Host "Banner written to $png"

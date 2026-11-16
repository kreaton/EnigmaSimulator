# Generates the app / home-screen icons for EnigmaSimulator from wwwroot\icons\favicon.svg (a brass rotor
# ring around a drum window showing "E"). The SVG is the single source: headless Edge renders it at
# 1024 px, and System.Drawing scales that down to the other sizes.
# Run with Windows PowerShell (System.Drawing):  powershell -File tools\make-icons.ps1
param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\src\EnigmaSimulator\wwwroot\icons")
)

Add-Type -AssemblyName System.Drawing
$OutDir = (Resolve-Path $OutDir).Path
$svg = Join-Path $OutDir "favicon.svg"
if (-not (Test-Path $svg)) { throw "Missing $svg" }

# 1. Render the SVG at 1024x1024 with headless Edge (into a temp folder, then load it)
$edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("enigma-icons-" + [guid]::NewGuid())
New-Item -ItemType Directory -Force $tmp | Out-Null
Copy-Item $svg (Join-Path $tmp "icon.svg")
Set-Content -Encoding UTF8 (Join-Path $tmp "render.html") `
    '<!doctype html><style>html,body{margin:0}</style><img src="icon.svg" width="1024" height="1024" style="display:block">'
$master = Join-Path $tmp "icon-1024.png"
& $edge --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 --window-size=1024,1024 `
    --user-data-dir="$tmp\profile" --screenshot="$master" "file:///$((Join-Path $tmp 'render.html') -replace '\\', '/')" 2>$null | Out-Null
if (-not (Test-Path $master)) { throw "Edge did not render $master" }
$src = [System.Drawing.Image]::FromFile($master)

# content: 1.0 = full size. The design already keeps the drum inside the maskable safe zone (40 % radius),
# so the maskable icon is the full image too; only the tips of the knurled ring may be cropped.
function Save-Icon([int]$size, [string]$file, [float]$content = 1.0) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.ColorTranslator]::FromHtml('#140e09'))
    $d = $size * $content
    $o = ($size - $d) / 2
    $g.DrawImage($src, [float]$o, [float]$o, [float]$d, [float]$d)
    $g.Dispose()
    $bmp.Save((Join-Path $OutDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $file"
}

Write-Host "Writing icons to $OutDir"
Save-Icon 180  'apple-touch-icon.png'
Save-Icon 167  'apple-touch-icon-167.png'
Save-Icon 152  'apple-touch-icon-152.png'
Save-Icon 192  'icon-192.png'
Save-Icon 512  'icon-512.png'
Save-Icon 1024 'icon-1024.png'
Save-Icon 512  'icon-512-maskable.png'
Save-Icon 32   'favicon-32.png'

$src.Dispose()
Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue

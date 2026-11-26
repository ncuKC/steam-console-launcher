param([string]$Root = (Resolve-Path "$PSScriptRoot/..").Path)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$bitmap=New-Object System.Drawing.Bitmap(256,256)
$graphics=[System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::FromArgb(11,16,24))
$brush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(114,203,255))
$graphics.FillEllipse($brush,20,20,216,216)
$points=[System.Drawing.Point[]]@([System.Drawing.Point]::new(105,76),[System.Drawing.Point]::new(105,180),[System.Drawing.Point]::new(181,128))
$graphics.FillPolygon([System.Drawing.Brushes]::MidnightBlue,$points)
$handle=$bitmap.GetHicon()
$icon=[System.Drawing.Icon]::FromHandle($handle)
$stream=[IO.File]::Create((Join-Path $Root 'src/launcher.ico'))
try{$icon.Save($stream)}finally{$stream.Dispose();$icon.Dispose();$graphics.Dispose();$brush.Dispose();$bitmap.Dispose()}

Add-Type -AssemblyName System.Drawing

$srcPath = "D:\WorkSpace\LoaInfo\Assets\@Resources\lostarkicon.png"
$fgPath = "D:\WorkSpace\LoaInfo\Assets\@Resources\app_icon_fg.png"
$legacyPath = "D:\WorkSpace\LoaInfo\Assets\@Resources\app_icon.png"

$src = [System.Drawing.Image]::FromFile($srcPath)
Write-Host ("Source: {0}x{1} {2}" -f $src.Width, $src.Height, $src.PixelFormat)

# Adaptive foreground: 432 canvas, content in ~66% safe zone
$size = 432
$safe = [int]($size * 0.66)
$canvas = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.Clear([System.Drawing.Color]::Transparent)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$ox = [int](($size - $safe) / 2)
$g.DrawImage($src, $ox, $ox, $safe, $safe)
$g.Dispose()
$canvas.Save($fgPath, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
Write-Host ("Wrote foreground: " + $fgPath)

# Legacy square icon 512
$legacySize = 512
$legacy = New-Object System.Drawing.Bitmap $legacySize, $legacySize
$lg = [System.Drawing.Graphics]::FromImage($legacy)
$lg.Clear([System.Drawing.Color]::FromArgb(255, 14, 16, 22))
$lg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$pad = [int]($legacySize * 0.08)
$lg.DrawImage($src, $pad, $pad, $legacySize - 2 * $pad, $legacySize - 2 * $pad)
$lg.Dispose()
$legacy.Save($legacyPath, [System.Drawing.Imaging.ImageFormat]::Png)
$legacy.Dispose()
$src.Dispose()
Write-Host ("Wrote legacy: " + $legacyPath)

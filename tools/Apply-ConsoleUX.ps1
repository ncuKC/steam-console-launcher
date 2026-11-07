param([string]$Root = (Resolve-Path "$PSScriptRoot/..").Path)
$ErrorActionPreference='Stop'
$file=Join-Path $Root 'src/Program.cs'
$text=[IO.File]::ReadAllText($file)
function Replace-Required([string]$Old,[string]$New){
  if(!$script:text.Contains($Old)){throw "Expected source fragment not found: $Old"}
  $script:text=$script:text.Replace($Old,$New)
}
Replace-Required 'public class LauncherWindow:Window{' 'public partial class LauncherWindow:Window{'
Replace-Required 'Loaded+=async(_,_)=>{await Reload();' 'Loaded+=async(_,_)=>{InitConsoleUX();await Reload();'
Replace-Required 'void Render(){var id=' 'void Render(){ConfigureConsoleView();var id='
Replace-Required 'if(g.Installed)WindowState=WindowState.Minimized;' 'if(g.Installed)WatchSession(g.Id);'
Replace-Required 'int columns=Math.Max(1,(int)(list.ActualWidth/206));' 'int columns=home?1:Math.Max(1,(int)(list.ActualWidth/206));'
Replace-Required 'list.Focus();}' 'FocusSelected();}'
[IO.File]::WriteAllText($file,$text,(New-Object System.Text.UTF8Encoding($false)))
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
$project=Join-Path $Root 'src/Launcher.csproj'
[xml]$xml=[IO.File]::ReadAllText($project)
$group=$xml.Project.PropertyGroup | Select-Object -First 1
$node=$group.SelectSingleNode('ApplicationIcon')
if($null -eq $node){$node=$xml.CreateElement('ApplicationIcon');[void]$group.AppendChild($node)}
$node.InnerText='launcher.ico'
$xml.Save($project)

param([string]$Root = (Resolve-Path "$PSScriptRoot/..").Path)
$ErrorActionPreference = 'Stop'
$file = Join-Path $Root 'src/Program.cs'
$text = [IO.File]::ReadAllText($file)
if ($text.Contains('NativeDetails? nativeContent')) { throw 'Patch already applied' }
if (!$text.Contains('public class LauncherWindow:Window{')) { throw 'Unexpected source layout' }
$text = $text.Replace('public class LauncherWindow:Window{', "public class LauncherWindow:Window{`n NativeDetails? nativeContent;")
$pattern = '(?m)^ async void Details\(\)\{[^\r\n]*\}'
if ([regex]::Matches($text,$pattern).Count -ne 1) { throw 'Expected one Details method' }
$new = ' void Details(){if(Selected is not Game g)return;nativeContent?.Dispose();nativeContent=new NativeDetails(g,Launch,()=>Store("https://store.steampowered.com/app/"+g.Id+"/"),Back);detail.Children.Clear();detail.Margin=new Thickness(42,24,42,24);nativeContent.Height=Math.Max(400,ActualHeight-100);detail.Children.Add(nativeContent);Show("details");nativeContent.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));}'
$text = [regex]::Replace($text,$pattern,[System.Text.RegularExpressions.MatchEvaluator]{param($m) $new})
$text = $text.Replace('void Back(){Show("library");list.Focus();}', 'void Back(){if(nativeContent?.HandleBack()==true)return;nativeContent?.Dispose();nativeContent=null;Experience.Play("back");Show("library");list.Focus();}')
$text = $text.Replace('b.Click+=(_,_)=>action();return b;', 'b.Click+=(_,_)=>{Experience.Play("confirm");action();};return b;')
$text = $text.Replace('list.SelectionChanged+=async(_,_)=>await Select();', 'list.SelectionChanged+=async(_,_)=>{Experience.Play("move");await Select();};')
$text = $text.Replace('int version=++selectionVersion;hero.Source=null;', 'int version=++selectionVersion;')
$text = $text.Replace('if(version==selectionVersion)hero.Source=image;', 'if(version==selectionVersion){hero.Source=image;Experience.Transition(hero);}')
$text = $text.Replace('void Show(string p){page=p;', 'void Show(string p){if(page=="details"&&p!="details"){nativeContent?.Dispose();nativeContent=null;}page=p;Experience.Transition(p=="library"?shell:p=="details"?detail:store);')
$text = $text.Replace('pad?.Dispose();browser.Dispose();', 'pad?.Dispose();nativeContent?.Dispose();browser.Dispose();')
[IO.File]::WriteAllText($file,$text,(New-Object System.Text.UTF8Encoding($false)))
$project = Join-Path $Root 'src/Launcher.csproj'
[xml]$xml = [IO.File]::ReadAllText($project)
$group = $xml.Project.PropertyGroup | Select-Object -First 1
foreach($pair in @(@('AssemblyName','_launcher'),@('RootNamespace','ConsoleLauncher'))){
 $node=$group.SelectSingleNode($pair[0]);if($null -eq $node){$node=$xml.CreateElement($pair[0]);[void]$group.AppendChild($node)};$node.InnerText=$pair[1]
}
$xml.Save($project)

param([string]$Root = (Resolve-Path "$PSScriptRoot/..").Path)
$ErrorActionPreference='Stop'
$utf8=New-Object System.Text.UTF8Encoding($false)
$path=Join-Path $Root 'src/Program.cs'
$text=[IO.File]::ReadAllText($path)
function Replace-Required([string]$Old,[string]$New){if(!$script:text.Contains($Old)){throw "Required source fragment missing: $Old"};$script:text=$script:text.Replace($Old,$New)}
Replace-Required 'public static void Main()=>new Program().Run(new LauncherWindow());' 'public static void Main(){Velopack.VelopackApp.Build().SetAutoApplyOnStartup(false).Run();new Program().Run(new LauncherWindow());}'
Replace-Required 'if(RouteExtraInput(b))continue;' 'if(RouteReleaseInput(b))continue;if(RouteExtraInput(b))continue;'
Replace-Required 'InitConsoleUX();await Reload();' 'InitConsoleUX();InitReleaseUX();await Reload();'
Replace-Required 'Show("library");search.Focus();' 'Tab(false);search.Focus();'
[IO.File]::WriteAllText($path,$text,$utf8)
$path=Join-Path $Root 'src/ConsoleUX.cs'
$text=[IO.File]::ReadAllText($path)
$text=$text.Replace('Button("Меню",OpenConsoleSettings)','Button("Меню",OpenReleaseMenu)')
$text=$text.Replace('("Назад",Back)','("Назад",CloseReleaseMenu)')
$old='Show("details");detail.MoveFocus'
if(!$text.Contains($old)){throw 'Settings insertion point missing'}
$text=$text.Replace($old,'AddReleaseMenu();Show("details");detail.MoveFocus')
$pattern='(?s) void ConfigureConsoleView\(\)\{.*?(?=\r?\n void AnimateConsoleCard)'
if([regex]::Matches($text,$pattern).Count -ne 1){throw 'Expected one legacy layout method'}
$text=[regex]::Replace($text,$pattern,' void ConfigureConsoleView(){if(!consoleReady)return;ConfigureScene();}'+"`n")
[IO.File]::WriteAllText($path,$text,$utf8)
$path=Join-Path $Root 'src/Launcher.csproj'
[xml]$xml=[IO.File]::ReadAllText($path)
$group=$xml.CreateElement('ItemGroup');$package=$xml.CreateElement('PackageReference');$package.SetAttribute('Include','Velopack');$package.SetAttribute('Version','1.2.161');[void]$group.AppendChild($package);[void]$xml.Project.AppendChild($group);$xml.Save($path)

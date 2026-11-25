param([string]$Root = (Resolve-Path "$PSScriptRoot/..").Path)
$ErrorActionPreference='Stop'
$utf8=New-Object System.Text.UTF8Encoding($false)
$path=Join-Path $Root 'src/Program.cs'
$text=[IO.File]::ReadAllText($path)
$pattern='(?m)^ Button Button\(string text,Action action,bool primary=false\)\{[^\r\n]*\}'
if([regex]::Matches($text,$pattern).Count -ne 1){throw 'Button factory not found'}
$text=[regex]::Replace($text,$pattern,' Button Button(string text,Action action,bool primary=false)=>UiKit.Make(text,action,primary);')
if(!$text.Contains('page=p;')){throw 'Page state not found'}
$text=$text.Replace('page=p;','page=p;MarkTabs();')
$text=$text.Replace('ConfigureConsoleView();var id=','ConfigureConsoleView();MarkTabs();var id=')
if(!$text.Contains('{int confirm=swapped?1:0')){throw 'Controller dispatch not found'}
$text=$text.Replace('{int confirm=swapped?1:0','{if(RouteExtraInput(b))continue;int confirm=swapped?1:0')
[IO.File]::WriteAllText($path,$text,$utf8)
$path=Join-Path $Root 'src/ConsoleUX.cs'
$text=[IO.File]::ReadAllText($path)
if(!$text.Contains('void ConfigureConsoleView(){if(!consoleReady)return;')){throw 'Console layout method not found'}
$text=$text.Replace('void ConfigureConsoleView(){if(!consoleReady)return;','void ConfigureConsoleView(){if(!consoleReady)return;if(configuredLayout==home)return;configuredLayout=home;')
[IO.File]::WriteAllText($path,$text,$utf8)
$path=Join-Path $Root 'src/Gamepad.cs'
$text=[IO.File]::ReadAllText($path)
$pattern='return pressed;\s*\}\s*(?=public void Dispose\s*\()'
$matches=[regex]::Matches($text,$pattern)
if($matches.Count -ne 1){throw "Expected exactly one controller return before Dispose; found $($matches.Count)"}
$new='if(active){int rightY=SDL_GetGamepadAxis(pad,3);if(rightY < -12000)pressed.Add(101);else if(rightY > 12000)pressed.Add(102);}return pressed;}'+"`n "
$text=[regex]::Replace($text,$pattern,$new)
[IO.File]::WriteAllText($path,$text,$utf8)

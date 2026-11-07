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
$old='return pressed;}'+"`n"+' public void Dispose'
if(!$text.Contains($old)){throw 'Controller return not found'}
$new='if(active){int rightY=SDL_GetGamepadAxis(pad,3);if(rightY < -12000)pressed.Add(101);else if(rightY > 12000)pressed.Add(102);}return pressed;}'+"`n"+' public void Dispose'
$text=$text.Replace($old,$new)
[IO.File]::WriteAllText($path,$text,$utf8)
$path=Join-Path $Root 'src/NativeDetails.cs'
$text=[IO.File]::ReadAllText($path)
$text=$text.Replace('public sealed class NativeDetails:Grid,IDisposable{','public sealed class NativeDetails:Grid,IDisposable{'+"`n"+' readonly ScrollViewer scroll; public bool HandlePad(int code){if(viewer.Visibility==Visibility.Visible){if(code==13){Step(-1);return true;}if(code==14){Step(1);return true;}return code==101||code==102;}if(code==101||code==102){scroll.ScrollToVerticalOffset(scroll.VerticalOffset+(code==101?-24:24));return true;}return false;}')
if(!$text.Contains('var scroll=new ScrollViewer')){throw 'Details scroll not found'}
$text=$text.Replace('var scroll=new ScrollViewer','scroll=new ScrollViewer')
$text=$text.Replace('Loaded+=async(_,_)=>{','PreviewMouseWheel+=(_,e)=>{if(viewer.Visibility==Visibility.Visible)Step(e.Delta>0?-1:1);else scroll.ScrollToVerticalOffset(scroll.VerticalOffset-e.Delta);e.Handled=true;};'+"`n"+'  Loaded+=async(_,_)=>{')
$pattern='(?m)^ static Button NativeButton\(string text,Action action\)\{[^\r\n]*\}'
if([regex]::Matches($text,$pattern).Count -ne 1){throw 'Native button factory not found'}
$text=[regex]::Replace($text,$pattern,' static Button NativeButton(string text,Action action)=>UiKit.Make(text,action);')
$old='Text(Plain(Str(data,"detailed_description")));'
if(!$text.Contains($old)){throw 'Description output not found'}
$text=$text.Replace($old,'body.Children.Add(UiKit.Description(Str(data,"detailed_description")));')
[IO.File]::WriteAllText($path,$text,$utf8)

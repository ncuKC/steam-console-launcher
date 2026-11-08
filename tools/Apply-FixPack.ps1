param([string]$Root = (Resolve-Path "$PSScriptRoot/..").Path)
$ErrorActionPreference='Stop'
$utf8=New-Object System.Text.UTF8Encoding($false)
function Patch([string]$Relative,[string]$Old,[string]$New){$file=Join-Path $Root $Relative;$text=[IO.File]::ReadAllText($file);if(!$text.Contains($Old)){throw "FixPack fragment missing in $Relative : $Old"};[IO.File]::WriteAllText($file,$text.Replace($Old,$New),$utf8)}
Patch 'src/LayoutPolish.cs' 'layoutPolished=true;InitStandaloneUX();' 'layoutPolished=true;InitStandaloneUX();InitFixPack();'
Patch 'src/LayoutPolish.cs' 'new GridLength(330)' 'new GridLength(356)'
Patch 'src/LayoutPolish.cs' 'caption.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);' 'caption.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);caption.SetValue(FrameworkElement.HeightProperty,44.0);caption.SetValue(TextBlock.LineHeightProperty,22.0);caption.SetBinding(FrameworkElement.ToolTipProperty,new Binding("Name"));'
Patch 'src/LayoutPolish.cs' 'style.Setters.Add(new Setter(Control.TemplateProperty,template));' 'style.Setters.Add(new Setter(Control.TemplateProperty,template));style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty,null));'
Patch 'src/Program.cs' 'if(RouteReleaseInput(b))continue;' 'if(RecoverPadFocus(b))continue;if(RouteReleaseInput(b))continue;'
$file=Join-Path $Root 'src/ConsoleUX.cs';$text=[IO.File]::ReadAllText($file)
$pattern='(?s) static void ScaleCard\(.*?(?=\r?\n void OpenConsoleSettings)'
if([regex]::Matches($text,$pattern).Count -ne 1){throw 'Expected one ScaleCard method'}
$text=[regex]::Replace($text,$pattern,' static void ScaleCard(ListBoxItem card,double value){card.RenderTransform=Transform.Identity;}'+"`n");[IO.File]::WriteAllText($file,$text,$utf8)
Patch 'src/ReleaseUX.cs' 'detail.Children.Add(updateStatus);' 'detail.Children.Add(updateStatus);AttachDownloadProgress();'
Patch 'src/ReleaseUX.cs' 'updateBusy=true;try{updateStatus.Text="Скачивание обновления…";' 'updateBusy=true;BeginDownloadProgress();try{updateStatus.Text="Скачивание обновления…";'
Patch 'src/ReleaseUX.cs' 'await updateManager!.DownloadUpdatesAsync(availableUpdate);' 'await updateManager!.DownloadUpdatesAsync(availableUpdate,ReportDownloadProgress);'
Patch 'src/ReleaseUX.cs' 'finally{updateBusy=false;}' 'finally{updateBusy=false;SetInstallEnabled(true);}'
Patch 'src/NativeDetails.cs' 'readonly StackPanel body=new();' 'readonly StackPanel body=new(){MaxWidth=1200,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(32)};'
Patch 'src/NativeDetails.cs' 'FontSize=size,Foreground=Brushes.White' 'FontSize=Math.Max(size,23),LineHeight=Math.Max(size,23)*1.5,Foreground=Brushes.White'
Patch 'src/NativeDetails.cs' 'foreach(var r in reviews.EnumerateArray()){' 'var reviewGrid=new WrapPanel();body.Children.Add(reviewGrid);foreach(var r in reviews.EnumerateArray()){'
$old='Text(r.TryGetProperty("voted_up",out var up)&&up.GetBoolean()?"Рекомендует":"Не рекомендует",16);var s=Str(r,"review");Text(s.Length>4000?s[..4000]+"…":s,17);'
$new='reviewGrid.Children.Add(ReviewCards.Make(r,id));'
Patch 'src/NativeDetails.cs' $old $new

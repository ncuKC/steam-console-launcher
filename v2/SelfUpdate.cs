using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;
namespace ConsoleLauncher;
public static class SelfUpdate{
 static UpdateManager? manager;
 static UpdateInfo? staged;
 static string ready="";
 static bool busy;
 static readonly Border toast=new(){Background=new SolidColorBrush(Color.FromArgb(0xE6,0x1B,0x28,0x38)),CornerRadius=new CornerRadius(12),Padding=new Thickness(20,12,20,12),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,150,40,0),MaxWidth=760,Visibility=Visibility.Collapsed,IsHitTestVisible=false};
 static readonly TextBlock text=new(){FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap};
 public static void Attach(Window window){
  toast.Child=text;
  var timer=new DispatcherTimer{Interval=TimeSpan.FromHours(6)};
  timer.Tick+=async(_,_)=>await Check(window);
  window.Loaded+=async(_,_)=>{
   if(window.Content is Viewbox box&&box.Child is Grid canvas)canvas.Children.Add(toast);
   await Task.Delay(TimeSpan.FromSeconds(60));
   timer.Start();
   await Check(window);
  };
  window.Closed+=(_,_)=>{
   timer.Stop();
   ApplyOnExit();
  };
 }
 static async Task Check(Window window){
  if(busy||!window.Topmost)return;
  busy=true;
  try{
   manager??=new UpdateManager(new GithubSource("https://github.com/ncuKC/pc-game-launcher",null,false));
   if(!manager.IsInstalled)return;
   var update=await manager.CheckForUpdatesAsync();
   if(update==null)return;
   var version=update.TargetFullRelease.Version.ToString();
   if(version==ready)return;
   await manager.DownloadUpdatesAsync(update);
   staged=update;
   ready=version;
   text.Text="⬇  Обновление лаунчера "+version+" загружено. Установится при закрытии лаунчера или через меню.";
   toast.Visibility=Visibility.Visible;
   await Task.Delay(TimeSpan.FromSeconds(15));
   toast.Visibility=Visibility.Collapsed;
  }catch{}
  finally{busy=false;}
 }
 static void ApplyOnExit(){
  try{
   if(manager!=null&&staged!=null)manager.WaitExitThenApplyUpdates(staged.TargetFullRelease,true,false);
  }catch{}
 }
}

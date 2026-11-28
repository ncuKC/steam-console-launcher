using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Velopack;
using Velopack.Sources;
namespace ConsoleLauncher;
public static class SelfUpdate{
 public enum Phase{Idle,Checking,Downloading,Ready,Failed}
 public static Phase State=Phase.Idle;
 public static int Percent;
 public static string Version="";
 public static string Error="";
 static UpdateManager? manager;
 static UpdateInfo? staged;
 static string toasted="";
 static bool busy;
 static readonly Border toast=new(){Background=new SolidColorBrush(Color.FromArgb(0xE6,0x1B,0x28,0x38)),CornerRadius=new CornerRadius(12),Padding=new Thickness(20,12,20,12),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,150,40,0),MaxWidth=760,Visibility=Visibility.Collapsed,IsHitTestVisible=false};
 static readonly TextBlock text=new(){FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap};
 public static void Attach(Window window){
  toast.Child=text;
  var timer=new DispatcherTimer{Interval=TimeSpan.FromHours(6)};
  timer.Tick+=async(_,_)=>await Check(window);
  window.Loaded+=async(_,_)=>{
   if(window.Content is Grid shell&&shell.Children.Count>0&&shell.Children[0] is Viewbox box&&box.Child is Grid canvas)canvas.Children.Add(toast);
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
  if(!window.Topmost)return;
  await Run(false);
  if(State==Phase.Ready&&Version!=toasted){
   toasted=Version;
   text.Text="⬇  Обновление лаунчера "+Version+" загружено. Установится при закрытии лаунчера или через меню.";
   toast.Visibility=Visibility.Visible;
   await Task.Delay(TimeSpan.FromSeconds(15));
   toast.Visibility=Visibility.Collapsed;
  }
 }
 public static async Task<string> Run(bool manual){
  if(busy)return "Проверка или загрузка уже идёт.";
  busy=true;
  try{
   manager??=new UpdateManager(new GithubSource("https://github.com/ncuKC/pc-game-launcher",null,false));
   if(!manager.IsInstalled)return "Для обновлений установите лаунчер через Setup.exe.";
   if(State!=Phase.Ready)State=Phase.Checking;
   var update=await manager.CheckForUpdatesAsync();
   if(update==null){
    if(State==Phase.Checking)State=Phase.Idle;
    return State==Phase.Ready?"Версия "+Version+" уже загружена.":"Установлена актуальная версия.";
   }
   var version=update.TargetFullRelease.Version.ToString();
   if(State==Phase.Ready&&Version==version)return "Версия "+version+" уже загружена.";
   Version=version;
   Percent=0;
   Error="";
   State=Phase.Downloading;
   await manager.DownloadUpdatesAsync(update,progress=>Percent=progress);
   staged=update;
   Percent=100;
   State=Phase.Ready;
   return "Версия "+version+" загружена.";
  }catch(Exception ex){
   State=Phase.Failed;
   Error=ex.Message;
   return "Не удалось обновиться: "+ex.Message;
  }
  finally{busy=false;}
 }
 public static bool InstallAndRestart(){
  try{
   if(manager==null||staged==null)return false;
   manager.ApplyUpdatesAndRestart(staged.TargetFullRelease);
   return true;
  }catch{return false;}
 }
 static void ApplyOnExit(){
  try{
   if(manager!=null&&staged!=null)manager.WaitExitThenApplyUpdates(staged.TargetFullRelease,true,false);
  }catch{}
 }
}

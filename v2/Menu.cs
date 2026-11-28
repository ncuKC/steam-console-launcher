using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Velopack;
using Velopack.Sources;
namespace ConsoleLauncher;
public sealed class SideMenu:Border{
 sealed class Entry{public Border Box=new();public TextBlock Label=new();public Func<Task> Run=()=>Task.CompletedTask;}
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 const string RunName="SteamConsoleLauncher";
 readonly List<Entry> entries=new();
 readonly StackPanel list=new();
 readonly TextBlock status=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(48,16,48,0)};
 readonly Entry autostart;
 int index;
 bool busy;
 UpdateManager? manager;
 UpdateInfo? update;
 public bool IsOpen=>Visibility==Visibility.Visible;
 public SideMenu(Action close,Action quit){
  Width=720;
  HorizontalAlignment=HorizontalAlignment.Left;
  Background=Ui.B("#F01B2838");
  Visibility=Visibility.Collapsed;
  var root=new StackPanel();
  Child=root;
  root.Children.Add(new TextBlock{Text="Меню",FontSize=44,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,Margin=new Thickness(48,48,48,6)});
  var version=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion??"development";
  root.Children.Add(new TextBlock{Text="Версия: "+version.Split('+')[0],FontSize=18,Foreground=Ui.B("#8F98A0"),Margin=new Thickness(48,0,48,28)});
  root.Children.Add(list);
  root.Children.Add(status);
  Add("Продолжить",()=>{close();return Task.CompletedTask;});
  autostart=Add("",ToggleAutostart);
  Add("Проверить обновления",Check);
  Add("Скачать обновление и перезапустить",Install);
  Add("Закрыть лаунчер",()=>{quit();return Task.CompletedTask;});
  RefreshAutostart();
 }
 Entry Add(string text,Func<Task> run){
  var entry=new Entry{Run=run};
  entry.Label=new TextBlock{Text=text,FontSize=24,Foreground=Brushes.White};
  entry.Box=new Border{Margin=new Thickness(48,0,48,10),Padding=new Thickness(24,12,24,12),CornerRadius=new CornerRadius(6),Background=Ui.B("#2A475E"),Child=entry.Label};
  list.Children.Add(entry.Box);
  entries.Add(entry);
  return entry;
 }
 public void Open(){Visibility=Visibility.Visible;index=0;status.Text="";RefreshAutostart();Highlight();}
 public void Close(){Visibility=Visibility.Collapsed;}
 public void Move(int delta){index=Math.Clamp(index+delta,0,entries.Count-1);Highlight();}
 public async void Activate(){await entries[index].Run();}
 void Highlight(){
  for(int i=0;i<entries.Count;i++){
   bool on=i==index;
   entries[i].Box.Background=on?Ui.B("#66C0F4"):Ui.B("#2A475E");
   entries[i].Label.Foreground=on?Ui.B("#0E141B"):Brushes.White;
  }
 }
 static bool AutostartOn(){
  try{using var key=Registry.CurrentUser.OpenSubKey(RunKey);return key?.GetValue(RunName)!=null;}catch{return false;}
 }
 static bool SetAutostart(bool on){
  try{
   using var key=Registry.CurrentUser.CreateSubKey(RunKey);
   if(key==null)return false;
   if(on){
    var exe=Environment.ProcessPath;
    if(string.IsNullOrEmpty(exe))return false;
    key.SetValue(RunName,"\""+exe+"\"");
   }else key.DeleteValue(RunName,false);
   return true;
  }catch{return false;}
 }
 void RefreshAutostart(){
  autostart.Label.Text="Автозапуск Windows: "+(AutostartOn()?"включён":"выключен");
 }
 Task ToggleAutostart(){
  bool ok=SetAutostart(!AutostartOn());
  RefreshAutostart();
  status.Text=ok?"":"Не удалось изменить автозапуск.";
  return Task.CompletedTask;
 }
 static bool GameRunning(){
  try{return Convert.ToInt64(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","RunningAppID",0))!=0;}catch{return true;}
 }
 async Task Check(){
  if(busy)return;
  busy=true;
  try{
   status.Text="Проверка обновлений…";
   manager??=new UpdateManager(new GithubSource("https://github.com/ncuKC/steam-console-launcher",null,false));
   if(!manager.IsInstalled){status.Text="Для обновлений установите лаунчер через Setup.exe.";return;}
   update=await manager.CheckForUpdatesAsync();
   status.Text=update==null?"Установлена актуальная версия.":"Доступна версия "+update.TargetFullRelease.Version+". Выберите «Скачать обновление и перезапустить».";
  }catch(Exception ex){status.Text="Не удалось проверить обновления: "+ex.Message;}
  finally{busy=false;}
 }
 async Task Install(){
  if(busy)return;
  if(update==null){await Check();if(update==null)return;}
  if(GameRunning()){status.Text="Завершите игру перед обновлением лаунчера.";return;}
  busy=true;
  try{
   status.Text="Скачивание обновления…";
   await manager!.DownloadUpdatesAsync(update);
   status.Text="Применение обновления…";
   manager.ApplyUpdatesAndRestart(update);
  }catch(Exception ex){status.Text="Обновление не применено: "+ex.Message;}
  finally{busy=false;}
 }
}

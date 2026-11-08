using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Velopack;
using Velopack.Sources;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 readonly TextBlock updateStatus=new(){TextWrapping=TextWrapping.Wrap,FontSize=16,Margin=new Thickness(0,0,0,18)};
 UpdateManager? updateManager; UpdateInfo? availableUpdate; bool updateBusy,releaseReady;
 bool menuVisible; string menuReturnPage="library"; IInputElement? menuReturnFocus;
 string BuildVersion=>Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion??"development";
 void InitReleaseUX(){
  if(releaseReady)return;releaseReady=true;
  Dispatcher.BeginInvoke(new Action(async()=>await CheckLauncherUpdates(false)));
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape&&menuVisible){CloseReleaseMenu();e.Handled=true;}};
  var version=new TextBlock{Text=BuildVersion,FontSize=12,Opacity=.65,Margin=new Thickness(12,0,12,12),VerticalAlignment=VerticalAlignment.Center};
  if(shell.Children[0] is DockPanel nav&&nav.Children[0] is StackPanel settings)settings.Children.Insert(0,version);
 }
 void OpenReleaseMenu(){if(menuVisible){CloseReleaseMenu();return;}menuReturnPage=page;menuReturnFocus=Keyboard.FocusedElement;OpenConsoleSettings();menuVisible=true;}
 void CloseReleaseMenu(){menuVisible=false;Show(menuReturnPage);if(menuReturnFocus is UIElement target&&target.IsVisible)target.Focus();else if(page=="library")FocusSelected();}
 bool RouteReleaseInput(int code){
  if(code==6){OpenReleaseMenu();return true;}
  if(menuVisible&&code==(swapped?0:1)){CloseReleaseMenu();return true;}
  if(code==9||code==10){menuVisible=false;int current=page=="store"?2:home?0:1;int next=(current+(code==9?2:1))%3;if(next==2)_=Store("https://store.steampowered.com/");else Tab(next==0);return true;}
  if(page!="library"||menuVisible)return false;
  var top=shell.Children[0] as DockPanel;
  var buttons=top?.Children.OfType<StackPanel>().SelectMany(g=>g.Children.OfType<System.Windows.Controls.Button>()).ToList();
  if(code==11&&list.IsKeyboardFocusWithin){int columns=home?list.Items.Count:Math.Max(1,(int)(list.ActualWidth/206));if(home||list.SelectedIndex<columns){if(home)play.Focus();else buttons?.FirstOrDefault(b=>(b.Content as string)=="Библиотека")?.Focus();return true;}}
  if(Keyboard.FocusedElement is System.Windows.Controls.Button focused){
   if(buttons!=null&&buttons.Contains(focused)){
    var ordered=buttons.Where(b=>(b.Content as string)!="Меню").Concat(buttons.Where(b=>(b.Content as string)=="Меню")).ToList();int index=ordered.IndexOf(focused);
    if(code==13||code==14){ordered[Math.Clamp(index+(code==13?-1:1),0,ordered.Count-1)].Focus();return true;}
    if(code==12){if(home)play.Focus();else FocusSelected();return true;}
   }
   if(focused==play){if(code==11){buttons?.FirstOrDefault(b=>(b.Content as string)==(home?"Главная":"Библиотека"))?.Focus();return true;}if(code==12){FocusSelected();return true;}}
  }
  return false;
 }
 void AddReleaseMenu(){
  detail.Children.Add(new TextBlock{Text="Версия: "+BuildVersion,FontSize=16,Margin=new Thickness(0,0,0,12)});
  if(updateStatus.Parent is Panel old)old.Children.Remove(updateStatus);detail.Children.Add(updateStatus);
  detail.Children.Add(Button("Проверить обновления лаунчера",async()=>await CheckLauncherUpdates(true)));
  detail.Children.Add(Button("Скачать обновление и перезапустить",async()=>await InstallLauncherUpdate()));
 }
 async Task CheckLauncherUpdates(bool showMenu){
  if(updateBusy)return;updateBusy=true;
  try{updateStatus.Text="Проверка обновлений…";updateManager??=new UpdateManager(new GithubSource("https://github.com/ncuKC/steam-console-launcher",null,false));if(!updateManager.IsInstalled){updateStatus.Text="Для обновлений установите сборку через Setup.exe. Текущий запуск — без установленного обновлятора.";return;}availableUpdate=await updateManager.CheckForUpdatesAsync();updateStatus.Text=availableUpdate==null?"Установлена актуальная версия.":"Доступна версия "+availableUpdate.TargetFullRelease.Version+". Обновление применяется только по вашей команде.";}
  catch(Exception ex){updateStatus.Text="Не удалось проверить обновления. Приложение продолжает работу. "+ex.Message;}
  finally{updateBusy=false;}
 }
 bool SteamGameRunning(){try{return Convert.ToInt64(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","RunningAppID",0))!=0||(sessionId!=null&&Convert.ToInt32(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\Apps\"+sessionId,"Running",0))!=0);}catch{return true;}}
 async Task InstallLauncherUpdate(){
  if(updateBusy)return;if(availableUpdate==null){await CheckLauncherUpdates(true);return;}
  if(SteamGameRunning()){updateStatus.Text="Завершите игру перед обновлением лаунчера.";return;}
  updateBusy=true;try{updateStatus.Text="Скачивание обновления…";await updateManager!.DownloadUpdatesAsync(availableUpdate);if(SteamGameRunning()){updateStatus.Text="Обновление скачано. Завершите игру и снова нажмите кнопку установки.";return;}updateStatus.Text="Применение обновления…";updateManager.ApplyUpdatesAndRestart(availableUpdate);}
  catch(Exception ex){updateStatus.Text="Обновление не применено. "+ex.Message;}finally{updateBusy=false;}
 }
}

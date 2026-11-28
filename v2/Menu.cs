using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Win32;
using Velopack;
using Velopack.Sources;
namespace ConsoleLauncher;
public sealed class SideMenu:Border{
 sealed class Entry{
  public Border Box=new();
  public SolidColorBrush Fill=new(Idle);
  public TextBlock Icon=new();
  public TextBlock Label=new();
  public ScaleTransform Scale=new(1,1);
  public TranslateTransform Shift=new(0,0);
  public bool Danger;
  public Func<Task> Run=()=>Task.CompletedTask;
 }
 static Color Hex(string text)=>(Color)ColorConverter.ConvertFromString(text);
 static readonly Color Idle=Color.FromArgb(0x26,255,255,255);
 static readonly Color Accent=Hex("#66C0F4");
 static readonly Color DangerColor=Hex("#E5484D");
 static readonly Brush Dark=new SolidColorBrush(Hex("#0E141B"));
 static readonly FontFamily Icons=new("Segoe MDL2 Assets");
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 const string RunName="SteamConsoleLauncher";
 readonly List<Entry> entries=new();
 readonly StackPanel list=new();
 readonly TextBlock status=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(36,10,36,0)};
 readonly TranslateTransform slide=new(-90,0);
 Entry autostart=new();
 Entry? restart,shutdown,armed;
 DateTime armedAt;
 int index,closing;
 bool busy,open;
 UpdateManager? manager;
 UpdateInfo? update;
 public bool IsOpen=>open;
 public SideMenu(Action close,Action quit,Action minimize){
  Width=640;
  HorizontalAlignment=HorizontalAlignment.Left;
  Background=new LinearGradientBrush(Hex("#F2203246"),Hex("#EB0B1119"),0);
  BorderBrush=new SolidColorBrush(Color.FromArgb(0x55,255,255,255));
  BorderThickness=new Thickness(0,0,1,0);
  Visibility=Visibility.Collapsed;
  Opacity=0;
  Effect=new DropShadowEffect{BlurRadius=60,ShadowDepth=0,Opacity=.8,Color=Colors.Black};
  RenderTransform=slide;
  var root=new StackPanel();
  Child=root;
  var header=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(36,40,36,4)};
  header.Children.Add(new TextBlock{Text="",FontFamily=Icons,FontSize=34,Foreground=new SolidColorBrush(Accent),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,18,0)});
  header.Children.Add(new TextBlock{Text="Меню",FontSize=44,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center});
  root.Children.Add(header);
  var version=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion??"development";
  root.Children.Add(new TextBlock{Text="Версия: "+version.Split('+')[0],FontSize=18,Foreground=Ui.B("#8F98A0"),Margin=new Thickness(36,0,36,22)});
  root.Children.Add(list);
  root.Children.Add(status);
  Add("Продолжить","",()=>{close();return Task.CompletedTask;});
  Add("Свернуть лаунчер","",()=>{minimize();return Task.CompletedTask;});
  autostart=Add("","",ToggleAutostart);
  Add("Проверить обновления","",Check);
  Add("Скачать обновление и перезапустить","",Install);
  restart=Add("Перезагрузить ПК","",()=>Power(restart!,"/r"),true);
  shutdown=Add("Выключить ПК","",()=>Power(shutdown!,"/s"),true);
  Add("Закрыть лаунчер","",()=>{quit();return Task.CompletedTask;});
  RefreshAutostart();
 }
 Entry Add(string text,string icon,Func<Task> run,bool danger=false){
  var entry=new Entry{Run=run,Danger=danger};
  entry.Icon=new TextBlock{Text=icon,FontFamily=Icons,FontSize=24,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
  entry.Label=new TextBlock{Text=text,FontSize=24,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};
  var grid=new Grid();
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(54)});
  grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  Grid.SetColumn(entry.Label,1);
  grid.Children.Add(entry.Icon);
  grid.Children.Add(entry.Label);
  var group=new TransformGroup();
  group.Children.Add(entry.Scale);
  group.Children.Add(entry.Shift);
  entry.Box=new Border{Height=58,Margin=new Thickness(36,0,36,8),Padding=new Thickness(10,0,16,0),CornerRadius=new CornerRadius(12),Background=entry.Fill,RenderTransformOrigin=new Point(0,.5),RenderTransform=group,Child=grid};
  list.Children.Add(entry.Box);
  entries.Add(entry);
  return entry;
 }
 public void Open(){
  open=true;
  closing++;
  Visibility=Visibility.Visible;
  index=0;
  status.Text="";
  ResetPower();
  RefreshAutostart();
  Highlight(true);
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(220)));
  slide.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-90,0,TimeSpan.FromMilliseconds(320)){EasingFunction=ease});
  for(int i=0;i<entries.Count;i++){
   var entry=entries[i];
   var delay=TimeSpan.FromMilliseconds(60+i*40);
   entry.Box.Opacity=0;
   entry.Shift.X=-50;
   entry.Shift.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-50,0,TimeSpan.FromMilliseconds(300)){BeginTime=delay,EasingFunction=ease});
   entry.Box.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(260)){BeginTime=delay});
  }
 }
 public void Close(){
  if(!open)return;
  open=false;
  int token=++closing;
  var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(190));
  fade.Completed+=(_,_)=>{if(token==closing)Visibility=Visibility.Collapsed;};
  BeginAnimation(OpacityProperty,fade);
  slide.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(0,-70,TimeSpan.FromMilliseconds(210)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseIn}});
 }
 public void Move(int delta){
  if(armed!=null){ResetPower();status.Text="";}
  index=Math.Clamp(index+delta,0,entries.Count-1);
  Highlight(false);
 }
 public async void Activate(){
  var entry=entries[index];
  if(armed!=null&&entry!=armed){ResetPower();status.Text="";}
  await entry.Run();
 }
 void Highlight(bool instant){
  for(int i=0;i<entries.Count;i++){
   var entry=entries[i];
   bool on=i==index;
   var target=on?(entry.Danger?DangerColor:Accent):Idle;
   Brush ink=on?(entry.Danger?Brushes.White:Dark):Brushes.White;
   entry.Label.Foreground=ink;
   entry.Icon.Foreground=ink;
   if(instant){
    entry.Fill.BeginAnimation(SolidColorBrush.ColorProperty,null);
    entry.Fill.Color=target;
   }else entry.Fill.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(target,TimeSpan.FromMilliseconds(130)));
   double size=on?1.03:1;
   entry.Scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(size,TimeSpan.FromMilliseconds(130)));
   entry.Scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(size,TimeSpan.FromMilliseconds(130)));
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
 void ResetPower(){
  armed=null;
  if(restart!=null)restart.Label.Text="Перезагрузить ПК";
  if(shutdown!=null)shutdown.Label.Text="Выключить ПК";
 }
 Task Power(Entry entry,string flag){
  bool off=flag=="/s";
  if(armed==entry&&(DateTime.Now-armedAt).TotalSeconds<5){
   try{Process.Start(new ProcessStartInfo("shutdown",flag+" /t 3"){CreateNoWindow=true,UseShellExecute=false});}catch{}
   status.Text=off?"Выключение…":"Перезагрузка…";
   armed=null;
   return Task.CompletedTask;
  }
  ResetPower();
  armed=entry;
  armedAt=DateTime.Now;
  entry.Label.Text="Подтвердите: "+(off?"выключить ПК":"перезагрузить ПК");
  status.Text="Нажмите ещё раз в течение 5 секунд.";
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

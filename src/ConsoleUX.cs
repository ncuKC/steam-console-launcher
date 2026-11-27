using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool consoleReady;Image? consoleBackdrop;ListBoxItem? previousCard;readonly DispatcherTimer sessionTimer=new(){Interval=TimeSpan.FromSeconds(2)};string? sessionId;bool sessionSeen;int sessionZeros;DateTime sessionRequested,sessionStarted;
 [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)]static extern bool SetForegroundWindow(IntPtr hwnd);
 void InitConsoleUX(){
  if(consoleReady)return;consoleReady=true;WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowState=WindowState.Maximized;
  var root=(Grid)Content;consoleBackdrop=new Image{Stretch=Stretch.UniformToFill,Opacity=.7,IsHitTestVisible=false};consoleBackdrop.SetBinding(Image.SourceProperty,new Binding("Source"){Source=hero});root.Children.Insert(0,consoleBackdrop);
  var shade=new Border{IsHitTestVisible=false,Background=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(235,8,13,22),0),new(Color.FromArgb(80,8,13,22),.45),new(Color.FromArgb(245,8,13,22),1)},new Point(0,0),new Point(0,1))};root.Children.Insert(1,shade);
  hero.Visibility=Visibility.Hidden;if(hero.Parent is Grid banner)banner.Background=Brushes.Transparent;
  shell.Margin=new Thickness(56,28,56,22);title.FontSize=52;title.MaxWidth=900;label.FontSize=14;
  if(shell.Children[0] is DockPanel nav){if(nav.Children[0] is StackPanel settings){settings.Children.Clear();settings.Children.Add(Button("Меню",OpenReleaseMenu));}if(nav.Children.Count>1&&nav.Children[1] is StackPanel tabs)foreach(var button in tabs.Children.OfType<System.Windows.Controls.Button>()){button.Background=Brushes.Transparent;button.FontSize=20;}}
  status.Visibility=Visibility.Collapsed;var hints=new TextBlock{Text="A  Выбрать       B  Назад       X  Играть       LB / RB  Разделы       F11  Оконный режим",FontSize=14,Foreground=B("#B7C6DB"),Margin=new Thickness(0,18,0,0)};Grid.SetRow(hints,4);shell.Children.Add(hints);
  list.SelectionChanged+=(_,_)=>Dispatcher.BeginInvoke(new Action(AnimateConsoleCard),DispatcherPriority.Loaded);
  sessionTimer.Tick+=(_,_)=>CheckSession();Closed+=(_,_)=>sessionTimer.Stop();ConfigureConsoleView();
  Dispatcher.BeginInvoke(new Action(FocusSelected),DispatcherPriority.Loaded);
 }
 void FocusSelected(){if(list.SelectedItem!=null&&list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) is ListBoxItem item)item.Focus();else list.Focus();}
 void ConfigureConsoleView(){if(!consoleReady)return;ConfigureScene();}
 void AnimateConsoleCard(){if(previousCard!=null)ScaleCard(previousCard,1);if(list.SelectedItem!=null&&list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) is ListBoxItem current){ScaleCard(current,1.04);previousCard=current;}}
 static void ScaleCard(ListBoxItem card,double value){card.RenderTransform=Transform.Identity;}
 bool menuSub;
 void SectionItem(System.Windows.Controls.Button b){b.HorizontalAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(0,0,0,16);detail.Children.Add(b);}
 void SectionPage(string caption,bool root=false){
  menuSub=!root;nativeContent?.Dispose();nativeContent=null;detail.Children.Clear();detail.Margin=new Thickness(60);
  detail.Children.Add(new TextBlock{Text=caption,FontSize=36,Foreground=Brushes.White,Margin=new Thickness(0,0,0,28)});
  Action backAction=root?(Action)CloseReleaseMenu:OpenConsoleSettings;
  SectionItem(Button(root?"Закрыть меню":"Назад",backAction));
 }
 void SectionFocus(){detail.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));}
 void AddAutostartItem(){
  string Caption()=>"Запуск вместе с Windows: "+(Autostart.Enabled?"вкл":"выкл");
  var toggle=Button(Caption(),()=>{});
  toggle.Click+=(_,_)=>{Autostart.Set(!Autostart.Enabled);toggle.Content=Caption();};
  SectionItem(toggle);
 }
 void OpenConsoleSettings(){
  SectionPage("Меню лаунчера",true);
  SectionItem(Button("Игра",OpenGameSection));
  SectionItem(Button("Звук и вид",OpenSoundSection));
  SectionItem(Button("Steam и библиотека",OpenSteamSection));
  SectionItem(Button("Обновления лаунчера",OpenUpdateSection));
  AddAutostartItem();
  SectionItem(Button("Питание ПК",OpenPowerMenu));
  SectionItem(Button("Выйти из лаунчера",Close));
  Show("details");SectionFocus();
 }
 void OpenGameSection(){
  SectionPage("Игра");
  var msg=new TextBlock{Text="Back+Start в игре сворачивает её и открывает лаунчер. Отсюда можно закрыть свёрнутую игру.",FontSize=16,Foreground=B("#8F98A0"),TextWrapping=TextWrapping.Wrap,MaxWidth=700,Margin=new Thickness(0,0,0,18)};
  detail.Children.Add(msg);
  SectionItem(Button("Закрыть свёрнутую игру",()=>{msg.Text=GameReturn.CloseLast()?"Игре отправлена команда закрытия. Если она спросит про сохранение, ответьте в окне игры.":"Свёрнутой игры нет. Сначала нажмите Back+Start в игре.";}));
  SectionFocus();
 }
 void OpenSoundSection(){
  SectionPage("Звук и вид");
  string VolumeCaption()=>"Громкость интерфейса: "+(Experience.Volume<=0?"выкл":Math.Round(Experience.Volume*100)+"%");
  string MotionCaption()=>"Анимации: "+(Experience.ReducedMotion?"выкл":"вкл");
  var volume=Button(VolumeCaption(),()=>{});volume.Click+=(_,_)=>{Experience.CycleVolume();volume.Content=VolumeCaption();Experience.Play("move");};SectionItem(volume);
  var motion=Button(MotionCaption(),()=>{});motion.Click+=(_,_)=>{Experience.ToggleMotion();motion.Content=MotionCaption();};SectionItem(motion);
  SectionItem(Button("Поменять подтверждение / отмену",()=>swapped=!swapped));
  SectionFocus();
 }
 void OpenSteamSection(){
  SectionPage("Steam и библиотека");
  AddSteamAuthMenu();
  if(detail.Children.Count>0)detail.Children.RemoveAt(detail.Children.Count-1);
  SectionItem(Button("Обновить библиотеку",async()=>await Reload()));
  SectionItem(Button("Импорт по ключу API (запасной способ)",Account));
  SectionFocus();
 }
 void OpenUpdateSection(){
  SectionPage("Обновления лаунчера");
  detail.Children.Add(new TextBlock{Text="Версия: "+BuildVersion,FontSize=16,Margin=new Thickness(0,0,0,12)});
  if(updateStatus.Parent is Panel old)old.Children.Remove(updateStatus);detail.Children.Add(updateStatus);AttachDownloadProgress();
  detail.Children.Add(Button("Проверить обновления лаунчера",async()=>await CheckLauncherUpdates(true)));
  detail.Children.Add(Button("Скачать обновление и перезапустить",async()=>await InstallLauncherUpdate()));
  SectionFocus();
 }
 void WatchSession(string id){sessionTimer.Stop();sessionId=id;sessionSeen=false;sessionZeros=0;sessionRequested=DateTime.UtcNow;sessionTimer.Start();}
 void CheckSession(){if(sessionId==null){sessionTimer.Stop();return;}try{
  var active=Convert.ToString(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","RunningAppID",0),CultureInfo.InvariantCulture)??"0";
  bool running=active==sessionId||Convert.ToInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\Apps\"+sessionId,"Running",0),CultureInfo.InvariantCulture)!=0;
  if(running){sessionZeros=0;if(!sessionSeen){sessionSeen=true;sessionStarted=DateTime.UtcNow;WindowState=WindowState.Minimized;}return;}
  if(!sessionSeen){if(DateTime.UtcNow-sessionRequested>TimeSpan.FromSeconds(180))sessionTimer.Stop();return;}
  if(active!="0"&&active!=""){sessionTimer.Stop();return;}
  if(DateTime.UtcNow-sessionStarted<TimeSpan.FromSeconds(15)||++sessionZeros<4)return;
  sessionTimer.Stop();Show();WindowState=WindowState.Maximized;Activate();SetForegroundWindow(new WindowInteropHelper(this).Handle);if(page=="library")FocusSelected();
 }catch{sessionTimer.Stop();}}
}

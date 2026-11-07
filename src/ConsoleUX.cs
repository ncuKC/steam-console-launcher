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
  if(shell.Children[0] is DockPanel nav){if(nav.Children[0] is StackPanel settings){settings.Children.Clear();settings.Children.Add(Button("Меню",OpenConsoleSettings));}if(nav.Children.Count>1&&nav.Children[1] is StackPanel tabs)foreach(var button in tabs.Children.OfType<System.Windows.Controls.Button>()){button.Background=Brushes.Transparent;button.FontSize=20;}}
  status.Visibility=Visibility.Collapsed;var hints=new TextBlock{Text="A  Выбрать       B  Назад       X  Играть       LB / RB  Разделы       F11  Оконный режим",FontSize=14,Foreground=B("#B7C6DB"),Margin=new Thickness(0,18,0,0)};Grid.SetRow(hints,4);shell.Children.Add(hints);
  list.SelectionChanged+=(_,_)=>Dispatcher.BeginInvoke(new Action(AnimateConsoleCard),DispatcherPriority.Loaded);
  sessionTimer.Tick+=(_,_)=>CheckSession();Closed+=(_,_)=>sessionTimer.Stop();ConfigureConsoleView();
  Dispatcher.BeginInvoke(new Action(FocusSelected),DispatcherPriority.Loaded);
 }
 void FocusSelected(){if(list.SelectedItem!=null&&list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) is ListBoxItem item)item.Focus();else list.Focus();}
 void ConfigureConsoleView(){if(!consoleReady)return;
  var factory=new FrameworkElementFactory(home?typeof(StackPanel):typeof(WrapPanel));if(home)factory.SetValue(StackPanel.OrientationProperty,Orientation.Horizontal);list.ItemsPanel=new ItemsPanelTemplate(factory);
  list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,home?ScrollBarVisibility.Auto:ScrollBarVisibility.Disabled);list.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty,home?ScrollBarVisibility.Disabled:ScrollBarVisibility.Auto);
  if(hero.Parent is Grid banner)banner.Visibility=home?Visibility.Visible:Visibility.Collapsed;
  shell.RowDefinitions[1].Height=home?new GridLength(3,GridUnitType.Star):GridLength.Auto;shell.RowDefinitions[3].Height=new GridLength(home?2:1,GridUnitType.Star);
  search.Visibility=home?Visibility.Collapsed:Visibility.Visible;if(search.Parent is StackPanel tools)foreach(var button in tools.Children.OfType<System.Windows.Controls.Button>())button.Visibility=home?Visibility.Collapsed:Visibility.Visible;
  if(consoleBackdrop!=null)consoleBackdrop.Opacity=home?.7:.2;
 }
 void AnimateConsoleCard(){if(previousCard!=null)ScaleCard(previousCard,1);if(list.SelectedItem!=null&&list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) is ListBoxItem current){ScaleCard(current,1.04);previousCard=current;}}
 static void ScaleCard(ListBoxItem card,double value){card.RenderTransformOrigin=new Point(.5,.5);var scale=card.RenderTransform as ScaleTransform;if(scale==null||scale.IsFrozen){scale=new ScaleTransform(1,1);card.RenderTransform=scale;}if(Experience.ReducedMotion){scale.ScaleX=scale.ScaleY=value;return;}scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(value,TimeSpan.FromMilliseconds(140)));scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(value,TimeSpan.FromMilliseconds(140)));}
 void OpenConsoleSettings(){nativeContent?.Dispose();nativeContent=null;detail.Children.Clear();detail.Margin=new Thickness(60);detail.Children.Add(new TextBlock{Text="Меню лаунчера",FontSize=36,Foreground=Brushes.White,Margin=new Thickness(0,0,0,28)});foreach(var pair in new (string,Action)[]{("Назад",Back),("Аккаунт Steam",Account),("Обновить библиотеку",async()=>await Reload()),("Звук и движение",()=>Experience.Settings(this)),("Поменять подтверждение / отмену",()=>swapped=!swapped),("Выйти из лаунчера",Close)}){var b=Button(pair.Item1,pair.Item2);b.HorizontalAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(0,0,0,16);detail.Children.Add(b);}Show("details");detail.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));}
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

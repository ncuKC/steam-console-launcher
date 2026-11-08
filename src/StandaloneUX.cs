using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool standaloneReady,keyboardOpen;
 const string StartupKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 void InitStandaloneUX(){
  if(standaloneReady)return;standaloneReady=true;
  search.GotKeyboardFocus+=(_,_)=>{if(!keyboardOpen)Dispatcher.BeginInvoke(new Action(OpenSearchKeyboard));};
  detail.IsVisibleChanged+=(_,_)=>{if(detail.IsVisible)Dispatcher.BeginInvoke(new Action(AddStandaloneSettings));};
 }
 void AddStandaloneSettings(){
  if(!detail.Children.OfType<TextBlock>().Any(t=>t.Text=="Меню лаунчера"))return;
  if(detail.Children.OfType<FrameworkElement>().Any(e=>(e.Tag as string)=="standalone-startup"))return;
  var startup=Button(StartupCaption(),()=>{});startup.Tag="standalone-startup";startup.HorizontalAlignment=HorizontalAlignment.Left;
  startup.Click+=(_,_)=>{try{using var key=Registry.CurrentUser.CreateSubKey(StartupKey);if(key.GetValue("SteamConsoleLauncher")!=null)key.DeleteValue("SteamConsoleLauncher",false);else{var executable=Environment.ProcessPath;if(string.IsNullOrWhiteSpace(executable))throw new InvalidOperationException("Не найден путь приложения");key.SetValue("SteamConsoleLauncher","\""+executable+"\"");}startup.Content=StartupCaption();}catch(Exception ex){updateStatus.Text="Не удалось изменить автозапуск: "+ex.Message;}};
  detail.Children.Add(startup);
 }
 string StartupCaption(){try{using var key=Registry.CurrentUser.OpenSubKey(StartupKey);return "Запуск при входе в Windows: "+(key?.GetValue("SteamConsoleLauncher")!=null?"включён":"выключен");}catch{return "Автозапуск: недоступен";}}
 void OpenSearchKeyboard(){
  if(keyboardOpen||page!="library"||!search.IsKeyboardFocusWithin)return;
  keyboardOpen=true;timer.Stop();
  var poll=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(33)};
  try{
   var dialog=new Window{Owner=this,Title="Поиск игр",Width=940,Height=620,WindowStartupLocation=WindowStartupLocation.CenterOwner,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=B("#101A29"),Foreground=Brushes.White};
   var root=new StackPanel{Margin=new Thickness(24)};dialog.Content=root;
   root.Children.Add(new TextBlock{Text="Поиск по библиотеке",FontSize=28,Margin=new Thickness(0,0,0,16)});
   var input=new TextBox{Text=search.Text,FontSize=25,Padding=new Thickness(12),Margin=new Thickness(0,0,0,18)};root.Children.Add(input);
   var keys=new WrapPanel();root.Children.Add(keys);bool russian=false;
   void Fill(){keys.Children.Clear();foreach(char letter in russian?"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ0123456789":"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"){char value=letter;var key=Button(value.ToString(),()=>{input.AppendText(value.ToString());input.CaretIndex=input.Text.Length;});key.Width=66;key.Height=52;key.Padding=new Thickness(4);keys.Children.Add(key);}}
   Fill();
   var actions=new WrapPanel{Margin=new Thickness(0,16,0,0)};root.Children.Add(actions);
   actions.Children.Add(Button("Пробел",()=>input.AppendText(" ")));
   actions.Children.Add(Button("Стереть",()=>{if(input.Text.Length>0)input.Text=input.Text[..^1];}));
   actions.Children.Add(Button("Очистить",()=>input.Clear()));
   actions.Children.Add(Button("RU / EN",()=>{russian=!russian;Fill();(keys.Children[0] as UIElement)?.Focus();}));
   actions.Children.Add(Button("Найти",()=>dialog.DialogResult=true,true));
   actions.Children.Add(Button("Отмена",()=>dialog.DialogResult=false));
   root.Children.Add(new TextBlock{Text="Крестовина / стик — выбор · A — нажать · B — отмена. Также доступен ввод обычной клавиатурой.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0),FontSize=14});
   dialog.PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){dialog.DialogResult=false;e.Handled=true;}else if(e.Key==Key.Enter&&input.IsKeyboardFocused){dialog.DialogResult=true;e.Handled=true;}};
   poll.Tick+=(_,_)=>{if(pad==null)return;foreach(int code in pad.Tick(dialog.IsActive)){if(code==(swapped?0:1)){dialog.DialogResult=false;break;}if(code==(swapped?1:0)){if(Keyboard.FocusedElement is System.Windows.Controls.Button button)button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));continue;}if(code>=11&&code<=14){var direction=code==11?FocusNavigationDirection.Up:code==12?FocusNavigationDirection.Down:code==13?FocusNavigationDirection.Left:FocusNavigationDirection.Right;(Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(direction));}}};
   dialog.Loaded+=(_,_)=>{(keys.Children[0] as UIElement)?.Focus();poll.Start();};
   if(dialog.ShowDialog()==true)search.Text=input.Text;
  }finally{poll.Stop();keyboardOpen=false;if(IsLoaded)timer.Start();Dispatcher.BeginInvoke(new Action(FocusSelected));}
 }
}

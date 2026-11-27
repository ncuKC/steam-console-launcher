using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace ConsoleLauncher;
public sealed class MainWindow:Window{
 readonly Image backdrop=new(){Stretch=Stretch.UniformToFill,Opacity=.55};
 readonly Shelves home=new(),library=new();
 readonly Grid store=new(){Visibility=Visibility.Collapsed};
 readonly Grid details=new(){Visibility=Visibility.Collapsed,Background=Ui.B("#0B0F15")};
 readonly Image detailArt=new(){Stretch=Stretch.UniformToFill,Opacity=.7};
 readonly TextBlock detailTitle=new(){FontSize=68,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap,MaxWidth=1100};
 readonly TextBlock detailStatus=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,10)};
 readonly TextBlock detailPlay=new(){FontSize=28,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly Border[] tabBorders=new Border[3];
 readonly TextBlock clock=new(){FontSize=26,Foreground=Ui.B("#C6D4DF"),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right};
 readonly WrapPanel hints=new(){VerticalAlignment=VerticalAlignment.Center};
 readonly Gamepad pad=new();
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(33)};
 List<Game> games=new();
 int tab,version,comboTicks;
 bool inDetails;
 Game? detailGame;
 Shelves? Active=>tab==0?home:tab==1?library:null;
 static TextBlock Hint(string text)=>new(){Text=text,FontSize=18,Foreground=Ui.B("#8F98A0"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,10,0)};
 public MainWindow(){
  Title="PLAY";
  WindowStyle=WindowStyle.None;
  WindowState=WindowState.Maximized;
  Background=Brushes.Black;
  Foreground=Brushes.White;
  FontFamily=new FontFamily("Segoe UI");
  var canvas=new Grid{Width=1920,Height=1080,Background=Ui.B("#0B0F15")};
  Content=new Viewbox{Stretch=Stretch.Uniform,Child=canvas};
  canvas.Children.Add(backdrop);
  canvas.Children.Add(new Border{IsHitTestVisible=false,Background=Ui.Gradient(new Point(0,0),new Point(1,0),250,70)});
  canvas.Children.Add(new Border{IsHitTestVisible=false,Background=Ui.Gradient(new Point(0,0),new Point(0,1),40,255)});
  var layout=new Grid{Margin=new Thickness(60,24,60,20)};
  layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(84)});
  layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(64)});
  canvas.Children.Add(layout);
  var top=new Grid();
  top.ColumnDefinitions.Add(new ColumnDefinition());
  top.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  top.ColumnDefinitions.Add(new ColumnDefinition());
  var tabs=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};
  Grid.SetColumn(tabs,1);
  top.Children.Add(tabs);
  tabs.Children.Add(Hint("LB"));
  var names=new[]{"Главная","Библиотека","Магазин"};
  for(int i=0;i<3;i++){
   var border=new Border{BorderThickness=new Thickness(0,0,0,4),BorderBrush=Brushes.Transparent,Padding=new Thickness(18,8,18,8),Margin=new Thickness(4,0,4,0),Child=new TextBlock{Text=names[i],FontSize=30,Foreground=Brushes.White}};
   tabBorders[i]=border;
   tabs.Children.Add(border);
  }
  tabs.Children.Add(Hint("RB"));
  Grid.SetColumn(clock,2);
  top.Children.Add(clock);
  layout.Children.Add(top);
  var pages=new Grid();
  Grid.SetRow(pages,1);
  layout.Children.Add(pages);
  pages.Children.Add(home);
  pages.Children.Add(library);
  pages.Children.Add(store);
  library.Visibility=Visibility.Collapsed;
  store.Children.Add(new TextBlock{Text="Магазин появится в следующей версии",FontSize=36,Foreground=Ui.B("#8F98A0"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});
  Grid.SetRow(hints,2);
  layout.Children.Add(hints);
  BuildDetails(canvas);
  home.Focused+=OnFocused;
  library.Focused+=OnFocused;
  home.Opened+=OpenDetails;
  library.Opened+=OpenDetails;
  PreviewKeyDown+=OnKey;
  Loaded+=async(_,_)=>{
   timer.Tick+=(_,_)=>Tick();
   timer.Start();
   SelectTab(0);
   games=await Library.Load();
   FillShelves();
  };
  Closed+=(_,_)=>{timer.Stop();pad.Dispose();};
 }
 void BuildDetails(Grid canvas){
  details.Children.Add(detailArt);
  details.Children.Add(new Border{IsHitTestVisible=false,Background=Ui.Gradient(new Point(0,0),new Point(1,0),255,40)});
  var panel=new StackPanel{HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(120,0,0,0)};
  panel.Children.Add(detailStatus);
  panel.Children.Add(detailTitle);
  panel.Children.Add(new Border{Background=Ui.B("#5BA32B"),CornerRadius=new CornerRadius(6),Padding=new Thickness(48,16,48,16),Margin=new Thickness(0,36,0,0),HorizontalAlignment=HorizontalAlignment.Left,Child=detailPlay});
  details.Children.Add(panel);
  details.Children.Add(new TextBlock{Text="X  Играть        B  Назад",FontSize=22,Foreground=Ui.B("#C6D4DF"),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(120,0,0,50)});
  canvas.Children.Add(details);
 }
 void SetHints(params (string Glyph,string Text)[] items){
  hints.Children.Clear();
  foreach(var (glyph,text) in items){
   var group=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,34,0)};
   group.Children.Add(new Border{MinWidth=36,Height=36,CornerRadius=new CornerRadius(18),BorderBrush=Ui.B("#66C0F4"),BorderThickness=new Thickness(2),Background=Ui.B("#1B2838"),Child=new TextBlock{Text=glyph,FontSize=18,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,8,0)}});
   group.Children.Add(new TextBlock{Text=text,FontSize=22,Foreground=Ui.B("#C6D4DF"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0)});
   hints.Children.Add(group);
  }
 }
 void SelectTab(int index){
  tab=index;
  for(int i=0;i<3;i++){
   tabBorders[i].BorderBrush=i==tab?Ui.B("#66C0F4"):Brushes.Transparent;
   ((TextBlock)tabBorders[i].Child).Foreground=i==tab?Brushes.White:Ui.B("#8F98A0");
  }
  home.Visibility=tab==0?Visibility.Visible:Visibility.Collapsed;
  library.Visibility=tab==1?Visibility.Visible:Visibility.Collapsed;
  store.Visibility=tab==2?Visibility.Visible:Visibility.Collapsed;
  if(Active?.Current is Game current)OnFocused(current);
  if(tab==2)SetHints(("LB/RB","Раздел"));
  else SetHints(("A","Открыть"),("X","Играть"),("LB/RB","Раздел"));
 }
 void FillShelves(){
  var recent=games.Where(item=>item.Installed&&item.LastPlayed>0).OrderByDescending(item=>item.LastPlayed).Take(12).ToList();
  var installed=games.Where(item=>item.Installed).OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  var others=games.Where(item=>!item.Installed).OrderByDescending(item=>item.LastPlayed).ThenBy(item=>item.Name).ToList();
  var othersByName=others.OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  home.SetRows(new[]{("Недавние",recent),("Установленные игры",installed),("Ещё в коллекции",others.Take(24).ToList())});
  library.SetRows(new[]{("Установленные",installed),("Не установленные",othersByName)});
  if(Active?.Current is Game current)OnFocused(current);
 }
 async void OnFocused(Game game){
  int mine=++version;
  var image=await Art.Hero(game.Id);
  if(mine==version)backdrop.Source=image;
 }
 async void OpenDetails(Game game){
  inDetails=true;
  detailGame=game;
  detailTitle.Text=game.Name;
  detailStatus.Text=game.Installed?"УСТАНОВЛЕНО":"В КОЛЛЕКЦИИ";
  detailPlay.Text=game.Installed?"Играть":"Установить";
  detailArt.Source=null;
  details.Visibility=Visibility.Visible;
  detailArt.Source=await Art.Hero(game.Id);
 }
 void CloseDetails(){
  inDetails=false;
  details.Visibility=Visibility.Collapsed;
 }
 void Launch(Game? game){
  if(game==null)return;
  Library.RecordPlayed(game.Id);
  try{Process.Start(new ProcessStartInfo((game.Installed?"steam://rungameid/":"steam://install/")+game.Id){UseShellExecute=true});}catch{}
 }
 void Press(int code){
  if(inDetails){
   if(code==1)CloseDetails();
   else if(code==0||code==2)Launch(detailGame);
   return;
  }
  switch(code){
   case 9:SelectTab((tab+2)%3);break;
   case 10:SelectTab((tab+1)%3);break;
   case 11:Active?.Move(0,-1);break;
   case 12:Active?.Move(0,1);break;
   case 13:Active?.Move(-1,0);break;
   case 14:Active?.Move(1,0);break;
   case 0:Active?.Open();break;
   case 2:Launch(Active?.Current);break;
  }
 }
 void OnKey(object sender,KeyEventArgs e){
  int code=e.Key switch{Key.Left=>13,Key.Right=>14,Key.Up=>11,Key.Down=>12,Key.Enter=>0,Key.Space=>2,Key.Q=>9,Key.E=>10,Key.Back=>1,_=>-1};
  if(e.Key==Key.Escape){if(inDetails)CloseDetails();else Close();e.Handled=true;return;}
  if(code>=0){Press(code);e.Handled=true;}
 }
 void Tick(){
  clock.Text=DateTime.Now.ToString("HH:mm");
  foreach(var code in pad.Tick(IsActive))Press(code);
  if(pad.ComboHeld()){comboTicks++;if(comboTicks>45)Close();}else comboTicks=0;
 }
}

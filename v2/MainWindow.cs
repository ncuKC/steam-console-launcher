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
 readonly TextBlock detailMeta=new(){FontSize=24,Foreground=Ui.B("#C6D4DF"),Margin=new Thickness(0,14,0,0)};
 readonly TextBlock detailDesc=new(){FontSize=24,Foreground=Ui.B("#C6D4DF"),TextWrapping=TextWrapping.Wrap,MaxWidth=900,MaxHeight=150,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,18,0,0)};
 readonly TextBlock detailHint=new(){FontSize=22,Foreground=Ui.B("#C6D4DF"),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(120,0,0,50)};
 readonly TextBlock chip=new(){FontSize=22,Foreground=Ui.B("#8F98A0"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,14,44,0),Visibility=Visibility.Collapsed};
 readonly Border[] tabBorders=new Border[3];
 readonly TextBlock clock=new(){FontSize=26,Foreground=Ui.B("#C6D4DF"),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right};
 readonly WrapPanel hints=new(){VerticalAlignment=VerticalAlignment.Center};
 readonly Gamepad pad=new();
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(33)};
 readonly SideMenu menu;
 readonly SearchKeyboard keyboard=new();
 List<Game> games=new();
 int tab,version,comboTicks,filter;
 string query="";
 bool inDetails,favChanged;
 Game? detailGame;
 Shelves? Active=>tab==0?home:tab==1?library:null;
 static TextBlock Hint(string text)=>new(){Text=text,FontSize=18,Foreground=Ui.B("#8F98A0"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,10,0)};
 static IEnumerable<(string Title,List<Game> Games)> Chunks(string title,List<Game> list){
  for(int i=0;i<list.Count;i+=5)yield return (i==0?title:"",list.Skip(i).Take(5).ToList());
 }
 public MainWindow(){
  Title="PLAY";
  WindowStyle=WindowStyle.None;
  WindowState=WindowState.Maximized;
  Background=Brushes.Black;
  Foreground=Brushes.White;
  Cursor=Cursors.None;
  FontFamily=new FontFamily("Segoe UI");
  library.FixedRows=true;
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
  pages.Children.Add(chip);
  library.Visibility=Visibility.Collapsed;
  store.Children.Add(new TextBlock{Text="Магазин появится в следующей версии",FontSize=36,Foreground=Ui.B("#8F98A0"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});
  Grid.SetRow(hints,2);
  layout.Children.Add(hints);
  BuildDetails(canvas);
  keyboard.Changed+=text=>{query=text;FillLibrary();};
  keyboard.FilterPressed+=CycleFilter;
  keyboard.Closed+=UpdateHints;
  canvas.Children.Add(keyboard);
  menu=new SideMenu(CloseMenu,()=>Close());
  canvas.Children.Add(menu);
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
  panel.Children.Add(detailMeta);
  panel.Children.Add(detailDesc);
  panel.Children.Add(new Border{Background=Ui.B("#5BA32B"),CornerRadius=new CornerRadius(6),Padding=new Thickness(48,16,48,16),Margin=new Thickness(0,36,0,0),HorizontalAlignment=HorizontalAlignment.Left,Child=detailPlay});
  details.Children.Add(panel);
  details.Children.Add(detailHint);
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
 void UpdateHints(){
  if(keyboard.IsOpen)SetHints(("A","Ввод"),("X","Стереть"),("Y","Пробел"),("B","Закрыть"));
  else if(menu.IsOpen)SetHints(("A","Выбрать"),("B","Назад"));
  else if(tab==2)SetHints(("≡","Меню"),("LB/RB","Раздел"));
  else SetHints(("A","Открыть"),("X","Играть"),("Y","Поиск"),("≡","Меню"),("LB/RB","Раздел"));
 }
 void CloseMenu(){
  menu.Close();
  UpdateHints();
 }
 string FilterName()=>filter==1?"Установленные":filter==2?"Не установленные":"Все";
 string FilterText()=>"Фильтр: "+FilterName();
 void CycleFilter(){
  filter=(filter+1)%3;
  keyboard.SetFilter(FilterText());
  FillLibrary();
 }
 void OpenSearch(){
  if(tab!=1)SelectTab(1);
  keyboard.Open(query,FilterText());
  UpdateHints();
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
  chip.Visibility=tab==1?Visibility.Visible:Visibility.Collapsed;
  if(Active?.Current is Game current)OnFocused(current);
  UpdateHints();
 }
 void FillShelves(){
  var favorites=games.Where(item=>Favorites.Has(item.Id)).OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  var recent=games.Where(item=>item.Installed&&item.LastPlayed>0).OrderByDescending(item=>item.LastPlayed).Take(12).ToList();
  var installed=games.Where(item=>item.Installed).OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  var others=games.Where(item=>!item.Installed).OrderByDescending(item=>item.LastPlayed).ThenBy(item=>item.Name).ToList();
  var rows=new List<(string,List<Game>)>();
  if(favorites.Count>0)rows.Add(("Избранное",favorites));
  rows.Add(("Недавние",recent));
  rows.Add(("Установленные игры",installed));
  rows.Add(("Ещё в коллекции",others.Take(24).ToList()));
  home.SetRows(rows);
  FillLibrary();
  if(Active?.Current is Game current)OnFocused(current);
 }
 void FillLibrary(){
  var visible=games.Where(item=>(filter!=1||item.Installed)&&(filter!=2||!item.Installed)&&item.Name.Contains(query,StringComparison.OrdinalIgnoreCase)).ToList();
  var installed=visible.Where(item=>item.Installed).OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  var others=visible.Where(item=>!item.Installed).OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  library.SetRows(Chunks("Установленные",installed).Concat(Chunks("Не установленные",others)));
  var text="Игр: "+visible.Count;
  if(query.Length>0)text+=" · Поиск: "+query;
  if(filter!=0)text+=" · "+FilterName();
  chip.Text=text;
  if(tab==1&&library.Current is Game current)OnFocused(current);
 }
 async void OnFocused(Game game){
  int mine=++version;
  var image=await Art.Hero(game.Id);
  if(mine==version)backdrop.Source=image;
 }
 void ShowDetailState(Game game){
  bool fav=Favorites.Has(game.Id);
  detailStatus.Text=(game.Installed?"УСТАНОВЛЕНО":"В КОЛЛЕКЦИИ")+(fav?"   ★ ИЗБРАННОЕ":"");
  detailHint.Text="A / X  "+(game.Installed?"Играть":"Установить")+"        Y  "+(fav?"Убрать из избранного":"В избранное")+"        B  Назад";
 }
 async void OpenDetails(Game game){
  inDetails=true;
  detailGame=game;
  detailTitle.Text=game.Name;
  detailMeta.Text="";
  detailDesc.Text="";
  detailPlay.Text=game.Installed?"Играть":"Установить";
  ShowDetailState(game);
  detailArt.Source=null;
  details.Visibility=Visibility.Visible;
  var infoTask=Details.Get(game.Id);
  detailArt.Source=await Art.Hero(game.Id);
  var info=await infoTask;
  if(info!=null&&inDetails&&detailGame==game){
   var parts=new[]{info.Genres,info.Developer,info.Year}.Where(part=>part.Length>0);
   detailMeta.Text=string.Join(" · ",parts);
   detailDesc.Text=info.Description;
  }
 }
 void CloseDetails(){
  inDetails=false;
  details.Visibility=Visibility.Collapsed;
  if(favChanged){favChanged=false;FillShelves();}
 }
 void Launch(Game? game){
  if(game==null)return;
  Library.RecordPlayed(game.Id);
  try{Process.Start(new ProcessStartInfo((game.Installed?"steam://rungameid/":"steam://install/")+game.Id){UseShellExecute=true});}catch{}
 }
 void Press(int code){
  if(keyboard.IsOpen){
   if(code==11)keyboard.Move(0,-1);
   else if(code==12)keyboard.Move(0,1);
   else if(code==13)keyboard.Move(-1,0);
   else if(code==14)keyboard.Move(1,0);
   else if(code==0)keyboard.Press();
   else if(code==2)keyboard.Backspace();
   else if(code==3)keyboard.Space();
   else if(code==1||code==6)keyboard.Close();
   return;
  }
  if(menu.IsOpen){
   if(code==11)menu.Move(-1);
   else if(code==12)menu.Move(1);
   else if(code==0)menu.Activate();
   else if(code==1||code==6)CloseMenu();
   return;
  }
  if(code==6){menu.Open();UpdateHints();return;}
  if(inDetails){
   if(code==1)CloseDetails();
   else if(code==0||code==2)Launch(detailGame);
   else if(code==3&&detailGame!=null){
    Favorites.Toggle(detailGame.Id);
    favChanged=true;
    ShowDetailState(detailGame);
   }
   return;
  }
  switch(code){
   case 3:OpenSearch();break;
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
  int code=e.Key switch{Key.Left=>13,Key.Right=>14,Key.Up=>11,Key.Down=>12,Key.Enter=>0,Key.Space=>2,Key.Q=>9,Key.E=>10,Key.Back=>1,Key.Tab=>6,Key.F=>3,_=>-1};
  if(e.Key==Key.Escape){
   if(keyboard.IsOpen)keyboard.Close();
   else if(menu.IsOpen)CloseMenu();
   else if(inDetails)CloseDetails();
   else Close();
   e.Handled=true;
   return;
  }
  if(code>=0){Press(code);e.Handled=true;}
 }
 void Tick(){
  clock.Text=DateTime.Now.ToString("HH:mm");
  foreach(var code in pad.Tick(IsActive))Press(code);
  if(pad.ComboHeld()){comboTicks++;if(comboTicks>45)Close();}else comboTicks=0;
 }
}

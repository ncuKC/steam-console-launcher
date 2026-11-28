using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
namespace ConsoleLauncher;
public sealed class MainWindow:Window{
 static readonly string[] FilterNames={"\u0412\u0441\u0435","\u0423\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043d\u044b\u0435","\u041d\u0435 \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043d\u044b\u0435","\u0418\u0437\u0431\u0440\u0430\u043d\u043d\u043e\u0435","\u041d\u0435\u0434\u0430\u0432\u043d\u0438\u0435"};
 static readonly string[] StoreNames={"\u0412\u0441\u0435 \u0438\u0433\u0440\u044b","Steam","Epic Games","GOG","\u0421\u0432\u043e\u0438"};
 static readonly string[] TabNames={"\u0413\u043b\u0430\u0432\u043d\u0430\u044f","\u0411\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430","\u041c\u0430\u0433\u0430\u0437\u0438\u043d"};
 readonly Image backdrop=new(){Stretch=Stretch.UniformToFill};
 readonly Image backdropFront=new(){Stretch=Stretch.UniformToFill,Opacity=0};
 readonly DispatcherTimer slideTimer=new(){Interval=TimeSpan.FromSeconds(9)};
 readonly Sections sections=new();
 readonly Shelves library=new();
 readonly Grid store=new(){Visibility=Visibility.Collapsed};
 readonly Grid details=new(){Visibility=Visibility.Collapsed,Background=Ui.B("#0B0F15")};
 readonly Image detailArt=new(){Stretch=Stretch.UniformToFill};
 readonly TextBlock detailTitle=new(){FontSize=68,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap,MaxWidth=1100};
 readonly TextBlock detailStatus=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,10)};
 readonly TextBlock detailPlay=new(){FontSize=28,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly TextBlock detailMeta=new(){FontSize=24,Foreground=Ui.B("#C6D4DF"),Margin=new Thickness(0,14,0,0)};
 readonly TextBlock detailStats=new(){FontSize=24,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,10,0,0)};
 readonly TextBlock detailDesc=new(){FontSize=24,Foreground=Ui.B("#C6D4DF"),TextWrapping=TextWrapping.Wrap,MaxWidth=900,MaxHeight=150,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,18,0,0)};
 readonly TextBlock detailHint=new(){FontSize=22,Foreground=Ui.B("#C6D4DF"),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(120,0,0,50)};
 readonly TextBlock chip=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,16,44,0),Visibility=Visibility.Collapsed};
 readonly TextBlock screenName=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center};
 readonly TextBlock tabDots=new(){FontSize=14,Foreground=Ui.B("#66C0F4"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(20,0,0,0)};
 readonly TextBlock clock=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right};
 readonly WrapPanel hints=new(){VerticalAlignment=VerticalAlignment.Center};
 readonly Gamepad pad=new();
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(33)};
 readonly SideMenu menu;
 readonly SearchKeyboard keyboard=new();
 readonly ListPopup popup=new();
 List<Game> games=new();
 List<Game> libraryOrder=new();
 List<string> letters=new();
 List<int> sectionIds=new();
 List<ImageSource> slides=new();
 int tab,version,comboTicks,filter,sort=1,letterIndex,pollTicks,watchTicks,source,slideIndex;
 bool reverse,watchSeen,keyMode;
 string? watchId;
 string query="";
 string pendingKey="";
 bool inDetails,favChanged;
 Game? detailGame;
 Shelves? Active=>tab==1?library:null;
 static IEnumerable<(string Title,List<Game> Games)> Chunks(string title,List<Game> list){
  if(list.Count>0)yield return (title,list);
 }
 static (Func<string>,Action<int>) Item(Func<string> text,Action<int> act)=>(text,act);
 static int Step(int direction)=>direction==0?1:direction;
 static string Letter(Game game)=>game.Name.Length>0?char.ToUpperInvariant(game.Name[0]).ToString():"#";
 static bool Matches(Game item,int mode)=>mode==0||(mode==1&&item.Installed)||(mode==2&&!item.Installed)||(mode==3&&Favorites.Has(item.Id))||(mode==4&&item.Installed&&item.LastPlayed>0);
 static void Appear(UIElement element,double dx,double dy){
  var move=new TranslateTransform(dx,dy);
  element.RenderTransform=move;
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(220)));
  if(dx!=0)move.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(dx,0,TimeSpan.FromMilliseconds(280)){EasingFunction=ease});
  if(dy!=0)move.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(dy,0,TimeSpan.FromMilliseconds(280)){EasingFunction=ease});
 }
 static void PlayFor(int code){
  if((code>=11&&code<=14)||code==9||code==10)Sound.Play("move");
  else if(code==0||code==2)Sound.Play("select");
  else if(code==1)Sound.Play("back");
  else if(code==3||code==6)Sound.Play("open");
 }
 static string Describe(PlayInfo? info){
  if(info==null)return "\u0412\u0440\u0435\u043c\u044f \u0432 \u0438\u0433\u0440\u0435: \u043d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445";
  int minutes=info.Minutes;
  var time=minutes<=0?"\u0435\u0449\u0451 \u043d\u0435 \u0438\u0433\u0440\u0430\u043b\u0438":minutes<60?minutes+" \u043c\u0438\u043d":(minutes/60)+" \u0447 "+(minutes%60)+" \u043c\u0438\u043d";
  var cloud=info.Cloud switch{
   "synchronized"=>"\u0441\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u0438\u044f \u0441\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u043d\u044b",
   "conflict"=>"\u043a\u043e\u043d\u0444\u043b\u0438\u043a\u0442 \u0441\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u0438\u0439",
   ""=>"\u043e\u0431\u043b\u0430\u043a\u043e: \u043d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445",
   "uploading" or "downloading" or "inprogress"=>"\u0438\u0434\u0451\u0442 \u0441\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0430\u0446\u0438\u044f",
   _=>"\u043e\u0431\u043b\u0430\u043a\u043e: "+info.Cloud
  };
  return "\u0412\u0440\u0435\u043c\u044f \u0432 \u0438\u0433\u0440\u0435: "+time+"   \u00b7   \u2601 "+cloud;
 }
 public MainWindow(){
  Sound.Load();
  Art.LoadKey();
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
  canvas.Children.Add(backdropFront);
  canvas.Children.Add(new Border{IsHitTestVisible=false,Width=1300,HorizontalAlignment=HorizontalAlignment.Left,Background=Ui.Gradient(new Point(0,0),new Point(1,0),210,0)});
  canvas.Children.Add(new Border{IsHitTestVisible=false,Height=620,VerticalAlignment=VerticalAlignment.Bottom,Background=Ui.Gradient(new Point(0,0),new Point(0,1),0,245)});
  var layout=new Grid();
  layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(56)});
  layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});
  canvas.Children.Add(layout);
  var top=new Grid{Margin=new Thickness(40,0,40,0)};
  var nameBox=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left};
  nameBox.Children.Add(screenName);
  nameBox.Children.Add(tabDots);
  top.Children.Add(nameBox);
  top.Children.Add(clock);
  layout.Children.Add(new Border{Background=Ui.B("#B3000000"),Child=top});
  var pages=new Grid();
  Grid.SetRow(pages,1);
  layout.Children.Add(pages);
  pages.Children.Add(sections);
  pages.Children.Add(library);
  pages.Children.Add(store);
  pages.Children.Add(chip);
  library.Visibility=Visibility.Collapsed;
  store.Children.Add(new TextBlock{Text="\u041c\u0430\u0433\u0430\u0437\u0438\u043d \u043f\u043e\u044f\u0432\u0438\u0442\u0441\u044f \u0432 \u0441\u043b\u0435\u0434\u0443\u044e\u0449\u0435\u0439 \u0432\u0435\u0440\u0441\u0438\u0438",FontSize=36,Foreground=Ui.B("#8F98A0"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});
  var bottomBar=new Border{Background=Ui.B("#B3000000"),Padding=new Thickness(40,0,40,0),Child=hints};
  Grid.SetRow(bottomBar,2);
  layout.Children.Add(bottomBar);
  BuildDetails(canvas);
  keyboard.Changed+=text=>{
   if(keyMode){pendingKey=text;return;}
   query=text;
   FillLibrary();
  };
  keyboard.FilterPressed+=()=>{if(!keyMode)CycleFilter();};
  keyboard.Closed+=()=>{
   if(keyMode){
    keyMode=false;
    if(pendingKey.Length>0){
     Art.SetKey(pendingKey);
     if(tab==1&&library.Current is Game current)OnFocused(current);
     else if(tab==0)sections.Refocus();
    }
   }
   UpdateHints();
  };
  canvas.Children.Add(keyboard);
  popup.Closed+=UpdateHints;
  canvas.Children.Add(popup);
  menu=new SideMenu(CloseMenu,()=>Close());
  canvas.Children.Add(menu);
  sections.Focused+=game=>{if(game!=null)OnFocused(game);};
  sections.Opened+=index=>{if(index>=0&&index<sectionIds.Count)OpenSection(sectionIds[index]);};
  library.Focused+=OnFocused;
  library.Opened+=OpenDetails;
  PreviewKeyDown+=OnKey;
  slideTimer.Tick+=(_,_)=>NextSlide();
  Loaded+=async(_,_)=>{
   timer.Tick+=(_,_)=>Tick();
   timer.Start();
   SelectTab(0);
   games=await Library.Load();
   FillShelves();
  };
  Closed+=(_,_)=>{timer.Stop();slideTimer.Stop();pad.Dispose();};
 }
 void BuildDetails(Grid canvas){
  details.Children.Add(detailArt);
  details.Children.Add(new Border{IsHitTestVisible=false,Width=1300,HorizontalAlignment=HorizontalAlignment.Left,Background=Ui.Gradient(new Point(0,0),new Point(1,0),235,0)});
  var panel=new StackPanel{HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(120,0,0,0)};
  panel.Children.Add(detailStatus);
  panel.Children.Add(detailTitle);
  panel.Children.Add(detailMeta);
  panel.Children.Add(detailStats);
  panel.Children.Add(detailDesc);
  panel.Children.Add(new Border{Background=Ui.B("#5BA32B"),CornerRadius=new CornerRadius(6),Padding=new Thickness(48,16,48,16),Margin=new Thickness(0,36,0,0),HorizontalAlignment=HorizontalAlignment.Left,Child=detailPlay});
  details.Children.Add(panel);
  details.Children.Add(detailHint);
  canvas.Children.Add(details);
 }
 void SetHints(params (string Glyph,string Text)[] items){
  hints.Children.Clear();
  foreach(var (glyph,text) in items){
   var group=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,30,0)};
   group.Children.Add(new Border{MinWidth=28,Height=28,CornerRadius=new CornerRadius(14),BorderBrush=Brushes.White,BorderThickness=new Thickness(1.5),Child=new TextBlock{Text=glyph,FontSize=14,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,6,0)}});
   group.Children.Add(new TextBlock{Text=text,FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,0,0)});
   hints.Children.Add(group);
  }
 }
 void UpdateHints(){
  if(keyboard.IsOpen)SetHints(("A","\u0412\u0432\u043e\u0434"),("X","\u0421\u0442\u0435\u0440\u0435\u0442\u044c"),("Y","\u041f\u0440\u043e\u0431\u0435\u043b"),("B","\u0417\u0430\u043a\u0440\u044b\u0442\u044c"));
  else if(popup.IsOpen)SetHints(("A","\u0412\u044b\u0431\u0440\u0430\u0442\u044c"),("\u25c4\u25ba","\u0418\u0437\u043c\u0435\u043d\u0438\u0442\u044c"),("B","\u0417\u0430\u043a\u0440\u044b\u0442\u044c"));
  else if(menu.IsOpen)SetHints(("A","\u0412\u044b\u0431\u0440\u0430\u0442\u044c"),("B","\u041d\u0430\u0437\u0430\u0434"));
  else if(tab==2)SetHints(("\u2261","\u041c\u0435\u043d\u044e"),("LB/RB","\u0412\u043a\u043b\u0430\u0434\u043a\u0430"));
  else if(tab==0)SetHints(("A","\u041e\u0442\u043a\u0440\u044b\u0442\u044c"),("\u2261","\u041c\u0435\u043d\u044e"),("LB/RB","\u0412\u043a\u043b\u0430\u0434\u043a\u0430"));
  else SetHints(("A","\u041e\u0442\u043a\u0440\u044b\u0442\u044c"),("X","\u0418\u0433\u0440\u0430\u0442\u044c"),("Y","\u0421\u043f\u0438\u0441\u043e\u043a"),("B","\u0420\u0430\u0437\u0434\u0435\u043b\u044b"),("\u2261","\u041c\u0435\u043d\u044e"));
 }
 void CloseMenu(){
  menu.Close();
  UpdateHints();
 }
 string FilterName()=>FilterNames[filter];
 string FilterText()=>"\u0424\u0438\u043b\u044c\u0442\u0440: "+FilterName();
 string SortName()=>sort==1?"\u043f\u043e\u0441\u043b\u0435\u0434\u043d\u0438\u0439 \u0437\u0430\u043f\u0443\u0441\u043a":sort==2?"\u0438\u0437\u0431\u0440\u0430\u043d\u043d\u044b\u0435 \u0441\u0432\u0435\u0440\u0445\u0443":"\u043d\u0430\u0437\u0432\u0430\u043d\u0438\u0435";
 void CycleFilter(){
  filter=(filter+1)%FilterNames.Length;
  keyboard.SetFilter(FilterText());
  FillLibrary();
 }
 void OpenSection(int index){
  source=index;
  filter=0;
  query="";
  sort=1;
  reverse=false;
  keyboard.SetFilter(FilterText());
  FillLibrary();
  SelectTab(1);
 }
 void OpenSearch(){
  if(tab!=1)SelectTab(1);
  keyMode=false;
  keyboard.Open(query,FilterText());
  Appear(keyboard,0,40);
  UpdateHints();
 }
 void OpenKeyEntry(){
  keyMode=true;
  pendingKey="";
  keyboard.Open("","SteamGridDB API key");
  Appear(keyboard,0,40);
  UpdateHints();
 }
 void OpenListMenu(){
  if(tab!=1)SelectTab(1);
  popup.SetItems(new[]{
   Item(()=>query.Length>0?"\u041f\u043e\u0438\u0441\u043a: "+query:"\u041f\u043e\u0438\u0441\u043a",_=>{popup.Close();OpenSearch();}),
   Item(()=>"\u041b\u0430\u0443\u043d\u0447\u0435\u0440: "+StoreNames[source],direction=>{source=(source+Step(direction)+StoreNames.Length)%StoreNames.Length;FillLibrary();}),
   Item(()=>"\u0421\u043e\u0440\u0442\u0438\u0440\u043e\u0432\u043a\u0430: "+SortName(),direction=>{sort=(sort+Step(direction)+3)%3;FillLibrary();}),
   Item(()=>"\u041f\u043e\u0440\u044f\u0434\u043e\u043a: "+(reverse?"\u043e\u0431\u0440\u0430\u0442\u043d\u044b\u0439":"\u043f\u0440\u044f\u043c\u043e\u0439"),_=>{reverse=!reverse;FillLibrary();}),
   Item(()=>FilterText(),direction=>{filter=(filter+Step(direction)+FilterNames.Length)%FilterNames.Length;keyboard.SetFilter(FilterText());FillLibrary();}),
   Item(()=>"\u041f\u0435\u0440\u0435\u0439\u0442\u0438 \u043a \u0431\u0443\u043a\u0432\u0435: "+(letters.Count>0?letters[letterIndex]:"-"),direction=>{
    if(letters.Count==0)return;
    if(direction==0){
     var hit=libraryOrder.FirstOrDefault(game=>Letter(game)==letters[letterIndex]);
     popup.Close();
     if(hit!=null)library.Select(hit.Id);
    }else letterIndex=(letterIndex+direction+letters.Count)%letters.Count;
   }),
   Item(()=>"\u0417\u0432\u0443\u043a\u0438: "+(Sound.Enabled?"\u0432\u043a\u043b":"\u0432\u044b\u043a\u043b"),_=>Sound.Toggle()),
   Item(()=>"\u0424\u043e\u043d\u044b SteamGridDB: "+(Art.HasKey?"\u043a\u043b\u044e\u0447 \u0437\u0430\u0434\u0430\u043d":"\u043d\u0443\u0436\u0435\u043d \u043a\u043b\u044e\u0447"),_=>{popup.Close();OpenKeyEntry();}),
   Item(()=>"\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u043f\u0430\u043f\u043a\u0443 \u00ab\u0421\u0432\u043e\u0438\u00bb",_=>{popup.Close();Stores.OpenOwn();}),
   Item(()=>"\u0421\u0431\u0440\u043e\u0441\u0438\u0442\u044c \u043f\u043e\u0438\u0441\u043a \u0438 \u0444\u0438\u043b\u044c\u0442\u0440",_=>{query="";filter=0;source=0;sort=1;reverse=false;keyboard.SetFilter(FilterText());FillLibrary();})
  });
  popup.Open();
  Appear(popup,0,30);
  UpdateHints();
 }
 void SelectTab(int index){
  int previous=tab;
  tab=index;
  screenName.Text=TabNames[tab];
  tabDots.Text=string.Join("  ",Enumerable.Range(0,3).Select(i=>i==tab?"\u25cf":"\u25cb"));
  sections.Visibility=tab==0?Visibility.Visible:Visibility.Collapsed;
  library.Visibility=tab==1?Visibility.Visible:Visibility.Collapsed;
  store.Visibility=tab==2?Visibility.Visible:Visibility.Collapsed;
  chip.Visibility=tab==1?Visibility.Visible:Visibility.Collapsed;
  if(index!=previous){
   UIElement page=tab==0?(UIElement)sections:tab==1?(UIElement)library:store;
   Appear(page,index>previous?60:-60,0);
  }
  if(tab==0)sections.Refocus();
  else if(tab==1&&library.Current is Game current)OnFocused(current);
  UpdateHints();
 }
 void FillShelves(){
  var items=new List<(string,int,Game?)>();
  sectionIds.Clear();
  for(int i=0;i<StoreNames.Length;i++){
   var list=games.Where(item=>i==0||item.Store==StoreNames[i]).ToList();
   var sample=list.FirstOrDefault(item=>item.Store=="Steam")??list.FirstOrDefault();
   items.Add((StoreNames[i],list.Count,sample));
   sectionIds.Add(i);
  }
  sections.Set(items);
  FillLibrary();
 }
 List<Game> Sort(List<Game> list){
  var name=StringComparer.CurrentCultureIgnoreCase;
  IEnumerable<Game> result=sort==1?list.OrderByDescending(item=>item.LastPlayed).ThenBy(item=>item.Name,name):sort==2?list.OrderByDescending(item=>Favorites.Has(item.Id)).ThenBy(item=>item.Name,name):list.OrderBy(item=>item.Name,name);
  var items=result.ToList();
  if(reverse)items.Reverse();
  return items;
 }
 void FillLibrary(){
  var visible=games.Where(item=>(source==0||item.Store==StoreNames[source])&&Matches(item,filter)&&item.Name.Contains(query,StringComparison.OrdinalIgnoreCase)).ToList();
  var installed=Sort(visible.Where(item=>item.Installed).ToList());
  var others=Sort(visible.Where(item=>!item.Installed).ToList());
  libraryOrder=installed.Concat(others).ToList();
  letters=libraryOrder.Select(Letter).Distinct().OrderBy(item=>item,StringComparer.CurrentCultureIgnoreCase).ToList();
  letterIndex=Math.Clamp(letterIndex,0,Math.Max(0,letters.Count-1));
  library.EmptyHint=source==4?"\u041f\u043e\u043b\u043e\u0436\u0438\u0442\u0435 \u044f\u0440\u043b\u044b\u043a \u0438\u043b\u0438 exe \u0432 \u043f\u0430\u043f\u043a\u0443 \u00ab\u0421\u0432\u043e\u0438\u00bb: Y \u2192 \u041e\u0442\u043a\u0440\u044b\u0442\u044c \u043f\u0430\u043f\u043a\u0443 \u00ab\u0421\u0432\u043e\u0438\u00bb.":source==2?"\u0417\u0434\u0435\u0441\u044c \u043f\u043e\u044f\u0432\u043b\u044f\u044e\u0442\u0441\u044f \u0438\u0433\u0440\u044b, \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043d\u044b\u0435 \u0447\u0435\u0440\u0435\u0437 Epic Games Launcher.":source==3?"\u0417\u0434\u0435\u0441\u044c \u043f\u043e\u044f\u0432\u043b\u044f\u044e\u0442\u0441\u044f \u0438\u0433\u0440\u044b, \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043d\u044b\u0435 \u0438\u0437 GOG.":"\u0418\u0437\u043c\u0435\u043d\u0438\u0442\u0435 \u043f\u043e\u0438\u0441\u043a \u0438\u043b\u0438 \u0444\u0438\u043b\u044c\u0442\u0440.";
  library.SetRows(Chunks("\u0423\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043d\u044b\u0435",installed).Concat(Chunks("\u041d\u0435 \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043d\u044b\u0435",others)));
  var text="\u0418\u0433\u0440: "+visible.Count;
  if(source!=0)text+=" \u00b7 "+StoreNames[source];
  if(query.Length>0)text+=" \u00b7 \u041f\u043e\u0438\u0441\u043a: "+query;
  if(filter!=0)text+=" \u00b7 "+FilterName();
  if(sort!=1||reverse)text+=" \u00b7 "+SortName();
  chip.Text=text;
  if(tab==1&&library.Current is Game current)OnFocused(current);
 }
 void Crossfade(ImageSource? image,int mine){
  var fade=new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(600));
  fade.Completed+=(_,_)=>{if(mine==version)backdrop.Source=image;};
  backdropFront.Source=image;
  backdropFront.BeginAnimation(UIElement.OpacityProperty,fade);
 }
 void NextSlide(){
  if(slides.Count<2)return;
  slideIndex=(slideIndex+1)%slides.Count;
  Crossfade(slides[slideIndex],version);
 }
 async void OnFocused(Game game){
  int mine=++version;
  slideTimer.Stop();
  slides=new List<ImageSource>();
  var image=await Art.Hero(game.Id);
  if(mine!=version)return;
  Crossfade(image,mine);
  if(image!=null)slides.Add(image);
  await Task.Delay(1200);
  if(mine!=version)return;
  var arts=await Art.Arts(game.Id);
  if(mine!=version)return;
  slides.AddRange(arts);
  slideIndex=image!=null?0:-1;
  if(slides.Count>1)slideTimer.Start();
 }
 void ShowDetailState(Game game){
  bool fav=Favorites.Has(game.Id);
  detailStatus.Text=(game.Installed?"\u0423\u0421\u0422\u0410\u041d\u041e\u0412\u041b\u0415\u041d\u041e":"\u0412 \u041a\u041e\u041b\u041b\u0415\u041a\u0426\u0418\u0418")+" \u00b7 "+game.Store.ToUpperInvariant()+(fav?"   \u2605 \u0418\u0417\u0411\u0420\u0410\u041d\u041d\u041e\u0415":"");
  detailHint.Text="A / X  "+(game.Installed?"\u0418\u0433\u0440\u0430\u0442\u044c":"\u0423\u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u044c")+"        Y  "+(fav?"\u0423\u0431\u0440\u0430\u0442\u044c \u0438\u0437 \u0438\u0437\u0431\u0440\u0430\u043d\u043d\u043e\u0433\u043e":"\u0412 \u0438\u0437\u0431\u0440\u0430\u043d\u043d\u043e\u0435")+"        B  \u041d\u0430\u0437\u0430\u0434";
 }
 void AnimateDetailsIn(){
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  var grow=new ScaleTransform(1.04,1.04);
  details.RenderTransformOrigin=new Point(.5,.5);
  details.RenderTransform=grow;
  details.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(220)));
  grow.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(1.04,1,TimeSpan.FromMilliseconds(280)){EasingFunction=ease});
  grow.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(1.04,1,TimeSpan.FromMilliseconds(280)){EasingFunction=ease});
 }
 async void OpenDetails(Game game){
  bool steam=game.Store=="Steam";
  inDetails=true;
  detailGame=game;
  detailTitle.Text=game.Name;
  detailMeta.Text=steam?"":game.Store;
  detailStats.Text="";
  detailDesc.Text="";
  detailPlay.Text=game.Installed?"\u0418\u0433\u0440\u0430\u0442\u044c":"\u0423\u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u044c";
  ShowDetailState(game);
  detailArt.Source=null;
  details.Visibility=Visibility.Visible;
  AnimateDetailsIn();
  var infoTask=steam?Details.Get(game.Id):Task.FromResult<GameInfo?>(null);
  var statsTask=steam?Stats.Get(game.Id):Task.FromResult<PlayInfo?>(null);
  detailArt.Source=await Art.Hero(game.Id);
  var stats=await statsTask;
  if(steam&&inDetails&&detailGame==game)detailStats.Text=Describe(stats);
  var info=await infoTask;
  if(info!=null&&inDetails&&detailGame==game){
   var parts=new[]{info.Genres,info.Developer,info.Year}.Where(part=>part.Length>0);
   detailMeta.Text=string.Join(" \u00b7 ",parts);
   detailDesc.Text=info.Description;
  }
 }
 void CloseDetails(){
  inDetails=false;
  var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(150));
  fade.Completed+=(_,_)=>{if(!inDetails)details.Visibility=Visibility.Collapsed;};
  details.BeginAnimation(UIElement.OpacityProperty,fade);
  if(favChanged){favChanged=false;FillShelves();}
 }
 void Launch(Game? game){
  if(game==null)return;
  Library.RecordPlayed(game.Id);
  if(game.Store!="Steam"){
   Stores.Start(game);
   return;
  }
  try{Process.Start(new ProcessStartInfo((game.Installed?"steam://rungameid/":"steam://install/")+game.Id){UseShellExecute=true});}catch{return;}
  if(game.Installed){
   watchId=game.Id;
   watchSeen=false;
   watchTicks=0;
   pollTicks=0;
  }
 }
 async void ReturnFromGame(){
  WindowState=WindowState.Maximized;
  Activate();
  Topmost=true;
  Topmost=false;
  games=await Library.Load();
  FillShelves();
 }
 void Press(int code){
  PlayFor(code);
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
  if(popup.IsOpen){
   if(code==11)popup.Move(-1);
   else if(code==12)popup.Move(1);
   else if(code==13)popup.Act(-1);
   else if(code==14)popup.Act(1);
   else if(code==0)popup.Act(0);
   else if(code==1||code==3||code==6)popup.Close();
   return;
  }
  if(menu.IsOpen){
   if(code==11)menu.Move(-1);
   else if(code==12)menu.Move(1);
   else if(code==0)menu.Activate();
   else if(code==1||code==6)CloseMenu();
   return;
  }
  if(code==6){menu.Open();Appear(menu,-60,0);UpdateHints();return;}
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
   case 1:if(tab==1)SelectTab(0);break;
   case 3:if(tab!=2)OpenListMenu();break;
   case 9:SelectTab((tab+2)%3);break;
   case 10:SelectTab((tab+1)%3);break;
   case 11:Active?.Move(0,-1);break;
   case 12:Active?.Move(0,1);break;
   case 13:if(tab==0)sections.Move(-1);else Active?.Move(-1,0);break;
   case 14:if(tab==0)sections.Move(1);else Active?.Move(1,0);break;
   case 0:if(tab==0)sections.Open();else Active?.Open();break;
   case 2:Launch(Active?.Current);break;
  }
 }
 void OnKey(object sender,KeyEventArgs e){
  int code=e.Key switch{Key.Left=>13,Key.Right=>14,Key.Up=>11,Key.Down=>12,Key.Enter=>0,Key.Space=>2,Key.Q=>9,Key.E=>10,Key.Back=>1,Key.Tab=>6,Key.F=>3,_=>-1};
  if(e.Key==Key.Escape){
   if(keyboard.IsOpen)keyboard.Close();
   else if(popup.IsOpen)popup.Close();
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
  if(watchId!=null&&++pollTicks>=30){
   pollTicks=0;
   var raw=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","RunningAppID",0);
   int running=raw is int value?value:0;
   if(running.ToString()==watchId){
    if(!watchSeen){watchSeen=true;WindowState=WindowState.Minimized;}
   }else if(watchSeen){
    watchId=null;
    watchSeen=false;
    ReturnFromGame();
   }else if(++watchTicks>90)watchId=null;
  }
 }
}

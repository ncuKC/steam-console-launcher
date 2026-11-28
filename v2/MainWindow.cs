using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;
using Shapes=System.Windows.Shapes;
namespace ConsoleLauncher;
public sealed class MainWindow:Window{
 static readonly string[] FilterNames={"Все","Установленные","Не установленные","Избранное","Недавние"};
 static readonly string[] StoreNames={"Все игры","Steam","Epic Games","GOG","Свои","Избранное"};
 static readonly string[] TabNames={"Главная","Библиотека","Магазин"};
 readonly Border scene=new(){Visibility=Visibility.Collapsed};
 readonly Image backdrop=new(){Stretch=Stretch.UniformToFill};
 readonly Image backdropFront=new(){Stretch=Stretch.UniformToFill,Opacity=0};
 readonly Border dim=new(){Background=Brushes.Black,Opacity=0,IsHitTestVisible=false};
 readonly Border syncToast=new(){Background=Ui.B("#E61B2838"),CornerRadius=new CornerRadius(12),Padding=new Thickness(20,12,20,12),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,76,40,0),Visibility=Visibility.Collapsed};
 readonly TextBlock syncText=new(){FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly DispatcherTimer slideTimer=new(){Interval=TimeSpan.FromSeconds(9)};
 readonly Grid splash=new(){Background=Ui.B("#06090E")};
 readonly ScaleTransform splashGlow=new(.85,.85);
 readonly ScaleTransform splashLogo=new(.7,.7);
 readonly TranslateTransform splashTitle=new(0,24);
 readonly TranslateTransform splashBar=new(-150,0);
 Shapes.Ellipse? glowEl;
 Viewbox? logoEl;
 TextBlock? titleEl,subEl;
 readonly Grid saverRoot=new(){Background=Brushes.Black,Visibility=Visibility.Collapsed,Opacity=0,IsHitTestVisible=false};
 readonly Image saverA=new(){Stretch=Stretch.UniformToFill,Opacity=0};
 readonly Image saverB=new(){Stretch=Stretch.UniformToFill,Opacity=0};
 readonly ScaleTransform scaleA=new(1,1);
 readonly ScaleTransform scaleB=new(1,1);
 readonly TranslateTransform moveA=new();
 readonly TranslateTransform moveB=new();
 readonly TextBlock saverTitle=new(){FontSize=34,Foreground=Brushes.White,Opacity=.85,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(80,0,0,70)};
 readonly TextBlock saverClock=new(){FontSize=84,FontWeight=FontWeights.Light,Foreground=Brushes.White,Opacity=.9,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,80,50)};
 readonly Sections sections=new();
 readonly Shelves library=new();
 readonly Grid store=new(){Visibility=Visibility.Collapsed};
 readonly Grid details=new(){Visibility=Visibility.Collapsed,Background=Ui.B("#0B0F15")};
 readonly Image detailArt=new(){Stretch=Stretch.UniformToFill,Opacity=0,RenderTransformOrigin=new Point(.5,.5)};
 readonly ScaleTransform artZoom=new(1,1);
 readonly List<ImageSource> gallery=new();
 readonly Border galleryBadge=new(){CornerRadius=new CornerRadius(14),Padding=new Thickness(18,8,18,8),Background=Ui.B("#99000000"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,60,70),Visibility=Visibility.Collapsed};
 readonly TextBlock galleryText=new(){FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly Border detailCard=new(){Width=1000,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(100,0,0,0),Padding=new Thickness(48,40,48,40),CornerRadius=new CornerRadius(24),Background=new LinearGradientBrush(Color.FromArgb(0xE6,0x18,0x25,0x34),Color.FromArgb(0xD0,0x0B,0x11,0x19),60),BorderBrush=new SolidColorBrush(Color.FromArgb(0x44,255,255,255)),BorderThickness=new Thickness(1),Effect=new DropShadowEffect{BlurRadius=70,ShadowDepth=0,Opacity=.8,Color=Colors.Black}};
 readonly TranslateTransform cardShift=new(-70,0);
 readonly StackPanel detailBody=new();
 readonly Border statusChip=new(){CornerRadius=new CornerRadius(14),Padding=new Thickness(14,5,14,5),Margin=new Thickness(0,0,10,0)};
 readonly Border favChip=new(){CornerRadius=new CornerRadius(14),Padding=new Thickness(14,5,14,5),Margin=new Thickness(0,0,10,0),Background=Ui.B("#CC8A6D1B"),Visibility=Visibility.Collapsed,Child=new TextBlock{Text="★ ИЗБРАННОЕ",FontSize=16,FontWeight=FontWeights.Bold,Foreground=Ui.B("#FFE08A")}};
 readonly Border padChip=new(){CornerRadius=new CornerRadius(14),Padding=new Thickness(14,5,14,5),Visibility=Visibility.Collapsed};
 readonly TextBlock detailPad=new(){FontSize=16,FontWeight=FontWeights.Bold};
 readonly TextBlock detailStatus=new(){FontSize=16,FontWeight=FontWeights.Bold};
 readonly TextBlock detailStore=new(){FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#C6D4DF")};
 readonly TextBlock detailTitle=new(){FontSize=64,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,18,0,0)};
 readonly TextBlock detailMeta=new(){FontSize=22,Foreground=Ui.B("#9FB3C8"),Margin=new Thickness(0,6,0,0),Visibility=Visibility.Collapsed};
 readonly TextBlock detailReviews=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,Margin=new Thickness(0,8,0,0),Visibility=Visibility.Collapsed};
 readonly TextBlock detailNews=new(){FontSize=20,Foreground=Ui.B("#9FB3C8"),TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,6,0,0),Visibility=Visibility.Collapsed};
 readonly Grid detailTiles=new(){Margin=new Thickness(0,24,0,0)};
 readonly Grid detailTiles2=new(){Margin=new Thickness(0,12,0,0)};
 readonly TextBlock detailStats=new(){FontSize=26,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly TextBlock detailSync=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),TextWrapping=TextWrapping.Wrap};
 readonly TextBlock detailLast=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly TextBlock detailSize=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly TextBlock detailState=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White};
 readonly TextBlock detailDesc=new(){FontSize=22,Foreground=Ui.B("#C6D4DF"),TextWrapping=TextWrapping.Wrap,MaxHeight=140,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(0,22,0,0),Visibility=Visibility.Collapsed};
 readonly TextBlock detailPlay=new(){FontSize=26,FontWeight=FontWeights.Bold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(14,0,0,0)};
 readonly TextBlock detailFavLabel=new(){FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0)};
 readonly (Border Box,TextBlock Text) badgeA=MakeBadge();
 readonly (Border Box,TextBlock Text) badgeY=MakeBadge();
 readonly (Border Box,TextBlock Text) badgeB=MakeBadge();
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
 int tab,version,comboTicks,filter,sort=1,letterIndex,watchTicks,source,slideIndex,superviseTicks,styleTicks,dirTicks,saverToken,galleryIndex,shotToken,fastTicks;
 bool reverse,watchSeen,inDetails,favChanged,userMinimized,dirSeen,folderBusy,splashing=true,saver,saverFlip;
 string? watchId,watchDir,lastSteamId,armedUninstall;
 string query="";
 DateTime graceUntil=DateTime.MinValue,syncUntil=DateTime.MinValue,lastInput=DateTime.Now;
 Game? detailGame;
 Shelves? Active=>tab==1?library:null;
 static Color Hex(string text)=>(Color)ColorConverter.ConvertFromString(text);
 static (Border,TextBlock) MakeBadge(){
  var text=new TextBlock{FontSize=15,FontWeight=FontWeights.Bold,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,6,0)};
  var box=new Border{MinWidth=34,Height=34,CornerRadius=new CornerRadius(17),BorderThickness=new Thickness(2),BorderBrush=Brushes.White,Background=Ui.B("#33000000"),Child=text};
  return (box,text);
 }
 static Border Tile(string label,TextBlock value,Thickness margin){
  var panel=new StackPanel();
  panel.Children.Add(new TextBlock{Text=label,FontSize=14,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#7F93A8"),Margin=new Thickness(0,0,0,4)});
  panel.Children.Add(value);
  return new Border{CornerRadius=new CornerRadius(14),Padding=new Thickness(18,12,18,14),Margin=margin,Background=Ui.B("#1FFFFFFF"),Child=panel};
 }
 static Border Soft(UIElement child)=>new Border{CornerRadius=new CornerRadius(14),Padding=new Thickness(20,12,26,12),Margin=new Thickness(0,0,16,0),Background=Ui.B("#26FFFFFF"),Child=child};
 static void Rise(UIElement element,int delay){
  var move=new TranslateTransform(0,26);
  element.BeginAnimation(UIElement.OpacityProperty,null);
  element.Opacity=0;
  element.RenderTransform=move;
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  var begin=TimeSpan.FromMilliseconds(delay);
  element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(300)){BeginTime=begin});
  move.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(26,0,TimeSpan.FromMilliseconds(380)){BeginTime=begin,EasingFunction=ease});
 }
 static IEnumerable<(string Title,List<Game> Games)> Chunks(string title,List<Game> list){
  if(list.Count>0)yield return (title,list);
 }
 static (Func<string>,Action<int>,string) Item(Func<string> text,Action<int> act,string icon)=>(text,act,icon);
 static int Step(int direction)=>direction==0?1:direction;
 static string Letter(Game game)=>game.Name.Length>0?char.ToUpperInvariant(game.Name[0]).ToString():"#";
 static bool Matches(Game item,int mode)=>mode==0||(mode==1&&item.Installed)||(mode==2&&!item.Installed)||(mode==3&&Favorites.Has(item.Id))||(mode==4&&item.Installed&&item.LastPlayed>0);
 static bool InSource(Game item,int index)=>index==0||(index==5?Favorites.Has(item.Id):item.Store==StoreNames[index]);
 static void SteamLink(string verb,string id){
  try{Process.Start(new ProcessStartInfo("steam://"+verb+"/"+id){UseShellExecute=true});}catch{}
 }
 static async Task Pace(DateTime since,int milliseconds){
  int left=milliseconds-(int)(DateTime.Now-since).TotalMilliseconds;
  if(left>0)await Task.Delay(left);
 }
 static TransformGroup Pair(ScaleTransform scale,TranslateTransform move){
  var group=new TransformGroup();
  group.Children.Add(scale);
  group.Children.Add(move);
  return group;
 }
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
 static string PlayTime(PlayInfo? info){
  if(info==null)return "нет данных";
  int minutes=info.Minutes;
  return minutes<=0?"ещё не играли":minutes<60?minutes+" мин":(minutes/60)+" ч "+(minutes%60)+" мин";
 }
 static string LastPlayedText(long unix){
  if(unix<=0)return "ещё не запускали";
  var when=DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
  int days=(DateTime.Today-when.Date).Days;
  if(days<=0)return "сегодня, "+when.ToString("HH:mm");
  if(days==1)return "вчера";
  if(days<30)return days+" дн. назад";
  return when.ToString("dd.MM.yyyy");
 }
 static UIElement ControllerLogo(){
  var canvas=new Canvas{Width=200,Height=140};
  canvas.Children.Add(new Shapes.Path{Data=Geometry.Parse("M52,18 L148,18 Q176,18 186,62 Q194,100 178,118 Q160,132 144,108 L132,92 L68,92 L56,108 Q40,132 22,118 Q6,100 14,62 Q24,18 52,18 Z"),Stroke=Brushes.White,StrokeThickness=7,StrokeLineJoin=PenLineJoin.Round,Fill=new SolidColorBrush(Color.FromArgb(0x22,255,255,255))});
  canvas.Children.Add(new Shapes.Line{X1=52,Y1=46,X2=52,Y2=74,Stroke=Brushes.White,StrokeThickness=7,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round});
  canvas.Children.Add(new Shapes.Line{X1=38,Y1=60,X2=66,Y2=60,Stroke=Brushes.White,StrokeThickness=7,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round});
  foreach(var (x,y) in new[]{(140.0,40.0),(154.0,54.0),(140.0,68.0),(126.0,54.0)}){
   var dot=new Shapes.Ellipse{Width=12,Height=12,Fill=Brushes.White};
   Canvas.SetLeft(dot,x);
   Canvas.SetTop(dot,y);
   canvas.Children.Add(dot);
  }
  return canvas;
 }
 public MainWindow(){
  Sound.Load();
  Controller.Refresh();
  Title="PLAY";
  WindowStyle=WindowStyle.None;
  WindowState=WindowState.Maximized;
  Topmost=true;
  Background=Brushes.Black;
  Foreground=Brushes.White;
  Cursor=Cursors.None;
  FontFamily=new FontFamily("Segoe UI");
  library.FixedRows=true;
  var canvas=new Grid{Width=1920,Height=1080,Background=Ui.B("#0B0F15")};
  Content=new Viewbox{Stretch=Stretch.Uniform,Child=canvas};
  canvas.Children.Add(scene);
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
  store.Children.Add(new TextBlock{Text="Магазин появится в следующей версии",FontSize=36,Foreground=Ui.B("#8F98A0"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});
  var bottomBar=new Border{Background=Ui.B("#B3000000"),Padding=new Thickness(40,0,40,0),Child=hints};
  Grid.SetRow(bottomBar,2);
  layout.Children.Add(bottomBar);
  BuildDetails(canvas);
  syncToast.Child=syncText;
  canvas.Children.Add(syncToast);
  canvas.Children.Add(dim);
  keyboard.Changed+=text=>{query=text;FillLibrary();};
  keyboard.FilterPressed+=CycleFilter;
  keyboard.Closed+=UpdateHints;
  canvas.Children.Add(keyboard);
  popup.Closed+=()=>{if(!menu.IsOpen)Dim(false);UpdateHints();};
  canvas.Children.Add(popup);
  menu=new SideMenu(CloseMenu,()=>Close(),MinimizeLauncher);
  canvas.Children.Add(menu);
  BuildSaver(canvas);
  BuildSplash(canvas);
  sections.StoreFocused+=ShowScene;
  sections.Opened+=index=>{if(index>=0&&index<sectionIds.Count)OpenSection(sectionIds[index]);};
  library.Focused+=OnFocused;
  library.Opened+=OpenDetails;
  PreviewKeyDown+=OnKey;
  MouseMove+=(_,_)=>{lastInput=DateTime.Now;if(saver)StopSaver();};
  slideTimer.Tick+=(_,_)=>NextSlide();
  StateChanged+=(_,_)=>{if(WindowState!=WindowState.Minimized)userMinimized=false;};
  Activated+=(_,_)=>{if(watchDir==null&&watchId==null)graceUntil=DateTime.MinValue;};
  Loaded+=async(_,_)=>{
   var started=DateTime.Now;
   StartSplash();
   timer.Tick+=(_,_)=>Tick();
   timer.Start();
   SelectTab(0);
   SetStep("Поиск игр…",500);
   games=await Library.Load();
   FillShelves();
   await Pace(started,1600);
   var step=DateTime.Now;
   SetStep("Найдено игр: "+games.Count,0);
   await Pace(step,900);
   step=DateTime.Now;
   SetStep("Загрузка обложек…",0);
   try{await Task.WhenAny(Task.WhenAll(games.Take(8).Select(game=>Art.Wide(game.Id))),Task.Delay(1800));}catch{}
   await Pace(step,900);
   step=DateTime.Now;
   SetStep("Готово",0);
   await Pace(step,600);
   EndSplash();
  };
  Closed+=(_,_)=>{timer.Stop();slideTimer.Stop();pad.Dispose();};
 }
 void SetStep(string text,int delayMilliseconds){
  if(subEl==null)return;
  subEl.Text=text;
  subEl.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(350)){BeginTime=TimeSpan.FromMilliseconds(delayMilliseconds)});
 }
 void BuildSplash(Grid canvas){
  glowEl=new Shapes.Ellipse{Width=1200,Height=1200,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Fill=new RadialGradientBrush(Color.FromArgb(0x66,0x66,0xC0,0xF4),Colors.Transparent),Opacity=0,RenderTransformOrigin=new Point(.5,.5),RenderTransform=splashGlow};
  logoEl=new Viewbox{Width=320,Height=224,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,-150,0,0),Child=ControllerLogo(),Opacity=0,RenderTransformOrigin=new Point(.5,.5),RenderTransform=splashLogo};
  titleEl=new TextBlock{Text="G A M E   L A U N C H E R",FontSize=76,FontWeight=FontWeights.Light,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,150,0,0),Opacity=0,RenderTransform=splashTitle};
  subEl=new TextBlock{Text="",FontSize=22,Foreground=Ui.B("#8F98A0"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,300,0,0),Opacity=0};
  var track=new Border{Width=420,Height=4,CornerRadius=new CornerRadius(2),Background=Ui.B("#22FFFFFF"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,370,0,0),ClipToBounds=true};
  track.Child=new Border{Width=150,Height=4,CornerRadius=new CornerRadius(2),Background=Ui.B("#66C0F4"),HorizontalAlignment=HorizontalAlignment.Left,RenderTransform=splashBar};
  splash.Children.Add(glowEl);
  splash.Children.Add(logoEl);
  splash.Children.Add(titleEl);
  splash.Children.Add(subEl);
  splash.Children.Add(track);
  canvas.Children.Add(splash);
 }
 void StartSplash(){
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  var back=new BackEase{EasingMode=EasingMode.EaseOut,Amplitude=.4};
  var sine=new SineEase{EasingMode=EasingMode.EaseInOut};
  glowEl!.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(1400)));
  var pulse=new DoubleAnimation(.85,1.08,TimeSpan.FromSeconds(3.2)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=sine};
  splashGlow.BeginAnimation(ScaleTransform.ScaleXProperty,pulse);
  splashGlow.BeginAnimation(ScaleTransform.ScaleYProperty,pulse);
  logoEl!.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(700)));
  var pop=new DoubleAnimation(.7,1,TimeSpan.FromMilliseconds(900)){EasingFunction=back};
  splashLogo.BeginAnimation(ScaleTransform.ScaleXProperty,pop);
  splashLogo.BeginAnimation(ScaleTransform.ScaleYProperty,pop);
  var delay=TimeSpan.FromMilliseconds(450);
  titleEl!.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(700)){BeginTime=delay});
  splashTitle.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(24,0,TimeSpan.FromMilliseconds(800)){BeginTime=delay,EasingFunction=ease});
  splashBar.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-150,420,TimeSpan.FromMilliseconds(1300)){RepeatBehavior=RepeatBehavior.Forever,EasingFunction=sine});
  Sound.Play("open");
 }
 void EndSplash(){
  splashing=false;
  lastInput=DateTime.Now;
  var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(800));
  fade.Completed+=(_,_)=>splash.Visibility=Visibility.Collapsed;
  splash.BeginAnimation(UIElement.OpacityProperty,fade);
  if(Content is UIElement root){
   var zoom=new ScaleTransform(1.06,1.06);
   root.RenderTransformOrigin=new Point(.5,.5);
   root.RenderTransform=zoom;
   var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
   zoom.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(1.06,1,TimeSpan.FromMilliseconds(1000)){EasingFunction=ease});
   zoom.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(1.06,1,TimeSpan.FromMilliseconds(1000)){EasingFunction=ease});
  }
 }
 void BuildSaver(Grid canvas){
  saverA.RenderTransformOrigin=new Point(.5,.5);
  saverA.RenderTransform=Pair(scaleA,moveA);
  saverB.RenderTransformOrigin=new Point(.5,.5);
  saverB.RenderTransform=Pair(scaleB,moveB);
  saverRoot.Children.Add(saverA);
  saverRoot.Children.Add(saverB);
  var shade=new Border{Background=Ui.Gradient(new Point(0,0),new Point(0,1),0,230),Height=420,VerticalAlignment=VerticalAlignment.Bottom,IsHitTestVisible=false};
  Panel.SetZIndex(shade,5);
  Panel.SetZIndex(saverTitle,6);
  Panel.SetZIndex(saverClock,6);
  saverRoot.Children.Add(shade);
  saverRoot.Children.Add(saverTitle);
  saverRoot.Children.Add(saverClock);
  canvas.Children.Add(saverRoot);
 }
 void StartSaver(){
  if(saver)return;
  saver=true;
  int token=++saverToken;
  saverRoot.Visibility=Visibility.Visible;
  saverRoot.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(1200)));
  RunSaver(token);
 }
 void StopSaver(){
  if(!saver)return;
  saver=false;
  int token=++saverToken;
  var fade=new DoubleAnimation(saverRoot.Opacity,0,TimeSpan.FromMilliseconds(300));
  fade.Completed+=(_,_)=>{if(token==saverToken)saverRoot.Visibility=Visibility.Collapsed;};
  saverRoot.BeginAnimation(UIElement.OpacityProperty,fade);
 }
 async void RunSaver(int token){
  var random=new Random();
  while(saver&&token==saverToken){
   var pool=games.ToList();
   if(pool.Count==0){await Task.Delay(2000);continue;}
   var game=pool[random.Next(pool.Count)];
   ImageSource? image=null;
   if(random.Next(2)==0){
    var arts=await Art.Arts(game.Id);
    if(arts.Count>0)image=arts[random.Next(arts.Count)];
   }
   image??=await Art.Hero(game.Id);
   if(!saver||token!=saverToken)return;
   if(image==null){await Task.Delay(250);continue;}
   ShowSaverSlide(image,game.Name,random);
   await Task.Delay(13000);
  }
 }
 void ShowSaverSlide(ImageSource image,string name,Random random){
  saverFlip=!saverFlip;
  var front=saverFlip?saverB:saverA;
  var back=saverFlip?saverA:saverB;
  var scale=saverFlip?scaleB:scaleA;
  var move=saverFlip?moveB:moveA;
  front.Source=image;
  Panel.SetZIndex(front,2);
  Panel.SetZIndex(back,1);
  var span=TimeSpan.FromSeconds(16);
  scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(1.03,1.18,span));
  scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(1.03,1.18,span));
  move.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(0,(random.NextDouble()-.5)*120,span));
  move.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,(random.NextDouble()-.5)*70,span));
  front.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(1500)));
  back.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(1500)){BeginTime=TimeSpan.FromMilliseconds(1500)});
  saverTitle.Text=name;
 }
 void BuildDetails(Grid canvas){
  detailArt.RenderTransform=artZoom;
  details.Children.Add(detailArt);
  details.Children.Add(new Border{IsHitTestVisible=false,Width=1500,HorizontalAlignment=HorizontalAlignment.Left,Background=Ui.Gradient(new Point(0,0),new Point(1,0),235,0)});
  details.Children.Add(new Border{IsHitTestVisible=false,Height=500,VerticalAlignment=VerticalAlignment.Bottom,Background=Ui.Gradient(new Point(0,0),new Point(0,1),0,200)});
  var chips=new StackPanel{Orientation=Orientation.Horizontal};
  statusChip.Child=detailStatus;
  chips.Children.Add(statusChip);
  chips.Children.Add(new Border{CornerRadius=new CornerRadius(14),Padding=new Thickness(14,5,14,5),Margin=new Thickness(0,0,10,0),Background=Ui.B("#26FFFFFF"),Child=detailStore});
  chips.Children.Add(favChip);
  padChip.Child=detailPad;
  chips.Children.Add(padChip);
  detailBody.Children.Add(chips);
  detailBody.Children.Add(detailTitle);
  detailBody.Children.Add(detailMeta);
  detailBody.Children.Add(detailReviews);
  detailBody.Children.Add(detailNews);
  detailTiles.ColumnDefinitions.Add(new ColumnDefinition());
  detailTiles.ColumnDefinitions.Add(new ColumnDefinition());
  var timeTile=Tile("ВРЕМЯ В ИГРЕ",detailStats,new Thickness(0,0,8,0));
  var syncTile=Tile("СОХРАНЕНИЯ",detailSync,new Thickness(8,0,0,0));
  Grid.SetColumn(syncTile,1);
  detailTiles.Children.Add(timeTile);
  detailTiles.Children.Add(syncTile);
  detailBody.Children.Add(detailTiles);
  detailTiles2.ColumnDefinitions.Add(new ColumnDefinition());
  detailTiles2.ColumnDefinitions.Add(new ColumnDefinition());
  detailTiles2.ColumnDefinitions.Add(new ColumnDefinition());
  var lastTile=Tile("ПОСЛЕДНИЙ ЗАПУСК",detailLast,new Thickness(0,0,8,0));
  var sizeTile=Tile("РАЗМЕР",detailSize,new Thickness(4,0,4,0));
  var stateTile=Tile("СОСТОЯНИЕ",detailState,new Thickness(8,0,0,0));
  Grid.SetColumn(sizeTile,1);
  Grid.SetColumn(stateTile,2);
  detailTiles2.Children.Add(lastTile);
  detailTiles2.Children.Add(sizeTile);
  detailTiles2.Children.Add(stateTile);
  detailBody.Children.Add(detailTiles2);
  detailBody.Children.Add(detailDesc);
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,30,0,0)};
  var play=new StackPanel{Orientation=Orientation.Horizontal};
  play.Children.Add(badgeA.Box);
  play.Children.Add(detailPlay);
  buttons.Children.Add(new Border{CornerRadius=new CornerRadius(14),Padding=new Thickness(26,12,34,12),Margin=new Thickness(0,0,16,0),Background=new LinearGradientBrush(Hex("#6FCB36"),Hex("#3F8A1F"),90),Child=play});
  var fav=new StackPanel{Orientation=Orientation.Horizontal};
  fav.Children.Add(badgeY.Box);
  fav.Children.Add(detailFavLabel);
  buttons.Children.Add(Soft(fav));
  var back=new StackPanel{Orientation=Orientation.Horizontal};
  back.Children.Add(badgeB.Box);
  back.Children.Add(new TextBlock{Text="Назад",FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0)});
  buttons.Children.Add(Soft(back));
  detailBody.Children.Add(buttons);
  detailCard.Child=detailBody;
  detailCard.RenderTransform=cardShift;
  details.Children.Add(detailCard);
  galleryBadge.Child=galleryText;
  details.Children.Add(galleryBadge);
  canvas.Children.Add(details);
 }
 void Dim(bool on)=>dim.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(on?.55:0,TimeSpan.FromMilliseconds(240)));
 void SetHints(params (string Glyph,string Text)[] items){
  hints.Children.Clear();
  foreach(var (glyph,text) in items){
   var tint=Controller.Tint(glyph);
   var group=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,30,0)};
   group.Children.Add(new Border{MinWidth=28,Height=28,CornerRadius=new CornerRadius(14),BorderBrush=tint,BorderThickness=new Thickness(1.5),Child=new TextBlock{Text=Controller.Label(glyph),FontSize=14,FontWeight=FontWeights.SemiBold,Foreground=tint,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,6,0)}});
   group.Children.Add(new TextBlock{Text=text,FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0,0,0)});
   hints.Children.Add(group);
  }
 }
 void UpdateHints(){
  if(keyboard.IsOpen)SetHints(("A","Ввод"),("X","Стереть"),("Y","Пробел"),("B","Закрыть"));
  else if(popup.IsOpen)SetHints(("A","Выбрать"),("◄►","Изменить"),("B","Закрыть"));
  else if(menu.IsOpen)SetHints(("A","Выбрать"),("B","Назад"));
  else if(inDetails)SetHints(("A","Играть"),("Y","Действия"),("LB/RB","Скриншоты"),("B","Назад"));
  else if(tab==2)SetHints(("≡","Меню"),("LB/RB","Вкладка"));
  else if(tab==0)SetHints(("A","Открыть"),("≡","Меню"),("LB/RB","Вкладка"));
  else SetHints(("A","Открыть"),("X","Играть"),("Y","Список"),("B","Разделы"),("≡","Меню"));
 }
 void CloseMenu(){
  menu.Close();
  Dim(false);
  UpdateHints();
 }
 void MinimizeLauncher(){
  CloseMenu();
  userMinimized=true;
  WindowState=WindowState.Minimized;
 }
 string FilterName()=>FilterNames[filter];
 string FilterText()=>"Фильтр: "+FilterName();
 string SortName()=>sort==1?"последний запуск":sort==2?"избранные сверху":"название";
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
  keyboard.Open(query,FilterText());
  Appear(keyboard,0,40);
  UpdateHints();
 }
 void OpenListMenu(){
  if(tab!=1)SelectTab(1);
  popup.Title="Список игр";
  popup.SetItems(new[]{
   Item(()=>query.Length>0?"Поиск: "+query:"Поиск",_=>{popup.Close();OpenSearch();},"\ue721"),
   Item(()=>"Лаунчер: "+StoreNames[source],direction=>{source=(source+Step(direction)+StoreNames.Length)%StoreNames.Length;FillLibrary();},"\ue7fc"),
   Item(()=>"Сортировка: "+SortName(),direction=>{sort=(sort+Step(direction)+3)%3;FillLibrary();},"\ue8cb"),
   Item(()=>"Порядок: "+(reverse?"обратный":"прямой"),_=>{reverse=!reverse;FillLibrary();},"\ue70d"),
   Item(()=>FilterText(),direction=>{filter=(filter+Step(direction)+FilterNames.Length)%FilterNames.Length;keyboard.SetFilter(FilterText());FillLibrary();},"\ue71c"),
   Item(()=>"Перейти к букве: "+(letters.Count>0?letters[letterIndex]:"-"),direction=>{
    if(letters.Count==0)return;
    if(direction==0){
     var hit=libraryOrder.FirstOrDefault(game=>Letter(game)==letters[letterIndex]);
     popup.Close();
     if(hit!=null)library.Select(hit.Id);
    }else letterIndex=(letterIndex+direction+letters.Count)%letters.Count;
   },"\ue8d2"),
   Item(()=>"Звуки: "+(Sound.Enabled?"вкл":"выкл"),_=>Sound.Toggle(),"\ue767"),
   Item(()=>"Открыть папку «Свои»",_=>{popup.Close();Stores.OpenOwn();},"\ue8b7"),
   Item(()=>"Сбросить поиск и фильтр",_=>{query="";filter=0;source=0;sort=1;reverse=false;keyboard.SetFilter(FilterText());FillLibrary();},"\ue7a7")
  });
  popup.Open();
  Dim(true);
  UpdateHints();
 }
 void OpenActions(Game game){
  armedUninstall=null;
  var items=new List<(Func<string>,Action<int>,string)>();
  items.Add(Item(()=>Favorites.Has(game.Id)?"Убрать из избранного":"В избранное",_=>{
   Favorites.Toggle(game.Id);
   favChanged=true;
   ShowDetailState(game);
  },"\ue734"));
  if(game.Store=="Steam"&&game.Installed){
   items.Add(Item(()=>"Проверить файлы игры",_=>{
    SteamLink("validate",game.Id);
    popup.Close();
   },"\ue73e"));
   items.Add(Item(()=>armedUninstall==game.Id?"Удалить игру: нажмите ещё раз":"Удалить игру",_=>{
    if(armedUninstall==game.Id){
     armedUninstall=null;
     SteamLink("uninstall",game.Id);
     popup.Close();
    }else armedUninstall=game.Id;
   },"\ue74d"));
  }
  popup.Title="Действия";
  popup.SetItems(items);
  popup.Open();
  Dim(true);
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
  scene.Visibility=tab==0?Visibility.Visible:Visibility.Collapsed;
  chip.Visibility=tab==1?Visibility.Visible:Visibility.Collapsed;
  if(index!=previous){
   UIElement page=tab==0?(UIElement)sections:tab==1?(UIElement)library:store;
   Appear(page,index>previous?60:-60,0);
  }
  if(tab==0)sections.Refocus();
  else if(tab==1&&library.Current is Game current)OnFocused(current);
  UpdateHints();
 }
 void ShowScene(string name){
  if(tab!=0)return;
  version++;
  slideTimer.Stop();
  slides=new List<ImageSource>();
  backdrop.Source=null;
  backdropFront.BeginAnimation(UIElement.OpacityProperty,null);
  backdropFront.Opacity=0;
  backdropFront.Source=null;
  scene.Child=Sections.Scene(name);
  scene.Visibility=Visibility.Visible;
  scene.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.15,1,TimeSpan.FromMilliseconds(420)));
 }
 void FillShelves(){
  var items=new List<(string,int,Game?)>();
  sectionIds.Clear();
  for(int i=0;i<StoreNames.Length;i++){
   var list=games.Where(item=>InSource(item,i)).ToList();
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
  var visible=games.Where(item=>InSource(item,source)&&Matches(item,filter)&&item.Name.Contains(query,StringComparison.OrdinalIgnoreCase)).ToList();
  var installed=Sort(visible.Where(item=>item.Installed).ToList());
  var others=Sort(visible.Where(item=>!item.Installed).ToList());
  libraryOrder=installed.Concat(others).ToList();
  letters=libraryOrder.Select(Letter).Distinct().OrderBy(item=>item,StringComparer.CurrentCultureIgnoreCase).ToList();
  letterIndex=Math.Clamp(letterIndex,0,Math.Max(0,letters.Count-1));
  library.EmptyHint=source==5?"Отмечайте игры звездой: в карточке игры нажмите Y → В избранное.":source==4?"Положите ярлык или exe в папку «Свои»: Y → Открыть папку «Свои».":source==2?"Здесь появляются игры, установленные через Epic Games Launcher.":source==3?"Здесь появляются игры, установленные из GOG.":"Измените поиск или фильтр.";
  library.SetRows(Chunks("Установленные",installed).Concat(Chunks("Не установленные",others)));
  var text="Игр: "+visible.Count;
  if(source!=0)text+=" · "+StoreNames[source];
  if(query.Length>0)text+=" · Поиск: "+query;
  if(filter!=0)text+=" · "+FilterName();
  if(sort!=1||reverse)text+=" · "+SortName();
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
  if(tab!=1)return;
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
  detailStatus.Text=game.Installed?"УСТАНОВЛЕНО":"В КОЛЛЕКЦИИ";
  detailStatus.Foreground=Ui.B(game.Installed?"#C8F59B":"#C6D4DF");
  statusChip.Background=Ui.B(game.Installed?"#CC3E7A22":"#CC3A4656");
  detailStore.Text=game.Store.ToUpperInvariant();
  favChip.Visibility=fav?Visibility.Visible:Visibility.Collapsed;
  detailPlay.Text=game.Installed?"Играть":"Установить";
  detailFavLabel.Text="Действия";
  badgeA.Text.Text=Controller.Label("A");
  badgeA.Box.BorderBrush=Controller.Tint("A");
  badgeY.Text.Text=Controller.Label("Y");
  badgeY.Box.BorderBrush=Controller.Tint("Y");
  badgeB.Text.Text=Controller.Label("B");
  badgeB.Box.BorderBrush=Controller.Tint("B");
 }
 void ShowLocal(Game game){
  var local=Extra.Local(game.Id);
  detailSize.Text=local?.Size??"—";
  detailState.Text=local?.State??"—";
  detailState.Foreground=Ui.B(local?.Tone??"#C6D4DF");
 }
 async void LoadPadChip(Game game){
  padChip.Visibility=Visibility.Collapsed;
  var support=await Extra.ControllerSupport(game.Id);
  if(!inDetails||detailGame!=game)return;
  if(support=="full"){
   detailPad.Text="ГЕЙМПАД: ПОЛНАЯ ПОДДЕРЖКА";
   detailPad.Foreground=Ui.B("#C8F59B");
   padChip.Background=Ui.B("#CC3E7A22");
   padChip.Visibility=Visibility.Visible;
  }else if(support=="partial"){
   detailPad.Text="ГЕЙМПАД: ЧАСТИЧНАЯ ПОДДЕРЖКА";
   detailPad.Foreground=Ui.B("#FFE08A");
   padChip.Background=Ui.B("#CC8A6D1B");
   padChip.Visibility=Visibility.Visible;
  }else if(support=="none"){
   detailPad.Text="ГЕЙМПАД: НЕ ЗАЯВЛЕНА";
   detailPad.Foreground=Ui.B("#FFB3B3");
   padChip.Background=Ui.B("#CC5A2B2B");
   padChip.Visibility=Visibility.Visible;
  }
 }
 async void LoadReviews(Game game){
  var info=await Extra.Reviews(game.Id);
  if(info==null||!inDetails||detailGame!=game)return;
  detailReviews.Text="★ "+info.Text+(info.Detail.Length>0?" · "+info.Detail:"");
  detailReviews.Foreground=Ui.B(info.Tone);
  detailReviews.Visibility=Visibility.Visible;
 }
 async void LoadNews(Game game){
  var info=await Extra.News(game.Id);
  if(info==null||info.Title.Length==0||!inDetails||detailGame!=game)return;
  detailNews.Text="Новость · "+LastPlayedText(info.Date)+" · "+info.Title;
  detailNews.Visibility=Visibility.Visible;
 }
 void ShowShot(int index){
  if(gallery.Count==0)return;
  galleryIndex=(index+gallery.Count)%gallery.Count;
  detailArt.Source=gallery[galleryIndex];
  detailArt.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(350)));
  galleryText.Text="◄  "+(galleryIndex+1)+" / "+gallery.Count+"  ►";
 }
 void MoveShot(int delta){
  if(gallery.Count>1)ShowShot(galleryIndex+delta);
 }
 async void LoadShots(Game game,int token){
  var urls=await Extra.Screenshots(game.Id);
  for(int i=0;i<urls.Count;i++){
   var image=await Extra.Shot(game.Id,i,urls[i]);
   if(!inDetails||detailGame!=game||token!=shotToken)return;
   if(image==null)continue;
   gallery.Add(image);
   if(detailArt.Source==null)ShowShot(0);
   galleryBadge.Visibility=gallery.Count>1?Visibility.Visible:Visibility.Collapsed;
   galleryText.Text="◄  "+(galleryIndex+1)+" / "+gallery.Count+"  ►";
  }
 }
 void AnimateDetailsIn(){
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  details.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(250)));
  artZoom.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(1.02,1.1,TimeSpan.FromSeconds(30)));
  artZoom.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(1.02,1.1,TimeSpan.FromSeconds(30)));
  cardShift.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-70,0,TimeSpan.FromMilliseconds(420)){EasingFunction=ease});
  detailCard.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(350)));
  for(int i=0;i<detailBody.Children.Count;i++)Rise(detailBody.Children[i],120+i*70);
 }
 async void OpenDetails(Game game){
  bool steam=game.Store=="Steam";
  inDetails=true;
  int token=++shotToken;
  gallery.Clear();
  galleryIndex=0;
  galleryBadge.Visibility=Visibility.Collapsed;
  detailGame=game;
  detailTitle.Text=game.Name;
  detailMeta.Text="";
  detailMeta.Visibility=Visibility.Collapsed;
  detailReviews.Text="";
  detailReviews.Visibility=Visibility.Collapsed;
  detailNews.Text="";
  detailNews.Visibility=Visibility.Collapsed;
  detailStats.Text="…";
  var first=steam?CloudLog.Status(game.Id):null;
  detailSync.Text=steam?CloudLog.Describe(first):"";
  detailSync.Foreground=Ui.B(CloudLog.Tone(first));
  detailTiles.Visibility=steam?Visibility.Visible:Visibility.Collapsed;
  detailTiles2.Visibility=steam&&game.Installed?Visibility.Visible:Visibility.Collapsed;
  detailLast.Text=LastPlayedText(game.LastPlayed);
  ShowLocal(game);
  padChip.Visibility=Visibility.Collapsed;
  detailDesc.Text="";
  detailDesc.Visibility=Visibility.Collapsed;
  ShowDetailState(game);
  detailArt.BeginAnimation(UIElement.OpacityProperty,null);
  detailArt.Opacity=0;
  detailArt.Source=null;
  details.Visibility=Visibility.Visible;
  AnimateDetailsIn();
  UpdateHints();
  if(steam)LoadPadChip(game);
  if(steam)LoadReviews(game);
  if(steam)LoadNews(game);
  var infoTask=steam?Details.Get(game.Id):Task.FromResult<GameInfo?>(null);
  var statsTask=steam?Stats.Get(game.Id):Task.FromResult<PlayInfo?>(null);
  var art=await Art.Hero(game.Id);
  if(inDetails&&detailGame==game&&token==shotToken){
   detailArt.Source=art;
   if(art!=null){
    gallery.Add(art);
    detailArt.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(700)));
   }
   if(steam)LoadShots(game,token);
  }
  var stats=await statsTask;
  if(steam&&inDetails&&detailGame==game)detailStats.Text=PlayTime(stats);
  var info=await infoTask;
  if(info!=null&&inDetails&&detailGame==game){
   var parts=new[]{info.Genres,info.Developer,info.Year}.Where(part=>part.Length>0);
   detailMeta.Text=string.Join(" · ",parts);
   detailMeta.Visibility=detailMeta.Text.Length>0?Visibility.Visible:Visibility.Collapsed;
   detailDesc.Text=info.Description;
   detailDesc.Visibility=info.Description.Length>0?Visibility.Visible:Visibility.Collapsed;
  }
 }
 void CloseDetails(){
  inDetails=false;
  shotToken++;
  galleryBadge.Visibility=Visibility.Collapsed;
  var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(150));
  fade.Completed+=(_,_)=>{if(!inDetails)details.Visibility=Visibility.Collapsed;};
  details.BeginAnimation(UIElement.OpacityProperty,fade);
  if(favChanged){favChanged=false;FillShelves();}
  UpdateHints();
 }
 void LowerBehind(){
  Topmost=false;
  Native.Lower(this);
  slideTimer.Stop();
  if(Content is UIElement root)root.Visibility=Visibility.Hidden;
 }
 void ShowAgain(){
  if(Content is UIElement root&&root.Visibility!=Visibility.Visible){
   root.Visibility=Visibility.Visible;
   if(tab==1&&slides.Count>1)slideTimer.Start();
  }
 }
 void Launch(Game? game){
  if(game==null)return;
  Library.RecordPlayed(game.Id);
  userMinimized=false;
  if(game.Store!="Steam"){
   Stores.Start(game);
   Topmost=false;
   if(game.Watch.Length>0){
    watchDir=game.Watch;
    dirSeen=false;
    dirTicks=0;
    graceUntil=DateTime.Now.AddSeconds(20);
   }else graceUntil=DateTime.Now.AddHours(6);
   return;
  }
  try{Process.Start(new ProcessStartInfo((game.Installed?"steam://rungameid/":"steam://install/")+game.Id){UseShellExecute=true});}catch{return;}
  lastSteamId=game.Id;
  syncUntil=DateTime.Now.AddSeconds(90);
  if(game.Installed){
   Topmost=false;
   watchId=game.Id;
   watchSeen=false;
   watchTicks=0;
  }
 }
 async void ReturnFromGame(){
  userMinimized=false;
  lastInput=DateTime.Now;
  syncUntil=DateTime.Now.AddSeconds(90);
  ShowAgain();
  WindowState=WindowState.Maximized;
  Activate();
  Topmost=true;
  Native.Raise(this);
  games=await Library.Load();
  FillShelves();
 }
 void WatchFolder(){
  if(folderBusy||watchDir==null)return;
  folderBusy=true;
  var dir=watchDir;
  Task.Run(()=>Native.AnyUnder(dir)).ContinueWith(task=>Dispatcher.Invoke(()=>{
   folderBusy=false;
   if(watchDir==null)return;
   bool alive=task.Status==TaskStatus.RanToCompletion&&task.Result;
   if(alive){
    if(!dirSeen){dirSeen=true;LowerBehind();}
   }else if(dirSeen){
    watchDir=null;
    dirSeen=false;
    ReturnFromGame();
   }else if(++dirTicks>120)watchDir=null;
  }));
 }
 void SyncTick(){
  var id=watchId??lastSteamId;
  if(id!=null&&(watchId!=null||DateTime.Now<syncUntil)){
   var info=CloudLog.Status(id);
   if(info==null||info.When==DateTime.MinValue||(DateTime.Now-info.When).TotalMinutes>10)syncToast.Visibility=Visibility.Collapsed;
   else{
    syncText.Text="\u2601  "+CloudLog.Describe(info);
    syncText.Foreground=Ui.B(CloudLog.Tone(info));
    syncToast.Visibility=Visibility.Visible;
   }
  }else if(syncToast.Visibility==Visibility.Visible)syncToast.Visibility=Visibility.Collapsed;
  if(inDetails&&detailGame!=null&&detailGame.Store=="Steam"){
   var current=CloudLog.Status(detailGame.Id);
   detailSync.Text=CloudLog.Describe(current);
   detailSync.Foreground=Ui.B(CloudLog.Tone(current));
   if(detailGame.Installed)ShowLocal(detailGame);
  }
 }
 void Supervise(){
  var raw=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","RunningAppID",0);
  int running=raw is int value?value:0;
  if(watchId!=null){
   if(running.ToString()==watchId){
    if(!watchSeen){watchSeen=true;LowerBehind();}
   }else if(watchSeen){
    watchId=null;
    watchSeen=false;
    ReturnFromGame();
   }else if(++watchTicks>90)watchId=null;
  }
  if(watchDir!=null)WatchFolder();
  bool busy=running!=0||watchId!=null||watchDir!=null||DateTime.Now<graceUntil;
  if(busy){
   if(Topmost)Topmost=false;
   if(saver)StopSaver();
  }else{
   if(!Topmost)Topmost=true;
   ShowAgain();
   if(!userMinimized){
    if(WindowState==WindowState.Minimized)WindowState=WindowState.Maximized;
    Native.Raise(this);
    if(!saver&&!splashing&&(DateTime.Now-lastInput).TotalMinutes>=5)StartSaver();
   }
  }
  SyncTick();
 }
 void Press(int code){
  lastInput=DateTime.Now;
  if(saver){StopSaver();return;}
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
  if(code==6){menu.Open();Dim(true);UpdateHints();return;}
  if(inDetails){
   if(code==1)CloseDetails();
   else if(code==0||code==2)Launch(detailGame);
   else if(code==3&&detailGame!=null)OpenActions(detailGame);
   else if(code==9)MoveShot(-1);
   else if(code==10)MoveShot(1);
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
  if(splashing){e.Handled=true;return;}
  lastInput=DateTime.Now;
  if(saver){StopSaver();e.Handled=true;return;}
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
  if(saver)saverClock.Text=clock.Text;
  foreach(var code in pad.Tick(IsActive)){
   if(!splashing)Press(code);
  }
  if(pad.ComboHeld()){comboTicks++;if(comboTicks>45)Close();}else comboTicks=0;
  if(watchId!=null&&watchSeen&&++fastTicks>=10){
   fastTicks=0;
   var fastRaw=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","RunningAppID",0);
   int fastRunning=fastRaw is int fastValue?fastValue:0;
   if(fastRunning.ToString()!=watchId){
    watchId=null;
    watchSeen=false;
    ReturnFromGame();
   }
  }
  if(++styleTicks>=90){
   styleTicks=0;
   if(Controller.Refresh()){
    UpdateHints();
    if(inDetails&&detailGame!=null)ShowDetailState(detailGame);
   }
  }
  if(++superviseTicks>=30){
   superviseTicks=0;
   Supervise();
  }
 }
}

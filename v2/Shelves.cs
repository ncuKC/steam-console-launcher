using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace ConsoleLauncher;
public sealed class Shelves:Grid{
 const double RowStep=270,TileStep=308;
 sealed class Tile:Border{
  public Game Game{get;}
  readonly ScaleTransform scale=new(1,1);
  public Tile(Game game){
   Game=game;
   Width=280;Height=158;Margin=new Thickness(0,21,28,21);
   CornerRadius=new CornerRadius(6);
   BorderThickness=new Thickness(3);BorderBrush=Brushes.Transparent;
   Background=Ui.B("#1A2432");Opacity=.85;
   RenderTransformOrigin=new Point(.5,.5);RenderTransform=scale;
   var holder=new Grid{ClipToBounds=true};
   Child=holder;
   var face=new Image{Stretch=Stretch.UniformToFill};
   holder.Children.Add(face);
   var label=new TextBlock{Text=game.Name,FontSize=18,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(10,0,10,10),Visibility=Visibility.Collapsed};
   holder.Children.Add(label);
   Loaded+=async(_,_)=>{
    if(face.Source!=null)return;
    face.Source=await Art.Wide(game.Id);
    if(face.Source==null)label.Visibility=Visibility.Visible;
   };
  }
  public void SetFocus(bool on){
   double size=on?1.12:1;
   Panel.SetZIndex(this,on?10:0);
   BorderBrush=on?Brushes.White:Brushes.Transparent;
   var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
   var span=TimeSpan.FromMilliseconds(160);
   scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(size,span){EasingFunction=ease});
   scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(size,span){EasingFunction=ease});
   BeginAnimation(OpacityProperty,new DoubleAnimation(on?1:.85,span));
  }
 }
 readonly StackPanel host=new(){Margin=new Thickness(0,0,0,270)};
 readonly ScrollViewer vertical=new(){Height=270,VerticalAlignment=VerticalAlignment.Bottom,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false};
 readonly TextBlock caption=new(){FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,8)};
 readonly Image logo=new(){Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,MaxWidth=620,MaxHeight=150,Visibility=Visibility.Collapsed};
 readonly TextBlock title=new(){FontSize=48,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=1200,HorizontalAlignment=HorizontalAlignment.Left,Visibility=Visibility.Collapsed};
 readonly TextBlock meta=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),MinHeight=26,Margin=new Thickness(0,8,0,0)};
 readonly List<List<Tile>> rows=new();
 readonly List<ScrollViewer> viewers=new();
 Tile? active;
 int ri,ci,heroVersion;
 public bool FixedRows;
 public event Action<Game>? Focused;
 public event Action<Game>? Opened;
 public Game? Current=>active?.Game;
 public Shelves(){
  RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  var info=new StackPanel{Margin=new Thickness(44,0,0,12),VerticalAlignment=VerticalAlignment.Bottom};
  info.Children.Add(caption);
  info.Children.Add(logo);
  info.Children.Add(title);
  info.Children.Add(meta);
  Children.Add(info);
  Grid.SetRow(vertical,1);
  vertical.Content=host;
  Children.Add(vertical);
 }
 async void ShowHero(Game game){
  int mine=++heroVersion;
  title.Text=game.Name;
  logo.Visibility=Visibility.Collapsed;
  title.Visibility=Visibility.Collapsed;
  var image=await Art.Logo(game.Id);
  if(mine!=heroVersion)return;
  if(image!=null){
   logo.Source=image;
   logo.Visibility=Visibility.Visible;
  }else title.Visibility=Visibility.Visible;
 }
 public void SetRows(IEnumerable<(string Title,List<Game> Games)> data){
  host.Children.Clear();rows.Clear();viewers.Clear();active=null;ri=ci=0;
  foreach(var (name,games) in data){
   if(games.Count==0)continue;
   var list=new List<Tile>();
   var strip=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(44,0,700,0)};
   foreach(var game in games){var tile=new Tile(game);list.Add(tile);strip.Children.Add(tile);}
   var viewer=new ScrollViewer{Height=200,Margin=new Thickness(0,0,0,34),VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,Focusable=false,Content=strip};
   host.Children.Add(new TextBlock{Text=name,FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#C6D4DF"),Height=36,Margin=new Thickness(44,0,0,0)});
   host.Children.Add(viewer);
   rows.Add(list);viewers.Add(viewer);
  }
  if(rows.Count==0){
   heroVersion++;
   caption.Text="";
   logo.Visibility=Visibility.Collapsed;
   title.Text="Ничего не найдено";
   title.Visibility=Visibility.Visible;
   meta.Text="Измените поиск или фильтр, либо установите игру в Steam.";
   return;
  }
  Place(false);
 }
 void Place(bool notify){
  if(rows.Count==0)return;
  ri=Math.Clamp(ri,0,rows.Count-1);
  ci=Math.Clamp(ci,0,rows[ri].Count-1);
  active?.SetFocus(false);
  active=rows[ri][ci];
  active.SetFocus(true);
  var game=active.Game;
  caption.Text=(game.Installed?"УСТАНОВЛЕНО":"В КОЛЛЕКЦИИ")+" · STEAM";
  ShowHero(game);
  meta.Text=game.LastPlayed>0?"Последняя активность: "+DateTimeOffset.FromUnixTimeSeconds(game.LastPlayed).LocalDateTime.ToString("d MMMM yyyy"):"";
  Scroll();
  Dispatcher.BeginInvoke(new Action(Scroll),DispatcherPriority.Loaded);
  Focused?.Invoke(game);
 }
 void Scroll(){
  if(rows.Count==0||ri>=viewers.Count)return;
  vertical.ScrollToVerticalOffset(ri*RowStep);
  viewers[ri].ScrollToHorizontalOffset(Math.Max(0,(ci-1)*TileStep));
 }
 public void Move(int dx,int dy){
  if(rows.Count==0)return;
  int r=ri,c=ci+dx;
  if(dy!=0){r=Math.Clamp(ri+dy,0,rows.Count-1);c=ci;}
  c=Math.Clamp(c,0,rows[r].Count-1);
  if(r==ri&&c==ci)return;
  ri=r;ci=c;
  Place(true);
 }
 public bool Select(string id){
  for(int r=0;r<rows.Count;r++){
   for(int c=0;c<rows[r].Count;c++){
    if(rows[r][c].Game.Id==id){
     ri=r;ci=c;
     Place(true);
     return true;
    }
   }
  }
  return false;
 }
 public void Open(){if(active!=null)Opened?.Invoke(active.Game);}
}

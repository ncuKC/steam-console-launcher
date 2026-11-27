using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace ConsoleLauncher;
public sealed class Shelves:Grid{
 const double RowStep=330,TileStep=356;
 sealed class Tile:Border{
  public Game Game{get;}
  readonly ScaleTransform scale=new(1,1);
  public Tile(Game game){
   Game=game;
   Width=320;Height=180;Margin=new Thickness(0,40,36,40);
   CornerRadius=new CornerRadius(8);ClipToBounds=true;
   BorderThickness=new Thickness(3);BorderBrush=Brushes.Transparent;
   Background=Ui.B("#1A2432");Opacity=.8;
   RenderTransformOrigin=new Point(.5,.5);RenderTransform=scale;
   var holder=new Grid();Child=holder;
   var face=new Image{Stretch=Stretch.UniformToFill};
   holder.Children.Add(face);
   holder.Children.Add(new Border{
    VerticalAlignment=VerticalAlignment.Bottom,
    Background=Ui.B("#CC0B1016"),
    Child=new TextBlock{Text=game.Name,FontSize=18,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,Padding=new Thickness(12,8,12,9),TextTrimming=TextTrimming.CharacterEllipsis}
   });
   Loaded+=async(_,_)=>{if(face.Source==null)face.Source=await Art.Wide(game.Id);};
  }
  public void SetFocus(bool on){
   double size=on?1.12:1;
   Panel.SetZIndex(this,on?10:0);
   BorderBrush=on?Brushes.White:Brushes.Transparent;
   var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
   var span=TimeSpan.FromMilliseconds(160);
   scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(size,span){EasingFunction=ease});
   scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(size,span){EasingFunction=ease});
   BeginAnimation(OpacityProperty,new DoubleAnimation(on?1:.8,span));
  }
 }
 readonly StackPanel host=new(){Margin=new Thickness(0,0,0,520)};
 readonly ScrollViewer vertical=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false};
 readonly TextBlock caption=new(){FontSize=18,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,6)};
 readonly TextBlock title=new(){FontSize=64,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=1600,HorizontalAlignment=HorizontalAlignment.Left};
 readonly TextBlock meta=new(){FontSize=22,Foreground=Ui.B("#8F98A0"),MinHeight=30};
 readonly List<List<Tile>> rows=new();
 readonly List<ScrollViewer> viewers=new();
 Tile? active;
 int ri,ci;
 public event Action<Game>? Focused;
 public event Action<Game>? Opened;
 public Game? Current=>active?.Game;
 public Shelves(){
  RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  var info=new StackPanel{Margin=new Thickness(44,0,0,10)};
  info.Children.Add(caption);info.Children.Add(title);info.Children.Add(meta);
  Children.Add(info);
  Grid.SetRow(vertical,1);
  vertical.Content=host;
  Children.Add(vertical);
 }
 public void SetRows(IEnumerable<(string Title,List<Game> Games)> data){
  host.Children.Clear();rows.Clear();viewers.Clear();active=null;ri=ci=0;
  foreach(var (name,games) in data){
   if(games.Count==0)continue;
   var list=new List<Tile>();
   var strip=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(44,0,700,0)};
   foreach(var game in games){var tile=new Tile(game);list.Add(tile);strip.Children.Add(tile);}
   var viewer=new ScrollViewer{Height=260,Margin=new Thickness(0,0,0,26),VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,Focusable=false,Content=strip};
   host.Children.Add(new TextBlock{Text=name,FontSize=28,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,Height=44,Margin=new Thickness(44,0,0,0)});
   host.Children.Add(viewer);
   rows.Add(list);viewers.Add(viewer);
  }
  if(rows.Count==0){
   caption.Text="";
   title.Text="Игр пока нет";
   meta.Text="Установите игру в Steam, и она появится здесь.";
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
  title.Text=game.Name;
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
 public void Open(){if(active!=null)Opened?.Invoke(active.Game);}
}

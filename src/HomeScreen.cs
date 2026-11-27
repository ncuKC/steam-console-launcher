using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace ConsoleLauncher;
public sealed class HomeScreen:Grid{
 const double RowHeight=444,Step=240;
 static Brush B(string s)=>(Brush)new BrushConverter().ConvertFromString(s)!;
 sealed class Tile:Border{
  public Game Game{get;}
  readonly ScaleTransform scale=new(1,1);
  readonly TextBlock name=new();
  readonly Image face=new(),back=new();
  public Tile(Game game,Func<string,bool,Task<BitmapImage?>> art){
   Game=game;Width=200;Height=300;Margin=new Thickness(0,50,40,50);Background=B("#16202D");BorderThickness=new Thickness(3);BorderBrush=Brushes.Transparent;CornerRadius=new CornerRadius(6);ClipToBounds=true;Opacity=.6;RenderTransformOrigin=new Point(.5,.5);RenderTransform=scale;
   var holder=new Grid();Child=holder;
   name.Text=game.Name;name.FontSize=20;name.Foreground=B("#8F98A0");name.TextWrapping=TextWrapping.Wrap;name.TextAlignment=TextAlignment.Center;name.VerticalAlignment=VerticalAlignment.Center;name.Margin=new Thickness(14);
   back.Stretch=Stretch.UniformToFill;back.Opacity=.55;back.Effect=new BlurEffect{Radius=28};back.Visibility=Visibility.Collapsed;
   face.Stretch=Stretch.UniformToFill;
   holder.Children.Add(name);holder.Children.Add(back);holder.Children.Add(face);
   _=Load(art);
  }
  async Task Load(Func<string,bool,Task<BitmapImage?>> art){
   try{
    var bitmap=await art(Game.Id,false);if(bitmap==null)return;
    face.Source=bitmap;name.Visibility=Visibility.Collapsed;
    if(bitmap.PixelWidth>bitmap.PixelHeight){face.Stretch=Stretch.Uniform;back.Source=bitmap;back.Visibility=Visibility.Visible;}
   }catch{}
  }
  public void SetFocus(bool on){
   double size=on?1.3:1,fade=on?1:.6;
   Panel.SetZIndex(this,on?10:0);BorderBrush=on?Brushes.White:Brushes.Transparent;
   if(Experience.ReducedMotion){
    scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);scale.ScaleX=size;scale.ScaleY=size;
    BeginAnimation(OpacityProperty,null);Opacity=fade;return;
   }
   var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
   scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(size,TimeSpan.FromMilliseconds(170)){EasingFunction=ease});
   scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(size,TimeSpan.FromMilliseconds(170)){EasingFunction=ease});
   BeginAnimation(OpacityProperty,new DoubleAnimation(fade,TimeSpan.FromMilliseconds(170)));
  }
 }
 sealed class Row{
  public readonly List<Tile> Tiles=new();
  public readonly StackPanel Strip=new(){Orientation=Orientation.Horizontal,Margin=new Thickness(40,0,240,0)};
  public readonly ScrollViewer Viewer=new(){Height=400,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,Focusable=false};
 }
 readonly Func<string,bool,Task<BitmapImage?>> art;
 readonly Action<Game> choose;
 readonly Action open;
 readonly ScrollViewer vertical=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false};
 readonly StackPanel host=new(){Margin=new Thickness(0,0,0,420)};
 readonly TextBlock caption=new(){FontSize=15,Foreground=B("#66C0F4"),Margin=new Thickness(44,0,0,6)};
 readonly TextBlock gameName=new(){FontSize=58,FontWeight=FontWeights.SemiBold,Foreground=B("#FFFFFF"),TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=1500,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(44,0,0,4)};
 readonly TextBlock meta=new(){FontSize=21,Foreground=B("#8F98A0"),Margin=new Thickness(44,0,0,0)};
 readonly Button playButton;
 readonly List<Row> rows=new();
 int ri,ci;
 public bool AtTop=>ri==0;
 public HomeScreen(Func<string,bool,Task<BitmapImage?>> art,Action<Game> choose,Action open,Action launch){
  this.art=art;this.choose=choose;this.open=open;
  Focusable=true;FocusVisualStyle=null;
  RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  var info=new StackPanel{Margin=new Thickness(0,6,40,6),HorizontalAlignment=HorizontalAlignment.Left};
  info.Children.Add(caption);info.Children.Add(gameName);info.Children.Add(meta);
  playButton=UiKit.Make("Играть",launch,true);playButton.Focusable=false;
  var more=UiKit.Make("Подробнее",open);more.Focusable=false;
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(44,14,0,0)};buttons.Children.Add(playButton);buttons.Children.Add(more);info.Children.Add(buttons);
  Children.Add(info);
  vertical.Content=host;Grid.SetRow(vertical,1);Children.Add(vertical);
  KeyDown+=(_,e)=>{
   int code=e.Key switch{Key.Left=>13,Key.Right=>14,Key.Up=>11,Key.Down=>12,_=>0};
   if(code!=0){if(HandlePad(code))e.Handled=true;}
   else if(e.Key==Key.Enter){open();e.Handled=true;}
  };
  IsVisibleChanged+=(_,e)=>{if((bool)e.NewValue)Dispatcher.BeginInvoke(new Action(ApplyScroll),DispatcherPriority.Loaded);};
 }
 public void TakeFocus(){Keyboard.Focus(this);}
 void AddRow(string title,List<Game> items){
  if(items.Count==0)return;
  var row=new Row();rows.Add(row);int r=rows.Count-1;
  host.Children.Add(new TextBlock{Text=title,FontSize=26,FontWeight=FontWeights.SemiBold,Foreground=B("#FFFFFF"),Height=44,Margin=new Thickness(44,0,0,0)});
  row.Viewer.Content=row.Strip;
  for(int i=0;i<items.Count;i++){
   int c=i;var tile=new Tile(items[i],art);
   tile.MouseLeftButtonUp+=(_,_)=>{if(ri==r&&ci==c)open();else MoveTo(r,c,true);};
   row.Tiles.Add(tile);row.Strip.Children.Add(tile);
  }
  host.Children.Add(row.Viewer);
 }
 public void SetGames(IReadOnlyList<Game> games,string? keep){
  host.Children.Clear();rows.Clear();ri=ci=0;
  var recent=games.Where(g=>g.Installed&&g.LastPlayed>0).OrderByDescending(g=>g.LastPlayed).Take(12).ToList();
  var installed=games.Where(g=>g.Installed).OrderBy(g=>g.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
  var others=games.Where(g=>!g.Installed).OrderByDescending(g=>g.LastPlayed).ThenBy(g=>g.Name).Take(24).ToList();
  AddRow("Недавние",recent);AddRow("Установленные игры",installed);AddRow("Ещё в коллекции",others);
  if(rows.Count==0){caption.Text="";gameName.Text="Игр пока нет";meta.Text="Откройте Библиотеку или импортируйте коллекцию: Меню → Аккаунт Steam.";playButton.Visibility=Visibility.Collapsed;return;}
  playButton.Visibility=Visibility.Visible;
  int fr=0,fc=0;
  if(keep!=null)for(int r=0;r<rows.Count;r++){int c=rows[r].Tiles.FindIndex(t=>t.Game.Id==keep);if(c>=0){fr=r;fc=c;break;}}
  MoveTo(fr,fc,false);
 }
 public void SyncSelection(Game? game){
  if(game==null||rows.Count==0)return;
  if(ri<rows.Count&&ci<rows[ri].Tiles.Count&&rows[ri].Tiles[ci].Game.Id==game.Id)return;
  for(int r=0;r<rows.Count;r++){int c=rows[r].Tiles.FindIndex(t=>t.Game.Id==game.Id);if(c>=0){MoveTo(r,c,false);return;}}
 }
 void MoveTo(int r,int c,bool notify){
  if(rows.Count==0)return;
  r=Math.Clamp(r,0,rows.Count-1);c=Math.Clamp(c,0,rows[r].Tiles.Count-1);
  if(ri<rows.Count&&ci<rows[ri].Tiles.Count)rows[ri].Tiles[ci].SetFocus(false);
  ri=r;ci=c;var tile=rows[r].Tiles[c];tile.SetFocus(true);
  var g=tile.Game;
  caption.Text=(g.Installed?"УСТАНОВЛЕНО":"В КОЛЛЕКЦИИ")+" · "+g.Platform.ToUpperInvariant();
  gameName.Text=g.Name;
  meta.Text=g.LastPlayed>0?"Последняя активность: "+DateTimeOffset.FromUnixTimeSeconds(g.LastPlayed).LocalDateTime.ToString("d MMMM yyyy"):"";
  playButton.Content=g.Installed?"Играть":"Установить";
  ApplyScroll();Dispatcher.BeginInvoke(new Action(ApplyScroll),DispatcherPriority.Loaded);
  if(notify)choose(g);
 }
 void ApplyScroll(){
  if(rows.Count==0||ri>=rows.Count)return;
  vertical.ScrollToVerticalOffset(ri*RowHeight);
  rows[ri].Viewer.ScrollToHorizontalOffset(Math.Max(0,(ci-1)*Step));
 }
 public bool HandlePad(int code){
  if(rows.Count==0)return code>=11&&code<=14&&code!=11;
  int r=ri,c=ci;
  switch(code){
   case 13:c--;break;
   case 14:c++;break;
   case 11:if(ri==0)return false;r--;c=Math.Min(c,rows[r].Tiles.Count-1);break;
   case 12:if(ri>=rows.Count-1)return true;r++;c=Math.Min(c,rows[r].Tiles.Count-1);break;
   default:return false;
  }
  if(c<0||c>=rows[r].Tiles.Count)return true;
  if(r!=ri||c!=ci){Experience.Play("move");MoveTo(r,c,true);}
  return true;
 }
}

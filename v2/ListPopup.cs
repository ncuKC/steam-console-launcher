using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
namespace ConsoleLauncher;
public sealed class ListPopup:Border{
 sealed class Row{
  public Border Box=new();
  public SolidColorBrush Fill=new(Idle);
  public TextBlock Icon=new();
  public TextBlock Name=new();
  public TextBlock Value=new();
  public ScaleTransform Scale=new(1,1);
  public Func<string> Text=()=>"";
  public Action<int> Act=_=>{};
 }
 static Color Hex(string text)=>(Color)ColorConverter.ConvertFromString(text);
 static readonly Color Idle=Color.FromArgb(0x30,255,255,255);
 static readonly Color Accent=Hex("#66C0F4");
 static readonly Brush Dark=new SolidColorBrush(Hex("#0E141B"));
 static readonly Brush Soft=new SolidColorBrush(Hex("#9FD8F6"));
 static readonly Brush AccentBrush=new SolidColorBrush(Accent);
 static readonly FontFamily Icons=new("Segoe MDL2 Assets");
 readonly StackPanel list=new();
 readonly List<Row> rows=new();
 readonly TextBlock title=new(){Text="Список игр",FontSize=34,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center};
 readonly TranslateTransform shift=new(0,28);
 readonly ScaleTransform grow=new(.96,.96);
 int index,closing;
 bool open;
 public bool IsOpen=>open;
 public string Title{get=>title.Text;set=>title.Text=value;}
 public event Action? Closed;
 public ListPopup(){
  Width=800;
  HorizontalAlignment=HorizontalAlignment.Center;
  VerticalAlignment=VerticalAlignment.Center;
  CornerRadius=new CornerRadius(20);
  Background=new LinearGradientBrush(Hex("#F2243449"),Hex("#F20E1824"),90);
  BorderBrush=new SolidColorBrush(Color.FromArgb(0x55,255,255,255));
  BorderThickness=new Thickness(1);
  Padding=new Thickness(28,24,28,18);
  Visibility=Visibility.Collapsed;
  Opacity=0;
  Effect=new DropShadowEffect{BlurRadius=60,ShadowDepth=0,Opacity=.75,Color=Colors.Black};
  RenderTransformOrigin=new Point(.5,.5);
  var group=new TransformGroup();
  group.Children.Add(grow);
  group.Children.Add(shift);
  RenderTransform=group;
  var root=new StackPanel();
  Child=root;
  var header=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(8,0,0,10)};
  header.Children.Add(new TextBlock{Text="\ue8fd",FontFamily=Icons,FontSize=30,Foreground=AccentBrush,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,16,0)});
  header.Children.Add(title);
  root.Children.Add(header);
  root.Children.Add(new Border{Height=2,Margin=new Thickness(8,0,8,14),Background=new LinearGradientBrush(Accent,Colors.Transparent,0)});
  root.Children.Add(list);
 }
 static (string,string) Split(string text){
  int cut=text.IndexOf(": ",StringComparison.Ordinal);
  return cut<0?(text,""):(text.Substring(0,cut),text.Substring(cut+2));
 }
 public void SetItems(IEnumerable<(Func<string> Text,Action<int> Act,string Icon)> items){
  list.Children.Clear();
  rows.Clear();
  foreach(var (text,act,icon) in items){
   var row=new Row{Text=text,Act=act};
   row.Icon=new TextBlock{Text=icon,FontFamily=Icons,FontSize=26,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,Foreground=AccentBrush};
   row.Name=new TextBlock{FontSize=26,VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.White};
   row.Value=new TextBlock{FontSize=24,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=360,Foreground=Soft};
   var grid=new Grid();
   grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(56)});
   grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
   grid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
   Grid.SetColumn(row.Name,1);
   Grid.SetColumn(row.Value,2);
   grid.Children.Add(row.Icon);
   grid.Children.Add(row.Name);
   grid.Children.Add(row.Value);
   row.Box=new Border{Height=62,CornerRadius=new CornerRadius(12),Padding=new Thickness(12,0,20,0),Margin=new Thickness(0,0,0,6),Background=row.Fill,RenderTransformOrigin=new Point(.5,.5),RenderTransform=row.Scale,Child=grid};
   rows.Add(row);
   list.Children.Add(row.Box);
  }
  index=Math.Clamp(index,0,Math.Max(0,rows.Count-1));
 }
 public void Open(){
  open=true;
  closing++;
  Visibility=Visibility.Visible;
  index=0;
  Refresh();
  var ease=new CubicEase{EasingMode=EasingMode.EaseOut};
  var span=TimeSpan.FromMilliseconds(260);
  BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(200)));
  shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(28,0,span){EasingFunction=ease});
  grow.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(.96,1,span){EasingFunction=ease});
  grow.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(.96,1,span){EasingFunction=ease});
 }
 public void Close(){
  if(!open)return;
  open=false;
  int token=++closing;
  var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(170));
  fade.Completed+=(_,_)=>{if(token==closing)Visibility=Visibility.Collapsed;};
  BeginAnimation(OpacityProperty,fade);
  var ease=new CubicEase{EasingMode=EasingMode.EaseIn};
  var span=TimeSpan.FromMilliseconds(170);
  shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,18,span){EasingFunction=ease});
  grow.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(1,.97,span){EasingFunction=ease});
  grow.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(1,.97,span){EasingFunction=ease});
  Closed?.Invoke();
 }
 public void Move(int delta){
  if(rows.Count==0)return;
  index=Math.Clamp(index+delta,0,rows.Count-1);
  Refresh();
 }
 public void Act(int direction){
  if(rows.Count==0)return;
  rows[index].Act(direction);
  if(open){
   Refresh();
   if(index<rows.Count){
    var scale=rows[index].Scale;
    var span=TimeSpan.FromMilliseconds(180);
    var ease=new BackEase{EasingMode=EasingMode.EaseOut,Amplitude=.5};
    scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(.94,1.025,span){EasingFunction=ease});
    scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(.94,1.025,span){EasingFunction=ease});
   }
  }
 }
 public void Refresh(){
  for(int i=0;i<rows.Count;i++){
   var row=rows[i];
   var (name,value)=Split(row.Text());
   row.Name.Text=name;
   row.Value.Text=value;
   bool on=i==index;
   row.Name.Foreground=on?Dark:Brushes.White;
   row.Icon.Foreground=on?Dark:AccentBrush;
   row.Value.Foreground=on?Dark:Soft;
   row.Fill.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(on?Accent:Idle,TimeSpan.FromMilliseconds(130)));
   double size=on?1.025:1;
   row.Scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(size,TimeSpan.FromMilliseconds(130)));
   row.Scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(size,TimeSpan.FromMilliseconds(130)));
  }
 }
}

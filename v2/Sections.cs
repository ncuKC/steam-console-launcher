using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Shapes=System.Windows.Shapes;
namespace ConsoleLauncher;
public sealed class Sections:Grid{
 static Color Hex(string text)=>(Color)ColorConverter.ConvertFromString(text);
 static Brush Back(string name){
  var (from,to)=name switch{
   "Steam"=>("#16202D","#2F6A93"),
   "Epic Games"=>("#121215","#3C3C48"),
   "GOG"=>("#3E1650","#8A3A96"),
   "\u0421\u0432\u043e\u0438"=>("#0F2B28","#2C7A6E"),
   _=>("#1A2432","#3B5878")
  };
  return new LinearGradientBrush(Hex(from),Hex(to),45);
 }
 static T At<T>(T item,double x,double y) where T:UIElement{
  Canvas.SetLeft(item,x);
  Canvas.SetTop(item,y);
  return item;
 }
 static Shapes.Rectangle Box(double x,double y,double size,Brush fill)=>At(new Shapes.Rectangle{Width=size,Height=size,RadiusX=8,RadiusY=8,Fill=fill},x,y);
 static TextBlock Letters(string text,double size,double top)=>At(new TextBlock{Text=text,Width=100,TextAlignment=TextAlignment.Center,FontSize=size,FontWeight=FontWeights.Bold,Foreground=Brushes.White},0,top);
 static Shapes.Line Bar(double x1,double y1,double x2,double y2,double thickness)=>new Shapes.Line{X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=Brushes.White,StrokeThickness=thickness,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};
 static UIElement Emblem(string name){
  var canvas=new Canvas{Width=100,Height=100};
  switch(name){
   case "Steam":
    canvas.Children.Add(At(new Shapes.Ellipse{Width=76,Height=76,Stroke=Brushes.White,StrokeThickness=7},12,12));
    canvas.Children.Add(Bar(34,62,66,40,8));
    canvas.Children.Add(At(new Shapes.Ellipse{Width=24,Height=24,Fill=Brushes.White},54,28));
    canvas.Children.Add(At(new Shapes.Ellipse{Width=20,Height=20,Fill=Brushes.White},24,52));
    break;
   case "Epic Games":
    canvas.Children.Add(new Shapes.Path{Data=Geometry.Parse("M18,12 L82,12 L82,58 Q82,78 50,92 Q18,78 18,58 Z"),Stroke=Brushes.White,StrokeThickness=5,Fill=new SolidColorBrush(Hex("#22FFFFFF"))});
    canvas.Children.Add(Letters("E",46,20));
    break;
   case "GOG":
    canvas.Children.Add(At(new Shapes.Rectangle{Width=88,Height=60,RadiusX=16,RadiusY=16,Stroke=Brushes.White,StrokeThickness=5},6,20));
    canvas.Children.Add(Letters("GOG",30,30));
    break;
   case "\u0421\u0432\u043e\u0438":
    canvas.Children.Add(At(new Shapes.Rectangle{Width=76,Height=76,RadiusX=14,RadiusY=14,Stroke=Brushes.White,StrokeThickness=4,StrokeDashArray=new DoubleCollection{4,3}},12,12));
    canvas.Children.Add(Bar(50,30,50,70,7));
    canvas.Children.Add(Bar(30,50,70,50,7));
    break;
   default:
    var soft=new SolidColorBrush(Hex("#E6FFFFFF"));
    canvas.Children.Add(Box(10,10,34,soft));
    canvas.Children.Add(Box(56,10,34,soft));
    canvas.Children.Add(Box(10,56,34,soft));
    canvas.Children.Add(Box(56,56,34,new SolidColorBrush(Hex("#66C0F4"))));
    break;
  }
  return new Viewbox{Width=84,Height=84,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,14,0,0),Child=canvas};
 }
 sealed class Card:Border{
  readonly ScaleTransform scale=new(1,1);
  public Card(string name,int total,Game? sample){
   Width=280;Height=158;Margin=new Thickness(0,21,28,21);
   CornerRadius=new CornerRadius(6);
   BorderThickness=new Thickness(3);
   BorderBrush=Brushes.Transparent;
   Background=Ui.B("#1A2432");
   Opacity=.85;
   RenderTransformOrigin=new Point(.5,.5);
   RenderTransform=scale;
   var holder=new Grid{ClipToBounds=true,Background=Back(name)};
   Child=holder;
   holder.Children.Add(Emblem(name));
   holder.Children.Add(new Border{VerticalAlignment=VerticalAlignment.Bottom,Height=70,Background=Ui.Gradient(new Point(0,0),new Point(0,1),0,200)});
   var text=new StackPanel{VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(14,0,14,10)};
   text.Children.Add(new TextBlock{Text=name,FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis});
   text.Children.Add(new TextBlock{Text="\u0418\u0433\u0440: "+total,FontSize=15,Foreground=Ui.B("#C6D4DF")});
   holder.Children.Add(text);
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
 const double TileStep=308;
 readonly StackPanel strip=new(){Orientation=Orientation.Horizontal,Margin=new Thickness(44,0,700,0)};
 readonly ScrollViewer viewer=new(){Height=200,Margin=new Thickness(0,0,0,34),VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,Focusable=false};
 readonly TextBlock caption=new(){FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,8),Text="\u0420\u0410\u0417\u0414\u0415\u041b"};
 readonly TextBlock title=new(){FontSize=48,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=1200,HorizontalAlignment=HorizontalAlignment.Left};
 readonly TextBlock meta=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),MinHeight=26,Margin=new Thickness(0,8,0,0)};
 readonly List<Card> cards=new();
 readonly List<Game?> samples=new();
 readonly List<string> names=new();
 readonly List<int> counts=new();
 int index;
 public event Action<Game?>? Focused;
 public event Action<int>? Opened;
 public Sections(){
  RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  var info=new StackPanel{Margin=new Thickness(44,0,0,12),VerticalAlignment=VerticalAlignment.Bottom};
  info.Children.Add(caption);
  info.Children.Add(title);
  info.Children.Add(meta);
  Children.Add(info);
  viewer.Content=strip;
  var bottom=new StackPanel{VerticalAlignment=VerticalAlignment.Bottom};
  bottom.Children.Add(new TextBlock{Text="\u0420\u0430\u0437\u0434\u0435\u043b\u044b",FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#C6D4DF"),Height=36,Margin=new Thickness(44,0,0,0)});
  bottom.Children.Add(viewer);
  Grid.SetRow(bottom,1);
  Children.Add(bottom);
 }
 public void Set(IEnumerable<(string Name,int Count,Game? Sample)> items){
  strip.Children.Clear();
  cards.Clear();
  samples.Clear();
  names.Clear();
  counts.Clear();
  foreach(var (name,count,sample) in items){
   var card=new Card(name,count,sample);
   cards.Add(card);
   samples.Add(sample);
   names.Add(name);
   counts.Add(count);
   strip.Children.Add(card);
  }
  index=Math.Clamp(index,0,Math.Max(0,cards.Count-1));
  Place();
 }
 void Place(){
  if(cards.Count==0)return;
  for(int i=0;i<cards.Count;i++)cards[i].SetFocus(i==index);
  title.Text=names[index];
  meta.Text="\u0418\u0433\u0440: "+counts[index];
  viewer.ScrollToHorizontalOffset(Math.Max(0,(index-1)*TileStep));
  Dispatcher.BeginInvoke(new Action(()=>viewer.ScrollToHorizontalOffset(Math.Max(0,(index-1)*TileStep))),DispatcherPriority.Loaded);
  Focused?.Invoke(samples[index]);
 }
 public void Refocus(){
  if(samples.Count>0)Focused?.Invoke(samples[index]);
 }
 public void Move(int dx){
  if(cards.Count==0)return;
  int next=Math.Clamp(index+dx,0,cards.Count-1);
  if(next==index)return;
  index=next;
  Place();
 }
 public void Open(){
  if(cards.Count>0)Opened?.Invoke(index);
 }
}

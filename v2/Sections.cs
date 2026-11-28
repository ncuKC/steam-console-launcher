using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Shapes=System.Windows.Shapes;
namespace ConsoleLauncher;
public sealed class Sections:Grid{
 static Color Hex(string text)=>(Color)ColorConverter.ConvertFromString(text);
 static (string,string,string) Palette(string name)=>name switch{
  "Steam"=>("#16202D","#2F6A93","#66C0F4"),
  "Epic Games"=>("#121215","#3C3C48","#9AA0C8"),
  "GOG"=>("#3E1650","#8A3A96","#E08BF0"),
  "Свои"=>("#0F2B28","#2C7A6E","#4FE3C1"),
  _=>("#1A2432","#3B5878","#7FB2E5")
 };
 static Brush Back(string name){
  var (from,to,_)=Palette(name);
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
 static Canvas Drawing(string name){
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
   case "Свои":
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
  return canvas;
 }
 static UIElement Emblem(string name)=>new Viewbox{Width=84,Height=84,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,14,0,0),Child=Drawing(name)};
 static Shapes.Ellipse Blob(Color color,double size,double x,double y,double dx,double dy,double secondsX,double secondsY){
  var move=new TranslateTransform();
  var blob=new Shapes.Ellipse{Width=size,Height=size,Fill=new RadialGradientBrush(Color.FromArgb(0x80,color.R,color.G,color.B),Colors.Transparent),RenderTransform=move,IsHitTestVisible=false};
  Canvas.SetLeft(blob,x);
  Canvas.SetTop(blob,y);
  var ease=new SineEase{EasingMode=EasingMode.EaseInOut};
  move.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-dx,dx,TimeSpan.FromSeconds(secondsX)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=ease});
  move.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(-dy,dy,TimeSpan.FromSeconds(secondsY)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=ease});
  return blob;
 }
 static Shapes.Ellipse Dot(Random random){
  double size=2+random.NextDouble()*5;
  double seconds=16+random.NextDouble()*18;
  var dot=new Shapes.Ellipse{Width=size,Height=size,Fill=Brushes.White,Opacity=0,IsHitTestVisible=false};
  Canvas.SetLeft(dot,random.NextDouble()*1920);
  var begin=TimeSpan.FromSeconds(-random.NextDouble()*seconds);
  dot.BeginAnimation(Canvas.TopProperty,new DoubleAnimation(1100,-30,TimeSpan.FromSeconds(seconds)){RepeatBehavior=RepeatBehavior.Forever,BeginTime=begin});
  var fade=new DoubleAnimationUsingKeyFrames{Duration=TimeSpan.FromSeconds(seconds),RepeatBehavior=RepeatBehavior.Forever,BeginTime=begin};
  fade.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromPercent(0)));
  fade.KeyFrames.Add(new LinearDoubleKeyFrame(.5,KeyTime.FromPercent(.2)));
  fade.KeyFrames.Add(new LinearDoubleKeyFrame(.5,KeyTime.FromPercent(.8)));
  fade.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromPercent(1)));
  dot.BeginAnimation(UIElement.OpacityProperty,fade);
  return dot;
 }
 public static UIElement Scene(string name){
  var (_,to,accent)=Palette(name);
  var tone=Hex(accent);
  var deep=Hex(to);
  var random=new Random(name.Sum(ch=>(int)ch));
  var grid=new Grid{Background=Back(name),ClipToBounds=true};
  var canvas=new Canvas{Width=1920,Height=1080,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,IsHitTestVisible=false};
  canvas.Children.Add(Blob(tone,1300,-300,-250,260,160,19,23));
  canvas.Children.Add(Blob(deep,1500,700,150,300,120,23,17));
  canvas.Children.Add(Blob(tone,900,1100,-100,180,220,17,21));
  canvas.Children.Add(Blob(deep,1100,200,500,220,160,21,25));
  var scale=new ScaleTransform(1,1);
  var tilt=new RotateTransform(0);
  var group=new TransformGroup();
  group.Children.Add(scale);
  group.Children.Add(tilt);
  var mark=new Viewbox{Width=860,Height=860,Opacity=.2,Child=Drawing(name),RenderTransformOrigin=new Point(.5,.5),RenderTransform=group};
  Canvas.SetLeft(mark,1000);
  Canvas.SetTop(mark,110);
  var ease=new SineEase{EasingMode=EasingMode.EaseInOut};
  var breathe=new DoubleAnimation(1,1.06,TimeSpan.FromSeconds(6)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=ease};
  scale.BeginAnimation(ScaleTransform.ScaleXProperty,breathe);
  scale.BeginAnimation(ScaleTransform.ScaleYProperty,breathe);
  tilt.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(-3,3,TimeSpan.FromSeconds(9)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever,EasingFunction=ease});
  canvas.Children.Add(mark);
  for(int i=0;i<24;i++)canvas.Children.Add(Dot(random));
  grid.Children.Add(canvas);
  return grid;
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
   text.Children.Add(new TextBlock{Text="Игр: "+total,FontSize=15,Foreground=Ui.B("#C6D4DF")});
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
 readonly TextBlock caption=new(){FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,8),Text="ЛАУНЧЕР"};
 readonly TextBlock title=new(){FontSize=48,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=1200,HorizontalAlignment=HorizontalAlignment.Left};
 readonly TextBlock meta=new(){FontSize=20,Foreground=Ui.B("#C6D4DF"),MinHeight=26,Margin=new Thickness(0,8,0,0)};
 readonly List<Card> cards=new();
 readonly List<string> names=new();
 readonly List<int> counts=new();
 int index;
 public event Action<string>? StoreFocused;
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
  bottom.Children.Add(new TextBlock{Text="Лаунчеры",FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#C6D4DF"),Height=36,Margin=new Thickness(44,0,0,0)});
  bottom.Children.Add(viewer);
  Grid.SetRow(bottom,1);
  Children.Add(bottom);
 }
 public void Set(IEnumerable<(string Name,int Count,Game? Sample)> items){
  strip.Children.Clear();
  cards.Clear();
  names.Clear();
  counts.Clear();
  foreach(var (name,count,sample) in items){
   var card=new Card(name,count,sample);
   cards.Add(card);
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
  meta.Text="Игр: "+counts[index];
  viewer.ScrollToHorizontalOffset(Math.Max(0,(index-1)*TileStep));
  Dispatcher.BeginInvoke(new Action(()=>viewer.ScrollToHorizontalOffset(Math.Max(0,(index-1)*TileStep))),DispatcherPriority.Loaded);
  StoreFocused?.Invoke(names[index]);
 }
 public void Refocus(){
  if(names.Count>0)StoreFocused?.Invoke(names[index]);
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

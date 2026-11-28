using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace ConsoleLauncher;
public sealed class Sections:Grid{
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
   var holder=new Grid{ClipToBounds=true};
   Child=holder;
   var face=new Image{Stretch=Stretch.UniformToFill};
   holder.Children.Add(face);
   holder.Children.Add(new Border{VerticalAlignment=VerticalAlignment.Bottom,Height=90,Background=Ui.Gradient(new Point(0,0),new Point(0,1),0,230)});
   var text=new StackPanel{VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(14,0,14,10)};
   text.Children.Add(new TextBlock{Text=name,FontSize=22,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis});
   text.Children.Add(new TextBlock{Text="Игр: "+total,FontSize=15,Foreground=Ui.B("#C6D4DF")});
   holder.Children.Add(text);
   Loaded+=async(_,_)=>{if(sample!=null&&face.Source==null)face.Source=await Art.Wide(sample.Id);};
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
 readonly TextBlock caption=new(){FontSize=16,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,8),Text="РАЗДЕЛ"};
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
  bottom.Children.Add(new TextBlock{Text="Разделы",FontSize=20,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#C6D4DF"),Height=36,Margin=new Thickness(44,0,0,0)});
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
  meta.Text="Игр: "+counts[index];
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

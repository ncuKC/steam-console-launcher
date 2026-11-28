using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace ConsoleLauncher;
public sealed class Sections:Grid{
 sealed class Card:Border{
  readonly ScaleTransform scale=new(1,1);
  public Card(string name,int total,Game? sample){
   Width=320;Height=200;Margin=new Thickness(0,40,24,40);
   CornerRadius=new CornerRadius(10);
   BorderThickness=new Thickness(3);
   BorderBrush=Brushes.Transparent;
   Background=Ui.B("#1A2432");
   Opacity=.8;
   RenderTransformOrigin=new Point(.5,.5);
   RenderTransform=scale;
   var holder=new Grid{ClipToBounds=true};
   Child=holder;
   var face=new Image{Stretch=Stretch.UniformToFill,Opacity=.5};
   holder.Children.Add(face);
   var text=new StackPanel{VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(18,0,18,14)};
   text.Children.Add(new TextBlock{Text=name,FontSize=30,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap});
   text.Children.Add(new TextBlock{Text="Игр: "+total,FontSize=20,Foreground=Ui.B("#C6D4DF")});
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
   BeginAnimation(OpacityProperty,new DoubleAnimation(on?1:.8,span));
  }
 }
 readonly StackPanel strip=new(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(44,0,0,120)};
 readonly TextBlock caption=new(){FontSize=18,FontWeight=FontWeights.SemiBold,Foreground=Ui.B("#66C0F4"),Margin=new Thickness(0,0,0,6),Text="РАЗДЕЛ"};
 readonly TextBlock title=new(){FontSize=64,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=1600,HorizontalAlignment=HorizontalAlignment.Left};
 readonly List<Card> cards=new();
 readonly List<Game?> samples=new();
 readonly List<string> names=new();
 int index;
 public event Action<Game?>? Focused;
 public event Action<int>? Opened;
 public Sections(){
  RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  var info=new StackPanel{Margin=new Thickness(44,0,0,10)};
  info.Children.Add(caption);
  info.Children.Add(title);
  Children.Add(info);
  Grid.SetRow(strip,1);
  Children.Add(strip);
 }
 public void Set(IEnumerable<(string Name,int Count,Game? Sample)> items){
  strip.Children.Clear();
  cards.Clear();
  samples.Clear();
  names.Clear();
  foreach(var (name,count,sample) in items){
   var card=new Card(name,count,sample);
   cards.Add(card);
   samples.Add(sample);
   names.Add(name);
   strip.Children.Add(card);
  }
  index=Math.Clamp(index,0,Math.Max(0,cards.Count-1));
  Place();
 }
 void Place(){
  if(cards.Count==0)return;
  for(int i=0;i<cards.Count;i++)cards[i].SetFocus(i==index);
  title.Text=names[index];
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

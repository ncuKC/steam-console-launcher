using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool layoutPolished;
 internal void PolishVisibleLayout(){
  if(layoutPolished)return;layoutPolished=true;
  if(hero.Parent is Grid banner){banner.Background=Brushes.Transparent;foreach(var layer in banner.Children.OfType<Border>())layer.Visibility=Visibility.Collapsed;foreach(var info in banner.Children.OfType<StackPanel>()){info.VerticalAlignment=VerticalAlignment.Bottom;info.Margin=new Thickness(24,0,0,36);}}
  var cards=new Style(typeof(ListBoxItem),list.ItemContainerStyle);cards.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Top));cards.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty,VerticalAlignment.Top));list.ItemContainerStyle=cards;list.Padding=new Thickness(10,12,10,16);
  title.FontSize=56;title.MaxWidth=880;play.FontSize=23;play.Padding=new Thickness(32,16,32,16);
  if(shell.Children[0] is DockPanel nav&&nav.Children[0] is StackPanel right){right.Children.Insert(0,new TextBlock{Text="LAYOUT 02",FontSize=12,Foreground=B("#94A9C2"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,18,12)});}
  MarkTabs();
 }
}

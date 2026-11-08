using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool layoutPolished;bool? conceptHome;double conceptHeight=-1;
 readonly DispatcherTimer conceptClock=new(){Interval=TimeSpan.FromMinutes(1)};
 internal void PolishVisibleLayout(){
  if(layoutPolished)return;layoutPolished=true;InitStandaloneUX();
  Background=B("#0B0F16");
  if(Content is Grid root){
   if(root.Children.Count>1&&root.Children[1] is Border shade)shade.Background=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(235,8,12,18),0),new(Color.FromArgb(165,8,12,18),.42),new(Color.FromArgb(20,8,12,18),1)},new Point(0,0),new Point(1,0));
   root.Children.Insert(2,new Border{IsHitTestVisible=false,Background=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(45,8,12,18),0),new(Color.FromArgb(5,8,12,18),.3),new(Color.FromArgb(225,8,12,18),.74),new(Color.FromArgb(255,8,12,18),1)},new Point(0,0),new Point(0,1))});
  }
  shell.Margin=new Thickness(56,24,56,18);
  if(hero.Parent is Grid banner){
   banner.Background=Brushes.Transparent;banner.ClipToBounds=true;
   foreach(var layer in banner.Children.OfType<Border>())layer.Visibility=Visibility.Collapsed;
   foreach(var info in banner.Children.OfType<StackPanel>()){info.VerticalAlignment=VerticalAlignment.Center;info.Margin=new Thickness(0,8,0,18);}
  }
  label.FontSize=12;label.Foreground=B("#B5BCC7");label.Margin=new Thickness(0,0,0,8);
  title.FontWeight=FontWeights.SemiBold;title.MaxWidth=760;title.Margin=new Thickness(0,4,0,24);title.LineHeight=60;
  play.Background=B("#F3F4F6");play.Foreground=B("#111821");play.FontSize=21;play.Padding=new Thickness(38,14,38,14);play.MinWidth=190;
  if(play.Parent is StackPanel actions)foreach(var button in actions.Children.OfType<System.Windows.Controls.Button>().Where(b=>b!=play)){button.Background=Brushes.Transparent;button.FontSize=18;button.Foreground=B("#DADEE5");}
  if(shell.Children[0] is DockPanel nav){
   nav.Margin=new Thickness(0,0,0,12);nav.LastChildFill=true;
   if(nav.Children[0] is StackPanel settings){
    foreach(var text in settings.Children.OfType<TextBlock>())text.Visibility=Visibility.Collapsed;
    var find=Button("Поиск",()=>{Tab(false);search.Focus();});settings.Children.Insert(0,find);
    var clock=new TextBlock{Text=DateTime.Now.ToString("HH:mm"),FontSize=16,Foreground=B("#C3CBD6"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(16,0,0,10)};settings.Children.Add(clock);
    conceptClock.Tick+=(_,_)=>clock.Text=DateTime.Now.ToString("HH:mm");conceptClock.Start();Closed+=(_,_)=>conceptClock.Stop();
    foreach(var button in settings.Children.OfType<System.Windows.Controls.Button>())StyleConceptNav(button);
   }
   if(nav.Children.Count>1&&nav.Children[1] is StackPanel tabs){
    foreach(var text in tabs.Children.OfType<TextBlock>())text.Visibility=Visibility.Collapsed;
    tabs.Children.Insert(0,new TextBlock{Text="LB",FontSize=11,Foreground=B("#A6AFBD"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,14,10)});
    foreach(var button in tabs.Children.OfType<System.Windows.Controls.Button>())StyleConceptNav(button);
    tabs.Children.Add(new TextBlock{Text="RB",FontSize=11,Foreground=B("#A6AFBD"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,0,10)});
   }
  }
  foreach(var tools in shell.Children.OfType<DockPanel>().Where(p=>Grid.GetRow(p)==2))tools.Margin=new Thickness(0,0,0,8);
  section.FontSize=19;section.FontWeight=FontWeights.Medium;section.Foreground=B("#DCE1E8");
  list.Padding=new Thickness(6,10,6,8);list.BorderThickness=new Thickness(0);list.Background=Brushes.Transparent;
  list.SetValue(ScrollViewer.CanContentScrollProperty,false);
  var card=new FrameworkElementFactory(typeof(StackPanel));card.SetValue(FrameworkElement.WidthProperty,176.0);
  var coverFrame=new FrameworkElementFactory(typeof(Border));coverFrame.SetValue(FrameworkElement.HeightProperty,200.0);coverFrame.SetValue(Border.BackgroundProperty,B("#17202C"));coverFrame.SetValue(Border.CornerRadiusProperty,new CornerRadius(6));coverFrame.SetValue(UIElement.ClipToBoundsProperty,true);
  var cover=new FrameworkElementFactory(typeof(Image));cover.SetValue(Image.StretchProperty,Stretch.UniformToFill);cover.AddHandler(FrameworkElement.LoadedEvent,new RoutedEventHandler(CoverLoaded));coverFrame.AppendChild(cover);card.AppendChild(coverFrame);
  var name=new FrameworkElementFactory(typeof(TextBlock));name.SetBinding(TextBlock.TextProperty,new Binding("Name"));name.SetValue(TextBlock.ForegroundProperty,B("#E6E9EF"));name.SetValue(TextBlock.FontSizeProperty,14.0);name.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);name.SetValue(FrameworkElement.MarginProperty,new Thickness(2,10,2,2));card.AppendChild(name);list.ItemTemplate=new DataTemplate{VisualTree=card};
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="CardFrame";frame.SetValue(Border.BackgroundProperty,Brushes.Transparent);frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));frame.SetValue(Border.BorderThicknessProperty,new Thickness(2));frame.SetValue(Border.BorderBrushProperty,Brushes.Transparent);frame.SetValue(Border.PaddingProperty,new Thickness(4));frame.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var template=new ControlTemplate(typeof(ListBoxItem)){VisualTree=frame};
  var selected=new Trigger{Property=ListBoxItem.IsSelectedProperty,Value=true};selected.Setters.Add(new Setter(Border.BorderBrushProperty,B("#ABB8C9"),"CardFrame"));template.Triggers.Add(selected);
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusWithinProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"CardFrame"));template.Triggers.Add(focus);
  var style=new Style(typeof(ListBoxItem));style.Setters.Add(new Setter(Control.TemplateProperty,template));style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Top));style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty,VerticalAlignment.Top));style.Setters.Add(new Setter(Control.MarginProperty,new Thickness(0,0,16,10)));list.ItemContainerStyle=style;
  foreach(var hint in shell.Children.OfType<TextBlock>().Where(t=>Grid.GetRow(t)==4&&t!=status)){hint.Text="A  Выбрать       B  Назад       X  Запустить       Y  Поиск       START  Меню";hint.FontSize=13;hint.Foreground=B("#9DA8B8");hint.Margin=new Thickness(0,12,0,0);}
  list.SelectionChanged+=(_,_)=>{if(home)label.Text="ВЫБРАННАЯ ИГРА";};
  shell.LayoutUpdated+=(_,_)=>ApplyConceptGeometry();ApplyConceptGeometry();MarkTabs();
 }
 void ApplyConceptGeometry(){
  double height=ActualHeight;if(conceptHome==home&&Math.Abs(conceptHeight-height)<1)return;conceptHome=home;conceptHeight=height;
  shell.RowDefinitions[0].Height=new GridLength(64);
  shell.RowDefinitions[1].Height=home?new GridLength(1,GridUnitType.Star):new GridLength(0);
  shell.RowDefinitions[2].Height=new GridLength(home?36:70);
  shell.RowDefinitions[3].Height=home?new GridLength(274):new GridLength(1,GridUnitType.Star);
  shell.RowDefinitions[4].Height=new GridLength(40);
  title.FontSize=Math.Clamp(height*.052,32,56);title.LineHeight=title.FontSize*1.1;
  if(consoleBackdrop!=null)consoleBackdrop.Opacity=home?.95:.16;
  if(home){label.Text="ВЫБРАННАЯ ИГРА";section.Text="Установленные игры";}
 }
 static void StyleConceptNav(System.Windows.Controls.Button button){
  button.FontSize=18;button.Background=Brushes.Transparent;button.Foreground=Brushes.White;button.Padding=new Thickness(14,9,14,9);button.Margin=new Thickness(0,0,12,10);
  var line=new FrameworkElementFactory(typeof(Border));line.Name="NavLine";line.SetValue(Border.BackgroundProperty,Brushes.Transparent);line.SetValue(Border.BorderBrushProperty,Brushes.Transparent);line.SetValue(Border.BorderThicknessProperty,new Thickness(0,0,0,2));line.SetValue(Border.PaddingProperty,new Thickness(12,9,12,9));line.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var template=new ControlTemplate(typeof(System.Windows.Controls.Button)){VisualTree=line};
  var active=new Trigger{Property=FrameworkElement.TagProperty,Value="active"};active.Setters.Add(new Setter(Border.BorderBrushProperty,B("#A9B8CB"),"NavLine"));template.Triggers.Add(active);
  var focused=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focused.Setters.Add(new Setter(Border.BackgroundProperty,B("#273344"),"NavLine"));focused.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"NavLine"));template.Triggers.Add(focused);
  var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Border.BackgroundProperty,B("#202B39"),"NavLine"));template.Triggers.Add(hover);button.Template=template;
 }
}

using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
namespace ConsoleLauncher;
public static class UiKit{
 static Brush B(string color)=>(Brush)new BrushConverter().ConvertFromString(color)!;
 public static Button Make(string caption,Action action,bool primary=false){
  var b=new Button{Content=caption,Padding=new Thickness(22,12,22,12),Margin=new Thickness(0,0,12,12),FontSize=17,Cursor=System.Windows.Input.Cursors.Hand,Background=B(primary?"#81D3FF":"#1B293C"),Foreground=primary?B("#081421"):Brushes.White};
  var f=new FrameworkElementFactory(typeof(Border));f.Name="Frame";f.SetValue(Border.CornerRadiusProperty,new CornerRadius(12));f.SetValue(Border.BorderThicknessProperty,new Thickness(2));f.SetValue(Border.BorderBrushProperty,Brushes.Transparent);
  f.SetBinding(Border.BackgroundProperty,new Binding("Background"){RelativeSource=new(RelativeSourceMode.TemplatedParent)});f.SetBinding(Border.PaddingProperty,new Binding("Padding"){RelativeSource=new(RelativeSourceMode.TemplatedParent)});var c=new FrameworkElementFactory(typeof(ContentPresenter));c.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);c.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);f.AppendChild(c);var t=new ControlTemplate(typeof(Button)){VisualTree=f};
  var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Border.BorderBrushProperty,B("#527594"),"Frame"));t.Triggers.Add(hover);
  var active=new Trigger{Property=FrameworkElement.TagProperty,Value="active"};active.Setters.Add(new Setter(Border.BackgroundProperty,B("#263E58"),"Frame"));active.Setters.Add(new Setter(Border.BorderBrushProperty,B("#81D3FF"),"Frame"));t.Triggers.Add(active);
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Frame"));t.Triggers.Add(focus);
  var pressed=new Trigger{Property=System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty,Value=true};pressed.Setters.Add(new Setter(UIElement.OpacityProperty,.72,"Frame"));t.Triggers.Add(pressed);
  var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.35,"Frame"));t.Triggers.Add(disabled);b.Template=t;b.Click+=(_,_)=>{Experience.Play("confirm");action();};return b;
 }
 public static FrameworkElement Description(string html){
  var panel=new StackPanel{MaxWidth=960,HorizontalAlignment=HorizontalAlignment.Left};TextBlock? current=null;bool bold=false,italic=false;int heading=0;
  TextBlock Block(){if(current==null){current=new TextBlock{TextWrapping=TextWrapping.Wrap,FontSize=heading>0?28:18,FontWeight=heading>0?FontWeights.SemiBold:FontWeights.Normal,LineHeight=heading>0?38:29,Foreground=B("#D3DEEC"),Margin=new Thickness(0,heading>0?24:0,0,16)};panel.Children.Add(current);}return current;}
  foreach(Match match in Regex.Matches(html,"<[^>]*>|[^<]+")){
   var token=match.Value;if(!token.StartsWith("<")){var value=WebUtility.HtmlDecode(token);value=Regex.Replace(value,@"\s+"," ");if(!string.IsNullOrWhiteSpace(value)){var run=new Run(value){FontWeight=bold?FontWeights.Bold:heading>0?FontWeights.SemiBold:FontWeights.Normal,FontStyle=italic?FontStyles.Italic:FontStyles.Normal};Block().Inlines.Add(run);}continue;}
   var tag=Regex.Match(token,@"^<\s*(/?)\s*([a-zA-Z0-9]+)");if(!tag.Success)continue;bool close=tag.Groups[1].Value=="/";string name=tag.Groups[2].Value.ToLowerInvariant();
   if(name=="strong"||name=="b"){bold=!close;continue;}if(name=="i"||name=="em"){italic=!close;continue;}if(name=="br"){Block().Inlines.Add(new LineBreak());continue;}
   if(name.Length==2&&name[0]=='h'&&char.IsDigit(name[1])){current=null;heading=close?0:1;continue;}
   if(name=="p"||name=="div"||name=="ul"||name=="ol"){current=null;continue;}
   if(name=="li"){current=null;if(!close)Block().Inlines.Add(new Run("• "));}
  }
  return panel;
 }
}
public partial class LauncherWindow{
 bool? configuredLayout;
 void MarkTabs(){if(shell.Children.Count==0||shell.Children[0] is not DockPanel nav)return;foreach(var group in nav.Children.OfType<StackPanel>())foreach(var b in group.Children.OfType<System.Windows.Controls.Button>()){string text=b.Content as string??"";bool active=text=="Главная"?page!="store"&&home:text=="Библиотека"?page!="store"&&!home:text=="Магазин"&&page=="store";b.Tag=active?"active":null;}}
 bool RouteExtraInput(int code){if(page=="details"&&nativeContent?.HandlePad(code)==true)return true;if(code!=101&&code!=102)return false;if(page=="library"){var scroll=FindScroll(list);if(scroll!=null){double delta=code==101?-24:24;if(home)scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset+delta);else scroll.ScrollToVerticalOffset(scroll.VerticalOffset+delta);}return true;}return true;}
 static ScrollViewer? FindScroll(DependencyObject root){if(root is ScrollViewer s)return s;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var found=FindScroll(VisualTreeHelper.GetChild(root,i));if(found!=null)return found;}return null;}
}

using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
namespace ConsoleLauncher;
public static class UiKit{
 static UiKit(){EventManager.RegisterClassHandler(typeof(LauncherWindow),FrameworkElement.LoadedEvent,new RoutedEventHandler((sender,args)=>{if(sender is LauncherWindow window&&ReferenceEquals(sender,args.OriginalSource))window.Dispatcher.BeginInvoke(new Action(window.PolishVisibleLayout),DispatcherPriority.ContextIdle);}));}
 static Brush B(string color)=>(Brush)new BrushConverter().ConvertFromString(color)!;
 public static Button Make(string caption,Action action,bool primary=false){
  var b=new Button{Content=caption,Padding=new Thickness(22,12,22,12),Margin=new Thickness(0,0,12,12),FontSize=18,Cursor=System.Windows.Input.Cursors.Hand,Background=B(primary?"#81D3FF":"#182230"),Foreground=primary?B("#081421"):Brushes.White};
  var f=new FrameworkElementFactory(typeof(Border));f.Name="Frame";f.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));f.SetValue(Border.BorderThicknessProperty,new Thickness(1.5));f.SetValue(Border.BorderBrushProperty,Brushes.Transparent);
  f.SetBinding(Border.BackgroundProperty,new Binding("Background"){RelativeSource=new(RelativeSourceMode.TemplatedParent)});f.SetBinding(Border.PaddingProperty,new Binding("Padding"){RelativeSource=new(RelativeSourceMode.TemplatedParent)});var c=new FrameworkElementFactory(typeof(ContentPresenter));c.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);c.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);f.AppendChild(c);var t=new ControlTemplate(typeof(Button)){VisualTree=f};
  var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Border.BorderBrushProperty,B("#527594"),"Frame"));t.Triggers.Add(hover);
  var active=new Trigger{Property=FrameworkElement.TagProperty,Value="active"};active.Setters.Add(new Setter(Border.BackgroundProperty,B("#203044"),"Frame"));active.Setters.Add(new Setter(Border.BorderBrushProperty,B("#81D3FF"),"Frame"));t.Triggers.Add(active);
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Frame"));focus.Setters.Add(new Setter(Border.BorderThicknessProperty,new Thickness(2),"Frame"));t.Triggers.Add(focus);
  var pressed=new Trigger{Property=System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty,Value=true};pressed.Setters.Add(new Setter(UIElement.OpacityProperty,.72,"Frame"));t.Triggers.Add(pressed);
  var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.35,"Frame"));t.Triggers.Add(disabled);b.Template=t;
  b.Loaded+=(_,_)=>{if(b.Content is Image){b.Padding=new Thickness(3);b.Background=Brushes.Transparent;}};
  b.Click+=(_,_)=>{Experience.Play("confirm");action();};return b;
 }
 public static FrameworkElement Description(string html){
  var panel=new StackPanel{MaxWidth=940,HorizontalAlignment=HorizontalAlignment.Left};TextBlock? current=null;bool bold=false,italic=false,inItem=false,ordered=false;int heading=0,number=0;string marker="";
  TextBlock Block(){if(current!=null)return current;current=new TextBlock{TextWrapping=TextWrapping.Wrap,FontSize=heading>0?27:19,FontWeight=heading>0?FontWeights.SemiBold:FontWeights.Normal,LineHeight=heading>0?38:30,Foreground=B("#D3DEEC")};if(inItem){var row=new Grid{Margin=new Thickness(0,0,0,12)};row.ColumnDefinitions.Add(new(){Width=new GridLength(32)});row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.Children.Add(new TextBlock{Text=marker,FontSize=19,LineHeight=30,Foreground=B("#8DCFFF")});Grid.SetColumn(current,1);row.Children.Add(current);panel.Children.Add(row);}else{current.Margin=new Thickness(0,heading>0?22:0,0,heading>0?14:16);panel.Children.Add(current);}return current;}
  html=Regex.Replace(html,@"<(script|style)\b[^>]*>[\s\S]*?</\1\s*>","",RegexOptions.IgnoreCase);
  foreach(Match match in Regex.Matches(html,"<[^>]*>|[^<]+")){
   var token=match.Value;if(!token.StartsWith("<")){var value=Regex.Replace(WebUtility.HtmlDecode(token),@"\s+"," ");if(!string.IsNullOrWhiteSpace(value))Block().Inlines.Add(new Run(value){FontWeight=bold?FontWeights.Bold:heading>0?FontWeights.SemiBold:FontWeights.Normal,FontStyle=italic?FontStyles.Italic:FontStyles.Normal});continue;}
   var tag=Regex.Match(token,@"^<\s*(/?)\s*([a-zA-Z0-9]+)");if(!tag.Success)continue;bool close=tag.Groups[1].Value=="/";string name=tag.Groups[2].Value.ToLowerInvariant();
   if(name=="strong"||name=="b"){bold=!close;continue;}if(name=="i"||name=="em"){italic=!close;continue;}if(name=="br"){if(current!=null)current.Inlines.Add(new LineBreak());continue;}
   if(name=="ul"||name=="ol"){if(!inItem)current=null;if(!close){ordered=name=="ol";number=0;}continue;}
   if(name=="li"){current=null;heading=0;inItem=!close;if(!close)marker=ordered?(++number).ToString()+".":"•";continue;}
   if(name.Length==2&&name[0]=='h'&&char.IsDigit(name[1])){current=null;heading=close?0:1;continue;}
   if(name=="p"||name=="div"){if(!inItem)current=null;else if(close&&current!=null)current.Inlines.Add(new LineBreak());}
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool layoutPolished,sceneReady;
 readonly DispatcherTimer sceneClock=new(){Interval=TimeSpan.FromMinutes(1)};
 internal void PolishVisibleLayout(){
  if(layoutPolished)return;layoutPolished=true;InitStandaloneUX();
  var root=(Grid)Content;int slot=root.Children.IndexOf(shell);root.Children.Remove(shell);
  foreach(var element in new FrameworkElement[]{title,label,play,list,search,section})if(element.Parent is Panel parent)parent.Children.Remove(element);
  shell.Children.Clear();shell.RowDefinitions.Clear();shell.Margin=new Thickness(80,30,80,24);
  foreach(var size in new[]{new GridLength(80),new GridLength(1,GridUnitType.Star),new GridLength(44),new GridLength(330),new GridLength(50)})shell.RowDefinitions.Add(new(){Height=size});
  var canvas=new Grid{Width=1920,Height=1080};canvas.Children.Add(shell);root.Children.Insert(slot,new Viewbox{Stretch=Stretch.Uniform,Child=canvas});
  Background=B("#090E16");
  foreach(var shade in root.Children.OfType<Border>())shade.Visibility=Visibility.Collapsed;
  int foreground=root.Children.IndexOf(root.Children.OfType<Viewbox>().First());
  root.Children.Insert(foreground,new Border{IsHitTestVisible=false,Background=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(240,7,11,17),0),new(Color.FromArgb(155,7,11,17),.4),new(Color.FromArgb(10,7,11,17),1)},new Point(0,0),new Point(1,0))});
  root.Children.Insert(foreground+1,new Border{IsHitTestVisible=false,Background=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(70,7,11,17),0),new(Color.FromArgb(5,7,11,17),.35),new(Color.FromArgb(225,7,11,17),.72),new(Color.FromArgb(255,7,11,17),1)},new Point(0,0),new Point(0,1))});
  var nav=new DockPanel{LastChildFill=true};shell.Children.Add(nav);
  var settings=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(settings,Dock.Right);nav.Children.Add(settings);
  settings.Children.Add(SceneNav("Поиск",()=>{Tab(false);search.Focus();}));settings.Children.Add(SceneNav("Меню",OpenReleaseMenu));
  var clock=new TextBlock{FontSize=19,Foreground=B("#C4CBD5"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(24,0,0,16),Text=DateTime.Now.ToString("HH:mm")};settings.Children.Add(clock);
  sceneClock.Tick+=(_,_)=>clock.Text=DateTime.Now.ToString("HH:mm");sceneClock.Start();Closed+=(_,_)=>sceneClock.Stop();
  var tabs=new StackPanel{Orientation=Orientation.Horizontal};nav.Children.Add(tabs);
  tabs.Children.Add(SceneHint("LB"));tabs.Children.Add(SceneNav("Главная",()=>Tab(true)));tabs.Children.Add(SceneNav("Библиотека",()=>Tab(false)));tabs.Children.Add(SceneNav("Магазин",async()=>await Store("https://store.steampowered.com/")));tabs.Children.Add(SceneHint("RB"));
  var spotlight=new Grid{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left,MaxWidth=880};Grid.SetRow(spotlight,1);shell.Children.Add(spotlight);
  var copy=new StackPanel();spotlight.Children.Add(copy);label.FontSize=14;label.Foreground=B("#B9C2CE");label.Margin=new Thickness(0,0,0,16);copy.Children.Add(label);
  title.FontSize=64;title.LineHeight=70;title.FontWeight=FontWeights.SemiBold;title.MaxWidth=880;title.MaxHeight=210;title.TextTrimming=TextTrimming.CharacterEllipsis;title.Margin=new Thickness(0,0,0,32);copy.Children.Add(title);
  var actions=new StackPanel{Orientation=Orientation.Horizontal};copy.Children.Add(actions);play.FontSize=24;play.Background=B("#F3F4F6");play.Foreground=B("#0B1320");play.Padding=new Thickness(42,16,42,16);play.MinWidth=210;play.Margin=new Thickness(0,0,22,0);actions.Children.Add(play);
  var more=Button("Подробнее",Details);more.Background=Brushes.Transparent;more.FontSize=22;more.Padding=new Thickness(20,16,20,16);more.Margin=new Thickness(0);actions.Children.Add(more);
  var tools=new DockPanel{Margin=new Thickness(0,0,0,8)};Grid.SetRow(tools,2);shell.Children.Add(tools);
  var filters=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(filters,Dock.Right);tools.Children.Add(filters);search.Width=320;search.FontSize=19;search.Margin=new Thickness(0,0,18,0);filters.Children.Add(search);
  Button? filterButton=null;filterButton=Button("Все",()=>{filter=(filter+1)%3;filterButton!.Content=new[]{"Все","Установленные","Не установленные"}[filter];Render();});filterButton.Margin=new Thickness(0);filterButton.Padding=new Thickness(16,8,16,8);filters.Children.Add(filterButton);
  section.FontSize=23;section.FontWeight=FontWeights.Medium;section.VerticalAlignment=VerticalAlignment.Center;tools.Children.Add(section);
  Grid.SetRow(list,3);shell.Children.Add(list);list.Background=Brushes.Transparent;list.Padding=new Thickness(6,10,6,8);list.BorderThickness=new Thickness(0);list.SetValue(ScrollViewer.CanContentScrollProperty,false);
  var card=new FrameworkElementFactory(typeof(StackPanel));card.SetValue(FrameworkElement.WidthProperty,176.0);
  var artwork=new FrameworkElementFactory(typeof(Border));artwork.SetValue(FrameworkElement.HeightProperty,264.0);artwork.SetValue(Border.BackgroundProperty,B("#17212D"));
  var cover=new FrameworkElementFactory(typeof(Image));cover.SetValue(Image.StretchProperty,Stretch.UniformToFill);cover.AddHandler(FrameworkElement.LoadedEvent,new RoutedEventHandler(CoverLoaded));artwork.AppendChild(cover);card.AppendChild(artwork);
  var caption=new FrameworkElementFactory(typeof(TextBlock));caption.SetBinding(TextBlock.TextProperty,new Binding("Name"));caption.SetValue(TextBlock.FontSizeProperty,16.0);caption.SetValue(TextBlock.ForegroundProperty,B("#E4E8EE"));caption.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);caption.SetValue(FrameworkElement.MarginProperty,new Thickness(2,10,2,0));card.AppendChild(caption);list.ItemTemplate=new DataTemplate{VisualTree=card};
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="Card";frame.SetValue(Border.BorderThicknessProperty,new Thickness(2));frame.SetValue(Border.BorderBrushProperty,Brushes.Transparent);frame.SetValue(Border.BackgroundProperty,Brushes.Transparent);frame.SetValue(Border.PaddingProperty,new Thickness(4));frame.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var template=new ControlTemplate(typeof(ListBoxItem)){VisualTree=frame};var selected=new Trigger{Property=ListBoxItem.IsSelectedProperty,Value=true};selected.Setters.Add(new Setter(Border.BorderBrushProperty,B("#A4B2C6"),"Card"));template.Triggers.Add(selected);var focused=new Trigger{Property=UIElement.IsKeyboardFocusWithinProperty,Value=true};focused.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Card"));template.Triggers.Add(focused);
  var style=new Style(typeof(ListBoxItem));style.Setters.Add(new Setter(Control.TemplateProperty,template));style.Setters.Add(new Setter(Control.MarginProperty,new Thickness(0,0,16,8)));style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Top));style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty,VerticalAlignment.Top));list.ItemContainerStyle=style;
  var hints=new TextBlock{Text="A  Выбрать       B  Назад       X  Запустить       Y  Поиск       START  Меню",FontSize=16,Foreground=B("#ADB7C6"),VerticalAlignment=VerticalAlignment.Bottom};Grid.SetRow(hints,4);shell.Children.Add(hints);
  list.SelectionChanged+=(_,_)=>{if(home)label.Text="ВЫБРАННАЯ ИГРА";};sceneReady=true;ConfigureScene();MarkTabs();Dispatcher.BeginInvoke(new Action(FocusSelected));
 }
 void ConfigureScene(){
  if(!sceneReady)return;
  shell.Children[1].Visibility=home?Visibility.Visible:Visibility.Collapsed;
  shell.RowDefinitions[1].Height=home?new GridLength(1,GridUnitType.Star):new GridLength(0);shell.RowDefinitions[2].Height=new GridLength(home?44:76);shell.RowDefinitions[3].Height=home?new GridLength(330):new GridLength(1,GridUnitType.Star);
  var panel=new FrameworkElementFactory(home?typeof(StackPanel):typeof(WrapPanel));if(home)panel.SetValue(StackPanel.OrientationProperty,Orientation.Horizontal);list.ItemsPanel=new ItemsPanelTemplate(panel);
  list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,home?ScrollBarVisibility.Hidden:ScrollBarVisibility.Disabled);list.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty,home?ScrollBarVisibility.Disabled:ScrollBarVisibility.Auto);
  search.Visibility=home?Visibility.Collapsed:Visibility.Visible;if(search.Parent is Panel filters)filters.Visibility=home?Visibility.Collapsed:Visibility.Visible;
  if(consoleBackdrop!=null)consoleBackdrop.Opacity=home?1:.18;if(home){label.Text="ВЫБРАННАЯ ИГРА";section.Text="Установленные игры";}
 }
 TextBlock SceneHint(string text)=>new(){Text=text,FontSize=13,Foreground=B("#A5AFBE"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,18,16)};
 System.Windows.Controls.Button SceneNav(string text,Action action){
  var button=Button(text,action);button.FontSize=23;button.Foreground=Brushes.White;button.Margin=new Thickness(0,0,22,16);
  var border=new FrameworkElementFactory(typeof(Border));border.Name="Underline";border.SetValue(Border.BorderThicknessProperty,new Thickness(0,0,0,2));border.SetValue(Border.BorderBrushProperty,Brushes.Transparent);border.SetValue(Border.PaddingProperty,new Thickness(10,12,10,12));border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));var template=new ControlTemplate(typeof(System.Windows.Controls.Button)){VisualTree=border};
  var active=new Trigger{Property=FrameworkElement.TagProperty,Value="active"};active.Setters.Add(new Setter(Border.BorderBrushProperty,B("#B7C3D4"),"Underline"));template.Triggers.Add(active);var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BackgroundProperty,B("#29364A"),"Underline"));focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Underline"));template.Triggers.Add(focus);button.Template=template;return button;
 }
}

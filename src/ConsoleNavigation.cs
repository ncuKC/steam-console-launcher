using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace ConsoleLauncher;
public static class ReviewDiscussion{public static Action<string>? Navigate;public static void Open(string author,string app)=>Navigate?.Invoke("https://steamcommunity.com/profiles/"+author+"/recommended/"+app+"/");}
public partial class LauncherWindow{
 int inputZone=2,inputIndex,detailIndex;string inputPage="",promptSignature="";Point lastPointer;readonly WrapPanel graphicHints=new();
 void InitConsoleNavigation(){
  PreviewMouseMove+=(_,e)=>{var point=e.GetPosition(this);if((point-lastPointer).Length>6){Mouse.OverrideCursor=null;lastPointer=point;}};Closed+=(_,_)=>Mouse.OverrideCursor=null;
  ReviewDiscussion.Navigate=url=>{_=Store(url);};
  foreach(var text in shell.Children.OfType<TextBlock>().Where(t=>Grid.GetRow(t)==4).ToList())shell.Children.Remove(text);Grid.SetRow(graphicHints,4);graphicHints.VerticalAlignment=VerticalAlignment.Bottom;shell.Children.Add(graphicHints);
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="Selection";frame.SetValue(Border.PaddingProperty,new Thickness(6));frame.SetValue(Border.BackgroundProperty,Brushes.Transparent);frame.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));var template=new ControlTemplate(typeof(ListBoxItem)){VisualTree=frame};var selected=new Trigger{Property=ListBoxItem.IsSelectedProperty,Value=true};selected.Setters.Add(new Setter(Border.BackgroundProperty,B("#34465D"),"Selection"));template.Triggers.Add(selected);
  var style=new Style(typeof(ListBoxItem));style.Setters.Add(new Setter(Control.TemplateProperty,template));style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty,null));style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Top));style.Setters.Add(new Setter(Control.MarginProperty,new Thickness(0,0,16,8)));list.ItemContainerStyle=style;RefreshGraphicHints();
 }
 void RefreshGraphicHints(){
  string Label(int code)=>pad?.ButtonLabel(code)??new[]{"A","B","X","Y"}[code];string signature=string.Join("|",Enumerable.Range(0,4).Select(Label))+swapped;if(signature==promptSignature)return;promptSignature=signature;graphicHints.Children.Clear();
  foreach(var action in new[]{(Label(swapped?1:0),"Выбрать"),(Label(swapped?0:1),"Назад"),(Label(2),"Запустить"),(Label(3),"Поиск"),("☰","Меню")}){var group=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,26,0)};group.Children.Add(new Border{Width=30,Height=30,CornerRadius=new CornerRadius(15),BorderBrush=B("#C8D4E5"),BorderThickness=new Thickness(1.5),Background=B("#192435"),Child=new TextBlock{Text=action.Item1,FontFamily=new FontFamily("Segoe UI Symbol"),FontSize=19,Foreground=Brushes.White,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center}});group.Children.Add(new TextBlock{Text=action.Item2,FontSize=17,Foreground=B("#DAE1EC"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(9,0,0,0)});graphicHints.Children.Add(group);}
 }
 static List<System.Windows.Controls.Button> NavigationButtons(DependencyObject root){var result=new List<System.Windows.Controls.Button>();for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is UIElement ui&&!ui.IsVisible)continue;if(child is System.Windows.Controls.Button button&&button.IsEnabled)result.Add(button);else result.AddRange(NavigationButtons(child));}return result;}
 void ActivateInputButton(System.Windows.Controls.Button button){button.Focus();button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));}
 bool RouteConsoleInput(int code){
  Mouse.OverrideCursor=Cursors.None;lastPointer=Mouse.GetPosition(this);RefreshGraphicHints();
  if(code==9||code==10){inputPage="";return false;}
  if(inputPage!=page){inputPage=page;inputZone=2;inputIndex=detailIndex=0;}
  int confirm=swapped?1:0,back=swapped?0:1;
  if(code==6){OpenReleaseMenu();inputPage="";return true;}
  if(code==back){if(menuVisible)CloseReleaseMenu();else if(page=="details"&&nativeContent?.HandleBack()==true){}else Back();inputPage="";return true;}
  if(page=="store")return false;
  if(page=="details"){
   if(nativeContent?.HandlePad(code)==true)return true;var buttons=NavigationButtons(detail);if(buttons.Count==0)return code==confirm||code>=11&&code<=14;
   if(Keyboard.FocusedElement is System.Windows.Controls.Button current&&buttons.Contains(current))detailIndex=buttons.IndexOf(current);detailIndex=Math.Clamp(detailIndex,0,buttons.Count-1);
   if(code>=11&&code<=14){detailIndex=Math.Clamp(detailIndex+(code==11||code==13?-1:1),0,buttons.Count-1);buttons[detailIndex].Focus();buttons[detailIndex].BringIntoView();return true;}
   if(code==confirm){ActivateInputButton(buttons[detailIndex]);return true;}return code==2||code==3;
  }
  if(code==3){Tab(false);search.Focus();return true;}if(code==2){Launch();return true;}
  var nav=(DockPanel)shell.Children[0];var top=nav.Children.OfType<StackPanel>().Reverse().SelectMany(g=>g.Children.OfType<System.Windows.Controls.Button>()).Where(b=>b.IsEnabled&&b.IsVisible).ToList();
  var actions=play.Parent is DependencyObject actionRoot?NavigationButtons(actionRoot):new List<System.Windows.Controls.Button>();
  if(Keyboard.FocusedElement is System.Windows.Controls.Button focused){if(top.Contains(focused)){inputZone=0;inputIndex=top.IndexOf(focused);}else if(actions.Contains(focused)){inputZone=1;inputIndex=actions.IndexOf(focused);}}else if(list.IsKeyboardFocusWithin)inputZone=2;
  if(code==confirm){if(inputZone==0&&top.Count>0)ActivateInputButton(top[Math.Clamp(inputIndex,0,top.Count-1)]);else if(inputZone==1&&actions.Count>0)ActivateInputButton(actions[Math.Clamp(inputIndex,0,actions.Count-1)]);else Details();return true;}
  if(code<11||code>14)return false;
  if(inputZone==2){int columns=home?1:Math.Max(1,(int)(list.ActualWidth/206));if(code==11&&(home||list.SelectedIndex<columns)){inputZone=home?1:0;inputIndex=0;}else{int delta=code==13?-1:code==14?1:code==11?-columns:columns;if(list.Items.Count>0){list.SelectedIndex=Math.Clamp(list.SelectedIndex+delta,0,list.Items.Count-1);list.ScrollIntoView(list.SelectedItem);FocusSelected();}return true;}}
  else if(code==11){inputZone=0;inputIndex=0;}else if(code==12){inputZone=inputZone==0&&home?1:2;inputIndex=0;}else inputIndex+=code==13?-1:1;
  if(inputZone==2)FocusSelected();else{var buttons=inputZone==0?top:actions;if(buttons.Count>0){inputIndex=Math.Clamp(inputIndex,0,buttons.Count-1);buttons[inputIndex].Focus();}}return true;
 }
}

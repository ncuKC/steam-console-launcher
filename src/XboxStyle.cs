using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton=System.Windows.Controls.Button;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool xboxStyleReady,xboxStyling;
 static bool IsMenuTitle(string? text)=>text=="Меню лаунчера"||text=="Игра"||text=="Звук и вид"||text=="Steam и библиотека"||text=="Обновления лаунчера"||text=="Питание ПК";
 internal void InitXboxStyle(){
  if(xboxStyleReady)return;xboxStyleReady=true;
  detail.LayoutUpdated+=(_,_)=>StyleLauncherMenu();
  InitWebPad();
 }
 void StyleLauncherMenu(){
  if(xboxStyling||!detail.IsVisible)return;
  bool isMenu=detail.Children.OfType<TextBlock>().Any(t=>IsMenuTitle(t.Text));
  xboxStyling=true;
  try{
   if(!isMenu){
    if(detail.HorizontalAlignment!=HorizontalAlignment.Stretch)detail.HorizontalAlignment=HorizontalAlignment.Stretch;
    if(!double.IsNaN(detail.Width))detail.Width=double.NaN;
    if(detail.Background!=null)detail.Background=null;
    return;
   }
   if(detail.HorizontalAlignment!=HorizontalAlignment.Left)detail.HorizontalAlignment=HorizontalAlignment.Left;
   if(detail.Width!=720)detail.Width=720;
   if(detail.Background==null)detail.Background=B("#F01B2838");
   if(detail.Margin!=new Thickness(0))detail.Margin=new Thickness(0);
   if(shell.Visibility!=Visibility.Visible)shell.Visibility=Visibility.Visible;
   foreach(var child in detail.Children.OfType<FrameworkElement>().ToList()){
    if(child.Resources.Contains("xbox"))continue;
    child.Resources["xbox"]=true;
    if(child is WpfButton button)RestyleMenuButton(button);
    else if(child is TextBlock text){
     if(IsMenuTitle(text.Text)){text.FontSize=40;text.FontWeight=FontWeights.SemiBold;text.Foreground=B("#FFFFFF");text.Margin=new Thickness(48,40,48,22);}
     else{text.FontSize=Math.Max(text.FontSize,19);text.Foreground=B("#C6D4DF");text.Margin=new Thickness(48,10,48,10);text.MaxWidth=600;}
    }
   }
  }finally{xboxStyling=false;}
 }
 static void RestyleMenuButton(WpfButton button){
  button.HorizontalAlignment=HorizontalAlignment.Left;button.Width=600;button.MinHeight=46;button.FontSize=20;
  button.Padding=new Thickness(24,9,24,9);button.Margin=new Thickness(48,0,0,8);
  button.Background=B("#2A475E");button.Foreground=B("#FFFFFF");
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="Frame";
  frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(4));frame.SetValue(Border.BorderThicknessProperty,new Thickness(3));frame.SetValue(Border.BorderBrushProperty,Brushes.Transparent);
  frame.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  frame.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Left);presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
  frame.AppendChild(presenter);
  var template=new ControlTemplate(typeof(WpfButton)){VisualTree=frame};
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Frame"));focus.Setters.Add(new Setter(Border.BackgroundProperty,B("#66C0F4"),"Frame"));focus.Setters.Add(new Setter(Control.ForegroundProperty,B("#0E141B")));template.Triggers.Add(focus);
  var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.4,"Frame"));template.Triggers.Add(disabled);
  button.Template=template;
 }
}

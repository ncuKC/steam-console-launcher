using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool libraryPolished,countBusy;
 void StyleLibraryTiles(){
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="Selection";
  frame.SetValue(Border.BorderThicknessProperty,new Thickness(3));frame.SetValue(Border.BorderBrushProperty,Brushes.Transparent);frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));frame.SetValue(Border.PaddingProperty,new Thickness(6));frame.SetValue(Border.BackgroundProperty,Brushes.Transparent);frame.SetValue(UIElement.OpacityProperty,.88);frame.SetValue(UIElement.RenderTransformOriginProperty,new Point(.5,.5));
  frame.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var template=new ControlTemplate(typeof(ListBoxItem)){VisualTree=frame};
  var selected=new Trigger{Property=ListBoxItem.IsSelectedProperty,Value=true};selected.Setters.Add(new Setter(Border.BackgroundProperty,B("#1B2838"),"Selection"));selected.Setters.Add(new Setter(UIElement.OpacityProperty,1.0,"Selection"));template.Triggers.Add(selected);
  var focused=new Trigger{Property=UIElement.IsKeyboardFocusWithinProperty,Value=true};focused.Setters.Add(new Setter(Border.BackgroundProperty,B("#2A475E"),"Selection"));focused.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Selection"));focused.Setters.Add(new Setter(UIElement.OpacityProperty,1.0,"Selection"));focused.Setters.Add(new Setter(UIElement.RenderTransformProperty,new ScaleTransform(1.08,1.08),"Selection"));template.Triggers.Add(focused);
  var style=new Style(typeof(ListBoxItem));
  style.Setters.Add(new Setter(Control.TemplateProperty,template));
  style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty,null));
  style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Top));
  style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty,VerticalAlignment.Top));
  style.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(6,10,30,32)));
  list.ItemContainerStyle=style;
  var bar=new Style(typeof(ScrollBar));bar.Setters.Add(new Setter(UIElement.OpacityProperty,.3));
  list.Resources[typeof(ScrollBar)]=bar;
 }
 internal void InitLibraryPolish(){
  if(libraryPolished)return;libraryPolished=true;
  try{
   if(search.Parent is StackPanel filters){
    filters.Children.Insert(0,new TextBlock{Text="Поиск · Y",FontSize=18,Foreground=B("#C6D4DF"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,14,0)});
    var filterButton=filters.Children.OfType<System.Windows.Controls.Button>().FirstOrDefault();
    if(filterButton!=null){
     filterButton.Content="Фильтр: "+filterButton.Content;
     filterButton.Click+=(_,_)=>filterButton.Content="Фильтр: "+filterButton.Content;
    }
   }
  }catch{}
  try{
   DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty,typeof(TextBlock)).AddValueChanged(section,(_,_)=>{
    if(countBusy||home||section.Text.Contains(" · "))return;
    countBusy=true;
    try{section.Text=section.Text+" · "+list.Items.Count;}finally{countBusy=false;}
   });
  }catch{}
  try{StyleLibraryTiles();}catch{}
 }
}

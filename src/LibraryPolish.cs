using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool libraryPolished,countBusy;
 static readonly System.Collections.Concurrent.ConcurrentDictionary<string,Lazy<Task<BitmapImage?>>> WideImages=new();
 static Task<BitmapImage?> WideArt(string id)=>WideImages.GetOrAdd(id,_=>new Lazy<Task<BitmapImage?>>(()=>LoadWide(id))).Value;
 static async Task<BitmapImage?> LoadWide(string id){
  await Gate.WaitAsync();
  try{
   var folder=Path.Combine(Root,"artwork");var file=Path.Combine(folder,id+"_wide.bin");
   if(File.Exists(file))try{var saved=await File.ReadAllBytesAsync(file);return await Task.Run(()=>Decode(saved,false));}catch{try{File.Delete(file);}catch{}}
   foreach(var name in new[]{"capsule_616x353.jpg","header.jpg"})foreach(var host in new[]{"https://shared.fastly.steamstatic.com","https://cdn.akamai.steamstatic.com"})try{
    var data=await Http.GetByteArrayAsync(host+"/steam/apps/"+id+"/"+name);
    var image=await Task.Run(()=>Decode(data,false));
    try{Directory.CreateDirectory(folder);await File.WriteAllBytesAsync(file,data);}catch{}
    return image;
   }catch{}
   return null;
  }finally{Gate.Release();}
 }
 async void WideCoverLoaded(object sender){
  if(sender is Image image&&image.DataContext is Game g){
   var art=await WideArt(g.Id);
   if(image.DataContext is Game current&&current.Id==g.Id)image.Source=art;
  }
 }
 int TileColumns(){
  if(list.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement first&&first.ActualWidth>0&&VisualTreeHelper.GetParent(first) is Panel panel&&panel.ActualWidth>0){
   double cell=first.ActualWidth+first.Margin.Left+first.Margin.Right;
   return Math.Max(1,(int)(panel.ActualWidth/cell+.01));
  }
  return Math.Max(1,(int)(list.ActualWidth/340));
 }
 void StyleLibraryTiles(){
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="Selection";
  frame.SetValue(Border.BorderThicknessProperty,new Thickness(3));frame.SetValue(Border.BorderBrushProperty,Brushes.Transparent);frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(9));frame.SetValue(Border.PaddingProperty,new Thickness(6));frame.SetValue(Border.BackgroundProperty,Brushes.Transparent);frame.SetValue(UIElement.OpacityProperty,.88);frame.SetValue(UIElement.RenderTransformOriginProperty,new Point(.5,.5));
  frame.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var template=new ControlTemplate(typeof(ListBoxItem)){VisualTree=frame};
  var selected=new Trigger{Property=ListBoxItem.IsSelectedProperty,Value=true};selected.Setters.Add(new Setter(Border.BackgroundProperty,B("#1B2838"),"Selection"));selected.Setters.Add(new Setter(UIElement.OpacityProperty,1.0,"Selection"));template.Triggers.Add(selected);
  var focused=new Trigger{Property=UIElement.IsKeyboardFocusWithinProperty,Value=true};focused.Setters.Add(new Setter(Border.BackgroundProperty,B("#2A475E"),"Selection"));focused.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Selection"));focused.Setters.Add(new Setter(UIElement.OpacityProperty,1.0,"Selection"));focused.Setters.Add(new Setter(UIElement.RenderTransformProperty,new ScaleTransform(1.06,1.06),"Selection"));template.Triggers.Add(focused);
  var style=new Style(typeof(ListBoxItem));
  style.Setters.Add(new Setter(Control.TemplateProperty,template));
  style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty,null));
  style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Top));
  style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty,VerticalAlignment.Top));
  style.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(6,10,24,28)));
  list.ItemContainerStyle=style;
  var card=new FrameworkElementFactory(typeof(Border));card.SetValue(FrameworkElement.WidthProperty,292.0);card.SetValue(FrameworkElement.HeightProperty,164.0);card.SetValue(Border.BackgroundProperty,B("#16202D"));card.SetValue(Border.CornerRadiusProperty,new CornerRadius(6));card.SetValue(UIElement.ClipToBoundsProperty,true);
  var stack=new FrameworkElementFactory(typeof(Grid));
  var placeholder=new FrameworkElementFactory(typeof(TextBlock));placeholder.SetBinding(TextBlock.TextProperty,new Binding("Name"));placeholder.SetValue(TextBlock.FontSizeProperty,22.0);placeholder.SetValue(TextBlock.ForegroundProperty,B("#8F98A0"));placeholder.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);placeholder.SetValue(TextBlock.TextAlignmentProperty,TextAlignment.Center);placeholder.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);placeholder.SetValue(FrameworkElement.MarginProperty,new Thickness(16));
  var cover=new FrameworkElementFactory(typeof(Image));cover.SetValue(Image.StretchProperty,Stretch.UniformToFill);cover.AddHandler(FrameworkElement.LoadedEvent,new RoutedEventHandler((s,e)=>WideCoverLoaded(s)));
  stack.AppendChild(placeholder);stack.AppendChild(cover);
  var shade=new FrameworkElementFactory(typeof(Border));shade.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Bottom);shade.SetValue(FrameworkElement.HeightProperty,76.0);shade.SetValue(UIElement.IsHitTestVisibleProperty,false);shade.SetValue(Border.BackgroundProperty,new LinearGradientBrush(Color.FromArgb(0,16,20,25),Color.FromArgb(235,16,20,25),90));
  stack.AppendChild(shade);
  var name=new FrameworkElementFactory(typeof(TextBlock));name.SetBinding(TextBlock.TextProperty,new Binding("Name"));name.SetValue(TextBlock.FontSizeProperty,19.0);name.SetValue(TextBlock.FontWeightProperty,FontWeights.SemiBold);name.SetValue(TextBlock.ForegroundProperty,Brushes.White);name.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);name.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Bottom);name.SetValue(FrameworkElement.MarginProperty,new Thickness(14,0,14,12));name.SetValue(UIElement.IsHitTestVisibleProperty,false);
  stack.AppendChild(name);
  card.AppendChild(stack);
  list.ItemTemplate=new DataTemplate{VisualTree=card};
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

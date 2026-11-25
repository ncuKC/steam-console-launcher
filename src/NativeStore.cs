using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ConsoleLauncher;
public record StoreItem(string Id,string Name,string ImageUrl,string Price,string Discount);
public sealed class NativeStore:Grid{
 static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(15)};
 static readonly SemaphoreSlim Gate=new(4);
 static Brush B(string s)=>(Brush)new BrushConverter().ConvertFromString(s)!;
 readonly TextBox searchBox=new(){Width=360,FontSize=18,Background=B("#1A1E25"),Foreground=B("#F2EFEA"),CaretBrush=B("#D9933D"),BorderThickness=new Thickness(0),Padding=new Thickness(14,10,14,10),Margin=new Thickness(0,0,12,0)};
 readonly ListBox grid=new(){Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(4,8,4,8)};
 readonly TextBlock statusText=new(){FontSize=15,Foreground=B("#A8A49E"),Margin=new Thickness(0,0,0,10)};
 readonly Button[] catButtons=new Button[4];
 readonly Action<StoreItem> openGame;
 readonly Action openWeb;
 readonly Action goBack;
 readonly Func<string,string?> openKeyboard;
 int activeCat;bool loaded;
 public NativeStore(Action<StoreItem> onOpenGame,Action onOpenWeb,Action onBack,Func<string,string?> onOpenKeyboard){
  openGame=onOpenGame;openWeb=onOpenWeb;goBack=onBack;openKeyboard=onOpenKeyboard;
  Background=B("#0D0F12");
  RowDefinitions.Add(new(){Height=GridLength.Auto});
  RowDefinitions.Add(new(){Height=GridLength.Auto});
  RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  var top=new DockPanel{Margin=new Thickness(48,28,48,12)};Children.Add(top);
  var right=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(right,Dock.Right);top.Children.Add(right);
  right.Children.Add(searchBox);
  right.Children.Add(MakeBtn("Найти",()=>TriggerSearch(),true));
  right.Children.Add(MakeBtn("Веб-версия Steam",openWeb));
  var left=new StackPanel{Orientation=Orientation.Horizontal};top.Children.Add(left);
  left.Children.Add(MakeBtn("Назад",goBack));
  left.Children.Add(new TextBlock{Text="Магазин",FontSize=32,FontWeight=FontWeights.SemiBold,Foreground=B("#F2EFEA"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(16,0,0,0)});
  var bar=new StackPanel{Margin=new Thickness(48,0,48,8)};Grid.SetRow(bar,1);Children.Add(bar);
  var tabs=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,8)};bar.Children.Add(tabs);
  string[] cats={"Лидеры продаж","Скидки","Новинки","Популярное"};
  for(int i=0;i<4;i++){int idx=i;catButtons[i]=MakeTab(cats[i],async()=>await LoadCategoryAsync(idx));tabs.Children.Add(catButtons[i]);}
  bar.Children.Add(statusText);
  bar.Children.Add(new Border{Height=1,Background=B("#262A32")});
  Grid.SetRow(grid,2);grid.Margin=new Thickness(44,8,44,24);
  grid.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);
  grid.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty,ScrollBarVisibility.Auto);
  grid.ItemsPanel=new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));
  var card=new FrameworkElementFactory(typeof(StackPanel));card.SetValue(FrameworkElement.WidthProperty,272.0);
  var art=new FrameworkElementFactory(typeof(Border));art.SetValue(FrameworkElement.HeightProperty,128.0);art.SetValue(Border.BackgroundProperty,B("#181C22"));art.SetValue(Border.CornerRadiusProperty,new CornerRadius(6));art.SetValue(UIElement.ClipToBoundsProperty,true);
  var img=new FrameworkElementFactory(typeof(Image));img.SetValue(Image.StretchProperty,Stretch.UniformToFill);img.AddHandler(FrameworkElement.LoadedEvent,new RoutedEventHandler(ThumbLoaded));art.AppendChild(img);card.AppendChild(art);
  var name=new FrameworkElementFactory(typeof(TextBlock));name.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Name"));name.SetValue(TextBlock.FontSizeProperty,16.0);name.SetValue(TextBlock.ForegroundProperty,B("#F2EFEA"));name.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);name.SetValue(FrameworkElement.MarginProperty,new Thickness(2,8,2,2));card.AppendChild(name);
  var priceRow=new FrameworkElementFactory(typeof(StackPanel));priceRow.SetValue(StackPanel.OrientationProperty,Orientation.Horizontal);
  var disc=new FrameworkElementFactory(typeof(TextBlock));disc.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Discount"));disc.SetValue(TextBlock.FontSizeProperty,14.0);disc.SetValue(TextBlock.FontWeightProperty,FontWeights.SemiBold);disc.SetValue(TextBlock.ForegroundProperty,B("#120E09"));disc.SetValue(TextBlock.BackgroundProperty,B("#D9933D"));disc.SetValue(FrameworkElement.MarginProperty,new Thickness(0,2,8,0));disc.SetValue(TextBlock.PaddingProperty,new Thickness(5,1,5,1));priceRow.AppendChild(disc);
  var price=new FrameworkElementFactory(typeof(TextBlock));price.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Price"));price.SetValue(TextBlock.FontSizeProperty,15.0);price.SetValue(TextBlock.ForegroundProperty,B("#D9A86C"));price.SetValue(FrameworkElement.MarginProperty,new Thickness(0,2,0,0));priceRow.AppendChild(price);
  card.AppendChild(priceRow);grid.ItemTemplate=new DataTemplate{VisualTree=card};
  var frame=new FrameworkElementFactory(typeof(Border));frame.Name="StoreCard";frame.SetValue(Border.BorderThicknessProperty,new Thickness(0));frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));frame.SetValue(Border.BackgroundProperty,B("#14171D"));frame.SetValue(Border.PaddingProperty,new Thickness(8));frame.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var tpl=new ControlTemplate(typeof(ListBoxItem)){VisualTree=frame};
  var sel=new Trigger{Property=ListBoxItem.IsSelectedProperty,Value=true};sel.Setters.Add(new Setter(Border.BackgroundProperty,B("#382A1A"),"StoreCard"));tpl.Triggers.Add(sel);
  var foc=new Trigger{Property=UIElement.IsKeyboardFocusWithinProperty,Value=true};foc.Setters.Add(new Setter(Border.BackgroundProperty,B("#7A5122"),"StoreCard"));tpl.Triggers.Add(foc);
  var st=new Style(typeof(ListBoxItem));st.Setters.Add(new Setter(Control.TemplateProperty,tpl));st.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty,null));st.Setters.Add(new Setter(Control.MarginProperty,new Thickness(0,0,14,14)));grid.ItemContainerStyle=st;
  grid.MouseDoubleClick+=(_,_)=>{if(grid.SelectedItem is StoreItem item)openGame(item);};
  grid.KeyDown+=(_,e)=>{if(e.Key==Key.Enter&&grid.SelectedItem is StoreItem item){openGame(item);e.Handled=true;}};
  searchBox.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){_=SearchAsync(searchBox.Text);e.Handled=true;}};
  Children.Add(grid);
  Loaded+=async(_,_)=>{if(!loaded){loaded=true;await LoadCategoryAsync(0);}};
 }
 void TriggerSearch(){
  var text=openKeyboard(searchBox.Text);
  if(text!=null){searchBox.Text=text;_=SearchAsync(text);}
  else FocusGrid();
 }
 static Button MakeBtn(string text,Action action,bool primary=false){
  var b=new Button{Content=text,FontSize=17,FontWeight=primary?FontWeights.SemiBold:FontWeights.Normal,Padding=new Thickness(18,10,18,10),Margin=new Thickness(0,0,10,0),Foreground=primary?B("#120E09"):B("#EDEAE4"),Background=B(primary?"#D9933D":"#1C2028"),Cursor=Cursors.Hand};
  var border=new FrameworkElementFactory(typeof(Border));border.Name="Frame";border.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));border.SetValue(Border.BorderThicknessProperty,new Thickness(2));border.SetValue(Border.BorderBrushProperty,Brushes.Transparent);
  border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var t=new ControlTemplate(typeof(Button)){VisualTree=border};
  var f=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};f.Setters.Add(new Setter(Border.BorderBrushProperty,B("#F0B462"),"Frame"));t.Triggers.Add(f);
  b.Template=t;b.Click+=(_,_)=>action();return b;
 }
 static Button MakeTab(string text,Action action){
  var b=new Button{Content=text,FontSize=19,Foreground=B("#EDEAE4"),Background=Brushes.Transparent,Padding=new Thickness(16,10,16,10),Margin=new Thickness(0,0,10,0),Cursor=Cursors.Hand};
  var border=new FrameworkElementFactory(typeof(Border));border.Name="TabLine";border.SetValue(Border.CornerRadiusProperty,new CornerRadius(6,6,0,0));border.SetValue(Border.BorderThicknessProperty,new Thickness(0,0,0,3));border.SetValue(Border.BorderBrushProperty,Brushes.Transparent);border.SetValue(Border.BackgroundProperty,Brushes.Transparent);
  border.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var t=new ControlTemplate(typeof(Button)){VisualTree=border};
  var a=new Trigger{Property=FrameworkElement.TagProperty,Value="active"};a.Setters.Add(new Setter(Border.BorderBrushProperty,B("#D9933D"),"TabLine"));a.Setters.Add(new Setter(Border.BackgroundProperty,B("#241E17"),"TabLine"));t.Triggers.Add(a);
  var f=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};f.Setters.Add(new Setter(Border.BackgroundProperty,B("#382A1A"),"TabLine"));f.Setters.Add(new Setter(Border.BorderBrushProperty,B("#F0B462"),"TabLine"));t.Triggers.Add(f);
  b.Template=t;b.Click+=(_,_)=>action();return b;
 }
 async Task LoadCategoryAsync(int cat){
  activeCat=Math.Clamp(cat,0,3);
  for(int i=0;i<catButtons.Length;i++)catButtons[i].Tag=i==activeCat?"active":null;
  statusText.Text="Загрузка витрины Steam…";
  try{
   var json=await Http.GetStringAsync("https://store.steampowered.com/api/featuredcategories?l=russian&cc=RU");
   using var doc=JsonDocument.Parse(json);
   string[] keys={"top_sellers","specials","new_releases","coming_soon"};
   var list=new List<StoreItem>();
   if(doc.RootElement.TryGetProperty(keys[activeCat],out var section)&&section.TryGetProperty("items",out var items)){
    foreach(var it in items.EnumerateArray()){
     string id=it.TryGetProperty("id",out var idp)?idp.ToString():"";
     string name=it.TryGetProperty("name",out var np)?np.GetString()??"":"";
     string img=it.TryGetProperty("header_image",out var ip)?ip.GetString()??"":"";
     if(string.IsNullOrEmpty(img))img=$"https://shared.fastly.steamstatic.com/steam/apps/{id}/header.jpg";
     int disc=it.TryGetProperty("discount_percent",out var dp)&&dp.TryGetInt32(out var dv)?dv:0;
     int finalPrice=it.TryGetProperty("final_price",out var fp)&&fp.TryGetInt32(out var fv)?fv:0;
     string cur=it.TryGetProperty("currency",out var cp)?cp.GetString()??"RUB":"RUB";
     string price=finalPrice==0?"Бесплатно":$"{finalPrice/100.0:0.##} {cur}";
     if(id.Length>0&&name.Length>0)list.Add(new StoreItem(id,name,img,price,disc>0?$"-{disc}%":""));
    }
   }
   grid.ItemsSource=list;
   if(list.Count>0){grid.SelectedIndex=0;FocusGrid();}
   statusText.Text=list.Count>0?$"Показано игр: {list.Count} · LB/RB — категории · Y — поиск · A — карточка · B — назад":"Подборка временно недоступна.";
  }catch{statusText.Text="Не удалось загрузить витрину Steam. Можно открыть веб-версию справа.";}
 }
 async Task SearchAsync(string term){
  term=term.Trim();if(term.Length==0){await LoadCategoryAsync(activeCat);return;}
  statusText.Text=$"Поиск «{term}»…";
  try{
   var json=await Http.GetStringAsync("https://store.steampowered.com/api/storesearch/?term="+Uri.EscapeDataString(term)+"&l=russian&cc=RU");
   using var doc=JsonDocument.Parse(json);
   var list=new List<StoreItem>();
   if(doc.RootElement.TryGetProperty("items",out var items)){
    foreach(var it in items.EnumerateArray()){
     string id=it.TryGetProperty("id",out var idp)?idp.ToString():"";
     string name=it.TryGetProperty("name",out var np)?np.GetString()??"":"";
     string img=it.TryGetProperty("tiny_image",out var ip)?ip.GetString()??"":"";
     if(string.IsNullOrEmpty(img))img=$"https://shared.fastly.steamstatic.com/steam/apps/{id}/header.jpg";
     string price="Подробнее";
     if(it.TryGetProperty("price",out var pr)&&pr.ValueKind==JsonValueKind.Object&&pr.TryGetProperty("final",out var fp)&&fp.TryGetInt32(out var fv))price=fv==0?"Бесплатно":$"{fv/100.0:0.##}";
     if(id.Length>0&&name.Length>0)list.Add(new StoreItem(id,name,img,price,""));
    }
   }
   grid.ItemsSource=list;
   if(list.Count>0){grid.SelectedIndex=0;FocusGrid();}
   statusText.Text=list.Count>0?$"Найдено: {list.Count} · Y — новый поиск · A — карточка · B — назад":"Ничего не найдено.";
  }catch{statusText.Text="Поиск временно недоступен.";}
 }
 async void ThumbLoaded(object sender,RoutedEventArgs e){
  if(sender is not Image image||image.DataContext is not StoreItem item||string.IsNullOrEmpty(item.ImageUrl))return;
  await Gate.WaitAsync();
  try{
   var bytes=await Http.GetByteArrayAsync(item.ImageUrl);
   var bmp=await Task.Run(()=>{using var ms=new MemoryStream(bytes);var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.DecodePixelWidth=460;b.StreamSource=ms;b.EndInit();b.Freeze();return b;});
   if(image.DataContext is StoreItem cur&&cur.Id==item.Id)image.Source=bmp;
  }catch{}finally{Gate.Release();}
 }
 public void FocusGrid(){
  if(grid.Items.Count==0)return;
  if(grid.SelectedIndex<0)grid.SelectedIndex=0;
  grid.ScrollIntoView(grid.SelectedItem);
  if(grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) is UIElement el)el.Focus();
  else grid.Focus();
 }
 public bool HandlePad(int code,bool swapped){
  int confirm=swapped?1:0,back=swapped?0:1;
  if(code==back){goBack();return true;}
  if(code==9){_=LoadCategoryAsync((activeCat+3)%4);return true;}
  if(code==10){_=LoadCategoryAsync((activeCat+1)%4);return true;}
  if(code==3){TriggerSearch();return true;}
  if(code==confirm&&grid.SelectedItem is StoreItem item){openGame(item);return true;}
  if(code>=11&&code<=14&&grid.Items.Count>0){
   int cols=Math.Max(1,(int)(grid.ActualWidth/302));
   int delta=code==13?-1:code==14?1:code==11?-cols:cols;
   grid.SelectedIndex=Math.Clamp(grid.SelectedIndex+delta,0,grid.Items.Count-1);
   FocusGrid();return true;
  }
  return false;
 }
}

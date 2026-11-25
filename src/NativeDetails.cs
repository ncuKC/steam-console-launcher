using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ConsoleLauncher;
public sealed class NativeDetails:Grid,IDisposable{
 static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(20)};
 static readonly SemaphoreSlim Images=new(3);
 static Brush B(string s)=>(Brush)new BrushConverter().ConvertFromString(s)!;
 readonly CancellationTokenSource cancel=new();
 readonly ScrollViewer scroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,8,0,24)};
 readonly StackPanel overviewPanel=new(){MaxWidth=1200,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(48,16,48,24)};
 readonly StackPanel mediaPanel=new(){MaxWidth=1280,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(48,16,48,24)};
 readonly StackPanel descPanel=new(){MaxWidth=1200,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(48,16,48,24)};
 readonly StackPanel reviewsPanel=new(){MaxWidth=1280,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(48,16,48,24)};
 readonly Button[] tabButtons=new Button[4];
 readonly StackPanel[] tabPanels;
 readonly Grid viewer=new(){Background=B("#080A0D"),Visibility=Visibility.Collapsed};
 readonly Image full=new(){Stretch=Stretch.Uniform};
 readonly MediaElement movie=new(){LoadedBehavior=MediaState.Manual,UnloadedBehavior=MediaState.Stop};
 readonly List<string> shots=new();
 UIElement? lastFocus;
 int activeTab,index,imageVersion;bool disposed,loaded,paused;
 static string Str(JsonElement e,string k)=>e.TryGetProperty(k,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString()??"":"";
 static string Plain(string s)=>WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(s,@"<(br\s*/?|/p|/li|/div)>","\n",RegexOptions.IgnoreCase),"<[^>]+>",""));
 static bool Safe(string s)=>Uri.TryCreate(s,UriKind.Absolute,out var u)&&u.Scheme=="https"&&(u.Host.EndsWith(".steamstatic.com",StringComparison.OrdinalIgnoreCase)||u.Host.EndsWith(".akamaihd.net",StringComparison.OrdinalIgnoreCase));
 public NativeDetails(Game game,Action launch,Func<Task> store,Action back){
  tabPanels=new[]{overviewPanel,mediaPanel,descPanel,reviewsPanel};
  Background=B("#0D0F12");
  RowDefinitions.Add(new(){Height=GridLength.Auto});
  RowDefinitions.Add(new(){Height=GridLength.Auto});
  RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  var top=new WrapPanel{Margin=new Thickness(48,28,48,8)};Children.Add(top);
  top.Children.Add(Btn("Назад",back));
  top.Children.Add(Btn(game.Installed?"Играть":"Установить",launch,true));
  top.Children.Add(Btn("Купить / Steam",async()=>await store()));
  top.Children.Add(Btn("Звук и движение",()=>Experience.Settings(Window.GetWindow(this))));
  var header=new StackPanel{Margin=new Thickness(48,8,48,0)};Grid.SetRow(header,1);Children.Add(header);
  header.Children.Add(new TextBlock{Text=game.State.ToUpperInvariant()+" · STEAM",FontSize=14,Foreground=B("#D9A86C"),Margin=new Thickness(0,0,0,6)});
  header.Children.Add(new TextBlock{Text=game.Name,FontSize=42,FontWeight=FontWeights.SemiBold,Foreground=B("#F2EFEA"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,16)});
  var tabBar=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,8)};header.Children.Add(tabBar);
  string[] names={"Обзор","Медиа","Описание","Отзывы"};
  for(int i=0;i<4;i++){int tabIndex=i;tabButtons[i]=TabBtn(names[i],()=>SelectTab(tabIndex));tabBar.Children.Add(tabButtons[i]);}
  header.Children.Add(new Border{Height=1,Background=B("#262A32")});
  Grid.SetRow(scroll,2);Children.Add(scroll);
  var loading=AddText(overviewPanel,"Загрузка описания…",20,"#C6C2BC");
  SelectTab(0);
  Grid.SetRowSpan(viewer,3);Children.Add(viewer);
  viewer.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  viewer.RowDefinitions.Add(new(){Height=GridLength.Auto});
  viewer.Children.Add(full);viewer.Children.Add(movie);movie.Visibility=Visibility.Collapsed;
  var controls=new WrapPanel{Margin=new Thickness(24,16,24,20),HorizontalAlignment=HorizontalAlignment.Center};Grid.SetRow(controls,1);viewer.Children.Add(controls);
  controls.Children.Add(Btn("Закрыть просмотр",()=>HandleBack()));
  controls.Children.Add(Btn("Предыдущий",()=>Step(-1)));
  controls.Children.Add(Btn("Следующий",()=>Step(1)));
  controls.Children.Add(Btn("Пауза / продолжить",()=>{if(movie.Visibility!=Visibility.Visible)return;if(paused)movie.Play();else movie.Pause();paused=!paused;}));
  movie.MediaFailed+=(_,_)=>{movie.Stop();movie.Visibility=Visibility.Collapsed;};
  PreviewMouseWheel+=(_,e)=>{if(viewer.Visibility==Visibility.Visible)Step(e.Delta>0?-1:1);else scroll.ScrollToVerticalOffset(scroll.VerticalOffset-e.Delta);e.Handled=true;};
  Loaded+=async(_,_)=>{if(loaded)return;loaded=true;await Load(game,loading);};
  Unloaded+=(_,_)=>movie.Stop();
 }
 void SelectTab(int tab){
  activeTab=Math.Clamp(tab,0,tabPanels.Length-1);
  scroll.Content=tabPanels[activeTab];
  scroll.ScrollToVerticalOffset(0);
  for(int i=0;i<tabButtons.Length;i++)tabButtons[i].Tag=i==activeTab?"active":null;
 }
 static Button Btn(string text,Action action,bool primary=false){
  var b=new Button{Content=text,FontSize=18,FontWeight=primary?FontWeights.SemiBold:FontWeights.Normal,Padding=new Thickness(22,12,22,12),Margin=new Thickness(0,0,12,10),Foreground=primary?B("#120E09"):B("#EDEAE4"),Background=B(primary?"#D9933D":"#1C2028"),Cursor=Cursors.Hand};
  var border=new FrameworkElementFactory(typeof(Border));border.Name="Frame";border.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));border.SetValue(Border.BorderThicknessProperty,new Thickness(2));border.SetValue(Border.BorderBrushProperty,Brushes.Transparent);
  border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var t=new ControlTemplate(typeof(Button)){VisualTree=border};
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,B("#F0B462"),"Frame"));t.Triggers.Add(focus);
  b.Template=t;b.Click+=(_,_)=>{Experience.Play("confirm");action();};return b;
 }
 static Button TabBtn(string text,Action action){
  var b=new Button{Content=text,FontSize=20,Foreground=B("#EDEAE4"),Background=Brushes.Transparent,Padding=new Thickness(16,10,16,10),Margin=new Thickness(0,0,12,0),Cursor=Cursors.Hand};
  var border=new FrameworkElementFactory(typeof(Border));border.Name="TabLine";border.SetValue(Border.CornerRadiusProperty,new CornerRadius(6,6,0,0));border.SetValue(Border.BorderThicknessProperty,new Thickness(0,0,0,3));border.SetValue(Border.BorderBrushProperty,Brushes.Transparent);border.SetValue(Border.BackgroundProperty,Brushes.Transparent);
  border.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var t=new ControlTemplate(typeof(Button)){VisualTree=border};
  var active=new Trigger{Property=FrameworkElement.TagProperty,Value="active"};active.Setters.Add(new Setter(Border.BorderBrushProperty,B("#D9933D"),"TabLine"));active.Setters.Add(new Setter(Border.BackgroundProperty,B("#241E17"),"TabLine"));t.Triggers.Add(active);
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BackgroundProperty,B("#382A1A"),"TabLine"));focus.Setters.Add(new Setter(Border.BorderBrushProperty,B("#F0B462"),"TabLine"));t.Triggers.Add(focus);
  b.Template=t;b.Click+=(_,_)=>{Experience.Play("confirm");action();};return b;
 }
 static TextBlock AddText(Panel target,string s,int size=20,string color="#EDEAE4"){
  var t=new TextBlock{Text=s,FontSize=size,LineHeight=size*1.45,Foreground=B(color),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,14)};
  target.Children.Add(t);return t;
 }
 async Task<JsonDocument> Data(string key,string url,int minutes){var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SteamConsoleLauncher","content");Directory.CreateDirectory(folder);var path=Path.Combine(folder,key+".json");if(File.Exists(path)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(path)<TimeSpan.FromMinutes(minutes))try{return JsonDocument.Parse(await File.ReadAllTextAsync(path,cancel.Token));}catch(JsonException){}try{var json=await Http.GetStringAsync(url,cancel.Token);var doc=JsonDocument.Parse(json);try{await File.WriteAllTextAsync(path,json,cancel.Token);}catch{}return doc;}catch{if(File.Exists(path))return JsonDocument.Parse(await File.ReadAllTextAsync(path,cancel.Token));throw;}}
 async Task Load(Game game,TextBlock loading){try{using var doc=await Data(game.Id+"_ru","https://store.steampowered.com/api/appdetails?appids="+game.Id+"&l=russian",1440);if(disposed)return;var entry=doc.RootElement.GetProperty(game.Id);if(!entry.GetProperty("success").GetBoolean())throw new Exception();var data=entry.GetProperty("data");loading.Visibility=Visibility.Collapsed;
  AddText(overviewPanel,Plain(Str(data,"short_description")),23,"#F2EFEA");
  var facts=new List<string>();foreach(var key in new[]{"developers","publishers"})if(data.TryGetProperty(key,out var list))facts.Add(string.Join(", ",list.EnumerateArray().Select(x=>x.GetString())));if(data.TryGetProperty("release_date",out var date))facts.Add(Str(date,"date"));if(data.TryGetProperty("genres",out var genres))facts.Add(string.Join(" · ",genres.EnumerateArray().Select(x=>Str(x,"description"))));if(facts.Count>0)AddText(overviewPanel,string.Join(" · ",facts),16,"#D9A86C");
  AddText(mediaPanel,"Скриншоты",24,"#F2EFEA");var thumbs=new WrapPanel();mediaPanel.Children.Add(thumbs);if(data.TryGetProperty("screenshots",out var screenshots))foreach(var shot in screenshots.EnumerateArray()){var url=Str(shot,"path_full");if(!Safe(url))continue;int at=shots.Count;shots.Add(url);var image=new Image{Width=260,Height=146,Stretch=Stretch.UniformToFill};var b=Btn("",()=>OpenShot(at));b.Padding=new Thickness(4);b.Content=image;thumbs.Children.Add(b);_=Picture(image,Str(shot,"path_thumbnail"));}if(shots.Count==0)AddText(mediaPanel,"Скриншоты недоступны.",17,"#A8A49E");
  AddText(mediaPanel,"Видео",24,"#F2EFEA");int count=0;var trailers=new WrapPanel();mediaPanel.Children.Add(trailers);if(data.TryGetProperty("movies",out var movies))foreach(var m in movies.EnumerateArray()){if(!m.TryGetProperty("mp4",out var mp4))continue;var url=Str(mp4,"480");if(url.Length==0)url=Str(mp4,"max");if(!Safe(url))continue;count++;string source=url;var b=Btn(Str(m,"name") is {Length:>0} n?n:"Смотреть трейлер",()=>{lastFocus=Keyboard.FocusedElement as UIElement;full.Visibility=Visibility.Collapsed;movie.Visibility=Visibility.Visible;viewer.Visibility=Visibility.Visible;movie.Volume=Experience.Volume;movie.Source=new Uri(source);paused=false;movie.Play();viewer.MoveFocus(new(FocusNavigationDirection.First));});trailers.Children.Add(b);}if(count==0)AddText(mediaPanel,"Прямой MP4-трейлер недоступен. HLS/DASH пока не поддерживаются.",17,"#A8A49E");
  var detailed=Str(data,"detailed_description");if(detailed.Length>0)descPanel.Children.Add(UiKit.Description(detailed));else AddText(descPanel,"Подробное описание недоступно.",18,"#A8A49E");
 }catch(OperationCanceledException){return;}catch{if(!disposed)loading.Text="Описание и медиа сейчас недоступны. Запуск игры от них не зависит.";}if(!disposed)await Reviews(game.Id);}
 async Task Reviews(string id){AddText(reviewsPanel,"Отзывы Steam",24,"#F2EFEA");var note=AddText(reviewsPanel,"До 10 полезных отзывов · русский язык · все способы приобретения",15,"#A8A49E");try{var input=JsonSerializer.Serialize(new{appid=uint.Parse(id),filter=0,languages=new[]{"russian"},num_per_page=10,purchase_type=1,display_language="russian"});using var doc=await Data(id+"_reviews_ru","https://api.steampowered.com/IUserReviewsService/GetAppReviews/v1/?input_json="+Uri.EscapeDataString(input),10);if(disposed)return;var root=doc.RootElement;if(root.TryGetProperty("response",out var response))root=response;if(!root.TryGetProperty("reviews",out var reviews)||reviews.GetArrayLength()==0){note.Text="Для выбранного языка отзывы не найдены или источник не вернул список.";return;}var reviewGrid=new WrapPanel();reviewsPanel.Children.Add(reviewGrid);foreach(var r in reviews.EnumerateArray())reviewGrid.Children.Add(ReviewCards.Make(r,id));}catch{if(!disposed)note.Text="Отзывы сейчас недоступны. Остальная карточка продолжает работать.";}}
 async Task Picture(Image image,string url,int version=-1){if(!Safe(url))return;try{await Images.WaitAsync(cancel.Token);try{var bytes=await Http.GetByteArrayAsync(url,cancel.Token);var result=await Task.Run(()=>{using var stream=new MemoryStream(bytes);var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.DecodePixelWidth=1600;b.StreamSource=stream;b.EndInit();b.Freeze();return b;});if(!disposed&&(version<0||version==imageVersion))image.Source=result;}finally{Images.Release();}}catch{}}
 void OpenShot(int at){if(shots.Count==0)return;if(viewer.Visibility!=Visibility.Visible)lastFocus=Keyboard.FocusedElement as UIElement;index=(at+shots.Count)%shots.Count;movie.Stop();movie.Visibility=Visibility.Collapsed;full.Visibility=Visibility.Visible;viewer.Visibility=Visibility.Visible;full.Source=null;_=Picture(full,shots[index],++imageVersion);Experience.Transition(viewer);viewer.MoveFocus(new(FocusNavigationDirection.First));}
 void Step(int delta){if(movie.Visibility!=Visibility.Visible)OpenShot(index+delta);}
 public bool HandlePad(int code){if(viewer.Visibility==Visibility.Visible){if(code==13){Step(-1);return true;}if(code==14){Step(1);return true;}return code==101||code==102;}if(code==101||code==102){scroll.ScrollToVerticalOffset(scroll.VerticalOffset+(code==101?-28:28));return true;}return false;}
 public bool HandleBack(){if(viewer.Visibility!=Visibility.Visible)return false;movie.Stop();viewer.Visibility=Visibility.Collapsed;Experience.Play("back");lastFocus?.Focus();return true;}
 public void Dispose(){if(disposed)return;disposed=true;cancel.Cancel();movie.Stop();}
}

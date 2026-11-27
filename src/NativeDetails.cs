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
 readonly ScrollViewer scroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,0,0,32)};
 readonly StackPanel content=new(){Margin=new Thickness(48,0,48,24)};
 readonly StackPanel mainHost=new(),reviewHost=new(),descHost=new();
 readonly List<List<Control>> rows=new();
 readonly List<Button> viewerButtons=new();
 readonly TextBlock factsText=new(){FontSize=18,Foreground=B("#8F98A0"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,18)};
 readonly Image banner=new(){Width=460,Height=215,Stretch=Stretch.UniformToFill};
 readonly Grid viewer=new(){Background=B("#0E141B"),Visibility=Visibility.Collapsed};
 readonly Image full=new(){Stretch=Stretch.Uniform};
 readonly MediaElement movie=new(){LoadedBehavior=MediaState.Manual,UnloadedBehavior=MediaState.Stop};
 readonly List<string> shots=new();
 UIElement? lastFocus;
 int index,imageVersion;bool disposed,loaded,paused;
 static string Str(JsonElement e,string k)=>e.TryGetProperty(k,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString()??"":"";
 static string Plain(string s)=>WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(s,@"<(br\s*/?|/p|/li|/div)>","\n",RegexOptions.IgnoreCase),"<[^>]+>",""));
 static bool Safe(string s)=>Uri.TryCreate(s,UriKind.Absolute,out var u)&&u.Scheme=="https"&&(u.Host.EndsWith(".steamstatic.com",StringComparison.OrdinalIgnoreCase)||u.Host.EndsWith(".akamaihd.net",StringComparison.OrdinalIgnoreCase));
 static IEnumerable<Button> Buttons(DependencyObject root){foreach(var child in LogicalTreeHelper.GetChildren(root)){if(child is Button b)yield return b;else if(child is DependencyObject d)foreach(var x in Buttons(d))yield return x;}}
 void FitHeight(){if(!double.IsNaN(Height))return;var w=Window.GetWindow(this);if(w==null)return;double total=w.Content is FrameworkElement root&&root.ActualHeight>0?root.ActualHeight:w.ActualHeight;Height=Math.Max(400,total-Margin.Top-Margin.Bottom);}
 public NativeDetails(Game game,Action launch,Func<Task> store,Action back){
  bool storeItem=game.Platform=="Магазин";
  Background=B("#171A21");
  RowDefinitions.Add(new(){Height=GridLength.Auto});
  RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  var header=new Grid{Margin=new Thickness(48,28,48,12)};Children.Add(header);
  header.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var left=new StackPanel();header.Children.Add(left);
  left.Children.Add(new TextBlock{Text=storeItem?"МАГАЗИН STEAM":game.State.ToUpperInvariant()+" · STEAM",FontSize=14,Foreground=B("#66C0F4"),Margin=new Thickness(0,0,0,6)});
  left.Children.Add(new TextBlock{Text=game.Name,FontSize=44,FontWeight=FontWeights.SemiBold,Foreground=B("#FFFFFF"),TextWrapping=TextWrapping.Wrap});
  left.Children.Add(factsText);
  var buttons=new WrapPanel();left.Children.Add(buttons);
  var backButton=Btn("Назад",back);
  var playButton=storeItem?Btn("Купить в Steam",()=>{_=store();},true):Btn(game.Installed?"Играть":"Установить через Steam",launch,true);
  buttons.Children.Add(backButton);buttons.Children.Add(playButton);
  if(storeItem)rows.Add(new List<Control>{backButton,playButton});
  else{var pageButton=Btn("Страница в Steam",()=>{_=store();});buttons.Children.Add(pageButton);rows.Add(new List<Control>{backButton,playButton,pageButton});}
  var bannerFrame=new Border{CornerRadius=new CornerRadius(6),ClipToBounds=true,Margin=new Thickness(32,0,0,0),Background=B("#16202D"),VerticalAlignment=VerticalAlignment.Top,Child=banner};Grid.SetColumn(bannerFrame,1);header.Children.Add(bannerFrame);
  Grid.SetRow(scroll,1);Children.Add(scroll);scroll.Content=content;
  var loading=AddText(content,"Загрузка описания…",20,"#8F98A0");
  content.Children.Add(mainHost);content.Children.Add(reviewHost);content.Children.Add(descHost);
  Grid.SetRowSpan(viewer,2);Children.Add(viewer);
  viewer.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  viewer.RowDefinitions.Add(new(){Height=GridLength.Auto});
  viewer.Children.Add(full);viewer.Children.Add(movie);movie.Visibility=Visibility.Collapsed;
  var controls=new WrapPanel{Margin=new Thickness(24,16,24,20),HorizontalAlignment=HorizontalAlignment.Center};Grid.SetRow(controls,1);viewer.Children.Add(controls);
  var close=Btn("Закрыть просмотр",()=>HandleBack());
  var prev=Btn("Предыдущий",()=>Step(-1));
  var next=Btn("Следующий",()=>Step(1));
  var pause=Btn("Пауза / продолжить",()=>{if(movie.Visibility!=Visibility.Visible)return;if(paused)movie.Play();else movie.Pause();paused=!paused;});
  foreach(var b in new[]{close,prev,next,pause}){controls.Children.Add(b);viewerButtons.Add(b);}
  movie.MediaFailed+=(_,_)=>{movie.Stop();movie.Visibility=Visibility.Collapsed;};
  PreviewMouseWheel+=(_,e)=>{if(viewer.Visibility==Visibility.Visible)Step(e.Delta>0?-1:1);else scroll.ScrollToVerticalOffset(scroll.VerticalOffset-e.Delta);e.Handled=true;};
  Loaded+=async(_,_)=>{if(loaded)return;loaded=true;FitHeight();playButton.Focus();await Load(game,loading);};
  Unloaded+=(_,_)=>movie.Stop();
 }
 static Button Btn(string text,Action action,bool primary=false){
  var b=new Button{Content=text,FontSize=18,FontWeight=primary?FontWeights.SemiBold:FontWeights.Normal,Padding=new Thickness(22,12,22,12),Margin=new Thickness(0,0,12,10),Foreground=primary?B("#FFFFFF"):B("#C6D4DF"),Background=B(primary?"#75B022":"#2A475E"),Cursor=Cursors.Hand};
  var border=new FrameworkElementFactory(typeof(Border));border.Name="Frame";border.SetValue(Border.CornerRadiusProperty,new CornerRadius(4));border.SetValue(Border.BorderThicknessProperty,new Thickness(2));border.SetValue(Border.BorderBrushProperty,Brushes.Transparent);
  border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
  border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
  var t=new ControlTemplate(typeof(Button)){VisualTree=border};
  var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.White,"Frame"));t.Triggers.Add(focus);
  b.Template=t;b.Click+=(_,_)=>{Experience.Play("confirm");action();};return b;
 }
 static TextBlock AddText(Panel target,string s,int size=20,string color="#C6D4DF"){
  var t=new TextBlock{Text=s,FontSize=size,LineHeight=size*1.45,Foreground=B(color),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,14)};
  target.Children.Add(t);return t;
 }
 static TextBlock AddHeading(Panel target,string s){
  var t=new TextBlock{Text=s,FontSize=26,FontWeight=FontWeights.SemiBold,Foreground=B("#FFFFFF"),Margin=new Thickness(0,30,0,12)};
  target.Children.Add(t);return t;
 }
 static StackPanel Strip(Panel host,string heading){
  AddHeading(host,heading);
  var panel=new StackPanel{Orientation=Orientation.Horizontal};
  host.Children.Add(new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=panel,Padding=new Thickness(0,4,0,8)});
  return panel;
 }
 async Task<JsonDocument> Data(string key,string url,int minutes){var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SteamConsoleLauncher","content");Directory.CreateDirectory(folder);var path=Path.Combine(folder,key+".json");if(File.Exists(path)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(path)<TimeSpan.FromMinutes(minutes))try{return JsonDocument.Parse(await File.ReadAllTextAsync(path,cancel.Token));}catch(JsonException){}try{var json=await Http.GetStringAsync(url,cancel.Token);var doc=JsonDocument.Parse(json);try{await File.WriteAllTextAsync(path,json,cancel.Token);}catch{}return doc;}catch{if(File.Exists(path))return JsonDocument.Parse(await File.ReadAllTextAsync(path,cancel.Token));throw;}}
 async Task Load(Game game,TextBlock loading){
  JsonDocument? doc=null;Exception? failure=null;
  try{
   var sources=new[]{(game.Id+"_ru2","https://store.steampowered.com/api/appdetails?appids="+game.Id+"&l=russian&cc=RU"),(game.Id+"_en","https://store.steampowered.com/api/appdetails?appids="+game.Id+"&l=english")};
   foreach(var (key,url) in sources){
    try{
     var candidate=await Data(key,url,1440);
     var item=candidate.RootElement.GetProperty(game.Id);
     if(item.GetProperty("success").GetBoolean()){doc=candidate;break;}
     candidate.Dispose();failure=new Exception("Steam не отдаёт описание этой игры для вашего региона");
    }catch(OperationCanceledException){return;}catch(Exception ex){failure=ex;}
   }
   if(disposed)return;
   if(doc==null)throw failure??new Exception("нет данных");
   using(doc){Build(loading,doc.RootElement.GetProperty(game.Id).GetProperty("data"));}
  }catch(OperationCanceledException){return;}
  catch(Exception ex){if(!disposed){loading.Text="Описание недоступно: "+ex.Message;_=Picture(banner,"https://shared.fastly.steamstatic.com/steam/apps/"+game.Id+"/header.jpg");}}
  if(!disposed)await Reviews(game.Id);
 }
 void Build(TextBlock loading,JsonElement data){
  loading.Visibility=Visibility.Collapsed;
  var facts=new List<string>();var makers=new List<string>();
  try{
   foreach(var key in new[]{"developers","publishers"})if(data.TryGetProperty(key,out var people))makers.AddRange(people.EnumerateArray().Select(x=>x.GetString()??"").Where(x=>x.Length>0));
   if(makers.Count>0)facts.Add(string.Join(", ",makers.Distinct()));
   if(data.TryGetProperty("release_date",out var date)&&Str(date,"date").Length>0)facts.Add(Str(date,"date"));
   if(data.TryGetProperty("genres",out var genres))facts.Add(string.Join(" · ",genres.EnumerateArray().Select(x=>Str(x,"description"))));
   if(data.TryGetProperty("is_free",out var free)&&free.ValueKind==JsonValueKind.True)facts.Add("Бесплатно");
   else if(data.TryGetProperty("price_overview",out var po)){var priceText=Str(po,"final_formatted");if(po.TryGetProperty("discount_percent",out var dp)&&dp.TryGetInt32(out var dv)&&dv>0)priceText+=" (-"+dv+"%)";if(priceText.Length>0)facts.Add(priceText);}
  }catch{}
  factsText.Text=string.Join(" · ",facts.Where(x=>x.Length>0));
  var headerImage=Str(data,"header_image");if(headerImage.Length>0)_=Picture(banner,headerImage);
  try{var shortText=Plain(Str(data,"short_description")).Trim();if(shortText.Length>0){var summary=AddText(mainHost,shortText,24,"#FFFFFF");summary.MaxWidth=1200;summary.HorizontalAlignment=HorizontalAlignment.Left;}}catch{}
  try{
   var thumbUrls=new List<string>();
   if(data.TryGetProperty("screenshots",out var screenshots))foreach(var shot in screenshots.EnumerateArray()){var src=Str(shot,"path_full");if(!Safe(src))continue;shots.Add(src);thumbUrls.Add(Str(shot,"path_thumbnail"));}
   if(shots.Count>0){
    var shotStrip=Strip(mainHost,"Скриншоты");var shotRow=new List<Control>();
    for(int at=0;at<Math.Min(8,shots.Count);at++){int pick=at;var image=new Image{Width=320,Height=180,Stretch=Stretch.UniformToFill};var b=Btn("",()=>OpenShot(pick));b.Padding=new Thickness(4);b.Content=image;shotStrip.Children.Add(b);shotRow.Add(b);_=Picture(image,thumbUrls[at]);}
    rows.Add(shotRow);
   }
  }catch{}
  try{
   StackPanel? trailerStrip=null;List<Control>? trailerRow=null;int count=0;
   if(data.TryGetProperty("movies",out var movies))foreach(var m in movies.EnumerateArray()){
    if(count>=3)break;
    if(!m.TryGetProperty("mp4",out var mp4))continue;
    var url=Str(mp4,"480");if(url.Length==0)url=Str(mp4,"max");if(!Safe(url))continue;
    if(trailerStrip==null){trailerStrip=Strip(mainHost,"Видео");trailerRow=new List<Control>();rows.Add(trailerRow);}
    count++;string source=url;var thumb=Str(m,"thumbnail");
    var preview=new Image{Width=320,Height=180,Stretch=Stretch.UniformToFill};var layer=new Grid();layer.Children.Add(preview);layer.Children.Add(new TextBlock{Text="▶",FontSize=48,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Opacity=.9});
    var tb=Btn("",()=>PlayMovie(source));tb.Padding=new Thickness(4);tb.Content=layer;trailerStrip.Children.Add(tb);trailerRow!.Add(tb);
    if(Safe(thumb))_=Picture(preview,thumb);
   }
  }catch{}
  try{var detailed=Str(data,"detailed_description");if(Plain(detailed).Trim().Length>300){AddHeading(descHost,"Об игре");descHost.Children.Add(UiKit.Description(detailed));}}catch{}
 }
 async Task Reviews(string id){
  try{
   var input=JsonSerializer.Serialize(new{appid=uint.Parse(id),filter=0,languages=new[]{"russian"},num_per_page=10,purchase_type=1,display_language="russian"});
   using var doc=await Data(id+"_reviews_ru","https://api.steampowered.com/IUserReviewsService/GetAppReviews/v1/?input_json="+Uri.EscapeDataString(input),10);
   if(disposed)return;
   var root=doc.RootElement;if(root.TryGetProperty("response",out var response))root=response;
   if(!root.TryGetProperty("reviews",out var reviews)||reviews.GetArrayLength()==0)return;
   var strip=Strip(reviewHost,"Отзывы Steam");var toggles=new List<Control>();var talks=new List<Control>();int n=0;
   foreach(var r in reviews.EnumerateArray()){var card=ReviewCards.Make(r,id);strip.Children.Add(card);var inside=Buttons(card).ToList();if(inside.Count>0)toggles.Add(inside[0]);if(inside.Count>1)talks.Add(inside[1]);if(++n>=8)break;}
   if(toggles.Count>0)rows.Add(toggles);if(talks.Count>0)rows.Add(talks);
  }catch{}
 }
 async Task Picture(Image image,string url,int version=-1){if(!Safe(url))return;try{await Images.WaitAsync(cancel.Token);try{var bytes=await Http.GetByteArrayAsync(url,cancel.Token);var result=await Task.Run(()=>{using var stream=new MemoryStream(bytes);var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.DecodePixelWidth=1600;b.StreamSource=stream;b.EndInit();b.Freeze();return b;});if(!disposed&&(version<0||version==imageVersion))image.Source=result;}finally{Images.Release();}}catch{}}
 void PlayMovie(string source){lastFocus=Keyboard.FocusedElement as UIElement;full.Visibility=Visibility.Collapsed;movie.Visibility=Visibility.Visible;viewer.Visibility=Visibility.Visible;movie.Volume=Experience.Volume;movie.Source=new Uri(source);paused=false;movie.Play();viewer.MoveFocus(new(FocusNavigationDirection.First));}
 void OpenShot(int at){if(shots.Count==0)return;if(viewer.Visibility!=Visibility.Visible)lastFocus=Keyboard.FocusedElement as UIElement;index=(at+shots.Count)%shots.Count;movie.Stop();movie.Visibility=Visibility.Collapsed;full.Visibility=Visibility.Visible;viewer.Visibility=Visibility.Visible;full.Source=null;_=Picture(full,shots[index],++imageVersion);Experience.Transition(viewer);viewer.MoveFocus(new(FocusNavigationDirection.First));}
 void Step(int delta){if(movie.Visibility!=Visibility.Visible)OpenShot(index+delta);}
 bool Move(int code){
  var all=rows.Where(r=>r.Count>0).ToList();if(all.Count==0)return false;
  var focused=Keyboard.FocusedElement as Control;int ri=-1,ci=-1;
  for(int i=0;i<all.Count&&focused!=null;i++){int j=all[i].IndexOf(focused);if(j>=0){ri=i;ci=j;break;}}
  if(ri<0){ri=0;ci=Math.Min(1,all[0].Count-1);}
  else if(code==11)ri=Math.Max(0,ri-1);
  else if(code==12)ri=Math.Min(all.Count-1,ri+1);
  else if(code==13)ci=Math.Max(0,ci-1);
  else ci=Math.Min(all[ri].Count-1,ci+1);
  ci=Math.Clamp(ci,0,all[ri].Count-1);
  var target=all[ri][ci];target.Focus();target.BringIntoView();Experience.Play("move");return true;
 }
 public bool HandlePad(int code){
  if(viewer.Visibility==Visibility.Visible){
   if(code==13){Step(-1);return true;}
   if(code==14){Step(1);return true;}
   if(code==11||code==12){int at=viewerButtons.IndexOf((Keyboard.FocusedElement as Button)!);at=at<0?(code==12?0:viewerButtons.Count-1):Math.Clamp(at+(code==12?1:-1),0,viewerButtons.Count-1);viewerButtons[at].Focus();return true;}
   return code==101||code==102||code==103||code==104;
  }
  if(code>=11&&code<=14)return Move(code);
  if(code==103||code==104){scroll.ScrollToVerticalOffset(scroll.VerticalOffset+(code==103?-1:1)*Math.Max(300,scroll.ViewportHeight*.8));Experience.Play("move");return true;}
  if(code==101||code==102){scroll.ScrollToVerticalOffset(scroll.VerticalOffset+(code==101?-40:40));return true;}
  return false;
 }
 public bool HandleBack(){if(viewer.Visibility!=Visibility.Visible)return false;movie.Stop();viewer.Visibility=Visibility.Collapsed;Experience.Play("back");lastFocus?.Focus();return true;}
 public void Dispose(){if(disposed)return;disposed=true;cancel.Cancel();movie.Stop();}
}

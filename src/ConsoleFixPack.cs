using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 readonly ProgressBar downloadProgress=new(){Minimum=0,Maximum=100,Height=12,Width=420,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,16),Visibility=Visibility.Collapsed};
 void InitFixPack(){list.PreviewMouseLeftButtonDown+=(_,e)=>{if(e.OriginalSource is DependencyObject source&&ItemsControl.ContainerFromElement(list,source) is ListBoxItem item){list.SelectedItem=item.DataContext;item.Focus();}};}
 bool RecoverPadFocus(int code){if(code<11||code>14||page=="store")return false;if(Keyboard.FocusedElement is UIElement element&&element.IsVisible&&Window.GetWindow(element)==this)return false;if(page=="library")FocusSelected();else detail.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));return true;}
 void AttachDownloadProgress(){if(downloadProgress.Parent is Panel old)old.Children.Remove(downloadProgress);detail.Children.Add(downloadProgress);}
 void BeginDownloadProgress(){downloadProgress.Value=0;downloadProgress.Visibility=Visibility.Visible;SetInstallEnabled(false);}
 void ReportDownloadProgress(int progress){Dispatcher.Invoke(()=>{downloadProgress.Value=Math.Clamp(progress,0,100);updateStatus.Text="Получение и подготовка обновления: "+Math.Clamp(progress,0,100)+"%";});}
 void SetInstallEnabled(bool enabled){foreach(var button in detail.Children.OfType<System.Windows.Controls.Button>())if((button.Content as string)=="Скачать обновление и перезапустить")button.IsEnabled=enabled;}
}
public static class ReviewCards{
 static readonly HttpClient Profiles=new(){Timeout=TimeSpan.FromSeconds(10)};
 static readonly SemaphoreSlim ProfileGate=new(2);
 static readonly System.Collections.Concurrent.ConcurrentDictionary<string,Lazy<Task<string?>>> Names=new();
 static string Field(JsonElement element,string key)=>element.TryGetProperty(key,out var value)?value.ValueKind==JsonValueKind.String?value.GetString()??"":value.ToString():"";
 static bool Numeric(string id)=>id.Length>0&&id.Length<=20&&id.All(char.IsAsciiDigit);
 public static FrameworkElement Make(JsonElement review,string appid){
  string steamid=review.TryGetProperty("author",out var author)?Field(author,"steamid"):"";string text=Field(review,"review");bool recommended=review.TryGetProperty("voted_up",out var up)&&up.ValueKind==JsonValueKind.True;
  var panel=new StackPanel();var name=new TextBlock{Text=Numeric(steamid)?"SteamID "+steamid:"Автор недоступен",FontSize=24,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap};panel.Children.Add(name);
  bool requested=false;name.Loaded+=async(_,_)=>{if(requested||!Numeric(steamid))return;requested=true;var nickname=await Names.GetOrAdd(steamid,id=>new Lazy<Task<string?>>(()=>Lookup(id))).Value;if(name.IsLoaded&&!string.IsNullOrWhiteSpace(nickname))name.Text=nickname;};
  panel.Children.Add(new TextBlock{Text=recommended?"Рекомендует":"Не рекомендует",FontSize=20,Foreground=new SolidColorBrush(recommended?Color.FromRgb(143,205,174):Color.FromRgb(229,161,153)),Margin=new Thickness(0,8,0,16)});
  var content=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,FontSize=23,LineHeight=34,MaxHeight=204,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=new SolidColorBrush(Color.FromRgb(220,226,235))};panel.Children.Add(content);
  bool expanded=false;var toggle=UiKit.Make("Читать полностью",()=>{});toggle.FontSize=20;toggle.HorizontalAlignment=HorizontalAlignment.Left;toggle.Margin=new Thickness(0,18,0,0);toggle.Click+=(_,_)=>{expanded=!expanded;content.MaxHeight=expanded?double.PositiveInfinity:204;content.TextTrimming=expanded?TextTrimming.None:TextTrimming.CharacterEllipsis;toggle.Content=expanded?"Свернуть отзыв":"Читать полностью";};panel.Children.Add(toggle);
  if(Numeric(steamid)&&Numeric(appid)){var discussion=UiKit.Make("Обсуждение в Steam",()=>{try{Process.Start(new ProcessStartInfo("steam://openurl/https://steamcommunity.com/profiles/"+steamid+"/recommended/"+appid+"/"){UseShellExecute=true});}catch{}});discussion.FontSize=20;discussion.HorizontalAlignment=HorizontalAlignment.Left;discussion.Margin=new Thickness(0,12,0,0);panel.Children.Add(discussion);}
  return new Border{Child=panel,Width=530,VerticalAlignment=VerticalAlignment.Top,Background=new SolidColorBrush(Color.FromRgb(21,30,43)),BorderBrush=new SolidColorBrush(Color.FromRgb(48,62,81)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(26),Margin=new Thickness(0,12,24,16)};
 }
 static async Task<string?> Lookup(string id){await ProfileGate.WaitAsync();try{var xml=await Profiles.GetStringAsync("https://steamcommunity.com/profiles/"+id+"?xml=1");using var input=new System.IO.StringReader(xml);using var reader=XmlReader.Create(input,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=524288});return XDocument.Load(reader).Root?.Element("steamID")?.Value;}catch{return null;}finally{ProfileGate.Release();}}
}

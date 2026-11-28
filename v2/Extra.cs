using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace ConsoleLauncher;
public sealed record LocalInfo(string Size,string State,string Tone,int Percent=-1,bool Update=false);
public sealed record ReviewInfo(string Text,string Detail,string Tone,DateTime When);
public sealed record NewsInfo(string Title,long Date,DateTime When);
public static class Extra{
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(12)};
 static readonly SemaphoreSlim ShotGate=new(3);
 static readonly ConcurrentDictionary<string,string> Support=new();
 static readonly ConcurrentDictionary<string,ReviewInfo> ReviewMemory=new();
 static Extra(){
  Web.DefaultRequestHeaders.UserAgent.ParseAdd("SteamConsoleLauncher/1.0");
 }
 public static async Task<string> ControllerSupport(string id){
  if(Support.TryGetValue(id,out var known))return known;
  var file=Path.Combine(Library.Root,"pad",id+".txt");
  try{
   if(File.Exists(file)){
    var saved=await File.ReadAllTextAsync(file);
    Support[id]=saved;
    return saved;
   }
  }catch{}
  try{
   var json=await Web.GetStringAsync("https://store.steampowered.com/api/appdetails?appids="+id+"&filters=categories");
   using var doc=JsonDocument.Parse(json);
   if(!doc.RootElement.TryGetProperty(id,out var entry)||!entry.TryGetProperty("data",out var data)||data.ValueKind!=JsonValueKind.Object)return "";
   string result="none";
   if(data.TryGetProperty("categories",out var list)&&list.ValueKind==JsonValueKind.Array){
    foreach(var item in list.EnumerateArray()){
     if(item.TryGetProperty("id",out var idValue)&&idValue.ValueKind==JsonValueKind.Number){
      int number=idValue.GetInt32();
      if(number==28){result="full";break;}
      if(number==18)result="partial";
     }
    }
   }
   Support[id]=result;
   try{
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    await File.WriteAllTextAsync(file,result);
   }catch{}
   return result;
  }catch{return "";}
 }
 public static async Task<List<string>> Screenshots(string id){
  var file=Path.Combine(Library.Root,"shots",id+".json");
  try{
   if(File.Exists(file)&&(DateTime.Now-File.GetLastWriteTime(file)).TotalDays<7){
    var saved=JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(file));
    if(saved!=null)return saved;
   }
  }catch{}
  var urls=new List<string>();
  try{
   var json=await Web.GetStringAsync("https://store.steampowered.com/api/appdetails?appids="+id+"&filters=screenshots");
   using var doc=JsonDocument.Parse(json);
   if(!doc.RootElement.TryGetProperty(id,out var entry)||!entry.TryGetProperty("data",out var data)||data.ValueKind!=JsonValueKind.Object)return urls;
   if(data.TryGetProperty("screenshots",out var list)&&list.ValueKind==JsonValueKind.Array){
    foreach(var item in list.EnumerateArray()){
     if(urls.Count>=8)break;
     if(item.TryGetProperty("path_full",out var path)&&path.GetString() is {Length:>0} link)urls.Add(link);
    }
   }
   try{
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    await File.WriteAllTextAsync(file,JsonSerializer.Serialize(urls));
   }catch{}
  }catch{}
  return urls;
 }
 public static async Task<BitmapImage?> Shot(string id,int index,string url){
  await ShotGate.WaitAsync();
  try{
   var folder=Path.Combine(Library.Root,"shots");
   var file=Path.Combine(folder,id+"_"+index+".bin");
   byte[] data;
   if(File.Exists(file))data=await File.ReadAllBytesAsync(file);
   else{
    data=await Web.GetByteArrayAsync(url);
    try{
     Directory.CreateDirectory(folder);
     await File.WriteAllBytesAsync(file,data);
    }catch{}
   }
   return await Task.Run(()=>{
    using var stream=new MemoryStream(data);
    var image=new BitmapImage();
    image.BeginInit();
    image.CacheOption=BitmapCacheOption.OnLoad;
    image.DecodePixelWidth=1920;
    image.StreamSource=stream;
    image.EndInit();
    image.Freeze();
    return image;
   });
  }catch{return null;}
  finally{ShotGate.Release();}
 }
 public static async Task<NewsInfo?> News(string id){
  var file=Path.Combine(Library.Root,"news",id+".json");
  try{
   if(File.Exists(file)){
    var saved=JsonSerializer.Deserialize<NewsInfo>(await File.ReadAllTextAsync(file));
    if(saved!=null&&(DateTime.Now-saved.When).TotalHours<6)return saved;
   }
  }catch{}
  try{
   var json=await Web.GetStringAsync("https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid="+id+"&count=1&maxlength=1&feeds=steam_community_announcements");
   using var doc=JsonDocument.Parse(json);
   var info=new NewsInfo("",0,DateTime.Now);
   if(doc.RootElement.TryGetProperty("appnews",out var news)&&news.TryGetProperty("newsitems",out var items)&&items.ValueKind==JsonValueKind.Array&&items.GetArrayLength()>0){
    var item=items[0];
    string title=item.TryGetProperty("title",out var titleValue)?titleValue.GetString()??"":"";
    info=new NewsInfo(title.Trim(),Long(item,"date"),DateTime.Now);
   }
   try{
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    await File.WriteAllTextAsync(file,JsonSerializer.Serialize(info));
   }catch{}
   return info;
  }catch{return null;}
 }
 static string Russian(string text)=>text switch{
  "Overwhelmingly Positive"=>"Крайне положительные",
  "Very Positive"=>"Очень положительные",
  "Positive"=>"Положительные",
  "Mostly Positive"=>"В основном положительные",
  "Mixed"=>"Смешанные",
  "Mostly Negative"=>"В основном отрицательные",
  "Negative"=>"Отрицательные",
  "Very Negative"=>"Очень отрицательные",
  "Overwhelmingly Negative"=>"Крайне отрицательные",
  _=>text
 };
 static long Long(JsonElement item,string name)=>item.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.Number?value.GetInt64():0;
 public static async Task<ReviewInfo?> Reviews(string id){
  if(ReviewMemory.TryGetValue(id,out var known))return known;
  var file=Path.Combine(Library.Root,"reviews",id+".json");
  try{
   if(File.Exists(file)){
    var saved=JsonSerializer.Deserialize<ReviewInfo>(await File.ReadAllTextAsync(file));
    if(saved!=null&&(DateTime.Now-saved.When).TotalDays<3){
     ReviewMemory[id]=saved;
     return saved;
    }
   }
  }catch{}
  try{
   var json=await Web.GetStringAsync("https://api.steampowered.com/IUserReviewsService/GetAppReviews/v1/?appid="+id+"&filter=1&languages[0]=all&purchase_type=1&num_per_page=1");
   using var doc=JsonDocument.Parse(json);
   if(!doc.RootElement.TryGetProperty("response",out var response)||!response.TryGetProperty("query_summary",out var summary))return null;
   long positive=Long(summary,"total_positive");
   long negative=Long(summary,"total_negative");
   long total=positive+negative;
   string desc=summary.TryGetProperty("review_score_desc",out var descValue)?descValue.GetString()??"":"";
   ReviewInfo info;
   if(total==0)info=new ReviewInfo("Нет отзывов","","#C6D4DF",DateTime.Now);
   else{
    int percent=(int)Math.Round(positive*100.0/total);
    string tone=percent>=70?"#C8F59B":percent>=40?"#FFB347":"#FF7A7A";
    string text=total<10?"Мало отзывов":Russian(desc);
    info=new ReviewInfo(text,percent+"% положительных · "+total.ToString("N0",CultureInfo.GetCultureInfo("ru-RU")),tone,DateTime.Now);
   }
   ReviewMemory[id]=info;
   try{
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    await File.WriteAllTextAsync(file,JsonSerializer.Serialize(info));
   }catch{}
   return info;
  }catch{return null;}
 }
 static string SteamPath()=>Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam");
 static string? Manifest(string id){
  try{
   var steam=SteamPath();
   var folders=new List<string>{Path.Combine(steam,"steamapps")};
   var vdf=Path.Combine(steam,"steamapps","libraryfolders.vdf");
   if(File.Exists(vdf)){
    foreach(var line in File.ReadAllLines(vdf)){
     var parts=line.Split('"');
     if(parts.Length>=4&&parts[1]=="path")folders.Add(Path.Combine(parts[3].Replace("\\\\","\\"),"steamapps"));
    }
   }
   foreach(var folder in folders){
    var file=Path.Combine(folder,"appmanifest_"+id+".acf");
    if(File.Exists(file))return file;
   }
  }catch{}
  return null;
 }
 static Dictionary<string,string> Fields(string text){
  var map=new Dictionary<string,string>();
  foreach(var line in text.Split('\n')){
   var parts=line.Split('"');
   if(parts.Length>=4)map.TryAdd(parts[1],parts[3]);
  }
  return map;
 }
 static long Number(Dictionary<string,string> map,string key)=>map.TryGetValue(key,out var value)&&long.TryParse(value,out var number)?number:0;
 static string Bytes(long value){
  double gigabytes=value/1073741824.0;
  if(gigabytes>=1)return gigabytes.ToString("0.#")+" ГБ";
  return (value/1048576.0).ToString("0")+" МБ";
 }
 public static LocalInfo? Local(string id){
  try{
   var file=Manifest(id);
   if(file==null)return null;
   string text;
   using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
   using(var reader=new StreamReader(stream))text=reader.ReadToEnd();
   var map=Fields(text);
   long flags=Number(map,"StateFlags");
   long size=Number(map,"SizeOnDisk");
   long toDownload=Number(map,"BytesToDownload");
   long downloaded=Number(map,"BytesDownloaded");
   string state;
   string tone="#C8F59B";
   int progress=-1;
   bool update=false;
   if((flags&512)!=0){state="Обновление на паузе";tone="#FFB347";}
   else if((flags&(256|1048576))!=0){
    int value=toDownload>0?(int)Math.Min(100,downloaded*100/toDownload):0;
    progress=value;
    state="Загрузка: "+value+"%";
    tone="#66C0F4";
   }
   else if((flags&2097152)!=0){state="Установка обновления";tone="#66C0F4";}
   else if((flags&2)!=0){state="Доступно обновление";tone="#FFB347";update=true;}
   else if((flags&4)!=0)state="Актуальна";
   else{state="Не готова";tone="#FF7A7A";}
   return new LocalInfo(size>0?Bytes(size):"—",state,tone,progress,update);
  }catch{return null;}
 }
}

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace ConsoleLauncher;
public static class Art{
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(15)};
 static readonly SemaphoreSlim Gate=new(4);
 static readonly SemaphoreSlim MatchGate=new(1);
 static readonly ConcurrentDictionary<string,Lazy<Task<BitmapImage?>>> Cache=new();
 static readonly ConcurrentDictionary<string,Lazy<Task<List<BitmapImage>>>> ArtCache=new();
 static readonly string[] Hosts={"https://shared.fastly.steamstatic.com","https://cdn.akamai.steamstatic.com"};
 static Dictionary<string,string>? matches;
 public static readonly ConcurrentDictionary<string,string> Names=new();
 static Art(){
  Web.DefaultRequestHeaders.UserAgent.ParseAdd("SteamConsoleLauncher/1.0");
 }
 public static Task<BitmapImage?> Wide(string id)=>Get(id+"_wide",new[]{"capsule_616x353.jpg","header.jpg"},id,600);
 public static Task<BitmapImage?> Hero(string id)=>Get(id+"_hero2",new[]{"library_hero_2x.jpg","library_hero.jpg"},id,2560);
 public static Task<BitmapImage?> Logo(string id)=>Get(id+"_logo",new[]{"logo.png","library_logo.png"},id,700);
 public static Task<List<BitmapImage>> Arts(string id)=>ArtCache.GetOrAdd(id,_=>new Lazy<Task<List<BitmapImage>>>(()=>LoadArts(id))).Value;
 static string Hash(string text)=>Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(text))).Substring(0,12);
 static string Clean(string name)=>Regex.Replace(Regex.Replace(name,"[™®©]",""),@"\s*\(.*?\)","").Trim();
 static async Task<List<string>> WallUrls(string name){
  var key=Norm(name);
  var urls=new List<string>();
  if(key.Length==0)return urls;
  var file=Path.Combine(Library.Root,"walls",Hash(key)+".json");
  try{
   if(File.Exists(file)){
    var cached=JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(file));
    if(cached!=null)return cached;
   }
  }catch{}
  bool reached=false;
  try{
   var json=await Web.GetStringAsync("https://wallhaven.cc/api/v1/search?q="+Uri.EscapeDataString(name)+"&categories=100&purity=100&atleast=1920x1080&ratios=16x9,16x10,21x9&sorting=relevance");
   reached=true;
   using var doc=JsonDocument.Parse(json);
   if(doc.RootElement.TryGetProperty("data",out var data)&&data.ValueKind==JsonValueKind.Array){
    foreach(var item in data.EnumerateArray()){
     if(urls.Count>=4)break;
     long size=item.TryGetProperty("file_size",out var sizeValue)&&sizeValue.ValueKind==JsonValueKind.Number?sizeValue.GetInt64():0;
     if(size>9000000)continue;
     if(item.TryGetProperty("path",out var path)&&path.GetString() is {Length:>0} link)urls.Add(link);
    }
   }
  }catch{}
  if(reached){
   try{
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    await File.WriteAllTextAsync(file,JsonSerializer.Serialize(urls));
   }catch{}
  }
  return urls;
 }
 static async Task<BitmapImage?> ArtImage(string tag,int index,string url){
  await Gate.WaitAsync();
  try{
   var folder=Path.Combine(Library.Root,"artwork");
   var file=Path.Combine(folder,tag+"_"+index+".bin");
   try{
    if(File.Exists(file)){
     var saved=await File.ReadAllBytesAsync(file);
     return await Task.Run(()=>Decode(saved,2560));
    }
    var data=await Web.GetByteArrayAsync(url);
    var image=await Task.Run(()=>Decode(data,2560));
    try{Directory.CreateDirectory(folder);await File.WriteAllBytesAsync(file,data);}catch{}
    return image;
   }catch{return null;}
  }finally{Gate.Release();}
 }
 static async Task<List<BitmapImage>> LoadArts(string id){
  var list=new List<BitmapImage>();
  if(!Names.TryGetValue(id,out var name)||name.Length==0)return list;
  var clean=Clean(name);
  var urls=await WallUrls(clean);
  var tag="wh_"+Hash(Norm(clean));
  for(int i=0;i<urls.Count;i++){
   var image=await ArtImage(tag,i,urls[i]);
   if(image!=null)list.Add(image);
  }
  return list;
 }
 static Task<BitmapImage?> Get(string key,string[] names,string id,int width){
  if(id.Length>0&&id.All(char.IsDigit))return Cache.GetOrAdd(key,_=>new Lazy<Task<BitmapImage?>>(()=>Load(key,names,id,width))).Value;
  return Cache.GetOrAdd(key,_=>new Lazy<Task<BitmapImage?>>(()=>Other(key,names,id,width))).Value;
 }
 static async Task<BitmapImage?> Other(string key,string[] names,string id,int width){
  var own=await Local(key,id,width);
  if(own!=null)return own;
  if(!Names.TryGetValue(id,out var name)||name.Length==0)return null;
  var app=await Match(name);
  if(app.Length==0)return null;
  return await Load(key,names,app,width);
 }
 static async Task<BitmapImage?> Local(string key,string id,int width){
  if(key.EndsWith("_logo")||!Stores.ArtPath.TryGetValue(id,out var path))return null;
  try{
   var bytes=await File.ReadAllBytesAsync(path);
   return await Task.Run(()=>Decode(bytes,width));
  }catch{return null;}
 }
 static string Norm(string text)=>new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
 static async Task<string> Match(string name){
  var key=Norm(name);
  if(key.Length==0)return "";
  await MatchGate.WaitAsync();
  try{
   var file=Path.Combine(Library.Root,"steam_match.json");
   if(matches==null){
    matches=new Dictionary<string,string>();
    try{
     if(File.Exists(file))matches=JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(file))??matches;
    }catch{}
   }
   if(matches.TryGetValue(key,out var known))return known;
   string found="";
   try{
    var json=await Web.GetStringAsync("https://store.steampowered.com/api/storesearch/?term="+Uri.EscapeDataString(name)+"&l=english&cc=US");
    using var doc=JsonDocument.Parse(json);
    if(doc.RootElement.TryGetProperty("items",out var items)&&items.ValueKind==JsonValueKind.Array){
     foreach(var item in items.EnumerateArray()){
      var title=item.TryGetProperty("name",out var titleValue)?titleValue.GetString()??"":"";
      if(Norm(title)==key&&item.TryGetProperty("id",out var idValue)&&idValue.ValueKind==JsonValueKind.Number){
       found=idValue.GetInt64().ToString();
       break;
      }
     }
    }
    matches[key]=found;
    try{
     Directory.CreateDirectory(Library.Root);
     await File.WriteAllTextAsync(file,JsonSerializer.Serialize(matches));
    }catch{}
   }catch{}
   return found;
  }finally{MatchGate.Release();}
 }
 static BitmapImage Decode(byte[] bytes,int width){
  using var stream=new MemoryStream(bytes);
  var image=new BitmapImage();
  image.BeginInit();
  image.CacheOption=BitmapCacheOption.OnLoad;
  image.DecodePixelWidth=width;
  image.StreamSource=stream;
  image.EndInit();
  image.Freeze();
  return image;
 }
 static byte[]? FromSteamCache(string id,string[] names){
  try{
   var steam=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam");
   var cache=Path.Combine(steam,"appcache","librarycache");
   foreach(var name in names){
    var flat=Path.Combine(cache,id+"_"+name);
    if(File.Exists(flat))return File.ReadAllBytes(flat);
    var dir=Path.Combine(cache,id);
    if(Directory.Exists(dir)){
     var hit=Directory.GetFiles(dir,name,SearchOption.AllDirectories).FirstOrDefault();
     if(hit!=null)return File.ReadAllBytes(hit);
    }
   }
  }catch{}
  return null;
 }
 static async Task<byte[]?> FromStoreApi(string id,bool big){
  try{
   var meta=await Web.GetStringAsync("https://store.steampowered.com/api/appdetails?appids="+id);
   using var doc=JsonDocument.Parse(meta);
   if(!doc.RootElement.TryGetProperty(id,out var entry)||!entry.TryGetProperty("data",out var data))return null;
   var urls=new List<string>();
   if(big&&data.TryGetProperty("background_raw",out var raw)&&raw.GetString() is {Length:>0} rawUrl)urls.Add(rawUrl);
   if(data.TryGetProperty("header_image",out var header)&&header.GetString() is {Length:>0} headerUrl)urls.Add(headerUrl);
   foreach(var url in urls){
    try{return await Web.GetByteArrayAsync(url);}catch{}
   }
  }catch{}
  return null;
 }
 static async Task<BitmapImage?> Load(string key,string[] names,string id,int width){
  await Gate.WaitAsync();
  try{
   var folder=Path.Combine(Library.Root,"artwork");
   var file=Path.Combine(folder,key+".bin");
   if(File.Exists(file)){
    try{
     var saved=await File.ReadAllBytesAsync(file);
     return await Task.Run(()=>Decode(saved,width));
    }catch{try{File.Delete(file);}catch{}}
   }
   foreach(var name in names){
    foreach(var host in Hosts){
     try{
      var data=await Web.GetByteArrayAsync(host+"/steam/apps/"+id+"/"+name);
      var image=await Task.Run(()=>Decode(data,width));
      try{Directory.CreateDirectory(folder);await File.WriteAllBytesAsync(file,data);}catch{}
      return image;
     }catch{}
    }
   }
   if(key.EndsWith("_logo"))return null;
   byte[]? fallback;
   if(key.Contains("_hero")){
    fallback=FromSteamCache(id,new[]{"library_hero.jpg"});
    fallback??=await FromStoreApi(id,true);
    fallback??=FromSteamCache(id,new[]{"library_header.jpg","header.jpg"});
   }else{
    fallback=FromSteamCache(id,new[]{"library_header.jpg","header.jpg"});
    fallback??=await FromStoreApi(id,false);
   }
   if(fallback!=null){
    try{
     var bytes=fallback;
     var image=await Task.Run(()=>Decode(bytes,width));
     try{Directory.CreateDirectory(folder);await File.WriteAllBytesAsync(file,bytes);}catch{}
     return image;
    }catch{}
   }
   return null;
  }finally{Gate.Release();}
 }
}

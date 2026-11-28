using System.Collections.Concurrent;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace ConsoleLauncher;
public static class Art{
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(12)};
 static readonly SemaphoreSlim Gate=new(4);
 static readonly SemaphoreSlim MatchGate=new(1);
 static readonly SemaphoreSlim GridGate=new(1);
 static readonly ConcurrentDictionary<string,Lazy<Task<BitmapImage?>>> Cache=new();
 static readonly ConcurrentDictionary<string,Lazy<Task<List<BitmapImage>>>> ArtCache=new();
 static readonly string[] Hosts={"https://shared.fastly.steamstatic.com","https://cdn.akamai.steamstatic.com"};
 static Dictionary<string,string>? matches;
 static Dictionary<string,string>? gridIds;
 static string apiKey="";
 public static readonly ConcurrentDictionary<string,string> Names=new();
 public static bool HasKey=>apiKey.Length>0;
 static string KeyFile=>Path.Combine(Library.Root,"steamgriddb.key");
 public static void LoadKey(){
  try{
   if(File.Exists(KeyFile))apiKey=File.ReadAllText(KeyFile).Trim();
  }catch{}
 }
 public static void SetKey(string value){
  apiKey=value.Trim();
  try{
   Directory.CreateDirectory(Library.Root);
   File.WriteAllText(KeyFile,apiKey);
  }catch{}
  ArtCache.Clear();
 }
 public static Task<BitmapImage?> Wide(string id)=>Get(id+"_wide",new[]{"capsule_616x353.jpg","header.jpg"},id,600);
 public static Task<BitmapImage?> Hero(string id)=>Get(id+"_hero2",new[]{"library_hero_2x.jpg","library_hero.jpg"},id,2560);
 public static Task<BitmapImage?> Logo(string id)=>Get(id+"_logo",new[]{"logo.png","library_logo.png"},id,700);
 public static Task<List<BitmapImage>> Arts(string id)=>ArtCache.GetOrAdd(id,_=>new Lazy<Task<List<BitmapImage>>>(()=>LoadArts(id))).Value;
 static async Task<string> GridGet(string path){
  try{
   using var request=new HttpRequestMessage(HttpMethod.Get,"https://www.steamgriddb.com/api/v2"+path);
   request.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",apiKey);
   using var response=await Web.SendAsync(request);
   if(!response.IsSuccessStatusCode)return "";
   return await response.Content.ReadAsStringAsync();
  }catch{return "";}
 }
 static async Task<string> GridGame(string id){
  bool steam=id.Length>0&&id.All(char.IsDigit);
  string name="";
  if(!steam){
   if(!Names.TryGetValue(id,out var known0)||known0.Length==0)return "";
   name=known0;
  }
  var key=steam?"steam:"+id:"name:"+Norm(name);
  await GridGate.WaitAsync();
  try{
   var file=Path.Combine(Library.Root,"sgdb_ids.json");
   if(gridIds==null){
    gridIds=new Dictionary<string,string>();
    try{
     if(File.Exists(file))gridIds=JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(file))??gridIds;
    }catch{}
   }
   if(gridIds.TryGetValue(key,out var known)&&known.Length>0)return known;
   string found="";
   try{
    if(steam){
     var json=await GridGet("/games/steam/"+id);
     if(json.Length>0){
      using var doc=JsonDocument.Parse(json);
      if(doc.RootElement.TryGetProperty("data",out var data)&&data.TryGetProperty("id",out var idValue)&&idValue.ValueKind==JsonValueKind.Number)found=idValue.GetInt64().ToString();
     }
    }else{
     var json=await GridGet("/search/autocomplete/"+Uri.EscapeDataString(name));
     if(json.Length>0){
      using var doc=JsonDocument.Parse(json);
      if(doc.RootElement.TryGetProperty("data",out var data)&&data.ValueKind==JsonValueKind.Array){
       foreach(var item in data.EnumerateArray()){
        var title=item.TryGetProperty("name",out var titleValue)?titleValue.GetString()??"":"";
        if(Norm(title)==Norm(name)&&item.TryGetProperty("id",out var itemId)&&itemId.ValueKind==JsonValueKind.Number){
         found=itemId.GetInt64().ToString();
         break;
        }
       }
      }
     }
    }
   }catch{}
   if(found.Length>0){
    gridIds[key]=found;
    try{
     Directory.CreateDirectory(Library.Root);
     await File.WriteAllTextAsync(file,JsonSerializer.Serialize(gridIds));
    }catch{}
   }
   return found;
  }finally{GridGate.Release();}
 }
 static async Task<List<string>> GridHeroes(string gid){
  var file=Path.Combine(Library.Root,"sgdb",gid+".json");
  try{
   if(File.Exists(file)){
    var cached=JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(file));
    if(cached!=null&&cached.Count>0)return cached;
   }
  }catch{}
  var urls=new List<string>();
  try{
   var json=await GridGet("/heroes/game/"+gid+"?dimensions=1920x620,3840x1240&types=static&nsfw=false&humor=false");
   if(json.Length>0){
    using var doc=JsonDocument.Parse(json);
    if(doc.RootElement.TryGetProperty("data",out var data)&&data.ValueKind==JsonValueKind.Array){
     foreach(var item in data.EnumerateArray()){
      if(urls.Count>=4)break;
      if(item.TryGetProperty("url",out var url)&&url.GetString() is {Length:>0} link)urls.Add(link);
     }
    }
   }
   if(urls.Count>0){
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    await File.WriteAllTextAsync(file,JsonSerializer.Serialize(urls));
   }
  }catch{}
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
  if(!HasKey)return list;
  var gid=await GridGame(id);
  if(gid.Length==0)return list;
  var urls=await GridHeroes(gid);
  for(int i=0;i<urls.Count;i++){
   var image=await ArtImage("sgdb_"+gid,i,urls[i]);
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

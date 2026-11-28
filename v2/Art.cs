using System.Collections.Concurrent;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace ConsoleLauncher;
public static class Art{
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(12)};
 static readonly SemaphoreSlim Gate=new(4);
 static readonly ConcurrentDictionary<string,Lazy<Task<BitmapImage?>>> Cache=new();
 static readonly string[] Hosts={"https://shared.fastly.steamstatic.com","https://cdn.akamai.steamstatic.com"};
 public static Task<BitmapImage?> Wide(string id)=>Get(id+"_wide",new[]{"capsule_616x353.jpg","header.jpg"},id,600);
 public static Task<BitmapImage?> Hero(string id)=>Get(id+"_hero",new[]{"library_hero.jpg","header.jpg"},id,1600);
 public static Task<BitmapImage?> Logo(string id)=>Get(id+"_logo",new[]{"logo.png","library_logo.png"},id,700);
 static Task<BitmapImage?> Get(string key,string[] names,string id,int width)=>Cache.GetOrAdd(key,_=>new Lazy<Task<BitmapImage?>>(()=>Load(key,names,id,width))).Value;
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
 static async Task<byte[]?> FromStoreApi(string id){
  try{
   var meta=await Web.GetStringAsync("https://store.steampowered.com/api/appdetails?appids="+id+"&filters=basic");
   using var doc=JsonDocument.Parse(meta);
   if(doc.RootElement.TryGetProperty(id,out var entry)&&entry.TryGetProperty("data",out var data)&&data.TryGetProperty("header_image",out var image)&&image.GetString() is {Length:>0} url)return await Web.GetByteArrayAsync(url);
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
   byte[]? fallback=FromSteamCache(id,key.EndsWith("_hero")?new[]{"library_hero.jpg","library_header.jpg","header.jpg"}:new[]{"library_header.jpg","header.jpg"});
   fallback??=await FromStoreApi(id);
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

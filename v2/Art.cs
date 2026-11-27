using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
namespace ConsoleLauncher;
public static class Art{
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(12)};
 static readonly SemaphoreSlim Gate=new(4);
 static readonly ConcurrentDictionary<string,Lazy<Task<BitmapImage?>>> Cache=new();
 static readonly string[] Hosts={"https://shared.fastly.steamstatic.com","https://cdn.akamai.steamstatic.com"};
 public static Task<BitmapImage?> Wide(string id)=>Get(id+"_wide",new[]{"capsule_616x353.jpg","header.jpg"},id,600);
 public static Task<BitmapImage?> Hero(string id)=>Get(id+"_hero",new[]{"library_hero.jpg","header.jpg"},id,1600);
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
   return null;
  }finally{Gate.Release();}
 }
}

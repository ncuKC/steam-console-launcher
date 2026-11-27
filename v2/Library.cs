using System.Text.Json;
using Microsoft.Win32;
namespace ConsoleLauncher;
public sealed record Game(string Id,string Name,bool Installed,long LastPlayed);
public static class Library{
 public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SteamConsoleLauncher");
 sealed class Saved{public string Id{get;set;}="";public string Name{get;set;}="";public long LastPlayed{get;set;}}
 static string SteamPath()=>Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam");
 static Dictionary<string,string> Fields(string text){
  var map=new Dictionary<string,string>();
  foreach(var line in text.Split('\n')){
   var parts=line.Split('"');
   if(parts.Length>=4)map.TryAdd(parts[1],parts[3]);
  }
  return map;
 }
 static List<Game> Scan(){
  var steam=SteamPath();
  var folders=new HashSet<string>(StringComparer.OrdinalIgnoreCase){Path.Combine(steam,"steamapps")};
  var vdf=Path.Combine(steam,"steamapps","libraryfolders.vdf");
  if(File.Exists(vdf)){
   foreach(var line in File.ReadAllText(vdf).Split('\n')){
    var parts=line.Split('"');
    if(parts.Length>=4&&parts[1]=="path")folders.Add(Path.Combine(parts[3].Replace("\\\\","\\"),"steamapps"));
   }
  }
  var result=new List<Game>();
  foreach(var folder in folders){
   try{
    if(!Directory.Exists(folder))continue;
    foreach(var manifest in Directory.GetFiles(folder,"appmanifest_*.acf")){
     try{
      var fields=Fields(File.ReadAllText(manifest));
      if(fields.TryGetValue("appid",out var id)&&fields.TryGetValue("StateFlags",out var rawFlags)&&int.TryParse(rawFlags,out var flags)&&(flags&4)!=0){
       long updated=0;
       if(fields.TryGetValue("LastUpdated",out var rawUpdated))long.TryParse(rawUpdated,out updated);
       result.Add(new Game(id,fields.TryGetValue("name",out var name)?name:id,true,updated));
      }
     }catch(IOException){}catch(UnauthorizedAccessException){}
    }
   }catch(IOException){}catch(UnauthorizedAccessException){}
  }
  return result;
 }
 public static async Task<List<Game>> Load(){
  var local=await Task.Run(Scan);
  var saved=new List<Saved>();
  var file=Path.Combine(Root,"library.json");
  if(File.Exists(file)){
   try{saved=JsonSerializer.Deserialize<List<Saved>>(await File.ReadAllTextAsync(file),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??new List<Saved>();}catch(JsonException){}
  }
  var played=new Dictionary<string,long>();
  var playedFile=Path.Combine(Root,"last_played.json");
  if(File.Exists(playedFile)){
   try{played=JsonSerializer.Deserialize<Dictionary<string,long>>(await File.ReadAllTextAsync(playedFile))??new Dictionary<string,long>();}catch(JsonException){}
  }
  var installed=local.Select(item=>item.Id).ToHashSet();
  var all=saved.Select(item=>new Game(item.Id,item.Name,false,item.LastPlayed)).Concat(local);
  return all
   .Where(item=>item.Id.Length>0&&item.Id!="228980")
   .GroupBy(item=>item.Id)
   .Select(group=>{
    long last=Math.Max(group.Max(x=>x.LastPlayed),played.TryGetValue(group.Key,out var stamp)?stamp:0);
    return new Game(group.Key,group.Last().Name,installed.Contains(group.Key),last);
   })
   .OrderByDescending(item=>item.LastPlayed)
   .ThenBy(item=>item.Name)
   .ToList();
 }
 public static void RecordPlayed(string id){
  try{
   var file=Path.Combine(Root,"last_played.json");
   var map=new Dictionary<string,long>();
   if(File.Exists(file))map=JsonSerializer.Deserialize<Dictionary<string,long>>(File.ReadAllText(file))??new Dictionary<string,long>();
   map[id]=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
   Directory.CreateDirectory(Root);
   File.WriteAllText(file,JsonSerializer.Serialize(map));
  }catch{}
 }
}

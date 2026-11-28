using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ConsoleLauncher;
public sealed record GameInfo(string Description,string Genres,string Developer,string Year);
public static class Details{
 static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(10)};
 static readonly Dictionary<string,GameInfo> Memory=new();
 static string CacheDir=>Path.Combine(Library.Root,"info");
 public static async Task<GameInfo?> Get(string id){
  if(Memory.TryGetValue(id,out var known))return known;
  var file=Path.Combine(CacheDir,id+".json");
  try{
   if(File.Exists(file)){
    var cached=JsonSerializer.Deserialize<GameInfo>(await File.ReadAllTextAsync(file));
    if(cached!=null){Memory[id]=cached;return cached;}
   }
  }catch{}
  try{
   var json=await Http.GetStringAsync("https://store.steampowered.com/api/appdetails?appids="+id+"&l=russian");
   using var doc=JsonDocument.Parse(json);
   var entry=doc.RootElement.GetProperty(id);
   if(!entry.GetProperty("success").GetBoolean())return null;
   var data=entry.GetProperty("data");
   string Text(string name)=>data.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
   var description=Regex.Replace(System.Net.WebUtility.HtmlDecode(Text("short_description")),"<.*?>","").Trim();
   var genreNames=new List<string>();
   if(data.TryGetProperty("genres",out var genreList)&&genreList.ValueKind==JsonValueKind.Array){
    foreach(var genre in genreList.EnumerateArray().Take(3)){
     if(genre.TryGetProperty("description",out var label)&&label.GetString() is string name&&name.Length>0)genreNames.Add(name);
    }
   }
   var developer="";
   if(data.TryGetProperty("developers",out var devs)&&devs.ValueKind==JsonValueKind.Array&&devs.GetArrayLength()>0)developer=devs[0].GetString()??"";
   var year="";
   if(data.TryGetProperty("release_date",out var release)&&release.TryGetProperty("date",out var date)){
    var match=Regex.Match(date.GetString()??"",@"\d{4}");
    if(match.Success)year=match.Value;
   }
   var info=new GameInfo(description,string.Join(", ",genreNames),developer,year);
   Memory[id]=info;
   Directory.CreateDirectory(CacheDir);
   await File.WriteAllTextAsync(file,JsonSerializer.Serialize(info));
   return info;
  }catch{return null;}
 }
}
public static class Favorites{
 static HashSet<string>? items;
 static string FilePath=>Path.Combine(Library.Root,"favorites.json");
 static HashSet<string> Items(){
  if(items!=null)return items;
  items=new HashSet<string>();
  try{
   if(File.Exists(FilePath))items=JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(FilePath))??new HashSet<string>();
  }catch{}
  return items;
 }
 public static bool Has(string id)=>Items().Contains(id);
 public static bool Toggle(string id){
  var set=Items();
  bool added=!set.Remove(id);
  if(added)set.Add(id);
  try{
   Directory.CreateDirectory(Library.Root);
   File.WriteAllText(FilePath,JsonSerializer.Serialize(set));
  }catch{}
  return added;
 }
}

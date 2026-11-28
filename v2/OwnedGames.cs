using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Win32;
namespace ConsoleLauncher;
public static class OwnedGames{
 sealed class Item{public string Id{get;set;}="";public string Name{get;set;}="";}
 sealed class Saved{public DateTime When{get;set;}public List<Item> Games{get;set;}=new();}
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(8)};
 static DateTime failedAt=DateTime.MinValue;
 static string SteamPath()=>Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam");
 static string? SteamId(){
  try{
   var steam=SteamPath();
   var users=Path.Combine(steam,"config","loginusers.vdf");
   if(File.Exists(users)){
    string? current=null;
    foreach(var line in File.ReadAllLines(users)){
     var parts=line.Split('"');
     if(parts.Length<4&&parts.Length>=2&&parts[1].Length==17&&parts[1].All(char.IsDigit))current=parts[1];
     else if(parts.Length>=4&&parts[1]=="MostRecent"&&parts[3]=="1"&&current!=null)return current;
    }
   }
   var userdata=Path.Combine(steam,"userdata");
   if(Directory.Exists(userdata)){
    var dir=Directory.GetDirectories(userdata).Select(path=>new DirectoryInfo(path)).Where(info=>long.TryParse(info.Name,out var number)&&number>0).OrderByDescending(info=>info.LastWriteTimeUtc).FirstOrDefault();
    if(dir!=null)return (76561197960265728L+long.Parse(dir.Name)).ToString();
   }
  }catch{}
  return null;
 }
 static async Task<List<Item>?> Fetch(){
  var id=SteamId();
  if(id==null)return null;
  try{
   var text=await Web.GetStringAsync("https://steamcommunity.com/profiles/"+id+"/games/?tab=all&xml=1");
   var doc=XDocument.Parse(text);
   var list=new List<Item>();
   foreach(var game in doc.Descendants("game")){
    var appId=(string?)game.Element("appID");
    var name=(string?)game.Element("name");
    if(!string.IsNullOrWhiteSpace(appId)&&!string.IsNullOrWhiteSpace(name))list.Add(new Item{Id=appId.Trim(),Name=name.Trim()});
   }
   return list.Count>0?list:null;
  }catch{return null;}
 }
 public static async Task<List<(string Id,string Name)>> Get(){
  var file=Path.Combine(Library.Root,"owned.json");
  Saved? saved=null;
  try{
   if(File.Exists(file))saved=JsonSerializer.Deserialize<Saved>(await File.ReadAllTextAsync(file));
  }catch{}
  if(saved!=null&&(DateTime.Now-saved.When).TotalHours<12)return saved.Games.Select(item=>(item.Id,item.Name)).ToList();
  if((DateTime.Now-failedAt).TotalMinutes>10){
   var fresh=await Fetch();
   if(fresh!=null){
    try{
     Directory.CreateDirectory(Library.Root);
     await File.WriteAllTextAsync(file,JsonSerializer.Serialize(new Saved{When=DateTime.Now,Games=fresh}));
    }catch{}
    return fresh.Select(item=>(item.Id,item.Name)).ToList();
   }
   failedAt=DateTime.Now;
  }
  return saved!=null?saved.Games.Select(item=>(item.Id,item.Name)).ToList():new List<(string Id,string Name)>();
 }
}

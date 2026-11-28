using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Win32;
namespace ConsoleLauncher;
public sealed record LocalInfo(string Size,string State,string Tone);
public static class Extra{
 static readonly HttpClient Web=new(){Timeout=TimeSpan.FromSeconds(12)};
 static readonly ConcurrentDictionary<string,string> Support=new();
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
   if((flags&512)!=0){state="Обновление на паузе";tone="#FFB347";}
   else if((flags&(256|1048576))!=0){
    int percent=toDownload>0?(int)Math.Min(100,downloaded*100/toDownload):0;
    state="Загрузка: "+percent+"%";
    tone="#66C0F4";
   }
   else if((flags&2097152)!=0){state="Установка обновления";tone="#66C0F4";}
   else if((flags&2)!=0){state="Доступно обновление";tone="#FFB347";}
   else if((flags&4)!=0)state="Актуальна";
   else{state="Не готова";tone="#FF7A7A";}
   return new LocalInfo(size>0?Bytes(size):"—",state,tone);
  }catch{return null;}
 }
}

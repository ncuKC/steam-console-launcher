using System.Text;
using Microsoft.Win32;
namespace ConsoleLauncher;
public sealed record PlayInfo(int Minutes,string Cloud);
public static class Stats{
 sealed class Node{
  public string? Value;
  public Dictionary<string,Node> Children=new(StringComparer.OrdinalIgnoreCase);
 }
 static readonly object Gate=new();
 static Node? cache;
 static string cacheFile="";
 static DateTime cacheStamp;
 static Node Parse(string text){
  var root=new Node();
  var stack=new Stack<Node>();
  stack.Push(root);
  string? key=null;
  int i=0;
  while(i<text.Length){
   char c=text[i];
   if(c=='"'){
    int end=i+1;
    var sb=new StringBuilder();
    while(end<text.Length&&text[end]!='"'){
     if(text[end]=='\\'&&end+1<text.Length)end++;
     sb.Append(text[end]);
     end++;
    }
    i=end+1;
    var token=sb.ToString();
    if(key==null)key=token;
    else{
     stack.Peek().Children[key]=new Node{Value=token};
     key=null;
    }
   }else if(c=='{'){
    var node=new Node();
    if(key!=null)stack.Peek().Children[key]=node;
    stack.Push(node);
    key=null;
    i++;
   }else if(c=='}'){
    if(stack.Count>1)stack.Pop();
    i++;
   }else i++;
  }
  return root;
 }
 static string? LocalConfig(){
  try{
   var steam=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam");
   var users=Path.Combine(steam,"userdata");
   if(!Directory.Exists(users))return null;
   string? best=null;
   DateTime bestStamp=DateTime.MinValue;
   foreach(var dir in Directory.GetDirectories(users)){
    var file=Path.Combine(dir,"config","localconfig.vdf");
    if(!File.Exists(file))continue;
    var stamp=File.GetLastWriteTimeUtc(file);
    if(best==null||stamp>bestStamp){best=file;bestStamp=stamp;}
   }
   return best;
  }catch{return null;}
 }
 static Node? Apps(){
  lock(Gate){
   var file=LocalConfig();
   if(file==null)return null;
   var stamp=File.GetLastWriteTimeUtc(file);
   if(cache==null||cacheFile!=file||cacheStamp!=stamp){
    string text;
    using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
    using(var reader=new StreamReader(stream))text=reader.ReadToEnd();
    cache=Parse(text);
    cacheFile=file;
    cacheStamp=stamp;
   }
   var node=cache;
   foreach(var name in new[]{"UserLocalConfigStore","Software","Valve","Steam","apps"}){
    if(node.Children.TryGetValue(name,out var next))node=next;
    else return null;
   }
   return node;
  }
 }
 public static Dictionary<string,long> LastPlayedAll(){
  var map=new Dictionary<string,long>();
  try{
   var apps=Apps();
   if(apps==null)return map;
   foreach(var pair in apps.Children){
    if(pair.Value.Children.TryGetValue("LastPlayed",out var value)&&long.TryParse(value.Value,out var stamp)&&stamp>0)map[pair.Key]=stamp;
   }
  }catch{}
  return map;
 }
 public static Task<PlayInfo?> Get(string id)=>Task.Run(()=>{
  try{
   var apps=Apps();
   if(apps==null||!apps.Children.TryGetValue(id,out var app))return null;
   int minutes=0;
   if(app.Children.TryGetValue("Playtime",out var played)&&int.TryParse(played.Value,out var parsed))minutes=parsed;
   var cloud="";
   if(app.Children.TryGetValue("cloud",out var block)&&block.Children.TryGetValue("last_sync_state",out var state)&&state.Value!=null)cloud=state.Value;
   return new PlayInfo(minutes,cloud);
  }catch{return null;}
 });
}

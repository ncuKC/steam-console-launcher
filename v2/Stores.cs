using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;
namespace ConsoleLauncher;
public static class Stores{
 public static readonly string OwnDir=Path.Combine(Library.Root,"Own");
 public static readonly ConcurrentDictionary<string,string> ArtPath=new();
 static string Safe(string text)=>new string(text.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());
 public static List<Game> Scan(){
  var list=new List<Game>();
  try{list.AddRange(Epic());}catch{}
  try{list.AddRange(Gog());}catch{}
  try{list.AddRange(Own());}catch{}
  return list;
 }
 static List<Game> Epic(){
  var list=new List<Game>();
  var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Epic","EpicGamesLauncher","Data","Manifests");
  if(!Directory.Exists(dir))return list;
  foreach(var file in Directory.GetFiles(dir,"*.item")){
   try{
    using var doc=JsonDocument.Parse(File.ReadAllText(file));
    var root=doc.RootElement;
    string Text(string field)=>root.TryGetProperty(field,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
    var app=Text("AppName");
    var name=Text("DisplayName");
    var main=Text("MainGameAppName");
    var exe=Text("LaunchExecutable");
    var location=Text("InstallLocation");
    var space=Text("CatalogNamespace");
    var item=Text("CatalogItemId");
    bool incomplete=root.TryGetProperty("bIsIncompleteInstall",out var flag)&&flag.ValueKind==JsonValueKind.True;
    if(app.Length==0||name.Length==0||exe.Length==0||incomplete)continue;
    if(main.Length>0&&main!=app)continue;
    if(location.Length==0||!Directory.Exists(location))continue;
    var path=space.Length>0&&item.Length>0?space+"%3A"+item+"%3A"+app:app;
    list.Add(new Game("epic_"+Safe(app),name,true,0,"Epic Games","com.epicgames.launcher://apps/"+path+"?action=launch&silent=true","",location,location));
   }catch{}
  }
  return list;
 }
 static List<Game> Gog(){
  var list=new List<Game>();
  using var games=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games")??Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\Games");
  if(games==null)return list;
  foreach(var sub in games.GetSubKeyNames()){
   try{
    using var key=games.OpenSubKey(sub);
    if(key==null)continue;
    var id=key.GetValue("gameID") as string??sub;
    var name=key.GetValue("gameName") as string??"";
    var path=key.GetValue("path") as string??"";
    var exe=key.GetValue("exe") as string??"";
    var args=key.GetValue("launchParam") as string??"";
    var depends=key.GetValue("dependsOn") as string??"";
    if(name.Length==0||exe.Length==0||depends.Length>0)continue;
    if(!Path.IsPathRooted(exe))exe=Path.Combine(path,exe);
    if(!File.Exists(exe))continue;
    list.Add(new Game("gog_"+Safe(id),name,true,0,"GOG",exe,args,path,path));
   }catch{}
  }
  return list;
 }
 static bool IsSystemFolder(string folder){
  var windows=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
  return folder.Length<=3||folder.StartsWith(windows,StringComparison.OrdinalIgnoreCase);
 }
 static string ShortcutTarget(string path){
  try{
   var type=Type.GetTypeFromProgID("WScript.Shell");
   if(type==null)return "";
   dynamic shell=Activator.CreateInstance(type)!;
   dynamic link=shell.CreateShortcut(path);
   string target=link.TargetPath;
   return target??"";
  }catch{return "";}
 }
 static List<Game> Own(){
  var list=new List<Game>();
  Directory.CreateDirectory(OwnDir);
  var allowed=new[]{".lnk",".exe",".url",".bat"};
  foreach(var file in Directory.GetFiles(OwnDir)){
   var ext=Path.GetExtension(file).ToLowerInvariant();
   if(!allowed.Contains(ext))continue;
   var name=Path.GetFileNameWithoutExtension(file);
   var id="own_"+Safe(name);
   foreach(var imageExt in new[]{".png",".jpg",".jpeg"}){
    var image=Path.Combine(OwnDir,name+imageExt);
    if(File.Exists(image)){ArtPath[id]=image;break;}
   }
   string watch="";
   try{
    string? folder=null;
    if(ext==".exe")folder=Path.GetDirectoryName(file);
    else if(ext==".lnk"){
     var target=ShortcutTarget(file);
     if(target.Length>0&&File.Exists(target))folder=Path.GetDirectoryName(target);
    }
    if(!string.IsNullOrEmpty(folder)&&!IsSystemFolder(folder))watch=folder;
   }catch{}
   list.Add(new Game(id,name,true,0,"Свои",file,"",OwnDir,watch));
  }
  return list;
 }
 public static void Start(Game game){
  try{
   var info=new ProcessStartInfo(game.Launch){UseShellExecute=true};
   if(game.Args.Length>0)info.Arguments=game.Args;
   if(game.Dir.Length>0&&Directory.Exists(game.Dir))info.WorkingDirectory=game.Dir;
   Process.Start(info);
  }catch{}
 }
 public static void OpenOwn(){
  try{
   Directory.CreateDirectory(OwnDir);
   Process.Start(new ProcessStartInfo(OwnDir){UseShellExecute=true});
  }catch{}
 }
}

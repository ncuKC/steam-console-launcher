using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Web.WebView2.Core;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 async Task<bool> NavigateAndWait(string url){
  var core=browser.CoreWebView2;if(core==null)return false;
  var tcs=new TaskCompletionSource<bool>();
  void Handler(object? s,CoreWebView2NavigationCompletedEventArgs e){core.NavigationCompleted-=Handler;tcs.TrySetResult(e.IsSuccess);}
  core.NavigationCompleted+=Handler;
  core.Navigate(url);
  var done=await Task.WhenAny(tcs.Task,Task.Delay(20000));
  if(done!=tcs.Task){core.NavigationCompleted-=Handler;return false;}
  return tcs.Task.Result;
 }
 async Task<string> EvalString(string script){
  var core=browser.CoreWebView2;if(core==null)return "";
  var args=JsonSerializer.Serialize(new{expression=script,awaitPromise=true,returnByValue=true});
  var raw=await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate",args);
  using var doc=JsonDocument.Parse(raw);
  if(doc.RootElement.TryGetProperty("result",out var r)&&r.TryGetProperty("value",out var v)&&v.ValueKind==JsonValueKind.String)return v.GetString()??"";
  return "";
 }
 static List<Game> ParseGamesXml(string text){
  var list=new List<Game>();
  var xml=XDocument.Parse(text);
  foreach(var g in xml.Descendants("game")){
   var id=((string?)g.Element("appID"))??"";var name=(string?)g.Element("name");
   if(id.Length>0&&id.All(char.IsDigit))list.Add(new Game(id,string.IsNullOrWhiteSpace(name)?"Без названия":name.Trim(),false,0,"Steam"));
  }
  return list;
 }
 static List<Game> ParseGamesJson(string text){
  var list=new List<Game>();
  using var doc=JsonDocument.Parse(text);
  if(!doc.RootElement.TryGetProperty("rgGames",out var arr)||arr.ValueKind!=JsonValueKind.Array)return list;
  foreach(var g in arr.EnumerateArray()){
   if(!g.TryGetProperty("appid",out var a))continue;
   var id=a.ToString();var name=g.TryGetProperty("name",out var n)?n.GetString():null;
   long last=g.TryGetProperty("rtime_last_played",out var rt)&&rt.TryGetInt64(out var rv)?rv:0;
   if(id.Length>0&&id.All(char.IsDigit))list.Add(new Game(id,string.IsNullOrWhiteSpace(name)?"Без названия":name.Trim(),false,last,"Steam"));
  }
  return list;
 }
 async Task ImportFromSteamSession(){
  if(steamSession==null){authStatus.Text="Сначала войдите в Steam.";return;}
  menuVisible=false;
  string id=steamSession.SteamId;
  var found=new List<Game>();
  try{
   await Store("https://steamcommunity.com/");
   webStatus.Text="Импорт игр из аккаунта Steam…";
   if(await NavigateAndWait("https://steamcommunity.com/profiles/"+id+"/games/?tab=all")){
    var xmlText=await EvalString("(async()=>{try{const r=await fetch('/profiles/"+id+"/games/?tab=all&xml=1',{credentials:'include'});return await r.text();}catch(e){return '';}})()");
    if(xmlText.TrimStart().StartsWith("<"))try{found=ParseGamesXml(xmlText);}catch{}
    if(found.Count==0){
     var json=await EvalString("(document.querySelector('#gameslist_config')?.getAttribute('data-profile-gameslist'))||''");
     if(json.Length>0)try{found=ParseGamesJson(json);}catch{}
    }
   }
  }catch{}
  found=found.GroupBy(g=>g.Id).Select(g=>g.First()).ToList();
  if(found.Count==0){
   Back();FocusSelected();
   status.Text="Не удалось получить список игр из аккаунта. Проверьте, что вы вошли в Steam и список игр в профиле не скрыт. Запасной способ: кнопка «Аккаунт» (импорт по SteamID64 и ключу API).";
   return;
  }
  try{
   Directory.CreateDirectory(Root);
   var file=Path.Combine(Root,"library.json");
   await File.WriteAllTextAsync(file+".tmp",JsonSerializer.Serialize(found));
   File.Move(file+".tmp",file,true);
   home=false;
   await Reload();
   Back();FocusSelected();
   status.Text="Импортировано игр из аккаунта Steam: "+found.Count;
  }catch{
   Back();FocusSelected();
   status.Text="Игры получены, но сохранить библиотеку не удалось.";
  }
 }
}

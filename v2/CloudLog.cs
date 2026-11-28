using System.Text.RegularExpressions;
using Microsoft.Win32;
namespace ConsoleLauncher;
public sealed record SyncInfo(string State,string Text,DateTime When,int Retries);
public static class CloudLog{
 static readonly Regex Stamp=new(@"^\[(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)\]");
 static readonly Regex Head=new(@"^\[[^\]]*\]\s*\[AppID \d+\]\s*");
 static string? LogFile(){
  try{
   var steam=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam");
   var file=Path.Combine(steam,"logs","cloud_log.txt");
   return File.Exists(file)?file:null;
  }catch{return null;}
 }
 static DateTime When(string line){
  var match=Stamp.Match(line.TrimStart());
  return match.Success&&DateTime.TryParse(match.Groups[1].Value,out var value)?value:DateTime.MinValue;
 }
 static string Classify(string low){
  if(low.Contains("conflict"))return "conflict";
  if(low.Contains("fail")||low.Contains("error")||low.Contains("unable")||low.Contains("timed out")||low.Contains("timeout")||low.Contains("could not"))return "error";
  if(low.Contains("retry"))return "retry";
  if(low.Contains("complete")||low.Contains("success")||low.Contains("finished")||low.Contains("done"))return "ok";
  if(low.Contains("upload")||low.Contains("download")||low.Contains("sync")||low.Contains("batch")||low.Contains("begin"))return "run";
  return "";
 }
 public static SyncInfo? Status(string id){
  try{
   var file=LogFile();
   if(file==null)return null;
   string text;
   using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)){
    stream.Seek(Math.Max(0,stream.Length-250000),SeekOrigin.Begin);
    using var reader=new StreamReader(stream);
    text=reader.ReadToEnd();
   }
   var tag="[AppID "+id+"]";
   var lines=text.Split('\n').Where(line=>line.Contains(tag)).TakeLast(16).Select(line=>line.TrimEnd('\r')).ToList();
   if(lines.Count==0)return null;
   int retries=lines.Count(line=>line.Contains("retry",StringComparison.OrdinalIgnoreCase));
   for(int i=lines.Count-1;i>=0;i--){
    var state=Classify(lines[i].ToLowerInvariant());
    if(state.Length==0)continue;
    var when=When(lines[i]);
    if(state=="run"&&when!=DateTime.MinValue&&(DateTime.Now-when).TotalSeconds>90)state="ok";
    return new SyncInfo(state,Head.Replace(lines[i].TrimStart(),""),when,retries);
   }
   return new SyncInfo("",Head.Replace(lines[^1].TrimStart(),""),When(lines[^1]),retries);
  }catch{return null;}
 }
 public static string Tone(SyncInfo? info){
  if(info==null)return "#C6D4DF";
  return info.State switch{"error"=>"#FF7A7A","conflict"=>"#FFB347","ok"=>"#7BD88F","run"=>"#66C0F4","retry"=>"#66C0F4",_=>"#C6D4DF"};
 }
 public static string Describe(SyncInfo? info){
  if(info==null)return "Сейвы: нет записей в журнале Steam";
  var label=info.State switch{
   "conflict"=>"Конфликт сохранений",
   "error"=>"Ошибка синхронизации",
   "retry"=>"Повтор синхронизации",
   "run"=>"Синхронизация сохранений…",
   "ok"=>"Сохранения синхронизированы",
   _=>"Сейвы: есть запись в журнале"
  };
  var result=label;
  if(info.Retries>0&&(info.State=="retry"||info.State=="error"))result+=" (повтор "+info.Retries+")";
  if(info.When!=DateTime.MinValue)result+=" · "+info.When.ToString("HH:mm");
  if((info.State=="error"||info.State=="conflict")&&info.Text.Length>0)result+="\n"+(info.Text.Length>90?info.Text.Substring(0,90)+"…":info.Text);
  return result;
 }
}

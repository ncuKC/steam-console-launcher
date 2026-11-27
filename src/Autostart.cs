using Microsoft.Win32;
namespace ConsoleLauncher;
static class Autostart{
 const string KeyPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
 const string ValueName="SteamConsoleLauncher";
 public static bool Enabled{get{try{using var key=Registry.CurrentUser.OpenSubKey(KeyPath);return key?.GetValue(ValueName)!=null;}catch{return false;}}}
 public static bool Set(bool on){
  try{
   using var key=Registry.CurrentUser.CreateSubKey(KeyPath);
   if(key==null)return false;
   if(on){var exe=Environment.ProcessPath;if(string.IsNullOrEmpty(exe))return false;key.SetValue(ValueName,"\""+exe+"\"");}
   else key.DeleteValue(ValueName,false);
   return true;
  }catch{return false;}
 }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
namespace ConsoleLauncher;
public static class Native{
 delegate bool EnumProc(IntPtr window,IntPtr param);
 [DllImport("user32.dll")]
 static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")]
 static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")]
 static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]
 static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
 [DllImport("user32.dll")]
 static extern bool AttachThreadInput(uint from,uint to,bool attach);
 [DllImport("user32.dll")]
 static extern bool ShowWindow(IntPtr window,int command);
 [DllImport("user32.dll")]
 static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")]
 static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")]
 static extern bool EnumWindows(EnumProc callback,IntPtr param);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]
 static extern int GetWindowText(IntPtr window,StringBuilder text,int max);
 [DllImport("kernel32.dll")]
 static extern uint GetCurrentThreadId();
 [DllImport("kernel32.dll",SetLastError=true)]
 static extern IntPtr OpenProcess(uint access,bool inherit,int id);
 [DllImport("kernel32.dll",SetLastError=true)]
 static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
 static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder name,ref uint size);
 public static void Raise(Window window){
  try{
   var handle=new WindowInteropHelper(window).Handle;
   if(handle==IntPtr.Zero)return;
   SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010);
   if(HideSteam())Focus(handle);
  }catch{}
 }
 static void Focus(IntPtr handle){
  var front=GetForegroundWindow();
  uint frontThread=front==IntPtr.Zero?0:GetWindowThreadProcessId(front,out _);
  uint mine=GetCurrentThreadId();
  bool attached=frontThread!=0&&frontThread!=mine&&AttachThreadInput(mine,frontThread,true);
  SetForegroundWindow(handle);
  if(attached)AttachThreadInput(mine,frontThread,false);
 }
 static bool HideSteam(){
  bool hid=false;
  EnumWindows((window,_)=>{
   try{
    if(!IsWindowVisible(window)||IsIconic(window))return true;
    var title=new StringBuilder(64);
    GetWindowText(window,title,64);
    if(title.ToString()!="Steam")return true;
    GetWindowThreadProcessId(window,out uint id);
    using var process=Process.GetProcessById((int)id);
    var name=process.ProcessName.ToLowerInvariant();
    if(name!="steam"&&name!="steamwebhelper")return true;
    ShowWindow(window,6);
    hid=true;
   }catch{}
   return true;
  },IntPtr.Zero);
  return hid;
 }
 public static bool AnyUnder(string dir){
  if(dir.Length==0)return false;
  var prefix=dir.TrimEnd('\\')+"\\";
  foreach(var process in Process.GetProcesses()){
   try{
    var handle=OpenProcess(0x1000,false,process.Id);
    if(handle==IntPtr.Zero)continue;
    try{
     var name=new StringBuilder(1024);
     uint size=1024;
     if(QueryFullProcessImageName(handle,0,name,ref size)&&name.ToString().StartsWith(prefix,StringComparison.OrdinalIgnoreCase))return true;
    }finally{CloseHandle(handle);}
   }catch{}
   finally{process.Dispose();}
  }
  return false;
 }
}

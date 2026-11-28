using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
namespace ConsoleLauncher;
public static class Native{
 [DllImport("user32.dll")]
 static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
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
  }catch{}
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

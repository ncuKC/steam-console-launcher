using System.Runtime.InteropServices;
using System.Text;
namespace ConsoleLauncher;
static class GameReturn{
 public static IntPtr Last;
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int cmd);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder text,int max);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool BringWindowToTop(IntPtr h);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")]static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
 public static void MinimizeForeground(){
  try{
   var h=GetForegroundWindow();if(h==IntPtr.Zero)return;
   GetWindowThreadProcessId(h,out var pid);
   if(pid==(uint)Environment.ProcessId)return;
   var text=new StringBuilder(256);GetClassName(h,text,256);
   var name=text.ToString();
   if(name=="Shell_TrayWnd"||name=="Shell_SecondaryTrayWnd"||name=="Progman"||name=="WorkerW")return;
   Last=h;
   ShowWindow(h,6);
  }catch{}
 }
 public static bool CloseLast(){
  var h=Last;Last=IntPtr.Zero;
  if(h==IntPtr.Zero)return false;
  try{if(!IsWindow(h))return false;PostMessage(h,0x0010,IntPtr.Zero,IntPtr.Zero);return true;}catch{return false;}
 }
 public static void Force(IntPtr h){
  try{keybd_event(0x12,0,0,UIntPtr.Zero);keybd_event(0x12,0,2,UIntPtr.Zero);BringWindowToTop(h);SetForegroundWindow(h);}catch{}
 }
}

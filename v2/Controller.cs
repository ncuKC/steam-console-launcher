using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
namespace ConsoleLauncher;
public enum PadStyle{Xbox,PlayStation,Nintendo}
public static class Controller{
 [StructLayout(LayoutKind.Sequential)]
 struct RawDevice{public IntPtr Handle;public uint Type;}
 [DllImport("user32.dll",SetLastError=true)]
 static extern uint GetRawInputDeviceList([Out] RawDevice[]? list,ref uint count,uint size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
 static extern uint GetRawInputDeviceInfo(IntPtr device,uint command,StringBuilder? data,ref uint size);
 static SolidColorBrush Make(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
 static readonly Brush[] XboxTints={Make("#6DC84B"),Make("#E0483E"),Make("#3F8FE0"),Make("#F2C230")};
 static readonly Brush[] PlayTints={Make("#7CA3E8"),Make("#E8707A"),Make("#E69BD0"),Make("#5CC9A6")};
 public static PadStyle Style{get;private set;}=PadStyle.Xbox;
 public static bool Refresh(){
  var next=Detect();
  bool changed=next!=Style;
  Style=next;
  return changed;
 }
 static PadStyle Detect(){
  try{
   uint size=(uint)Marshal.SizeOf<RawDevice>();
   uint count=0;
   if(GetRawInputDeviceList(null,ref count,size)!=0||count==0)return PadStyle.Xbox;
   var list=new RawDevice[count];
   uint got=GetRawInputDeviceList(list,ref count,size);
   if(got==uint.MaxValue)return PadStyle.Xbox;
   bool sony=false,nintendo=false;
   for(int i=0;i<got&&i<list.Length;i++){
    if(list[i].Type!=2)continue;
    uint length=0;
    GetRawInputDeviceInfo(list[i].Handle,0x20000007,null,ref length);
    if(length==0)continue;
    var builder=new StringBuilder((int)length);
    if(GetRawInputDeviceInfo(list[i].Handle,0x20000007,builder,ref length)==uint.MaxValue)continue;
    var name=builder.ToString().ToUpperInvariant();
    if(name.Contains("VID_054C")&&(name.Contains("PID_05C4")||name.Contains("PID_09CC")||name.Contains("PID_0BA0")||name.Contains("PID_0CE6")||name.Contains("PID_0DF2")))sony=true;
    else if(name.Contains("VID_057E"))nintendo=true;
   }
   if(sony)return PadStyle.PlayStation;
   if(nintendo)return PadStyle.Nintendo;
  }catch{}
  return PadStyle.Xbox;
 }
 public static string Label(string key){
  if(Style==PadStyle.PlayStation){
   return key switch{"A"=>"✕","B"=>"○","X"=>"□","Y"=>"△","LB/RB"=>"L1/R1",_=>key};
  }
  if(Style==PadStyle.Nintendo){
   return key switch{"A"=>"B","B"=>"A","X"=>"Y","Y"=>"X","LB/RB"=>"L/R","≡"=>"+",_=>key};
  }
  return key;
 }
 public static Brush Tint(string key){
  int slot=key switch{"A"=>0,"B"=>1,"X"=>2,"Y"=>3,_=>-1};
  if(slot<0)return Brushes.White;
  if(Style==PadStyle.Xbox)return XboxTints[slot];
  if(Style==PadStyle.PlayStation)return PlayTints[slot];
  return Brushes.White;
 }
}

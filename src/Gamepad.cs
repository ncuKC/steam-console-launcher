using System.Runtime.InteropServices;
namespace ConsoleLauncher;
public sealed class Gamepad:IDisposable{
 const string Dll="SDL3.dll";
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_Init(uint flags);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern void SDL_Quit();
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern void SDL_PumpEvents();
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern IntPtr SDL_GetGamepads(out int count);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern void SDL_free(IntPtr ptr);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern IntPtr SDL_OpenGamepad(uint id);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern void SDL_CloseGamepad(IntPtr pad);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_GamepadConnected(IntPtr pad);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_GetGamepadButton(IntPtr pad,int button);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern short SDL_GetGamepadAxis(IntPtr pad,int axis);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]static extern bool SDL_PollEvent(IntPtr ev);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)]static extern int SDL_GetGamepadButtonLabel(IntPtr gamepad,int button);public string ButtonLabel(int code){int label=pad==IntPtr.Zero?0:SDL_GetGamepadButtonLabel(pad,code);return label switch{1=>"A",2=>"B",3=>"X",4=>"Y",5=>"×",6=>"○",7=>"□",8=>"△",_=>new[]{"A","B","X","Y"}[Math.Clamp(code,0,3)]};}IntPtr pad;bool ready;bool ltBefore,rtBefore;readonly bool[] before=new bool[26];long next;
 public double LeftX,LeftY,RightX,RightY;public bool SuppressLeftStick;
 static double Norm(int v){double d=v/32767.0;return Math.Abs(d)<.2?0:d;}
 public Gamepad(){try{ready=SDL_Init(0x00002000);}catch(DllNotFoundException){}catch(BadImageFormatException){}}
 public IEnumerable<int> Tick(bool active){
  LeftX=LeftY=RightX=RightY=0;
  var pressed=new List<int>();if(!ready)return pressed;
  SDL_PumpEvents();var ev=Marshal.AllocHGlobal(128);try{while(SDL_PollEvent(ev)){}}finally{Marshal.FreeHGlobal(ev);}
  if(pad!=IntPtr.Zero&&!SDL_GamepadConnected(pad)){SDL_CloseGamepad(pad);pad=IntPtr.Zero;Array.Clear(before);ltBefore=rtBefore=false;}
  if(pad==IntPtr.Zero){var ids=SDL_GetGamepads(out int count);try{if(count>0&&ids!=IntPtr.Zero)pad=SDL_OpenGamepad(unchecked((uint)Marshal.ReadInt32(ids)));}finally{if(ids!=IntPtr.Zero)SDL_free(ids);}}
  if(pad==IntPtr.Zero)return pressed;
  for(int i=0;i<26;i++){bool down=SDL_GetGamepadButton(pad,i);if(active&&down&&!before[i])pressed.Add(i);before[i]=down;}
  bool lt=SDL_GetGamepadAxis(pad,4)>14000,rt=SDL_GetGamepadAxis(pad,5)>14000;
  if(active&&lt&&!ltBefore)pressed.Add(103);if(active&&rt&&!rtBefore)pressed.Add(104);
  ltBefore=lt;rtBefore=rt;
  if(active){LeftX=Norm(SDL_GetGamepadAxis(pad,0));LeftY=Norm(SDL_GetGamepadAxis(pad,1));RightX=Norm(SDL_GetGamepadAxis(pad,2));RightY=Norm(SDL_GetGamepadAxis(pad,3));}
  long now=Environment.TickCount64;
  if(active&&now>=next&&!SuppressLeftStick){int x=SDL_GetGamepadAxis(pad,0),y=SDL_GetGamepadAxis(pad,1);int dir=Math.Abs(x)>Math.Abs(y)?(x>12000?14:x< -12000?13:-1):(y>12000?12:y< -12000?11:-1);if(dir>=0){pressed.Add(dir);next=now+180;}}
  if(active){int rightY=SDL_GetGamepadAxis(pad,3);if(rightY < -12000)pressed.Add(101);else if(rightY > 12000)pressed.Add(102);}return pressed;}
 public void Dispose(){if(!ready)return;if(pad!=IntPtr.Zero)SDL_CloseGamepad(pad);SDL_Quit();ready=false;}
}

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
 IntPtr pad;bool ready;readonly bool[] before=new bool[26];long next;
 public Gamepad(){try{ready=SDL_Init(0x00002000);}catch(DllNotFoundException){}catch(BadImageFormatException){}}
 public IEnumerable<int> Tick(bool active){var pressed=new List<int>();if(!ready)return pressed;SDL_PumpEvents();var ev=Marshal.AllocHGlobal(128);try{while(SDL_PollEvent(ev)){}}finally{Marshal.FreeHGlobal(ev);}if(pad!=IntPtr.Zero&&!SDL_GamepadConnected(pad)){SDL_CloseGamepad(pad);pad=IntPtr.Zero;Array.Clear(before);}if(pad==IntPtr.Zero){var ids=SDL_GetGamepads(out int count);try{if(count>0&&ids!=IntPtr.Zero)pad=SDL_OpenGamepad(unchecked((uint)Marshal.ReadInt32(ids)));}finally{if(ids!=IntPtr.Zero)SDL_free(ids);}}if(pad==IntPtr.Zero)return pressed;for(int i=0;i<26;i++){bool down=SDL_GetGamepadButton(pad,i);if(active&&down&&!before[i])pressed.Add(i);before[i]=down;}long now=Environment.TickCount64;if(active&&now>=next){int x=SDL_GetGamepadAxis(pad,0),y=SDL_GetGamepadAxis(pad,1);int dir=Math.Abs(x)>Math.Abs(y)?(x>12000?14:x< -12000?13:-1):(y>12000?12:y< -12000?11:-1);if(dir>=0){pressed.Add(dir);next=now+180;}}return pressed;}
 public void Dispose(){if(!ready)return;if(pad!=IntPtr.Zero)SDL_CloseGamepad(pad);SDL_Quit();ready=false;}
}

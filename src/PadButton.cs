namespace ConsoleLauncher;
// Button codes used by Gamepad.Tick(). 0-14 match SDL3 SDL_GamepadButton; 101+ are synthetic actions.
public static class PadButton{
 public const int South=0,East=1,West=2,North=3,Back=4,Guide=5,Start=6,LeftStick=7,RightStick=8,LeftShoulder=9,RightShoulder=10;
 public const int DpadUp=11,DpadDown=12,DpadLeft=13,DpadRight=14;
 public const int ScrollUp=101,ScrollDown=102,LeftTrigger=103,RightTrigger=104;
 public static bool IsDirection(int code)=>code>=DpadUp&&code<=DpadRight;
}

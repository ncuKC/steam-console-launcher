using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 DispatcherTimer? comboTimer;int comboTicks;bool comboFired;
 void InitReturnCombo(){
  if(comboTimer!=null)return;
  comboTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
  comboTimer.Tick+=(_,_)=>{
   bool held=pad!=null&&pad.ComboHeld();
   if(!held){comboTicks=0;comboFired=false;return;}
   if(comboFired)return;
   if(++comboTicks<12)return;
   comboFired=true;
   if(IsActive&&WindowState!=WindowState.Minimized)return;
   try{var self=new WindowInteropHelper(this).Handle;GameReturn.MinimizeForeground();Show();WindowState=WindowState.Maximized;Activate();SetForegroundWindow(self);GameReturn.Force(self);if(page=="library")FocusSelected();}catch{}
  };
  comboTimer.Start();
 }
 void AddPowerMenu(){
  var b=Button("Питание ПК",OpenPowerMenu);
  b.HorizontalAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(0,0,0,16);
  detail.Children.Add(b);
 }
 void OpenPowerMenu(){
  detail.Children.Clear();detail.Margin=new Thickness(60);
  detail.Children.Add(new TextBlock{Text="Питание ПК",FontSize=36,Foreground=Brushes.White,Margin=new Thickness(0,0,0,28)});
  var back=Button("Назад",OpenConsoleSettings);back.HorizontalAlignment=HorizontalAlignment.Left;back.Margin=new Thickness(0,0,0,16);detail.Children.Add(back);
  AddPowerItem("Сон","rundll32.exe","powrprof.dll,SetSuspendState 0,1,0");
  AddPowerItem("Перезагрузить ПК","shutdown.exe","/r /t 0");
  AddPowerItem("Выключить ПК","shutdown.exe","/s /t 0");
  detail.Children.Add(new TextBlock{Text="Для защиты от случайного нажатия действие выполняется со второго нажатия.",FontSize=15,Foreground=B("#8F98A0"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
  detail.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
 }
 void AddPowerItem(string caption,string file,string args){
  System.Windows.Controls.Button? item=null;DispatcherTimer? reset=null;bool armed=false;
  item=Button(caption,()=>{
   if(!armed){
    armed=true;item!.Content="Нажмите ещё раз: "+caption;
    reset?.Stop();reset=new DispatcherTimer{Interval=TimeSpan.FromSeconds(4)};
    reset.Tick+=(_,_)=>{reset?.Stop();armed=false;item!.Content=caption;};
    reset.Start();return;
   }
   reset?.Stop();armed=false;item!.Content=caption;
   try{Process.Start(new ProcessStartInfo(file,args){UseShellExecute=false,CreateNoWindow=true});}catch{}
  });
  item.HorizontalAlignment=HorizontalAlignment.Left;item.Margin=new Thickness(0,0,0,16);
  detail.Children.Add(item);
 }
}

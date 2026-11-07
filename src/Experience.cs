using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace ConsoleLauncher;
public static class Experience{
 static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SteamConsoleLauncher","experience");static readonly MediaPlayer Player=new();public static double Volume=.15;public static bool ReducedMotion;static long last;
 static Experience(){try{using var d=JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder,"settings.json")));Volume=Math.Clamp(d.RootElement.GetProperty("volume").GetDouble(),0,1);ReducedMotion=d.RootElement.GetProperty("reducedMotion").GetBoolean();}catch{}}
 public static void Transition(UIElement element){if(!ReducedMotion)element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(220)){FillBehavior=FillBehavior.Stop});}
 public static void Play(string kind){if(Volume<=0||Environment.TickCount64-last<65)return;last=Environment.TickCount64;try{Directory.CreateDirectory(Folder);var path=Path.Combine(Folder,kind+".wav");if(!File.Exists(path)){const int rate=22050;int count=(int)(rate*.055);using var s=File.Create(path);using var w=new BinaryWriter(s);w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);double hz=kind=="move"?530:kind=="back"?380:740;for(int i=0;i<count;i++)w.Write((short)(6000*Math.Sin(2*Math.PI*hz*i/rate)*Math.Sin(Math.PI*i/count)));}Player.Stop();Player.Volume=Volume;Player.Open(new Uri(path));Player.Play();}catch{}}
 public static void Settings(Window owner){var d=new Window{Owner=owner,Title="Звук и движение",Width=420,Height=260,WindowStartupLocation=WindowStartupLocation.CenterOwner};var p=new StackPanel{Margin=new Thickness(24)};d.Content=p;p.Children.Add(new TextBlock{Text="Громкость интерфейса (0 — без звука)"});var v=new Slider{Minimum=0,Maximum=1,Value=Volume,Margin=new Thickness(0,16,0,16)};p.Children.Add(v);var r=new CheckBox{Content="Уменьшить движение",IsChecked=ReducedMotion};p.Children.Add(r);var save=new Button{Content="Сохранить",Margin=new Thickness(0,20,0,0)};p.Children.Add(save);save.Click+=(_,_)=>{Volume=v.Value;ReducedMotion=r.IsChecked==true;try{Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"settings.json"),JsonSerializer.Serialize(new{volume=Volume,reducedMotion=ReducedMotion}));}catch{}d.Close();};d.ShowDialog();}
}

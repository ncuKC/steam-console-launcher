using System.Windows;
namespace ConsoleLauncher;
public static class Program{
 [STAThread]
 public static void Main(){
  Velopack.VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
  new Application().Run(new MainWindow());
 }
}

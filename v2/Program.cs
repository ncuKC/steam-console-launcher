using System.Windows;
namespace ConsoleLauncher;
public static class Program{
 [STAThread]
 public static void Main(){
  Velopack.VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
  var app=new Application();
  var window=new MainWindow();
  SelfUpdate.Attach(window);
  app.Run(window);
 }
}

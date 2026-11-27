using System.Windows;
namespace ConsoleLauncher;
public static class Program{
 [STAThread]
 public static void Main(){new Application().Run(new MainWindow());}
}

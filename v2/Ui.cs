using System.Windows;
using System.Windows.Media;
namespace ConsoleLauncher;
public static class Ui{
 public static Brush B(string color)=>(Brush)new BrushConverter().ConvertFromString(color)!;
 public static Brush Gradient(Point from,Point to,byte a1,byte a2)=>new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(a1,11,15,21),0),new GradientStop(Color.FromArgb(a2,11,15,21),1)},from,to);
}

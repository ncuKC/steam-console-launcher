using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 readonly ProgressBar downloadProgress=new(){Minimum=0,Maximum=100,Height=12,Width=420,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,16),Visibility=Visibility.Collapsed};
 void InitFixPack(){list.PreviewMouseMove+=(_,e)=>{if(e.OriginalSource is DependencyObject source&&ItemsControl.ContainerFromElement(list,source) is ListBoxItem item&&!ReferenceEquals(list.SelectedItem,item.DataContext)){list.SelectedItem=item.DataContext;item.Focus();}};}
 void AttachDownloadProgress(){if(downloadProgress.Parent is Panel old)old.Children.Remove(downloadProgress);detail.Children.Add(downloadProgress);}
 void BeginDownloadProgress(){downloadProgress.Value=0;downloadProgress.Visibility=Visibility.Visible;SetInstallEnabled(false);}
 void ReportDownloadProgress(int progress){Dispatcher.Invoke(()=>{downloadProgress.Value=Math.Clamp(progress,0,100);updateStatus.Text="Получение и подготовка обновления: "+Math.Clamp(progress,0,100)+"%";});}
 void SetInstallEnabled(bool enabled){foreach(var button in detail.Children.OfType<System.Windows.Controls.Button>())if((button.Content as string)=="Скачать обновление и перезапустить")button.IsEnabled=enabled;}
}
public static class ReviewCards{
 public static FrameworkElement Make(bool recommended,string text){
  var panel=new StackPanel();panel.Children.Add(new TextBlock{Text=recommended?"Рекомендует":"Не рекомендует",FontSize=19,FontWeight=FontWeights.SemiBold,Foreground=new SolidColorBrush(recommended?Color.FromRgb(143,205,174):Color.FromRgb(229,161,153)),Margin=new Thickness(0,0,0,14)});
  var content=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,FontSize=19,LineHeight=30,MaxHeight=150,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=new SolidColorBrush(Color.FromRgb(220,226,235))};panel.Children.Add(content);
  bool expanded=false;var toggle=UiKit.Make("Читать полностью",()=>{});toggle.HorizontalAlignment=HorizontalAlignment.Left;toggle.Margin=new Thickness(0,16,0,0);toggle.Click+=(_,_)=>{expanded=!expanded;content.MaxHeight=expanded?double.PositiveInfinity:150;content.TextTrimming=expanded?TextTrimming.None:TextTrimming.CharacterEllipsis;toggle.Content=expanded?"Свернуть отзыв":"Читать полностью";};panel.Children.Add(toggle);
  return new Border{Child=panel,MaxWidth=1000,HorizontalAlignment=HorizontalAlignment.Left,Background=new SolidColorBrush(Color.FromRgb(21,30,43)),BorderBrush=new SolidColorBrush(Color.FromRgb(48,62,81)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(26),Margin=new Thickness(0,12,0,16)};
 }
}

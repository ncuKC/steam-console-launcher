using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace ConsoleLauncher;
public sealed class ListPopup:Border{
 sealed class Row{public Border Box=new();public TextBlock Label=new();public Func<string> Text=()=>"";public Action<int> Act=_=>{};}
 readonly StackPanel list=new();
 readonly List<Row> rows=new();
 int index;
 public bool IsOpen=>Visibility==Visibility.Visible;
 public event Action? Closed;
 public ListPopup(){
  Width=760;
  HorizontalAlignment=HorizontalAlignment.Center;
  VerticalAlignment=VerticalAlignment.Center;
  CornerRadius=new CornerRadius(12);
  Background=Ui.B("#F21B2838");
  Padding=new Thickness(28);
  Visibility=Visibility.Collapsed;
  var root=new StackPanel();
  Child=root;
  root.Children.Add(new TextBlock{Text="Список игр",FontSize=34,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,Margin=new Thickness(8,0,0,18)});
  root.Children.Add(list);
 }
 public void SetItems(IEnumerable<(Func<string> Text,Action<int> Act)> items){
  list.Children.Clear();
  rows.Clear();
  foreach(var (text,act) in items){
   var row=new Row{Text=text,Act=act};
   row.Label=new TextBlock{FontSize=28,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center};
   row.Box=new Border{Height=64,CornerRadius=new CornerRadius(6),Padding=new Thickness(20,0,20,0),Margin=new Thickness(0,0,0,6),Child=row.Label};
   rows.Add(row);
   list.Children.Add(row.Box);
  }
  index=Math.Clamp(index,0,Math.Max(0,rows.Count-1));
 }
 public void Open(){
  Visibility=Visibility.Visible;
  index=0;
  Refresh();
 }
 public void Close(){
  Visibility=Visibility.Collapsed;
  Closed?.Invoke();
 }
 public void Move(int delta){
  if(rows.Count==0)return;
  index=Math.Clamp(index+delta,0,rows.Count-1);
  Refresh();
 }
 public void Act(int direction){
  if(rows.Count==0)return;
  rows[index].Act(direction);
  if(IsOpen)Refresh();
 }
 public void Refresh(){
  for(int i=0;i<rows.Count;i++){
   var row=rows[i];
   row.Label.Text=row.Text();
   bool on=i==index;
   row.Box.Background=on?Ui.B("#66C0F4"):Ui.B("#2A475E");
   row.Label.Foreground=on?Ui.B("#0E141B"):Brushes.White;
  }
 }
}

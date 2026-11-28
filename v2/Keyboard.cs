using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace ConsoleLauncher;
public sealed class SearchKeyboard:Border{
 sealed class Key{public Border Box=new();public TextBlock Label=new();public Action Do=()=>{};}
 static readonly string[] En={"1234567890","QWERTYUIOP","ASDFGHJKL","ZXCVBNM"};
 static readonly string[] Ru={"1234567890","ЙЦУКЕНГШЩЗХ","ФЫВАПРОЛДЖЭ","ЯЧСМИТЬБЮ"};
 readonly StackPanel keyPanel=new();
 readonly TextBlock display=new(){FontSize=34,Foreground=Brushes.White,Margin=new Thickness(36,24,36,16),TextTrimming=TextTrimming.CharacterEllipsis};
 readonly List<List<Key>> rows=new();
 string text="";
 string filterText="";
 bool russian;
 int row,col;
 public event Action<string>? Changed;
 public event Action? FilterPressed;
 public event Action? Closed;
 public bool IsOpen=>Visibility==Visibility.Visible;
 public SearchKeyboard(){
  Width=1200;
  HorizontalAlignment=HorizontalAlignment.Center;
  VerticalAlignment=VerticalAlignment.Bottom;
  Margin=new Thickness(0,0,0,40);
  CornerRadius=new CornerRadius(12);
  Background=Ui.B("#F21B2838");
  Visibility=Visibility.Collapsed;
  var root=new StackPanel();
  Child=root;
  root.Children.Add(display);
  keyPanel.Margin=new Thickness(0,0,0,24);
  root.Children.Add(keyPanel);
  Build();
  Refresh();
 }
 public void Open(string current,string filterLabel){
  text=current;
  filterText=filterLabel;
  row=1;col=0;
  Visibility=Visibility.Visible;
  Build();
  Refresh();
 }
 public void Close(){
  Visibility=Visibility.Collapsed;
  Closed?.Invoke();
 }
 public void SetFilter(string label){
  filterText=label;
  Build();
 }
 Key MakeKey(string label,double width,Action action){
  var key=new Key{Do=action};
  key.Label=new TextBlock{Text=label,FontSize=28,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
  key.Box=new Border{Width=width,Height=64,Margin=new Thickness(4,0,4,0),CornerRadius=new CornerRadius(6),Background=Ui.B("#2A475E"),Child=key.Label};
  return key;
 }
 void Build(){
  keyPanel.Children.Clear();
  rows.Clear();
  foreach(var letters in russian?Ru:En){
   var line=new List<Key>();
   var panel=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,8)};
   foreach(var ch in letters){
    var letter=ch.ToString();
    var item=MakeKey(letter,84,()=>Type(letter));
    line.Add(item);
    panel.Children.Add(item.Box);
   }
   rows.Add(line);
   keyPanel.Children.Add(panel);
  }
  var control=new List<Key>();
  var bottom=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
  var specials=new[]{MakeKey(filterText,300,()=>FilterPressed?.Invoke()),MakeKey(russian?"EN":"RU",100,Switch),MakeKey("Пробел",260,Space),MakeKey("⌫",100,Backspace),MakeKey("Готово",180,Close)};
  foreach(var special in specials){control.Add(special);bottom.Children.Add(special.Box);}
  rows.Add(control);
  keyPanel.Children.Add(bottom);
  row=Math.Clamp(row,0,rows.Count-1);
  col=Math.Clamp(col,0,rows[row].Count-1);
  Highlight();
 }
 void Highlight(){
  for(int r=0;r<rows.Count;r++){
   for(int c=0;c<rows[r].Count;c++){
    bool on=r==row&&c==col;
    rows[r][c].Box.Background=on?Ui.B("#66C0F4"):Ui.B("#2A475E");
    rows[r][c].Label.Foreground=on?Ui.B("#0E141B"):Brushes.White;
   }
  }
 }
 void Refresh(){
  if(text.Length==0){display.Text="Начните вводить название игры";display.Foreground=Ui.B("#8F98A0");}
  else{display.Text=text+"|";display.Foreground=Brushes.White;}
 }
 void Switch(){
  russian=!russian;
  Build();
 }
 void Type(string value){
  if(text.Length>=40)return;
  text+=value;
  Refresh();
  Changed?.Invoke(text);
 }
 public void Space(){Type(" ");}
 public void Backspace(){
  if(text.Length==0)return;
  text=text.Substring(0,text.Length-1);
  Refresh();
  Changed?.Invoke(text);
 }
 public void Move(int dx,int dy){
  row=Math.Clamp(row+dy,0,rows.Count-1);
  col=Math.Clamp(col+dx,0,rows[row].Count-1);
  Highlight();
 }
 public void Press(){rows[row][col].Do();}
}

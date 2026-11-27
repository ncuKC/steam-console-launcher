using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool libraryPolished,countBusy;
 internal void InitLibraryPolish(){
  if(libraryPolished)return;libraryPolished=true;
  try{
   if(search.Parent is StackPanel filters){
    filters.Children.Insert(0,new TextBlock{Text="Поиск · Y",FontSize=18,Foreground=B("#C6D4DF"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,14,0)});
    var filterButton=filters.Children.OfType<System.Windows.Controls.Button>().FirstOrDefault();
    if(filterButton!=null){
     filterButton.Content="Фильтр: "+filterButton.Content;
     filterButton.Click+=(_,_)=>filterButton.Content="Фильтр: "+filterButton.Content;
    }
   }
  }catch{}
  try{
   DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty,typeof(TextBlock)).AddValueChanged(section,(_,_)=>{
    if(countBusy||home||section.Text.Contains(" · "))return;
    countBusy=true;
    try{section.Text=section.Text+" · "+list.Items.Count;}finally{countBusy=false;}
   });
  }catch{}
 }
}

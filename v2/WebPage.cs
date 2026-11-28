using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
namespace ConsoleLauncher;
public sealed class WebPage:Grid{
 const string Script="""
(()=>{
 if(window.top!==window||window.__padInit)return;
 window.__padInit=true;
 let x=Math.round(innerWidth/2),y=Math.round(innerHeight/2);
 try{const s=sessionStorage.getItem('__padPos');if(s){const p=JSON.parse(s);x=p[0];y=p[1];}}catch(e){}
 const cursor=document.createElement('div');
 cursor.style.cssText='position:fixed;left:0;top:0;width:30px;height:30px;z-index:2147483647;pointer-events:none;will-change:transform;';
 cursor.innerHTML='<svg width="30" height="30" viewBox="0 0 30 30"><path d="M4 2 L4 23 L9.5 18 L13.5 27 L17.5 25 L13.5 16 L21 16 Z" fill="#fff" stroke="#000" stroke-width="2" stroke-linejoin="round"/></svg>';
 const bar=document.createElement('div');
 bar.style.cssText='position:fixed;left:0;right:0;bottom:0;height:46px;background:rgba(0,0,0,.82);color:#fff;font:600 17px Segoe UI,sans-serif;display:flex;align-items:center;padding:0 32px;z-index:2147483646;pointer-events:none;';
 const place=()=>{cursor.style.transform='translate('+x+'px,'+y+'px)';try{sessionStorage.setItem('__padPos',JSON.stringify([x,y]));}catch(e){}};
 const attach=()=>{if(document.body){document.body.appendChild(bar);document.body.appendChild(cursor);place();}else requestAnimationFrame(attach);};
 attach();
 window.__setHint=t=>{bar.textContent=t;};
 const target=()=>document.elementFromPoint(x,y);
 const fire=(el,type)=>el.dispatchEvent(new MouseEvent(type,{bubbles:true,cancelable:true,view:window,clientX:x,clientY:y}));
 window.__pad={
  move(dx,dy){
   x=Math.min(Math.max(0,x+dx),innerWidth-2);
   y=Math.min(Math.max(0,y+dy),innerHeight-2);
   if(dy>0&&y>innerHeight-110)scrollBy(0,dy*1.5);
   if(dy<0&&y<90)scrollBy(0,dy*1.5);
   place();
   const el=target();
   if(el){fire(el,'mousemove');fire(el,'mouseover');}
  },
  click(){
   const el=target();
   if(!el)return;
   fire(el,'mousedown');
   fire(el,'mouseup');
   if(el.focus)el.focus();
   el.click();
  },
  scroll(dy){scrollBy({top:dy,behavior:'smooth'});}
 };
 window.addEventListener('keydown',e=>{if(e.key==='Escape'&&window.chrome&&window.chrome.webview)window.chrome.webview.postMessage('close');});
})();
""";
 WebView2? view;
 bool ready;
 int streak,lastCode;
 DateTime lastAt=DateTime.MinValue;
 readonly TextBlock note=new(){Text="Загрузка страницы…",FontSize=32,Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
 public bool IsOpen{get;private set;}
 public event Action? Closed;
 public event Action<string>? Failed;
 public WebPage(){
  Background=Brushes.Black;
  Visibility=Visibility.Collapsed;
  Children.Add(note);
 }
 static string Hint()=>"Крестовина — курсор   ·   "+Controller.Label("A")+" — клик   ·   "+Controller.Label("LB/RB")+" — прокрутка   ·   "+Controller.Label("B")+" — назад   ·   "+Controller.Label("Y")+" — закрыть";
 static void AddCookie(CoreWebView2 core,string name,string value){
  try{
   var cookie=core.CookieManager.CreateCookie(name,value,"store.steampowered.com","/");
   cookie.Expires=DateTime.Now.AddYears(1);
   core.CookieManager.AddOrUpdateCookie(cookie);
  }catch{}
 }
 public async void Open(string url){
  if(IsOpen)return;
  IsOpen=true;
  Visibility=Visibility.Visible;
  note.Visibility=Visibility.Visible;
  try{
   if(view==null){
    view=new WebView2{CreationProperties=new CoreWebView2CreationProperties{UserDataFolder=Path.Combine(Library.Root,"webview")}};
    Children.Add(view);
   }
   if(!ready){
    await view.EnsureCoreWebView2Async();
    var core=view.CoreWebView2;
    core.Settings.AreDevToolsEnabled=false;
    core.Settings.AreDefaultContextMenusEnabled=false;
    core.Settings.IsStatusBarEnabled=false;
    core.Settings.IsZoomControlEnabled=false;
    core.NewWindowRequested+=(_,e)=>{e.Handled=true;core.Navigate(e.Uri);};
    core.WebMessageReceived+=(_,e)=>{if(e.TryGetWebMessageAsString()=="close")Close();};
    core.NavigationCompleted+=(_,_)=>{
     note.Visibility=Visibility.Collapsed;
     _=core.ExecuteScriptAsync("window.__setHint&&window.__setHint("+JsonSerializer.Serialize(Hint())+")");
    };
    await core.AddScriptToExecuteOnDocumentCreatedAsync(Script);
    AddCookie(core,"birthtime","631152001");
    AddCookie(core,"lastagecheckage","1-0-1990");
    AddCookie(core,"wants_mature_content","1");
    ready=true;
   }
   if(!IsOpen)return;
   view.CoreWebView2.Navigate(url);
  }catch(Exception ex){
   Close();
   Failed?.Invoke(ex.Message);
  }
 }
 public void Close(){
  if(!IsOpen)return;
  IsOpen=false;
  Visibility=Visibility.Collapsed;
  try{view?.CoreWebView2?.Navigate("about:blank");}catch{}
  Closed?.Invoke();
 }
 void Run(string script){
  if(view?.CoreWebView2!=null)_=view.CoreWebView2.ExecuteScriptAsync(script);
 }
 public void Press(int code){
  if(view?.CoreWebView2==null){
   if(code==1||code==3||code==6)Close();
   return;
  }
  var now=DateTime.Now;
  if(code>=11&&code<=14){
   streak=code==lastCode&&(now-lastAt).TotalMilliseconds<260?streak+1:0;
   lastCode=code;
   lastAt=now;
  }
  int step=Math.Min(36+streak*10,140);
  switch(code){
   case 11:Run("window.__pad&&window.__pad.move(0,"+(-step)+")");break;
   case 12:Run("window.__pad&&window.__pad.move(0,"+step+")");break;
   case 13:Run("window.__pad&&window.__pad.move("+(-step)+",0)");break;
   case 14:Run("window.__pad&&window.__pad.move("+step+",0)");break;
   case 0:Run("window.__pad&&window.__pad.click()");break;
   case 9:Run("window.__pad&&window.__pad.scroll(-500)");break;
   case 10:Run("window.__pad&&window.__pad.scroll(500)");break;
   case 1:
    if(view.CoreWebView2.CanGoBack)view.CoreWebView2.GoBack();
    else Close();
    break;
   case 3:
   case 6:
    Close();
    break;
  }
 }
}

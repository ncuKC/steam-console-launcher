using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool webPadReady;double webZoom=2.0;
 readonly DispatcherTimer webCursorTimer=new(){Interval=TimeSpan.FromMilliseconds(33)};
 const string WebPadScript=@"(()=>{if(window.__pad)return;
const SEL='a[href],button,input:not([type=hidden]),select,textarea,summary,[role=button],[role=link],[onclick]';
const TEXT=['text','search','email','password','number','url','tel',''];
let cur=null,cx=innerWidth/2,cy=innerHeight/2;
const css=document.createElement('style');
css.textContent='.__padf{outline:5px solid #66C0F4!important;outline-offset:3px!important;box-shadow:0 0 0 9px rgba(102,192,244,.35)!important;}';
document.documentElement.appendChild(css);
const dot=document.createElement('div');
dot.style.cssText='position:fixed;z-index:2147483647;width:22px;height:22px;margin:-11px 0 0 -11px;border-radius:50%;background:rgba(102,192,244,.55);border:4px solid #fff;box-shadow:0 0 0 3px #1b2838;pointer-events:none;left:0;top:0;';
document.documentElement.appendChild(dot);
const draw=()=>{dot.style.left=cx+'px';dot.style.top=cy+'px';};draw();
const vis=e=>{const r=e.getBoundingClientRect();if(r.width<10||r.height<10)return false;const s=getComputedStyle(e);return s.visibility!=='hidden'&&s.display!=='none'&&e.getAttribute('disabled')===null;};
const items=()=>Array.from(document.querySelectorAll(SEL)).filter(vis);
const pick=()=>{const el=document.elementFromPoint(cx,cy);return el?(el.closest(SEL)||el):null;};
const bad=e=>!e||e===document.body||e===document.documentElement;
const mark=e=>{if(cur===e)return;if(cur)cur.classList.remove('__padf');cur=e;if(!bad(e))e.classList.add('__padf');};
const place=e=>{e.scrollIntoView({block:'center',inline:'center'});const r=e.getBoundingClientRect();cx=Math.min(innerWidth-1,Math.max(0,r.left+r.width/2));cy=Math.min(innerHeight-1,Math.max(0,r.top+r.height/2));draw();mark(e);};
window.__pad={
 cursor(dx,dy){cx=Math.min(innerWidth-1,Math.max(0,cx+dx));cy=Math.min(innerHeight-1,Math.max(0,cy+dy));if(dy>0&&cy>=innerHeight-6)window.scrollBy(0,dy);if(dy<0&&cy<=6)window.scrollBy(0,dy);draw();mark(pick());},
 move(d){const all=items();if(!all.length)return;
  if(bad(cur)||!document.contains(cur)){const p=pick();if(p&&all.includes(p)){place(p);return;}const v=all.find(e=>{const r=e.getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight;})||all[0];place(v);return;}
  const a=cur.getBoundingClientRect(),ax=a.left+a.width/2,ay=a.top+a.height/2;let best=null,bs=1e18;
  for(const e of all){if(e===cur||cur.contains(e)||e.contains(cur))continue;const r=e.getBoundingClientRect(),dx=r.left+r.width/2-ax,dy=r.top+r.height/2-ay;
   const ok=d==='up'?dy<-4:d==='down'?dy>4:d==='left'?dx<-4:dx>4;if(!ok)continue;
   const vert=d==='up'||d==='down',main=vert?Math.abs(dy):Math.abs(dx),cross=vert?Math.abs(dx):Math.abs(dy),s=main+cross*2.5;if(s<bs){bs=s;best=e;}}
  if(best)place(best);else window.scrollBy(0,d==='down'?320:d==='up'?-320:0);},
 press(){const t=pick()||cur;if(bad(t))return null;mark(t);
  const n=t.tagName;if(n==='TEXTAREA'||(n==='INPUT'&&TEXT.includes((t.type||'').toLowerCase()))){t.focus();return {input:true,value:t.value||''};}
  if(t.focus)t.focus({preventScroll:true});t.click();return null;},
 setValue(v){if(bad(cur))return;const proto=cur.tagName==='TEXTAREA'?HTMLTextAreaElement.prototype:HTMLInputElement.prototype;Object.getOwnPropertyDescriptor(proto,'value').set.call(cur,v);cur.dispatchEvent(new Event('input',{bubbles:true}));cur.dispatchEvent(new Event('change',{bubbles:true}));},
 scroll(dy){window.scrollBy(0,dy);}
};})();";
 void ApplyWebZoom(){try{browser.ZoomFactor=webZoom;}catch{}}
 void UpdateWebStatus(){webStatus.Text="Левый стик — курсор · A — нажать · B — назад · правый стик — прокрутка · X/Y — масштаб "+(int)Math.Round(webZoom*100)+"% · крестовина — прыжок по кнопкам";}
 void InitWebPad(){
  if(webPadReady)return;webPadReady=true;
  browser.CoreWebView2InitializationCompleted+=(_,e)=>{if(e.IsSuccess)try{_=browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(WebPadScript);}catch{}};
  browser.NavigationCompleted+=async(_,_)=>{ApplyWebZoom();try{await browser.ExecuteScriptAsync(WebPadScript);}catch{}UpdateWebStatus();};
  store.IsVisibleChanged+=(_,_)=>{if(store.IsVisible){ApplyWebZoom();UpdateWebStatus();}};
  webCursorTimer.Tick+=(_,_)=>WebCursorTick();webCursorTimer.Start();Closed+=(_,_)=>webCursorTimer.Stop();
 }
 void WebCursorTick(){
  bool active=page=="store"&&browser.CoreWebView2!=null&&!keyboardOpen;
  if(pad!=null)pad.SuppressLeftStick=active;
  if(!active||pad==null||!IsActive)return;
  double lx=pad.LeftX,ly=pad.LeftY,ry=pad.RightY;
  if(lx!=0||ly!=0){
   double sx=Math.Sign(lx)*lx*lx*30,sy=Math.Sign(ly)*ly*ly*30;
   WebRun("window.__pad&&window.__pad.cursor("+sx.ToString("0.##",CultureInfo.InvariantCulture)+","+sy.ToString("0.##",CultureInfo.InvariantCulture)+")");
  }
  if(ry!=0){double sc=Math.Sign(ry)*ry*ry*46;WebRun("window.__pad&&window.__pad.scroll("+sc.ToString("0.##",CultureInfo.InvariantCulture)+")");}
 }
 async void WebRun(string script){try{await browser.ExecuteScriptAsync(script);}catch{}}
 async void WebClick(){
  try{
   var raw=await browser.ExecuteScriptAsync("window.__pad?window.__pad.press():null");
   if(string.IsNullOrEmpty(raw)||raw=="null")return;
   using var doc=JsonDocument.Parse(raw);
   if(doc.RootElement.ValueKind==JsonValueKind.Object&&doc.RootElement.TryGetProperty("input",out _)){
    var current=doc.RootElement.TryGetProperty("value",out var v)?v.GetString()??"":"";
    var text=PromptOnScreenKeyboard("Ввод",current);
    if(text!=null)await browser.ExecuteScriptAsync("window.__pad.setValue("+JsonSerializer.Serialize(text)+")");
   }
  }catch{}
 }
 bool HandleWebPad(int code){
  if(page!="store"||browser.CoreWebView2==null||menuVisible)return false;
  int confirm=swapped?1:0,back=swapped?0:1;
  if(code==back){if(browser.CanGoBack)browser.GoBack();else Back();return true;}
  if(code==confirm){WebClick();return true;}
  if(code>=11&&code<=14){WebRun("window.__pad&&window.__pad.move('"+(code==11?"up":code==12?"down":code==13?"left":"right")+"')");return true;}
  if(code==101||code==102)return true;
  if(code==103||code==104){WebRun("window.__pad&&window.__pad.scroll("+(code==103?-720:720)+")");return true;}
  if(code==2||code==3){webZoom=Math.Clamp(webZoom+(code==3?.25:-.25),1.0,3.5);ApplyWebZoom();UpdateWebStatus();return true;}
  return false;
 }
}

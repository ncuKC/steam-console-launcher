using System.Text.Json;
using System.Windows;
namespace ConsoleLauncher;
public partial class LauncherWindow{
 bool webPadReady;
 const string WebPadScript=@"(()=>{if(window.__pad)return;
const SEL='a[href],button,input:not([type=hidden]),select,textarea,summary,[role=button],[role=link],[onclick]';
let cur=null;
const css=document.createElement('style');
css.textContent='.__padf{outline:5px solid #66C0F4!important;outline-offset:3px!important;box-shadow:0 0 0 9px rgba(102,192,244,.35)!important;}';
(document.head||document.documentElement).appendChild(css);
const vis=e=>{const r=e.getBoundingClientRect();if(r.width<10||r.height<10)return false;const s=getComputedStyle(e);return s.visibility!=='hidden'&&s.display!=='none'&&e.getAttribute('disabled')===null;};
const items=()=>Array.from(document.querySelectorAll(SEL)).filter(vis);
const set=e=>{if(cur)cur.classList.remove('__padf');cur=e;if(e){e.classList.add('__padf');e.scrollIntoView({block:'center',inline:'center'});}};
const TEXT=['text','search','email','password','number','url','tel',''];
window.__pad={
 move(d){const all=items();if(!all.length)return;
  if(!cur||!document.contains(cur)){const v=all.find(e=>{const r=e.getBoundingClientRect();return r.top>=0&&r.bottom<=innerHeight;})||all[0];set(v);return;}
  const a=cur.getBoundingClientRect(),ax=a.left+a.width/2,ay=a.top+a.height/2;let best=null,bs=1e18;
  for(const e of all){if(e===cur||cur.contains(e)||e.contains(cur))continue;const r=e.getBoundingClientRect(),dx=r.left+r.width/2-ax,dy=r.top+r.height/2-ay;
   const ok=d==='up'?dy<-4:d==='down'?dy>4:d==='left'?dx<-4:dx>4;if(!ok)continue;
   const vert=d==='up'||d==='down',main=vert?Math.abs(dy):Math.abs(dx),cross=vert?Math.abs(dx):Math.abs(dy),s=main+cross*2.5;if(s<bs){bs=s;best=e;}}
  if(best)set(best);else window.scrollBy(0,d==='down'?320:d==='up'?-320:0);},
 click(){if(!cur||!document.contains(cur)){this.move('down');return null;}
  const t=cur.tagName;if(t==='TEXTAREA'||(t==='INPUT'&&TEXT.includes((cur.type||'').toLowerCase()))){cur.focus();return {input:true,value:cur.value||''};}
  cur.click();return null;},
 setValue(v){if(!cur)return;const proto=cur.tagName==='TEXTAREA'?HTMLTextAreaElement.prototype:HTMLInputElement.prototype;Object.getOwnPropertyDescriptor(proto,'value').set.call(cur,v);cur.dispatchEvent(new Event('input',{bubbles:true}));cur.dispatchEvent(new Event('change',{bubbles:true}));},
 scroll(dy){window.scrollBy(0,dy);}
};})();";
 void InitWebPad(){
  if(webPadReady)return;webPadReady=true;
  browser.CoreWebView2InitializationCompleted+=(_,e)=>{if(e.IsSuccess)try{_=browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(WebPadScript);}catch{}};
  browser.NavigationCompleted+=async(_,_)=>{try{browser.ZoomFactor=1.6;}catch{}try{await browser.ExecuteScriptAsync(WebPadScript);}catch{}webStatus.Text="Крестовина — выбор · A — нажать · B — назад · правый стик и LT/RT — прокрутка · LB/RB — разделы";};
  store.IsVisibleChanged+=(_,_)=>{if(store.IsVisible)try{browser.ZoomFactor=1.6;}catch{}};
 }
 async void WebRun(string script){try{await browser.ExecuteScriptAsync(script);}catch{}}
 async void WebClick(){
  try{
   var raw=await browser.ExecuteScriptAsync("window.__pad?window.__pad.click():null");
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
  if(code==101||code==102){WebRun("window.__pad&&window.__pad.scroll("+(code==101?-80:80)+")");return true;}
  if(code==103||code==104){WebRun("window.__pad&&window.__pad.scroll("+(code==103?-720:720)+")");return true;}
  return false;
 }
}

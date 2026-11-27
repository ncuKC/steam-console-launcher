using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
namespace ConsoleLauncher;
public record SteamSession(string SteamId,string PersonaName);
public partial class LauncherWindow{
 readonly TextBlock accountBadge=new(){FontSize=16,Foreground=B("#66C0F4"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,18,16)};
 readonly TextBlock authStatus=new(){TextWrapping=TextWrapping.Wrap,FontSize=17,Foreground=B("#66C0F4"),Margin=new Thickness(0,0,0,14)};
 SteamSession? steamSession;bool authWatchAttached,awaitingSteamLogin;
 string AccountFile=>Path.Combine(Root,"account.json");
 void InitSteamAuth(){
  try{if(File.Exists(AccountFile))steamSession=JsonSerializer.Deserialize<SteamSession>(File.ReadAllText(AccountFile));}catch{steamSession=null;}
  if(shell.Children.Count>0&&shell.Children[0] is DockPanel nav&&nav.Children.Count>0&&nav.Children[0] is StackPanel settings&&accountBadge.Parent==null)settings.Children.Insert(0,accountBadge);
  RefreshSteamAuthUI();
  InitReturnCombo();
 }
 void RefreshSteamAuthUI(){
  accountBadge.Text=steamSession is {PersonaName.Length:>0} s?s.PersonaName:"Гость";
  authStatus.Text=steamSession!=null?$"Аккаунт Steam: {steamSession.PersonaName} ({steamSession.SteamId})":"Аккаунт Steam: вход не выполнен";
 }
 void AddSteamAuthMenu(){
  RefreshSteamAuthUI();
  if(authStatus.Parent is Panel old)old.Children.Remove(authStatus);
  detail.Children.Add(authStatus);
  detail.Children.Add(Button(steamSession==null?"Войти в Steam":"Сменить аккаунт Steam",async()=>await OpenSteamLogin()));
  if(steamSession!=null)detail.Children.Add(Button("Импортировать игры из аккаунта",async()=>await ImportFromSteamSession()));
  if(steamSession!=null)detail.Children.Add(Button("Выйти из Steam",async()=>await SignOutSteam()));
  AddPowerMenu();
 }
 async Task OpenSteamLogin(){
  menuVisible=false;awaitingSteamLogin=true;
  await Store("https://store.steampowered.com/login/");
  webStatus.Text="Вход в Steam (пароль или QR-код Steam Guard) · B — отмена";
  AttachSteamAuthWatch();
 }
 void AttachSteamAuthWatch(){
  if(authWatchAttached||browser.CoreWebView2==null)return;
  authWatchAttached=true;
  browser.CoreWebView2.NavigationCompleted+=async(_,e)=>{if(e.IsSuccess)await CheckSteamSessionAsync();};
 }
 async Task CheckSteamSessionAsync(){
  if(browser.CoreWebView2==null)return;
  try{
   var cookies=await browser.CoreWebView2.CookieManager.GetCookiesAsync("https://store.steampowered.com");
   var loginCookie=cookies.FirstOrDefault(c=>c.Name=="steamLoginSecure");
   if(loginCookie==null||string.IsNullOrWhiteSpace(loginCookie.Value))return;
   var match=Regex.Match(loginCookie.Value,"^([0-9]{17})");
   if(!match.Success)return;
   string steamId=match.Groups[1].Value;
   string persona=steamSession?.SteamId==steamId&&!string.IsNullOrWhiteSpace(steamSession.PersonaName)?steamSession.PersonaName:steamId;
   try{
    var raw=await browser.CoreWebView2.ExecuteScriptAsync("(document.querySelector('#account_pulldown')?.textContent||'').trim()");
    var parsed=JsonSerializer.Deserialize<string>(raw);
    if(!string.IsNullOrWhiteSpace(parsed))persona=parsed.Trim();
   }catch{}
   steamSession=new SteamSession(steamId,persona);
   Directory.CreateDirectory(Root);
   await File.WriteAllTextAsync(AccountFile,JsonSerializer.Serialize(steamSession));
   RefreshSteamAuthUI();
   if(awaitingSteamLogin){awaitingSteamLogin=false;Back();FocusSelected();}
  }catch{}
 }
 async Task SignOutSteam(){
  awaitingSteamLogin=false;steamSession=null;
  try{if(File.Exists(AccountFile))File.Delete(AccountFile);}catch{}
  try{
   if(browser.CoreWebView2!=null){
    browser.CoreWebView2.CookieManager.DeleteAllCookies();
   }
  }catch{}
  RefreshSteamAuthUI();
  await Task.CompletedTask;
 }
}

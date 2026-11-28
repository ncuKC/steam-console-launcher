using System.Media;
namespace ConsoleLauncher;
public static class Sound{
 static readonly Dictionary<string,byte[]> Cache=new();
 public static bool Enabled{get;private set;}=true;
 static string SettingFile=>Path.Combine(Library.Root,"sound.txt");
 public static void Load(){
  try{
   if(File.Exists(SettingFile))Enabled=File.ReadAllText(SettingFile).Trim()!="off";
  }catch{}
 }
 public static void Toggle(){
  Enabled=!Enabled;
  try{
   Directory.CreateDirectory(Library.Root);
   File.WriteAllText(SettingFile,Enabled?"on":"off");
  }catch{}
 }
 static byte[] Make(double[] freqs,double seconds,double volume){
  const int rate=22050;
  int count=(int)(rate*seconds);
  using var stream=new MemoryStream();
  using var writer=new BinaryWriter(stream);
  writer.Write(new[]{'R','I','F','F'});
  writer.Write(36+count*2);
  writer.Write(new[]{'W','A','V','E','f','m','t',' '});
  writer.Write(16);
  writer.Write((short)1);
  writer.Write((short)1);
  writer.Write(rate);
  writer.Write(rate*2);
  writer.Write((short)2);
  writer.Write((short)16);
  writer.Write(new[]{'d','a','t','a'});
  writer.Write(count*2);
  for(int i=0;i<count;i++){
   double t=(double)i/rate;
   double fall=1-(double)i/count;
   double envelope=Math.Min(1,t/0.004)*fall*fall;
   double sum=0;
   foreach(var freq in freqs)sum+=Math.Sin(2*Math.PI*freq*t);
   double sample=sum/freqs.Length*volume*envelope;
   writer.Write((short)(sample*short.MaxValue));
  }
  writer.Flush();
  return stream.ToArray();
 }
 static byte[] Build(string name)=>name switch{
  "move"=>Make(new[]{900.0},0.035,0.18),
  "select"=>Make(new[]{660.0,990.0},0.12,0.25),
  "back"=>Make(new[]{440.0},0.09,0.22),
  "open"=>Make(new[]{520.0,780.0},0.16,0.22),
  _=>Make(new[]{600.0},0.05,0.15)
 };
 public static void Play(string name){
  if(!Enabled)return;
  try{
   if(!Cache.TryGetValue(name,out var data)){
    data=Build(name);
    Cache[name]=data;
   }
   new SoundPlayer(new MemoryStream(data)).Play();
  }catch{}
 }
}

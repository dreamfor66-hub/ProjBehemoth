using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
public static class CharacterReviewPreparation {
 public static string Run(string dir) {
  var result=new StringBuilder("[");
  string[] names={"Warrior","Barbarian","Monk","Samurai","Wizard","Rogue"};
  for(int index=0;index<names.Length;index++) {
   string name=names[index],input=dir+"/"+name+".png",clean=dir+"/Generated/"+name+"-Cutout.png";
   using(var source=new Bitmap(input))
   using(var target=source.Clone(new Rectangle(0,0,source.Width,source.Height),PixelFormat.Format32bppArgb)) {
    int left=source.Width,top=source.Height,right=0,bottom=0;
    for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++) {
     Color p=source.GetPixel(x,y);
     bool faceLocked=name=="Rogue" && x>=560 && x<775 && y>=125 && y<305;
     int alpha=faceLocked?p.A:(p.A>=220?255:0);
     if(alpha!=p.A)target.SetPixel(x,y,Color.FromArgb(alpha,p.R,p.G,p.B));
     if(alpha>=220){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
    }
    target.Save(clean,ImageFormat.Png);
    if(index>0)result.Append(",");
    result.Append("{\"id\":\"").Append(name).Append("\",\"width\":").Append(source.Width).Append(",\"height\":").Append(source.Height).Append(",\"bounds\":[").Append(left).Append(",").Append(top).Append(",").Append(right-left+1).Append(",").Append(bottom-top+1).Append("]}");
   }
  }
  return result.Append("]").ToString();
 }
}

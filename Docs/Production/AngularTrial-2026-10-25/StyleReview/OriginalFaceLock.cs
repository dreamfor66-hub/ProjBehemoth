using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
public static class OriginalFaceLock {
 public static string FinalFaceOnly(string originalPath,string repairedPath,string outputPath) {
  using(var original=new Bitmap(originalPath))using(var repaired=new Bitmap(repairedPath)) {
   if(original.Size!=repaired.Size)throw new Exception("Cannot preserve original facial pixels on a different canvas.");
   var face=new Rectangle(560,125,215,180);
   using(var output=repaired.Clone(new Rectangle(0,0,repaired.Width,repaired.Height),PixelFormat.Format32bppArgb)) {
    for(int y=face.Top;y<face.Bottom;y++)for(int x=face.Left;x<face.Right;x++)output.SetPixel(x,y,original.GetPixel(x,y));
    output.Save(outputPath,ImageFormat.Png);
   }
   using(var final=new Bitmap(outputPath)) {
    int mismatch=0,oldBottom=0,newBottom=0;
    for(int y=face.Top;y<face.Bottom;y++)for(int x=face.Left;x<face.Right;x++)if(original.GetPixel(x,y).ToArgb()!=final.GetPixel(x,y).ToArgb())mismatch++;
    for(int y=0;y<original.Height;y++)for(int x=0;x<original.Width;x++){if(original.GetPixel(x,y).A>=220)oldBottom=Math.Max(oldBottom,y);if(final.GetPixel(x,y).A>=220)newBottom=Math.Max(newBottom,y);}
    return "{\"faceRegion\":[560,125,215,180],\"facePixelsCompared\":"+face.Width*face.Height+",\"facePixelMismatches\":"+mismatch+",\"originalOpaqueBottomY\":"+oldBottom+",\"correctedOpaqueBottomY\":"+newBottom+",\"canvas\":[1024,1536],\"method\":\"Built-in imagegen body correction and clothing repair; exact original face/forelock region reused without resampling\"}";
   }
  }
 }
 public static string Prepare(string originalPath, string generatedPath, string outputPath) {
  using(var original=new Bitmap(originalPath))
  using(var generated=new Bitmap(generatedPath)) {
   if(original.Width!=1024 || original.Height!=1536 || generated.Size!=original.Size) throw new Exception("Canvas mismatch: face must not be rescaled.");
   // Original face, forelock, and surrounding hood/scarf. No interpolation or color conversion.
   var region=new Rectangle(530,100,270,265);
   using(var output=generated.Clone(new Rectangle(0,0,generated.Width,generated.Height),PixelFormat.Format32bppArgb)) {
    // Find a discrete low-difference join below the protected head, rather than a visible rectangular patch.
    // Every pixel above the join is the untouched original; there is no feathering, new gradient, or resampling.
    const int minY=365,maxY=570;
    int count=maxY-minY+1,width=original.Width;
    var cost=new double[width,count];
    var parent=new int[width,count];
    for(int x=0;x<width;x++)for(int row=0;row<count;row++) {
     int y=minY+row;
     Color a=original.GetPixel(x,y),b=generated.GetPixel(x,y);
     double local;
     if(a.A<5 && b.A<5) local=0;
     else { double alpha=Math.Max(a.A,b.A)/255.0; local=alpha*((a.R-b.R)*(a.R-b.R)+(a.G-b.G)*(a.G-b.G)+(a.B-b.B)*(a.B-b.B))+3*(a.A-b.A)*(a.A-b.A); }
     local+=Math.Abs(y-420)*0.15;
     if(x==0) { cost[x,row]=local;parent[x,row]=row; }
     else { double best=double.MaxValue;int bestRow=row;for(int previous=Math.Max(0,row-3);previous<=Math.Min(count-1,row+3);previous++) { double candidate=cost[x-1,previous]+Math.Abs(previous-row)*16; if(candidate<best) {best=candidate;bestRow=previous;} } cost[x,row]=local+best;parent[x,row]=bestRow; }
    }
    int end=0;for(int row=1;row<count;row++)if(cost[width-1,row]<cost[width-1,end])end=row;
    var seam=new int[width];for(int x=width-1;x>=0;x--) {seam[x]=minY+end;end=parent[x,end];}
    for(int x=0;x<width;x++)for(int y=0;y<=seam[x];y++)output.SetPixel(x,y,original.GetPixel(x,y));
    output.Save(outputPath,ImageFormat.Png);
   }
   using(var saved=new Bitmap(outputPath)) {
    int mismatch=0;
    for(int y=region.Top;y<region.Bottom;y++)
     for(int x=region.Left;x<region.Right;x++)
      if(original.GetPixel(x,y).ToArgb()!=saved.GetPixel(x,y).ToArgb()) mismatch++;
    int oldBottom=0,newBottom=0;
    for(int y=0;y<original.Height;y++)for(int x=0;x<original.Width;x++) {
      if(original.GetPixel(x,y).A>=220) oldBottom=Math.Max(oldBottom,y);
      if(saved.GetPixel(x,y).A>=220) newBottom=Math.Max(newBottom,y);
    }
    return "{\"faceRegion\":[530,100,270,265],\"facePixelsCompared\":"+region.Width*region.Height+",\"facePixelMismatches\":"+mismatch+",\"originalOpaqueBottomY\":"+oldBottom+",\"correctedOpaqueBottomY\":"+newBottom+",\"canvas\":[1024,1536],\"method\":\"Built-in imagegen neck-down edit plus exact original face region restoration without resampling\"}";
   }
  }
 }
}


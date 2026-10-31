"""Compare the mixed font to independent originals and render a before/after atlas preview."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont

sys.dont_write_bytecode=True
from build_typography_assets import ASSETS, OUTPUT, FONTS, guid, meta
from verify_typography_assets import BitmapFont


def raster(font, char):
    mask,offset=font.getmask2(char,mode="L",anchor="ls")
    pixels=bytes(255 if value>=128 else 0 for value in bytes(mask))
    return mask.size,offset,font.getlength(char),pixels


def main():
    original_path=ASSETS/"Art/Fonts/DosStory/DOSIyagiMedium.ttf"
    gothic_path=ASSETS/"Art/Fonts/DosGothic/DOSGothic.ttf"
    mixed_path=ASSETS/FONTS[0][1]
    original,mixed=TTFont(original_path),TTFont(mixed_path)
    original_cmap,mixed_cmap=original.getBestCmap(),mixed.getBestCmap()
    assert set(original_cmap)==set(mixed_cmap),"Source Unicode coverage changed"
    replaced=set(map(ord,"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"))
    unchanged_glyphs=set()
    for code,name in original_cmap.items():
        if code in replaced:
            continue
        assert mixed_cmap[code]==name,f"Unexpected character remapping: U+{code:04X}"
        unchanged_glyphs.add(name)
    for name in unchanged_glyphs:
        assert original["glyf"][name].compile(original["glyf"])==mixed["glyf"][name].compile(mixed["glyf"]),name+": original outline changed"
        assert original["hmtx"].metrics[name]==mixed["hmtx"].metrics[name],name+": original metrics changed"
    for field in ("ascent","descent","lineGap"):
        assert getattr(original["hhea"],field)==getattr(mixed["hhea"],field),field
    fonts=[ImageFont.truetype(str(path),16) for path in (original_path,gothic_path,mixed_path)]
    saved=BitmapFont("DosStory")
    changed=0
    for code in replaced:
        char=chr(code)
        expected=raster(fonts[1],char)
        assert raster(fonts[2],char)==expected,char+": mixed source differs from Gothic"
        glyph,bx,by,advance=saved.glyphs[saved.characters[code]]
        assert (glyph.size,(int(bx),int(-by)),advance,glyph.tobytes())==expected,char+": saved atlas differs from Gothic"
        changed+=raster(fonts[0],char)!=expected
    source_guid=guid(FONTS[0][1])
    assert "m_SourceFontFileGUID: "+source_guid in saved.text
    assert "m_AtlasPopulationMode: 1" in saved.text

    image=Image.new("RGB",(1100,540),"#0c0b15")
    draw=ImageDraw.Draw(image)
    captions=BitmapFont("Galmuri11Condensed")
    samples=["B1 · 잊힌 미궁", "HP 75 / 75   MP 30   386", "ABC abc 0123456789"]
    for column,title,font in ((0,"BEFORE / DOS STORY",fonts[0]),(1,"AFTER / DOS STORY + DOS GOTHIC",fonts[2])):
        x=28+column*550
        captions.draw(image,title,x,22,2,"#d8bb62",BitmapFont("Galmuri11"))
        for index,sample in enumerate(samples):
            # Render the exact source glyph grid, including examples beyond the prewarmed CSV.
            band=Image.new("L",(520,48))
            ImageDraw.Draw(band).text((0,14),sample,font=font,anchor="ls",fill=255)
            band=band.point(lambda value:255 if value>=128 else 0)
            band=band.resize((1040,96),Image.Resampling.NEAREST).crop((0,0,520,96))
            image.paste("#f3e6bf",(x,90+index*135),band)
    captions.draw(image,"Offline source/atlas verification / actual Unity rendering checked separately",28,505,2,"#a49a91",BitmapFont("Galmuri11"))
    image.save(OUTPUT/"DosStoryGothicLatin.png")
    report={"result":"PASS","asciiLettersAndDigitsComparedToGothic":len(replaced),"visuallyChangedAlphanumerics":changed,
            "originalUnicodeCoverage":len(original_cmap),"unchangedOtherCodepoints":len(original_cmap)-len(replaced),
            "unchangedOriginalGlyphOutlines":len(unchanged_glyphs),"nativePointSize":16,
            "lineMetricsPreserved":True,"savedAtlasMatchesGothic":True,"dynamicSource":"Assets/"+FONTS[0][1],
            "scope":"Source outlines/metrics, independent native pixel comparison and saved TMP atlas; not a live playtest"}
    (OUTPUT/"MixedFontVerification.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    meta("Editor/Presentation/verify_dos_story_mixed.py")
    print(json.dumps(report))


if __name__=="__main__":
    main()

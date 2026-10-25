"""Validate saved TMP assets and render their actual atlas glyphs, without Unity or desktop input."""
import csv
import json
import re
import subprocess
import sys
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps
sys.dont_write_bytecode=True
from build_typography_assets import ASSETS, OUTPUT, FONTS, FONT_DIR, PROFILE, BINDING, ROLES, guid, profile_faces, saved_face


def docs(text):
    pieces = re.split(r"(?m)^--- !u!\d+ &(-?\d+)\n", text)
    return {pieces[i]: pieces[i+1] for i in range(1,len(pieces),2)}


class BitmapFont:
    def __init__(self, name):
        self.name = name
        self.path = ASSETS / FONT_DIR / (name+".asset")
        text = self.path.read_text(encoding="utf-8")
        self.text = text
        assert "mainObjectFileID: 11400000" in self.path.with_suffix(".asset.meta").read_text(encoding="utf-8"),name+": font is not the main imported asset"
        width = int(re.search(r"(?m)^  m_Width: (\d+)",text)[1])
        height = int(re.search(r"(?m)^  m_Height: (\d+)",text)[1])
        raw = bytes.fromhex(re.search(r"(?m)^  _typelessdata: ([0-9a-f]+)$",text)[1])
        assert len(raw)==width*height, name+": wrong texture byte count"
        self.atlas = ImageOps.flip(Image.frombytes("L",(width,height),raw))
        self.point = int(re.search(r"m_PointSize: (\d+)",text)[1])
        self.ascent = float(re.search(r"m_AscentLine: ([\d.]+)",text)[1])
        self.glyphs = {}
        glyph_table = text.split("  m_GlyphTable:\n")[1].split("  m_CharacterTable:")[0]
        for block in glyph_table.split("  - m_Index: ")[1:]:
            index = int(block.splitlines()[0])
            def number(field):
                return float(re.search(rf"(?m)^      {field}: (-?[\d.]+)",block)[1])
            x,y,w,h = (int(number(k)) for k in ("m_X","m_Y","m_Width","m_Height"))
            assert x>=0 and y>=0 and x+w<=width and y+h<=height, name+": glyph outside atlas"
            image=self.atlas.crop((x,height-y-h,x+w,height-y))
            if w*h:
                assert image.getbbox() is not None, name+": empty nonempty glyph"
            self.glyphs[index]=(image,number("m_HorizontalBearingX"),number("m_HorizontalBearingY"),number("m_HorizontalAdvance"))
        self.characters={int(a):int(b) for a,b in re.findall(r"m_Unicode: (\d+)\n    m_GlyphIndex: (\d+)",text)}
        assert set(self.characters.values())<=set(self.glyphs), name+": orphan character"
        assert "m_FilterMode: 0" in text and "m_AtlasPopulationMode: 1" in text
        assert "128e987d567d4e2c824d754223b3f3b0" in text, name+": wrong bitmap shader"

    def draw(self, target, text, x,y,scale=2,color="#e6d6b8",fallback=None):
        cursor=x
        for char in text:
            font=self if ord(char) in self.characters else fallback
            assert font is not None and ord(char) in font.characters, f"Missing preview glyph {char}"
            image,bx,by,advance=font.glyphs[font.characters[ord(char)]]
            if image.width and image.height:
                mask=image.resize((image.width*scale,image.height*scale),Image.Resampling.NEAREST)
                target.paste(color,(round(cursor+bx*scale),round(y+(font.ascent-by)*scale)),mask)
            cursor+=advance*scale
        return cursor-x


def main():
    fonts={name:BitmapFont(name) for name,_,_ in FONTS}
    fallback=fonts["Galmuri11"]
    with (ASSETS/"Data/Localization/kr.csv").open(encoding="utf-8-sig",newline="") as stream:
        localized={row["key"]:row["kr"] for row in csv.DictReader(stream)}
        source_text="".join(localized.values())
    chars={ord(c) for c in source_text if not c.isspace()}
    missing=chars-set(fallback.characters)
    assert not missing, f"Missing localized glyphs: {missing}"
    counts=Counter()
    faces=Counter()
    event_labels=[]
    base, rules=profile_faces()
    unchanged_rects=unchanged_texts=0
    head_differences=[]
    styled_files=[]
    for path in [*(ASSETS/"Scenes").glob("*.unity"),*(ASSETS/"Prefabs").rglob("*.prefab")]:
        source=path.read_text(encoding="utf-8-sig")
        parts=docs(source)
        headers=re.findall(r"(?m)^--- !u!\d+ &(-?\d+)$",source)
        assert len(headers)==len(set(headers)), str(path)+": duplicate serialized object ID"
        bindings={}
        for body in parts.values():
            if guid(BINDING) not in body:
                continue
            go=re.search(r"m_GameObject: \{fileID: (-?\d+)\}",body)[1]
            assert go not in bindings, str(path)+": duplicate role binding"
            role=int(re.search(r"(?m)^  role: (\d+)",body)[1])
            bindings[go]=role
            counts[ROLES[role]]+=1
            assert guid(PROFILE) in body, str(path)+": detached style profile"
        for body in parts.values():
            if "f4688fdb7df04437aeb418b961361dc5" not in body:
                continue
            go=re.search(r"m_GameObject: \{fileID: (-?\d+)\}",body)[1]
            assert go in bindings, str(path)+": TMP text missing semantic binding"
            font_id=re.search(r"m_fontAsset: \{fileID: 11400000, guid: (\w+)",body)[1]
            material_id=re.search(r"m_sharedMaterial: \{fileID: -?\d+, guid: (\w+)",body)[1]
            assert font_id==material_id, str(path)+": font and atlas material disagree"
            face=saved_face(bindings[go],body,base,rules)
            assert font_id==guid(FONT_DIR+"/"+face+".asset"), str(path)+": saved face disagrees with active role profile"
            faces[face]+=1
        for body in parts.values():
            if guid("Scripts/UI/RunExpansionView.cs") not in body:
                continue
            references=[]
            for field,role in (("modalTitle",2),("modalNarrative",3)):
                match=re.search(rf"(?m)^  {field}: \{{fileID: (-?\d+)\}}",body)
                if match:
                    references.append((field,match[1],role))
            for block in re.finditer(r"(?ms)^  (choices|compactChoices):\n(.*?)(?=^  [A-Za-z_])",body):
                for field,target in re.findall(r"(?m)^    (title|description): \{fileID: (-?\d+)\}",block[2]):
                    references.append((block[1]+"."+field,target,5 if field=="title" else 4))
            for field,target,expected_role in references:
                label=parts[target]
                go=re.search(r"m_GameObject: \{fileID: (-?\d+)\}",label)[1]
                assert bindings[go]==expected_role,f"{path}: {field} has the wrong event role"
                event_labels.append({"path":str(path.relative_to(ASSETS)),"field":field,"label":target,"role":ROLES[expected_role],"font":saved_face(expected_role,label,base,rules)})
        if bindings:
            styled_files.append(str(path.relative_to(ASSETS)))
            old=subprocess.run(["git","show","HEAD:Assets/"+path.relative_to(ASSETS).as_posix()],cwd=ASSETS,capture_output=True,text=True,encoding="utf-8",check=True).stdout
            previous=docs(old)
            for file_id,body in previous.items():
                if body.startswith("RectTransform:"):
                    if parts[file_id]!=body:
                        head_differences.append({"path":str(path.relative_to(ASSETS)),"fileID":file_id,"field":"RectTransform"})
                    else:
                        unchanged_rects+=1
                if "f4688fdb7df04437aeb418b961361dc5" in body:
                    old_text=re.search(r"(?ms)^  m_text:.*?(?=^  m_)" ,body)[0]
                    new_text=re.search(r"(?ms)^  m_text:.*?(?=^  m_)" ,parts[file_id])[0]
                    if new_text!=old_text:
                        head_differences.append({"path":str(path.relative_to(ASSETS)),"fileID":file_id,"field":"m_text"})
                    else:
                        unchanged_texts+=1

    assert not head_differences or "--allow-head-differences" in sys.argv, "Working tree has authored layout/text edits relative to HEAD; review before allowing: "+str(head_differences)

    image=Image.new("RGB",(1360,1010),"#100f1b")
    canvas=ImageDraw.Draw(image)
    body_face=FONTS[(rules[4][0] or base)-1][0]
    fonts["DosStory"].draw(image,"TYPOGRAPHY / DOS STORY BASE",48,28,2,"#d8bb62",fallback)
    fonts["Galmuri11Condensed"].draw(image,"Saved TMP atlas glyphs / native integer scaling / offline preview",48,90,2,"#a49a91",fallback)
    rows=[
        ("BASE / HEADING", "DosStory", "위자드    장비 정보    주문서",3),
        ("ACTION / BUTTON", "DosStory", "게임 시작    능력 상세    뒤로",3),
        ("DESCRIPTION / DENSE", body_face, "주문 쿨타임 -25%  가드 중 MP가 초당 0.9 추가 회복됩니다.",3),
        ("HUD / COMBAT VALUE", "DosStory", "HP 65 / 65    MP 30    123!!    0.85",3),
        ("MICRO / SYMBOLS", "Galmuri7", "\u00d73    12/24    +5%    \u2190 \u2191 \u2192 \u2193",4),
        ("NARRATIVE / LONG TEXT", body_face, "호기심에 의해 미궁에 발을 들인 탑의 마법사.",3),
    ]
    y=148
    for label,face,sample,scale in rows:
        canvas.rounded_rectangle((32,y,1328,y+125),radius=8,fill="#1c1b2b")
        fonts["Galmuri11Condensed"].draw(image,label+" / "+face,50,y+12,2,"#d8bb62",fallback)
        fonts[face].draw(image,sample,50,y+56,scale,"#f3e6bf",fallback)
        y+=136
    fonts["Galmuri11Condensed"].draw(image,"DOS Story by default / "+body_face+" trial for long text / Galmuri7 for tiny counts",48,975,2,"#a49a91",fallback)
    image.save(OUTPUT/"TypographyRoles.png")
    # Same content, two body faces: a typography comparison, not a Unity screenshot.
    comparison=Image.new("RGB",(1400,740),"#100f1b")
    ink=ImageDraw.Draw(comparison)
    def wrapped(font,value,x,y,width,max_lines):
        value=value.replace("\r\n","\n").replace("\r","\n")
        line="";lines=[]
        for character in value:
            advance=sum(font.glyphs[font.characters[ord(c)]][3] if ord(c) in font.characters else fallback.glyphs[fallback.characters[ord(c)]][3] for c in line+character if c!="\n")*2
            if character=="\n" or advance>width:
                lines.append(line);line="" if character=="\n" else character
            else:
                line+=character
        if line:lines.append(line)
        for line in lines[:max_lines]:
            font.draw(comparison,line,x,y,2,"#f3e6bf",fallback);y+=30
    for index,face in enumerate(("Galmuri11Condensed","Tangba")):
        x=30+index*700
        ink.rounded_rectangle((x-8,65,x+650,705),radius=8,fill="#1c1b2b")
        fonts["Galmuri11Condensed"].draw(comparison,("TRIAL OFF / " if index==0 else "TRIAL ON / ")+face,x,20,2,"#d8bb62",fallback)
        fonts["DosStory"].draw(comparison,localized["data.randomeventdata.bell.displayname"],x+16,90,2,"#d8bb62",fallback)
        wrapped(fonts[face],localized["data.randomeventdata.bell.narrative"],x+16,160,600,10)
        fonts["DosStory"].draw(comparison,"> "+localized["data.randomeventdata.bell.options.0.title"],x+16,520,2,"#d8bb62",fallback)
        wrapped(fonts[face],localized["data.randomeventdata.bell.options.0.description"],x+16,575,600,4)
    comparison.save(OUTPUT/"TangbaTrial.png")
    report={"result":"PASS","scope":"saved assets, atlas pixels, original layout/text, compiler checks run separately; not a Unity runtime playtest","styledFiles":styled_files,"textRoles":dict(counts),"savedPrimaryFaces":dict(faces),"totalStyledLabels":sum(counts.values()),"unchangedRectTransforms":unchanged_rects,"unchangedSerializedTextFields":unchanged_texts,"localizedCodepointsChecked":len(chars),"localizedCodepointsMissingAfterFallback":len(missing)}
    report["authoredDifferencesFromGitHead"]=head_differences
    report["eventPopupLabels"]=event_labels
    (OUTPUT/"StaticVerification.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False))


if __name__=="__main__":
    main()

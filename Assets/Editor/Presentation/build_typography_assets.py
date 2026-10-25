"""Build inspectable TMP bitmap atlases and initial semantic bindings without launching Unity.

Run from Assets with Python + Pillow + fonttools. Existing authored role bindings are preserved.
Unity's TypographyPreparation assigns roles to newly authored labels on subsequent project opens.
"""
import csv
import argparse
import json
import math
import re
import sys
import uuid
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps
from fontTools.ttLib import TTFont
from fontTools.pens.recordingPen import DecomposingRecordingPen
from fontTools.pens.ttGlyphPen import TTGlyphPen

ASSETS = Path(__file__).resolve().parents[2]
PROJECT = ASSETS.parent
OUTPUT = PROJECT / "Docs/Production/Typography"
NAMESPACE = uuid.UUID("d83aa8c4-f8ac-4b58-8347-684a1d6db7bd")
ROLES = ["Base", "Title", "Heading", "Narrative", "Description", "Button", "HudValue", "CompactValue", "MicroValue", "CombatNumber", "Hint"]
FONTS = [
    ("DosStory", "Art/Fonts/DosStory/DOSStoryGothicLatin.ttf", 16),
    ("Galmuri11", "Art/Fonts/Galmuri/Galmuri11.ttf", 12),
    ("Galmuri11Condensed", "Art/Fonts/Galmuri/Galmuri11-Condensed.ttf", 12),
    ("Galmuri9", "Art/Fonts/Galmuri/Galmuri9.ttf", 10),
    ("Galmuri7", "Art/Fonts/Galmuri/Galmuri7.ttf", 8),
    ("Tangba", "Art/Fonts/Tangba/Tangba12.ttf", 12),
]
FONT_DIR = "Art/Fonts/Typography"
ATLAS = 512
PROFILE = "Data/Presentation/Typography.asset"
BINDING = "Scripts/UI/TypographyRoleBinding.cs"
TYPEFACE = {"DosStory": 1, "Galmuri11": 2, "Galmuri11Condensed": 3, "Galmuri9": 4, "Galmuri7": 5, "Tangba": 6}
ROLE_FACE = [0, 0, 0, 3, 3, 0, 0, 0, 5, 0, 0]
YAML = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
MONO = """MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: GAMEOBJECT}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: SCRIPT, type: 3}
  m_Name:
  m_EditorClassIdentifier:
"""


def guid(path):
    meta = ASSETS / (path + ".meta")
    if meta.exists():
        return re.search(r"guid: (\w+)", meta.read_text(encoding="utf-8-sig"))[1]
    return uuid.uuid5(NAMESPACE, path).hex


def meta(path, importer=""):
    target = ASSETS / (path + ".meta")
    if target.exists():
        return
    if not importer and path.endswith(".asset"):
        importer="NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    target.write_text(f"fileFormatVersion: 2\nguid: {guid(path)}\n{importer}", encoding="utf-8")


def write(path, value):
    target = ASSETS / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text("\n".join(line.rstrip() for line in value.splitlines())+"\n", encoding="utf-8", newline="\n")
    meta(path)


def ref(path):
    return "{fileID: 11400000, guid: " + guid(path) + ", type: 2}"


def replace_block(text, field, block):
    return re.sub(rf"(?ms)^  {field}:.*?(?=^  [A-Za-z_])", f"  {field}:\n{block}", text, count=1)


def build_dos_story_mixed():
    """Retain Story's source data, with independently named Gothic ASCII alphanumerics.

    A real source font keeps TMP's dynamic glyph population and one atlas/material working,
    without rewriting localized strings, injecting rich-text tags or relying on missing glyphs.
    """
    story=TTFont(ASSETS/"Art/Fonts/DosStory/DOSIyagiMedium.ttf",recalcTimestamp=False)
    gothic=TTFont(ASSETS/"Art/Fonts/DosGothic/DOSGothic.ttf",recalcTimestamp=False)
    assert story["head"].unitsPerEm==gothic["head"].unitsPerEm
    assert (story["hhea"].ascent,story["hhea"].descent)==(gothic["hhea"].ascent,gothic["hhea"].descent)
    cmap=gothic.getBestCmap()
    glyph_set=gothic.getGlyphSet()
    order=list(story.getGlyphOrder())
    for char in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789":
        name="BehemothGothic"+str(ord(char))
        recording=DecomposingRecordingPen(glyph_set)
        glyph_set[cmap[ord(char)]].draw(recording)
        pen=TTGlyphPen(None)
        recording.replay(pen)
        story["glyf"][name]=pen.glyph()
        story["hmtx"].metrics[name]=gothic["hmtx"].metrics[cmap[ord(char)]]
        order.append(name)
        for table in story["cmap"].tables:
            if table.isUnicode() and ord(char) in table.cmap:
                table.cmap[ord(char)]=name
    story.setGlyphOrder(order)
    for name_id,value in ((1,"Behemoth DOS Story Mixed"),(2,"Regular"),
                          (3,"Behemoth DOS Story Mixed 1.0"),(4,"Behemoth DOS Story Mixed"),
                          (6,"BehemothDOSStoryGothicLatin")):
        story["name"].removeNames(nameID=name_id)
        story["name"].setName(value,name_id,3,1,0x409)
    # Do not retain an original preferred family that could override the derivative family name.
    story["name"].removeNames(nameID=16)
    story["name"].removeNames(nameID=17)
    destination=ASSETS/FONTS[0][1]
    story.save(destination)


def build_font(name, source, point, characters, template):
    source_font = TTFont(ASSETS / source)
    cmap = source_font.getBestCmap()
    pil = ImageFont.truetype(str(ASSETS / source), point)
    atlas = Image.new("L", (ATLAS, ATLAS))
    glyphs, entries, seen = [], [], {}
    x = y = row_height = 1
    for code in sorted(characters):
        glyph_name = cmap.get(code)
        if glyph_name is None:
            continue
        index = source_font.getGlyphID(glyph_name)
        if index not in seen:
            char = chr(code)
            mask, offset = pil.getmask2(char, mode="L", anchor="ls")
            width, height = mask.size
            if x + width + 2 > ATLAS:
                x = 1
                y += row_height + 2
                row_height = 1
            if y + height + 2 > ATLAS:
                raise RuntimeError(f"{name}: atlas full; increase dimensions explicitly")
            if width and height:
                glyph = Image.frombytes("L", (width, height), bytes(mask)).point(lambda v: 255 if v >= 128 else 0)
                atlas.paste(glyph, (x, y))
            seen[index] = True
            glyphs.append(f"""  - m_Index: {index}
    m_Metrics:
      m_Width: {width}
      m_Height: {height}
      m_HorizontalBearingX: {offset[0]}
      m_HorizontalBearingY: {-offset[1]}
      m_HorizontalAdvance: {pil.getlength(char):g}
    m_GlyphRect:
      m_X: {x}
      m_Y: {ATLAS-y-height}
      m_Width: {width}
      m_Height: {height}
    m_Scale: 1
    m_AtlasIndex: 0
    m_ClassDefinitionType: 0
""")
            x += width + 2
            row_height = max(row_height, height)
        entries.append(f"""  - m_ElementType: 1
    m_Unicode: {code}
    m_GlyphIndex: {index}
    m_Scale: 1
""")
    text = template.replace("Galmuri11 - Regular", name).replace("m_Name: ArcadeFont", "m_Name: " + name)
    text = text.replace("f5d3a0f6673112348b859c7844d461e2", guid(source))
    text = re.sub(r"m_Width: 2048", f"m_Width: {ATLAS}", text)
    text = re.sub(r"m_Height: 2048", f"m_Height: {ATLAS}", text)
    text = text.replace("4194304", str(ATLAS*ATLAS))
    text = re.sub(r"(?m)^  _typelessdata:.*$", "  _typelessdata: " + ImageOps.flip(atlas).tobytes().hex(), text)
    asc, desc = pil.getmetrics()
    line = (source_font["hhea"].ascent - source_font["hhea"].descent + source_font["hhea"].lineGap) * point / source_font["head"].unitsPerEm
    text = replace_block(text, "m_FaceInfo", f"""    m_FaceIndex: 0
    m_FamilyName: {source_font['name'].getDebugName(1)}
    m_StyleName: Regular
    m_PointSize: {point}
    m_Scale: 1
    m_UnitsPerEM: {source_font['head'].unitsPerEm}
    m_LineHeight: {line:g}
    m_AscentLine: {asc}
    m_CapLine: {-pil.getbbox('H',anchor='ls')[1]}
    m_MeanLine: {-pil.getbbox('x',anchor='ls')[1]}
    m_Baseline: 0
    m_DescentLine: {-desc}
    m_SuperscriptOffset: {asc}
    m_SuperscriptSize: 0.5
    m_SubscriptOffset: {-desc}
    m_SubscriptSize: 0.5
    m_UnderlineOffset: -1
    m_UnderlineThickness: 1
    m_StrikethroughOffset: {point*.3:g}
    m_StrikethroughThickness: 1
    m_TabWidth: {pil.getlength(' '):g}
""")
    text = replace_block(text, "m_GlyphTable", "".join(glyphs))
    text = replace_block(text, "m_CharacterTable", "".join(entries))
    used_height=y+row_height+1
    occupied=f"  m_UsedGlyphRects:\n  - m_X: 0\n    m_Y: {ATLAS-used_height}\n    m_Width: {ATLAS}\n    m_Height: {used_height}\n"
    available=f"  m_FreeGlyphRects:\n  - m_X: 0\n    m_Y: 0\n    m_Width: {ATLAS}\n    m_Height: {ATLAS-used_height}\n"
    text = re.sub(r"(?ms)^  m_UsedGlyphRects:.*?(?=^  m_FreeGlyphRects:)", occupied, text)
    text = re.sub(r"(?ms)^  m_FreeGlyphRects:.*?(?=^  [A-Za-z_])", available, text)
    # Keep source TTFs and dynamic population: newly authored CSV content uses this same face.
    # Its occupied band is reserved so Unity's glyph packer cannot overwrite prewarmed pixels.
    text = text.replace("m_ClearDynamicDataOnBuild: 1", "m_ClearDynamicDataOnBuild: 0")
    text = text.replace("m_AtlasWidth: 2048", f"m_AtlasWidth: {ATLAS}").replace("m_AtlasHeight: 2048", f"m_AtlasHeight: {ATLAS}")
    fallback = "Art/Fonts/ArcadeFont.asset" if name == "Galmuri11" else FONT_DIR + "/Galmuri11.asset"
    text = re.sub(r"(?ms)^  m_FallbackFontAssetTable:.*?(?=^  [A-Za-z_])", "  m_FallbackFontAssetTable:\n  - " + ref(fallback) + "\n", text)
    write(FONT_DIR + "/" + name + ".asset", text)
    atlas.save(OUTPUT / (name + "-atlas.png"))
    return {"face": name, "nativePointSize": point, "characters": len(entries), "uniqueGlyphs": len(glyphs), "source": source, "atlasHeightUsed": y + row_height, "directMissing": len(characters - set(cmap)), "pil": pil, "cmap": cmap}


def create_profile():
    fields = ["dosStory", "galmuri11", "galmuri11Condensed", "galmuri9", "galmuri7", "tangba"]
    text = YAML + "--- !u!114 &11400000\n" + MONO.replace("GAMEOBJECT", "0").replace("SCRIPT", guid("Scripts/UI/TypographyProfile.cs"))
    text = text.replace("  m_Name:\n", "  m_Name: Typography\n") + "  baseFace: 1\n"
    for field, (face, _, _) in zip(fields, FONTS):
        text += f"  {field}: {ref(FONT_DIR+'/'+face+'.asset')}\n"
    text += "  useTangbaForCondensed: 1\n  snapToNativePixels: 1\n  rules:\n"
    for i, face in enumerate(ROLE_FACE):
        text += f"  - role: {i}\n    face: {face}\n    narrowFace: 3\n    useNarrowFaceWhenNeeded: {int(i in (1,2,5))}\n    numericOnly: {int(i==8)}\n    sizeMultiplier: 1\n"
    if not (ASSETS / PROFILE).exists():
        write(PROFILE, text)


def profile_faces():
    text = (ASSETS / PROFILE).read_text(encoding="utf-8-sig")
    base = int(re.search(r"(?m)^  baseFace: (\d+)", text)[1]) or 1
    rules = {int(role):(int(face),int(numeric)) for role,face,numeric in re.findall(
        r"  - role: (\d+)\n    face: (\d+)\n.*?    numericOnly: (\d+)", text, re.S)}
    if re.search(r"(?m)^  useTangbaForCondensed: 1$",text):
        rules={role:(6 if face==3 else face,numeric) for role,(face,numeric) in rules.items()}
    return base, rules


def saved_face(role, body, base, rules):
    face, numeric = rules.get(role, (0,0))
    if numeric:
        value = re.search(r"(?ms)^  m_text: ?(.*?)(?=^  m_)", body)[1].strip()
        # Unity YAML can use \x escapes and folded quoted scalars, beyond JSON syntax.
        value = re.sub(r"\\(?:u([0-9a-fA-F]{4})|U([0-9a-fA-F]{8})|x([0-9a-fA-F]{2}))",
                       lambda match:chr(int(next(g for g in match.groups() if g is not None),16)),value)
        value = re.sub(r"\\[0abtnvfreN_LP]", " ", value)
        value = re.sub(r"[xX](?=\d)", "", value)
        if any(c.isalpha() for c in value):
            face = 0
    return FONTS[(face or base)-1][0]


def set_face(body, face):
    body = re.sub(r"(?m)^  m_fontAsset:.*$", "  m_fontAsset: " + ref(FONT_DIR+"/"+face+".asset"), body)
    return re.sub(r"(?m)^  m_sharedMaterial:.*$", "  m_sharedMaterial: {fileID: -8703805978501734097, guid: " + guid(FONT_DIR+"/"+face+".asset") + ", type: 2}", body)


def assign(path, owner_types, role_map, sync_faces=False, stage_bindings=None, sync_event_roles=False):
    source = path.read_text(encoding="utf-8-sig")
    parts = re.split(r"(?m)(^--- !u!\d+ &-?\d+\n)", source)
    docs = {}
    order = []
    for i in range(1, len(parts), 2):
        header = parts[i]
        fileid = re.search(r"&(-?\d+)", header)[1]
        docs[fileid] = [header, parts[i+1]]
        order.append(fileid)
    authored_layout = {key:body for key,(_,body) in docs.items() if body.startswith(("Transform:","RectTransform:"))}
    authored_text = {key:re.search(r"(?ms)^  m_text:.*?(?=^  m_)",body)[0] for key,(_,body) in docs.items() if "f4688fdb7df04437aeb418b961361dc5" in body}
    label_guid = "f4688fdb7df04437aeb418b961361dc5"
    labels, existing, gameobjects, parents, buttons = {}, {}, {}, {}, set()
    mapped, event_mapped, existing_ids = {}, {}, {}
    for fileid, (_, body) in docs.items():
        gameobject = re.search(r"  m_GameObject: \{fileID: (-?\d+)\}", body)
        go = gameobject[1] if gameobject else None
        script = re.search(r"m_Script: \{fileID: 11500000, guid: (\w+)", body)
        script_guid = script[1] if script else None
        if body.startswith("GameObject:"):
            gameobjects[fileid] = body
        if body.startswith("RectTransform:") or body.startswith("Transform:"):
            father = re.search(r"m_Father: \{fileID: (-?\d+)\}", body)
            parents[go] = father[1] if father else "0"
        if script_guid == guid(BINDING):
            existing[go] = int(re.search(r"(?m)^  role: (\d+)", body)[1])
            existing_ids[go] = fileid
            docs[fileid][1] = "\n".join(line.rstrip() for line in body.splitlines())+"\n"
        if script_guid == label_guid:
            labels[fileid] = (go, body)
        owner = owner_types.get(script_guid, "")
        if owner == "Button":
            buttons.add(go)
        field = None
        for line in body.splitlines():
            match = re.match(r"^  (\w+):", line)
            if match:
                field = match[1]
            reference = re.search(r"\{fileID: (-?\d+)\}", line)
            if not reference or field is None:
                continue
            nested = re.match(r"^    (\w+):",line)
            field_path=field+"."+nested[1] if nested else field
            leaf=nested[1] if nested else field
            role = role_map.get(owner+"."+field_path,role_map.get(owner+"."+field,role_map.get("*."+leaf)))
            if role is not None:
                mapped[reference[1]] = role
                if owner=="RunExpansionView" and field_path in ("modalTitle","modalNarrative","choices.title","choices.description","compactChoices.title","compactChoices.description"):
                    event_mapped[reference[1]]=role
    transforms_to_go = {}
    for fileid, (_, body) in docs.items():
        if body.startswith(("Transform:", "RectTransform:")):
            transforms_to_go[fileid] = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)[1]
    counts = Counter()
    base, rules = profile_faces()
    new_docs = []
    next_id = 1600000000000000000
    for label_id, (go, body) in labels.items():
        if go in existing:
            if sync_event_roles and label_id in event_mapped:
                existing[go]=event_mapped[label_id]
                binding_id=existing_ids[go]
                docs[binding_id][1]=re.sub(r"(?m)^  role: \d+$","  role: "+str(existing[go]),docs[binding_id][1])
            counts[ROLES[existing[go]]] += 1
            if sync_faces:
                docs[label_id][1] = set_face(body, saved_face(existing[go], body, base, rules))
            continue
        size = float(re.search(r"^  m_fontSize: ([\d.]+)", body, re.M)[1])
        ancestor = go
        is_button = False
        while ancestor and ancestor != "0":
            if ancestor in buttons:
                is_button = True
                break
            ancestor = transforms_to_go.get(parents.get(ancestor), "0")
        role = mapped.get(label_id, 5 if is_button else 1 if size >= 64 else 0)
        counts[ROLES[role]] += 1
        while str(next_id) in docs:
            next_id += 1
        component_id = str(next_id)
        next_id += 1
        go_body = docs[go][1]
        last_component = list(re.finditer(r"(?m)^  - component: \{fileID: -?\d+\}\n", go_body))[-1]
        at = last_component.end()
        docs[go][1] = go_body[:at] + f"  - component: {{fileID: {component_id}}}\n" + go_body[at:]
        docs[label_id][1] = set_face(body, saved_face(role, body, base, rules))
        binding = MONO.replace("GAMEOBJECT", go).replace("SCRIPT", guid(BINDING))
        binding += f"  profile: {ref(PROFILE)}\n  role: {role}\n  designSize: {size:g}\n  snapSize: 1\n"
        new_docs.append(f"--- !u!114 &{component_id}\n" + binding)
    assert all(docs[key][1]==body for key,body in authored_layout.items()), str(path)+": authored layout changed during font assignment"
    assert all(re.search(r"(?ms)^  m_text:.*?(?=^  m_)",docs[key][1])[0]==body for key,body in authored_text.items()), str(path)+": authored text changed during font assignment"
    result=parts[0] + "".join("".join(docs[k]) for k in order) + "".join(new_docs)
    if result!=source:
        destination = Path(stage_bindings) / path.relative_to(ASSETS) if stage_bindings else path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(result, encoding="utf-8", newline="\n")
    return counts


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sync-faces",action="store_true",help="Synchronize saved font/material references from the active profile")
    parser.add_argument("--stage-bindings",help="Stage changed scene/prefab files outside Assets for application by a host with compatible file-sharing access")
    parser.add_argument("--fonts-only",action="store_true",help="Build fonts only; do not assign or change scene/prefab bindings")
    parser.add_argument("--sync-event-roles",action="store_true",help="Repair only RunExpansionView event title/narrative/choice roles from their serialized field references")
    options=parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    build_dos_story_mixed()
    for path in ("Art/Fonts/DosGothic", "Art/Fonts/DosGothic/DOSGothic.ttf", "Art/Fonts/DosGothic/DOSGothic.woff", "Art/Fonts/DosGothic/LICENSE.txt", "Art/Fonts/DosGothic/SOURCE.md"):
        if (ASSETS/path).exists():
            if (ASSETS/path).is_dir():
                meta(path,"folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
            else:
                meta(path)
    for path in ("Scripts/UI/TypographyProfile.cs", BINDING, "Editor/Presentation/TypographyPreparation.cs", "Editor/Presentation/TypographyRoles.json"):
        meta(path)
    for _, source, _ in FONTS:
        meta(source, "TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  useLegacyBoundsCalculation: 0\n  shouldRoundAdvanceValue: 1\n")
    with (ASSETS / "Data/Localization/kr.csv").open(encoding="utf-8-sig", newline="") as stream:
        localized = [row["kr"] for row in csv.DictReader(stream)]
    characters = set(range(32,127)) | {ord(c) for c in "".join(localized) if not c.isspace()} | {32,0xA0,0x2190,0x2191,0x2192,0x2193,0xD7,0x2212,0x2026,0xFF01}
    template = (ASSETS / "Art/Fonts/ArcadeFont.asset").read_text(encoding="utf-8-sig")
    results = [build_font(*font, characters, template) for font in FONTS]
    if options.fonts_only:
        font_report=[{k:v for k,v in row.items() if k not in ("pil","cmap")} for row in results]
        (OUTPUT/"FontBuild.json").write_text(json.dumps({"fonts":font_report},indent=2),encoding="utf-8")
        audit_path=OUTPUT/"AssetAudit.json"
        if audit_path.exists():
            audit=json.loads(audit_path.read_text(encoding="utf-8"))
            audit["fonts"]=font_report
            audit_path.write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding="utf-8")
        print("Built fonts only; scene/prefab files untouched.")
        return
    create_profile()
    owner_types = {}
    for script in (ASSETS / "Scripts").rglob("*.cs"):
        content=script.read_text(encoding="utf-8-sig")
        match = re.search(r"class\s+("+re.escape(script.stem)+r")\b",content) or re.search(r"class\s+(\w+)",content)
        if match and script.with_suffix(".cs.meta").exists():
            owner_types[guid(script.relative_to(ASSETS).as_posix())] = match[1]
    owner_types["4e29b1a8efbd4b44bb3f3716e73f07ff"] = "Button"
    table = json.loads((ASSETS / "Editor/Presentation/TypographyRoles.json").read_text(encoding="utf-8"))
    role_map = {e["owner"]+"."+e["field"]:e["role"] for e in table["entries"]}
    total = Counter()
    files = {}
    for path in [*(ASSETS / "Scenes").glob("*.unity"), *(ASSETS / "Prefabs").rglob("*.prefab")]:
        counts = assign(path, owner_types, role_map, sync_faces=options.sync_faces, stage_bindings=options.stage_bindings,sync_event_roles=options.sync_event_roles)
        if counts:
            files[str(path.relative_to(ASSETS))] = dict(counts)
            total.update(counts)
    report = {"uniqueLocalizedCharacters":len(characters),"fonts":[{k:v for k,v in row.items() if k not in ("pil","cmap")} for row in results],"bindings":dict(total),"files":files}
    (OUTPUT / "AssetAudit.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps({"glyphCharacters":len(characters),"bindings":dict(total),"files":len(files)},ensure_ascii=False))


if __name__ == "__main__":
    main()

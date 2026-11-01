"""Install selected drawings; only uniform canvas sizing and portrait crops.

No palette, lighting, contour, alpha-matte, or drawing edits are performed here.
"""
import hashlib,json,pathlib,shutil
from PIL import Image,ImageOps,ImageDraw,ImageFont

folder=pathlib.Path(__file__).resolve().parent
root=folder.parents[3]
assets=root/'Assets'
selected=json.loads((folder.parent/'MonsterSelection.json').read_text(encoding='utf-8'))
before=folder.parent/'Before'
records=[]
hashes={}
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def backup(path):
    relative=path.relative_to(root)
    target=before/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    if not target.exists():shutil.copy2(path,target)
    return target
for item in selected:
    target=assets/item['target'];native=target.with_suffix('.asset')
    for path in (target,native,target.with_suffix('.png.meta'),native.with_suffix('.asset.meta')):
        backup(path)
        if path!=target:hashes[str(path.relative_to(assets))]=sha(path)
    old=Image.open(target).convert('RGBA')
    source=Image.open(item['source']).convert('RGBA')
    assert source.getchannel('A').getextrema()==(0,255),item
    fitted=ImageOps.contain(source,old.size,Image.Resampling.LANCZOS)
    result=Image.new('RGBA',old.size,(0,0,0,0));result.alpha_composite(fitted,((old.width-fitted.width)//2,(old.height-fitted.height)//2))
    result.save(target)
    records.append(dict(item,canvas=old.size,source_canvas=source.size,sha256=sha(target),change='independent state repaint; uniform full-canvas fit only'))

# Portraits are independent editable PNG crops of the same new Idle identity.
crops={'실인형':(.27,.14,.75,.62),'종토끼':(.30,.14,.80,.64),'공허의눈':(.25,.20,.89,.84),'숯불도롱뇽':(.41,.22,.99,.80)}
for species,rect in crops.items():
    target=assets/f'Art/Enemies/Portraits/Catalog/T_Sprite_Monster_{species}_Portrait.png';meta=target.with_suffix('.png.meta')
    backup(target);backup(meta);hashes[str(meta.relative_to(assets))]=sha(meta)
    old=Image.open(target);idle=Image.open(assets/f'Art/Combat/NativeMonsterFrames/T_Sprite_Monster_{species}_Idle.png').convert('RGBA')
    box=tuple(round(value*(idle.width if i%2==0 else idle.height)) for i,value in enumerate(rect))
    idle.crop(box).resize(old.size,Image.Resampling.LANCZOS).save(target)
    records.append(dict(species=species,state='Portrait',target=str(target.relative_to(assets)),crop=box,canvas=old.size,sha256=sha(target),change='editable portrait crop from the new Idle'))

for path,value in hashes.items():assert sha(assets/path)==value,'Unexpected native/meta geometry mutation: '+path
(folder/'SecondaryInstallation.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
(folder/'SecondaryAssetFiles.json').write_text(json.dumps([str(assets/r['target']) for r in records],ensure_ascii=False,indent=2),encoding='utf-8')
(folder/'SecondaryPreservedGeometry.json').write_text(json.dumps(hashes,ensure_ascii=False,indent=2),encoding='utf-8')

# Review boards are mechanical composites, never production-art replacements.
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
species_order=list(crops)
for species in species_order:
    poses=[r for r in records if r['species']==species]
    board=Image.new('RGB',(330*len(poses),780),(32,33,30));draw=ImageDraw.Draw(board)
    for i,r in enumerate(poses):
        target=assets/r['target'];old=Image.open(before/target.relative_to(root)).convert('RGBA');new=Image.open(target).convert('RGBA')
        draw.text((i*330+8,6),species+' '+r['state'],font=font,fill='white')
        for row,picture in enumerate((old,new)):
            picture.thumbnail((314,335),Image.Resampling.LANCZOS)
            board.paste(picture,(i*330+(330-picture.width)//2,40+row*365+(335-picture.height)//2),picture)
        draw.text((i*330+8,384),'AFTER',font=font,fill=(205,181,132))
    board.save(folder/(species+'-PoseReview.jpg'),quality=93)
bg=Image.open(assets/'Art/World/Angular/Restoration01/B1-Walls.png').convert('RGBA').resize((800,450))
board=Image.new('RGB',(1600,900))
for i,species in enumerate(species_order):
    panel=bg.copy();painting=Image.open(assets/f'Art/Combat/NativeMonsterFrames/T_Sprite_Monster_{species}_Idle.png').convert('RGBA');painting.thumbnail((370,350),Image.Resampling.LANCZOS)
    panel.alpha_composite(painting,((800-painting.width)//2,410-painting.height))
    ImageDraw.Draw(panel).text((12,10),species+' · scaled art comparison (runtime pending)',font=font,fill='white')
    board.paste(panel.convert('RGB'),((i%2)*800,(i//2)*450))
board.save(folder/'SecondaryRoomComposite.jpg',quality=94)
print('Installed',len(records),'PNG paintings; preserved',len(hashes),'native/meta files byte-for-byte. No Unity refresh started.')

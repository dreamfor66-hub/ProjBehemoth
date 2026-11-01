"""Inspect a pasted BHM1 record without Unity or the original generator version.

python inspect_play_code.py --file feedback-code.txt --out record-review
python inspect_play_code.py --file feedback-code.txt --floor 2 --room 9
"""
import argparse,base64,hashlib,json,pathlib,re,sys,zlib,html
MAX_BYTES=2*1024*1024
def decode(code):
    code=code.strip()
    if not code.startswith('BHM1-') or len(code)>MAX_BYTES:raise ValueError('Not a BHM1 play code')
    body,checksum=code[5:].rsplit('.',1)
    data=base64.urlsafe_b64decode(body+'='*(-len(body)%4))
    if hashlib.sha256(data).hexdigest()[:16].lower()!=checksum.lower():raise ValueError('Checksum mismatch')
    obj=zlib.decompressobj(-15);raw=obj.decompress(data,MAX_BYTES+1)
    if len(raw)>MAX_BYTES or obj.unconsumed_tail:raise ValueError('Record too large')
    result=json.loads(raw)
    if result.get('version')!=1:raise ValueError('Unsupported schema')
    return result
def types():
    assets=pathlib.Path(__file__).resolve().parents[3]/'Assets'
    for p in (assets/'Scripts').rglob('*.cs'):
        match=re.search(r'enum RoomEncounterType\s*\{([^}]+)\}',p.read_text(encoding='utf-8-sig'),re.S)
        if not match:continue
        result={};index=0
        for item in match[1].split(','):
            item=item.strip()
            if not item:continue
            if '=' in item:name,n=item.split('=');index=int(n)
            else:name=item
            result[index]=name.strip();index+=1
        return result
    return {}
def svg(floor,names):
    rooms=floor['rooms'];byid={r['id']:r for r in rooms};xs=[r['x'] for r in rooms];ys=[r['y'] for r in rooms]
    w=(max(xs)-min(xs)+2)*100;h=(max(ys)-min(ys)+2)*100
    def point(r):return ((r['x']-min(xs)+1)*100,(max(ys)-r['y']+1)*100)
    out=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="100%" height="100%" fill="#15171a"/>']
    gates={tuple(sorted((p['origin'],p['entrance']))):p for p in floor['passages']}
    for r in rooms:
        for id in r['connections']:
            if id<r['id']:continue
            x,y=point(r);xx,yy=point(byid[id]);g=gates.get((r['id'],id));stroke='#805ea2' if g else '#777770'
            dash='stroke-dasharray="5 6"' if g and (g['locked'] or not g['discovered']) else ''
            out.append(f'<path d="M{x},{y} L{xx},{yy}" stroke="{stroke}" stroke-width="3" {dash}/>')
    for r in rooms:
        x,y=point(r);name=names.get(r['type'],str(r['type']));color='#a4554c' if name in ('Combat','Elite','Boss') else '#5c8393' if name not in ('Empty','Start') else '#5b5d59'
        out.append(f'<rect x="{x-27}" y="{y-25}" width="54" height="50" rx="3" fill="{color}" stroke="{"#e7c779" if r["visited"] else "#555"}" stroke-width="2"/>')
        out.append(f'<text x="{x}" y="{y+4}" fill="white" font-family="sans-serif" font-size="17" text-anchor="middle">{r["id"]}</text>')
        out.append(f'<text x="{x}" y="{y+42}" fill="#ddd" font-family="sans-serif" font-size="11" text-anchor="middle">{html.escape(name)}</text>')
    out.append('</svg>');return ''.join(out)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--file',type=pathlib.Path,required=True);parser.add_argument('--floor',type=int);parser.add_argument('--room',type=int);parser.add_argument('--out',type=pathlib.Path)
    args=parser.parse_args();record=decode(args.file.read_text(encoding='utf-8-sig'));names=types()
    if args.out:
        args.out.mkdir(parents=True,exist_ok=True);(args.out/'Record.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
        for floor in record['floors']:(args.out/f'B{floor["floor"]}-Map.svg').write_text(svg(floor,names),encoding='utf-8')
    result=record
    if args.floor:
        result=next(f for f in record['floors'] if f['floor']==args.floor)
        if args.room is not None:result=next(r for r in result['rooms'] if r['id']==args.room)
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':
    sys.stdout.reconfigure(encoding='utf-8');main()

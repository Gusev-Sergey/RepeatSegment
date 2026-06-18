import sqlite3, zipfile, json, os

decks = r"C:\Users\gsa40\AppData\Roaming\RepeatSegment\decks"
files = [f for f in os.listdir(decks) if f.endswith('.apkg')]
if not files:
    print("NO APKG FILES - create a card first!")
    exit()
latest = sorted(files, key=lambda f: os.path.getmtime(os.path.join(decks, f)))[-1]
path = os.path.join(decks, latest)
print(f"Analyzing: {latest}")

z = zipfile.ZipFile(path)
db = z.read('collection.anki2')
with open('_tmp.anki2', 'wb') as f: f.write(db)
c = sqlite3.connect('_tmp.anki2').cursor()

# Models
c.execute("SELECT models FROM col")
models = json.loads(c.fetchone()[0])
for k, v in models.items():
    nm = v['name'].encode('ascii','replace').decode()
    print(f"\nModel: {nm} id={k}")
    print(f"  req={json.dumps(v.get('req',[]))}")
    for t in v['tmpls']:
        nm2 = t['name'].encode('ascii','replace').decode()
        q = t['qfmt']; a = t['afmt']
        print(f"  [{t['ord']}] {nm2}")
        print(f"    face img={'YES' if '{{image}}' in q else 'NO'}: {q[:60]}")
        print(f"    back img={'YES' if '{{image}}' in a else 'NO'}: {a[:60]}")

# Notes — check image field
c.execute("SELECT flds FROM notes")
for row in c.fetchall():
    f = row[0].split('\x1f')
    print(f"\nFields: word={f[0][:20]} | img={'<img' if '<img' in (f[3] if len(f)>3 else '') else 'EMPTY'} | sound={f[4][:40] if len(f)>4 else ''}")

os.remove('_tmp.anki2')

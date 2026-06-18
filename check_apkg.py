import sqlite3, zipfile, json, sys, os

# Find latest apkg
decks = r"C:\Users\gsa40\AppData\Roaming\RepeatSegment\decks"
files = [f for f in os.listdir(decks) if f.endswith('.apkg') and f.startswith('HP_')]
latest = sorted(files, key=lambda f: os.path.getmtime(os.path.join(decks, f)))[-1]
path = os.path.join(decks, latest)
print(f"Latest: {latest}")

# Extract collection.anki2
z = zipfile.ZipFile(path)
db = z.read('collection.anki2')
with open('_tmp.anki2', 'wb') as f: f.write(db)

conn = sqlite3.connect('_tmp.anki2')
c = conn.cursor()

# Check col -> models
c.execute("SELECT models FROM col")
models = c.fetchone()[0]
m = json.loads(models)
for k, v in m.items():
    req = v.get('req', [])
    print(f"\nModel {v['name']} (id={k}):")
    req_str = str(req)
    print(f"  req={req_str}")
    for tmpl in v['tmpls']:
        name = tmpl['name'].encode('ascii','replace').decode('ascii')
        print(f"  Card {tmpl['ord']}: {name}")

# Check cards
c.execute("SELECT id, nid, ord FROM cards ORDER BY id")
cards = c.fetchall()
print(f"\nCards: {len(cards)}")
for cid, nid, ord_ in cards:
    print(f"  id={cid} nid={nid} ord={ord_}")

# Check notes
c.execute("SELECT id, sfld FROM notes")
notes = c.fetchall()
print(f"\nNotes: {len(notes)}")
for nid, sfld in notes:
    print(f"  id={nid} sfld={sfld}")

conn.close()
os.remove('_tmp.anki2')

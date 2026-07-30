import re

f = r'C:\Abc\ATMegaPestaV1\ATMegaPestaV1\Views\TesteFuncionalidadesView.xaml'

with open(f, 'rb') as h:
    data = h.read()

dash   = '\u2014'.encode('utf-8')  # —
down   = '\u2193'.encode('utf-8')  # ↓
square = '\u25a0 '.encode('utf-8') # ■

# em dash: exact bytes found in file via hex dump
data = data.replace(b'\xc3\x83\xc2\xa2\xc3\xa2\xe2\x80\x9a\xc2\xac\xc3\xa2\xe2\x82\xac\xc2\x9d', b'\xe2\x80\x94')

# en dash (–) corrupted: c3 83 c2 a2  c3 a2 e2 80 9a c2 ac  c3 a2 e2 82 ac c5 93
data = data.replace(b'\xc3\x83\xc2\xa2\xc3\xa2\xe2\x80\x9a\xc2\xac\xc3\xa2\xe2\x82\xac\xc5\x93', b'\xe2\x80\x93')

# Física/Título: í = c3 83 c6 92 c3 82 c2 ad
data = data.replace(b'\xc3\x83\xc6\x92\xc3\x82\xc2\xad', b'\xc3\xad')  # í

# ê in Sequência: c3 83 c6 92 c3 82 c2 aa
data = data.replace(b'\xc3\x83\xc6\x92\xc3\x82\xc2\xaa', b'\xc3\xaa')  # ê

# çã double-encoded: c3 83 c6 92 c3 82 c2 a7  c3 83 c6 92 c3 82 c2 a3
data = data.replace(b'\xc3\x83\xc6\x92\xc3\x82\xc2\xa7\xc3\x83\xc6\x92\xc3\x82\xc2\xa3', b'\xc3\xa7\xc3\xa3')
data = data.replace(b'\xc3\x83\xc6\x92\xc3\x82\xc2\xa7', b'\xc3\xa7')  # ç alone
data = data.replace(b'\xc3\x83\xc6\x92\xc3\x82\xc2\xa9', b'\xc3\xa9')  # é
# I²C: 49 c3 83 e2 80 9a c3 82 c2 b2 43
data = data.replace(b'I\xc3\x83\xe2\x80\x9a\xc3\x82\xc2\xb2C', b'I\xc2\xb2C')

# ↓ Exportar log: c3 83 c2 a2  c3 a2 e2 82 ac c2 a0  c3 a2 e2 82 ac c5 93
data = data.replace(b'\xc3\x83\xc2\xa2\xc3\xa2\xe2\x82\xac\xc2\xa0\xc3\xa2\xe2\x82\xac\xc5\x93', b'\xe2\x86\x93')
# ■ Parar: c3 83 c2 a2  c3 a2 e2 82 ac e2 80 9c  c3 82 c2 a0
data = data.replace(b'\xc3\x83\xc2\xa2\xc3\xa2\xe2\x82\xac\xe2\x80\x9c\xc3\x82\xc2\xa0', b'\xe2\x96\xa0\xc2\xa0')


with open(f, 'wb') as h:
    h.write(data)

# Report remaining
content = data.decode('utf-8', errors='replace')
bad = [l for l in enumerate(content.split('\n'),1) if '\xc3\x83' in l[1] or 'Ã' in l[1]]
for i,l in bad:
    print(f"{i}: {l.strip()[:120]}")
print(f"Done. {len(bad)} lines still bad.")

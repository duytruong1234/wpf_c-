import glob
import os
import codecs

files = glob.glob(r'd:\QuanLyKhoNguyenLieuPizza\QuanLyKhoNguyenLieuPizza\**\*.xaml', recursive=True) + glob.glob(r'd:\QuanLyKhoNguyenLieuPizza\QuanLyKhoNguyenLieuPizza\**\*.cs', recursive=True)
count = 0
for f in files:
    if '\\obj\\' in f or '\\bin\\' in f:
        continue
    with open(f, 'rb') as fp:
        content = fp.read()
    if not content.startswith(codecs.BOM_UTF8):
        with open(f, 'wb') as fp:
            fp.write(codecs.BOM_UTF8 + content)
        count += 1
print(f'Added BOM to {count} files.')

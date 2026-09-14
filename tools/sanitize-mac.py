#!/usr/bin/env python3
"""Remove developer build paths from Sunward's release metadata before signing."""
import getpass
import struct
import subprocess
import sys
from pathlib import Path


def clean_codeview(path):
    data = bytearray(path.read_bytes())
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    if data[pe:pe + 4] != b'PE\0\0':
        raise ValueError('Expected managed PE assembly')
    count = struct.unpack_from('<H', data, pe + 6)[0]
    optional_size = struct.unpack_from('<H', data, pe + 20)[0]
    optional = pe + 24
    magic = struct.unpack_from('<H', data, optional)[0]
    directory = optional + {0x10b: 96, 0x20b: 112}[magic]
    debug_rva, debug_size = struct.unpack_from('<II', data, directory + 6 * 8)
    if not debug_rva:
        return 0
    sections = optional + optional_size

    def offset(rva):
        for n in range(count):
            size, va, raw_size, raw = struct.unpack_from('<IIII', data, sections + n * 40 + 8)
            if va <= rva < va + max(size, raw_size):
                return raw + rva - va
        raise ValueError('Unmapped PE debug directory')

    debug = offset(debug_rva)
    changed = 0
    for entry in range(debug, debug + debug_size, 28):
        kind, size, _, raw = struct.unpack_from('<IIII', data, entry + 12)
        if kind != 2:
            continue
        header = {b'RSDS': 24, b'NB10': 16}.get(bytes(data[raw:raw + 4]))
        if header is None:
            raise ValueError('Unsupported CodeView record')
        start = raw + header
        end = data.index(0, start, raw + size)
        old = bytes(data[start:end])
        name = old.replace(b'\\', b'/').rsplit(b'/', 1)[-1]
        if name != old:
            data[start:end] = name + bytes(len(old) - len(name))
            changed += 1
    if changed:
        path.write_bytes(data)
    return changed


app = Path(sys.argv[1]).resolve()
assembly = app / 'Contents/Resources/Data/Managed/Assembly-CSharp.dll'
changed = clean_codeview(assembly)
burst = app / 'Contents/PlugIns/lib_burst_generated.bundle'
if burst.exists():
    subprocess.run(['xcrun', 'install_name_tool', '-id', '@rpath/' + burst.name, str(burst)], check=True)

needles = [str(Path.home())]
if len(getpass.getuser()) >= 4:
    needles.append(getpass.getuser())
for file in app.rglob('*'):
    if not file.is_file() or file.is_symlink():
        continue
    data = file.read_bytes()
    if any(s.encode(encoding) in data for s in needles for encoding in ['utf-8', 'utf-16-le']):
        raise RuntimeError('Local account metadata remains in ' + str(file.relative_to(app)))

subprocess.run(['codesign', '--force', '--deep', '--sign', '-', str(app)], check=True)
subprocess.run(['codesign', '--verify', '--deep', '--strict', str(app)], check=True)
print(f'Release metadata sanitized; {changed} CodeView path(s) removed; local account scan passed.')

"""
DI Migration: Update all ViewModels to use constructor injection
"""
import re
import os

VM_DIR = r"d:\QuanLyKhoNguyenLieuPizza\QuanLyKhoNguyenLieuPizza\ViewModels"

# Map: (filename, field_name, field_type, old_init_pattern)
# We need to:
# 1. Change constructors to accept DatabaseService parameter
# 2. Remove ServiceLocator/new DatabaseService() calls
# 3. Add parameterless ctor that calls DI

files_to_process = []

for fname in os.listdir(VM_DIR):
    if not fname.endswith(".cs"):
        continue
    filepath = os.path.join(VM_DIR, fname)
    with open(filepath, 'r', encoding='utf-8-sig', errors='replace') as f:
        content = f.read()
    
    has_new_db = 'new DatabaseService()' in content
    has_service_locator = 'ServiceLocator' in content
    
    if not has_new_db and not has_service_locator:
        continue
    
    files_to_process.append((fname, filepath, content, has_new_db, has_service_locator))

print(f"Files to process: {len(files_to_process)}")

for fname, filepath, content, has_new_db, has_service_locator in files_to_process:
    original = content
    
    # Step 1: Remove ServiceLocator using
    content = content.replace('using QuanLyKhoNguyenLieuPizza.Services;\n', 'using QuanLyKhoNguyenLieuPizza.Services;\n')
    
    # Step 2: Replace ServiceLocator.Instance.GetService<IDatabaseService>() with App.Services resolution 
    content = content.replace(
        'ServiceLocator.Instance.GetService<IDatabaseService>()',
        'App.Services.GetRequiredService<DatabaseService>()'
    )
    
    # Step 3: Replace new DatabaseService() with App.Services resolution
    content = content.replace(
        'new DatabaseService()',
        'App.Services.GetRequiredService<DatabaseService>()'
    )
    
    # Step 4: Add Microsoft.Extensions.DependencyInjection using if not present
    if 'Microsoft.Extensions.DependencyInjection' not in content and 'GetRequiredService' in content:
        # Add using after the last using statement
        lines = content.split('\n')
        last_using_idx = 0
        for i, line in enumerate(lines):
            if line.strip().startswith('using '):
                last_using_idx = i
        lines.insert(last_using_idx + 1, 'using Microsoft.Extensions.DependencyInjection;')
        content = '\n'.join(lines)
    
    # Step 5: If field type was IDatabaseService but we now use DatabaseService, we need to keep IDatabaseService
    # Actually, let's keep using IDatabaseService where it was already used, and cast
    # But simpler: just change all to use DatabaseService for consistency since facade has all methods
    if 'IDatabaseService _databaseService' in content or 'IDatabaseService _db' in content:
        content = content.replace('IDatabaseService _databaseService', 'DatabaseService _databaseService')
        content = content.replace('IDatabaseService _db', 'DatabaseService _db')
        # Remove IDatabaseService using if DatabaseService is already imported
        content = content.replace(
            'using QuanLyKhoNguyenLieuPizza.Core.Interfaces;\n',
            ''
        )
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8-sig', errors='replace') as f:
            f.write(content)
        print(f"  Updated: {fname}")
    else:
        print(f"  No change: {fname}")

# Step 6: Also remove comments about ServiceLocator in PhieuNhapViewModel
phieuNhapPath = os.path.join(VM_DIR, "PhieuNhapViewModel.cs")
if os.path.exists(phieuNhapPath):
    with open(phieuNhapPath, 'r', encoding='utf-8-sig', errors='replace') as f:
        content = f.read()
    # Remove old ServiceLocator comments
    content = re.sub(r'// \? S.+ d.ng interface.*\n', '', content)
    content = re.sub(r'// \? D.ng ServiceLocator.*\n', '', content)
    with open(phieuNhapPath, 'w', encoding='utf-8-sig', errors='replace') as f:
        f.write(content)
    print("  Cleaned comments in PhieuNhapViewModel.cs")

print("\nDone!")


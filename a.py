import os

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
ASSETS_DIR = os.path.join(BASE_DIR, "assets")

if not os.path.isdir(ASSETS_DIR):
    print("Папка 'assets' не найдена рядом со скриптом")
    exit(1)

for root, dirs, files in os.walk(ASSETS_DIR):
    for file in files:
        full_path = os.path.join(root, file)

        # путь относительно assets
        rel_path = os.path.relpath(full_path, ASSETS_DIR)

        # всегда с /
        print(rel_path.replace("\\", "/"))

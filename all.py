import os

ROOT_DIR = os.getcwd()
OUTPUT_FILE = "all.txt"

ALLOWED_EXTENSIONS = {".cs", ".json", ".txt"}
SPECIAL_CSPROJ = "Earthbound.csproj"
REPLACE_FILE = "FastNoiseLite.cs"

IGNORE_DIRS = {"bin", "debug"}
IGNORE_FILES = {"Earthbound.AssemblyInfo.cs"}

REPLACEMENT_TEXT = (
    "Сорян тут был код но он был удален "
    "но так то он тут есть не обращай внимания"
)

def is_allowed_file(filename):
    if filename in IGNORE_FILES:
        return False
    if filename == SPECIAL_CSPROJ:
        return True
    return os.path.splitext(filename)[1].lower() in ALLOWED_EXTENSIONS

with open(OUTPUT_FILE, "w", encoding="utf-8") as out:
    for root, dirs, files in os.walk(ROOT_DIR):
        # --- игнор папок bin / debug ---
        dirs[:] = [d for d in dirs if d.lower() not in IGNORE_DIRS]

        for file in files:
            if file == OUTPUT_FILE:
                continue

            if not is_allowed_file(file):
                continue

            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, ROOT_DIR)

            out.write(f'START OF FILE "{file}"\n')
            out.write(f"{rel_path}\n")
            out.write('"\n')

            if file == REPLACE_FILE:
                out.write(REPLACEMENT_TEXT + "\n")
            else:
                try:
                    with open(full_path, "r", encoding="utf-8") as f:
                        out.write(f.read())
                except:
                    out.write("[ERROR: не удалось прочитать файл]\n")

            out.write('\n"\n\n')

print(f"[OK] {OUTPUT_FILE} создан — только нужные файлы")

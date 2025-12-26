import os
import subprocess

ROOT_DIR = os.path.dirname(os.path.abspath(__file__))

def convert_and_delete(flac_path):
    ogg_path = os.path.splitext(flac_path)[0] + ".ogg"

    if os.path.exists(ogg_path):
        print(f"[SKIP] Уже есть ogg: {ogg_path}")
        return

    cmd = [
        "ffmpeg",
        "-y",
        "-i", flac_path,
        "-c:a", "libvorbis",
        "-q:a", "5",
        ogg_path
    ]

    try:
        subprocess.run(
            cmd,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=True
        )
        os.remove(flac_path)
        print(f"[OK] {flac_path} -> {ogg_path} (flac удалён)")
    except subprocess.CalledProcessError:
        print(f"[ERR] Ошибка конвертации: {flac_path}")

def main():
    count = 0
    for root, _, files in os.walk(ROOT_DIR):
        for file in files:
            if file.lower().endswith(".flac"):
                flac_path = os.path.join(root, file)
                convert_and_delete(flac_path)
                count += 1

    print(f"\nГотово. Обработано файлов: {count}")

if __name__ == "__main__":
    main()

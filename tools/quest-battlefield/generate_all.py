"""クエストの戦場背景をまとめて生成する。

使い方：
  python3 tools/quest-battlefield/generate_all.py
  → client/public/image/quest/<stage_code>-battlefield.png を上書きする

ステージごとのスクリプト（stage_XX_*.py）を 1 つだけ実行してもよい。
どの画像をどのステージで使うかは、マスタデータ（stages シート）の battlefield_image_path で決まる。
"""
import runpy
from pathlib import Path

here = Path(__file__).resolve().parent
for script in sorted(here.glob('stage_*.py')):
    print(f'generate: {script.name}')
    runpy.run_path(str(script), run_name='__main__')

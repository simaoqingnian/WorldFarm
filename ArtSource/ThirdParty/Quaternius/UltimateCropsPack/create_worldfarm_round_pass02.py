import importlib.util
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().with_name("create_worldfarm_round_pass01.py")

spec = importlib.util.spec_from_file_location("worldfarm_round_pass_core", SCRIPT_PATH)
core = importlib.util.module_from_spec(spec)
spec.loader.exec_module(core)

core.PASS_SLUG = "round_pass_02"
core.PASS_FILE = "round_pass02"
core.PASS_LABEL = "RoundPass02"
core.OUTPUT_ROOT = core.ROOT / f"worldfarm_{core.PASS_SLUG}"
core.BLEND_DIR = core.OUTPUT_ROOT / "blends"
core.FBX_DIR = core.OUTPUT_ROOT / "fbx"
core.SCREENSHOT_DIR = core.OUTPUT_ROOT / "screenshots"
core.PREVIEW_PATH = core.SCREENSHOT_DIR / f"worldfarm_{core.PASS_FILE}_preview.png"

core.main()

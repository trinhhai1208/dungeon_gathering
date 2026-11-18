# Art Pipeline: Bộ hình "U tối · 48px/unit"
**Mã tài liệu:** `ART-01`
**Dự án:** Dungeon Gathering
**Trạng thái:** Bản "u tối v4" chỉ còn là **thiết kế tham khảo** (trong `Design/` và trên canvas). Ngày 2026-11-13 đã **gỡ toàn bộ khỏi Unity** để làm bản mới: Unity đang dùng lại sprite/animator gốc của pack (FantasyKnights, DungeonMonsters…).
**Cập nhật:** 2026-11-13

> **Bản v8 (2026-11-13, đang chờ duyệt): phong cách Soul Knight** trên khung v7: màu **bệt 3 sắc độ** tương phản cao cho mọi vùng kể cả neon (ramp 5/4 tông được rút về `sáng · gốc · tối`), không gradient, **không dither** (bỏ vệt lướt caro khi attack, `USE_SMEAR = False`), viền đen kín, mặt tối giản. Animation **2–4 khung hình chính mỗi trạng thái**, mỗi khung giữ 2 ô trong sheet 48 ô (`KEY` / `key_index()`), nhún nảy kiểu arcade. Ghi chú: luật cũ "neon 5 sắc độ" của v5 không còn áp dụng.
>
> **Bản v7**, theo yêu cầu "thiết kế theo kiểu 01Knight": mọi class dùng **khung FantasyKnight** đo từ `01Knight.png`: đầu/mũ vòm ~16×15px chiếm hơn nửa người, 2 khối tròn bên hông (giáp vai thép/vàng ở knight & paladin, ống tay vải theo màu class ở các class khác), ngực nhỏ có tấm nhấn màu, 2 chân xám + bàn chân màu, viền đen kín, mỗi khối 5 tông. Run nhảy lò cò 2–3px như bản gốc. Code: `v7_*()` trong `generate_core_entities.py` (v6 vẽ tay `char_maps.py` giữ làm tham khảo).
>
> **Bản v6**, sửa theo góp ý trên v5: nhân vật **vẽ tay từng pixel** (`Design/tools/char_maps.py`: đầu, thân, giáp vai dạng ma trận ký tự) thay cho dựng khối hình học (bị "vuông vức"); silhouette bất đối xứng, sừng là nét thuôn cong nằm sau mũ trùm; **viền ngoài cùng đen kín** (Void) như ảnh mẫu, sel-out chỉ còn ở chi tiết bên trong; **giáp vai chỉ còn ở knight và paladin**, các class khác bỏ. Quái giữ thiết kế v5 + viền đen kín.
>
> **Bản v5** thay bộ nhân vật/quái "u tối v4":
> - Chibi **No-Hands** kiểu Soul Knight, cao 30–34px, đầu ~55–60% (to, phá khối bằng tóc dựng / sừng / mũ chóp), thân nhỏ, chân 3px cách nhau 3px.
> - **2 lớp cầu vai tách riêng** `pB` / `pF` cử động độc lập (idle trễ nhịp, run ngược pha chân, attack đẩy vai trước).
> - Thân tối **ngả xanh** để tách khỏi sàn tím `#3a2c3f` + **1 mảng sáng chủ đạo** (tóc bạc, mũ trắng-vàng, mặt nạ trắng, khăn trùm ngà) + **1 neon 5 sắc độ** (`crimson5`, `holy5`, `void5`, `ecto5`, `jade5`; ngoại lệ có chủ đích của luật 3 tông, chỉ dùng cho phần phát sáng).
> - Run nghiêng người về trước (shear), attack có smear dither màu neon, hurt chớp trắng + giật ngửa, **death sụp tự nhiên** (khuỵu chân, thân xẹp, đầu đổ xuống sàn, cầu vai rơi, linh hồn bay lên), **không xoay 90°**.
> - Vũ khí: **tái sử dụng** `Sword/File.png`, `Weapons 1-3-Sheet`; chỉ bổ sung `bow_shadow`, `scythe_ecto` (`generate_weapons_extra.py`).
> - VFX: chỉ sinh Basic VFX còn thiếu (lửa nòng nhỏ, chém đơn, chém quái, tia trúng nhỏ 7 hệ); PVFX giữ cho skill/crit/nổ/boss. Bảng mapping: **`VFX_WEAPON_MAPPING.md`**.
> - `generate_items.py` (bộ vũ khí tự vẽ cũ) **không dùng** nữa cho vũ khí.

---

## 1. Quy chuẩn bắt buộc

### 1.1. Mật độ pixel và kích thước

| Hạng mục | Khung | PPU | Ghi chú |
|---|---|---|---|
| Nhân vật / quái dạng người | ô 48×48, hình cao **31px** | 48 | Đồng bộ với `Characters/FantasyKnights` (thân ~25×30). Mọi nhân vật cao bằng nhau, mũ nằm trong 31px |
| Sheet animation | 384×288 = 8 cột × 6 hàng | 48 | Bố cục giống FantasyKnights, xem §3 |
| Tile sàn / tường | 48×48 | 48 | 1 ô = 1 unit |
| Công trình lớn | 48×96, 96×96 | 48 | Cửa, cổng dịch chuyển, tượng, cờ |
| Vũ khí | 32×32, vẽ **chéo** (mũi trên-trái, chuôi dưới-phải) | 48 | Giống hướng của `Items/Weapon/Sword/File.png` để `MeleeWeapon` xoay như cũ |
| Vật phẩm rơi / icon trang bị | 24×24 | 48 | |
| VFX | 16–96px, strip ngang nhiều frame | 48 | Hướng mặc định **+X**, code xoay theo góc |
| UI | Vẽ ở **640×360**, phóng ×2 lên 1280×720 | n/a | 1 pixel UI = 1 pixel thế giới |

Camera đích: **Pixel Perfect Camera, Assets PPU 48, Reference 640×360** (13,3 × 7,5 ô; ×2 ở 720p, ×3 ở 1080p).

### 1.2. Màu
- 12 màu neo trong `StyleGuide`: Void `#1b1420`, Panel `#2a1f2e`, Floor `#3a2c3f`, Line `#4d3a52`, Brick `#8f6a5c`, Text `#efe6dc`, Gold `#f2b33d`, HP `#e0464b`, Armor `#a9b5d0`, Mana `#3fa9f5`, UI Slate `#5a6688`, Danger `#ff5a4f`.
- Ramp phụ (vải, kim loại, da, lửa…) được phép thêm, **tối đa 3 tông** (Highlight, Base, Shadow). Shadow ngả về tím lạnh. Toàn bộ ramp nằm trong `RAMPS` của `Design/tools/pixkit.py` (và `generate_core_entities.py` cho nhân vật).
- Nguồn sáng (mắt, rune, lửa) dùng 4 ramp: `ember` đỏ, `soul` tím, `ecto` xanh ma trơi, `holy` vàng.

### 1.3. Luật vẽ
- Sáng từ **Trên-Trái**. Không anti-alias, không mixel, **không dùng `#000000`**.
- **Selective outline**: cạnh trên/trái dùng tông Shadow của chính mảng đó; cạnh dưới/phải dùng Void `#1b1420` để tách khỏi sàn.
- **Không vẽ bóng đổ vào sprite**: prefab đã có con `Shadow` (Sorting Layer `Shadows`).
- Hiệu ứng mờ dần bằng **dither** (bỏ pixel theo mẫu Bayer 2×2), không dùng alpha gradient. Ngoại lệ: hào quang 1px với một mức alpha duy nhất (đầu lâu bay, cầu mana).

### 1.4. Concept
- Tông **u tối, ma mị**: thép đen, vàng xỉn, đỏ máu khô, mắt phát sáng, áo choàng rách.
- **Nhân vật không có tay**: hai bên thân là cầu vai hoặc áo choàng vai dính liền. Tay để trống vì vũ khí do `WeaponData` gắn riêng.
- **Quái có tay** và tay cử động theo animation.
- Chibi nhìn 3/4 hướng phải; quay trái bằng `flipX`.

---

## 2. Script sinh hình (`Design/tools/`)

Tất cả dùng Python 3 + Pillow, chạy từ thư mục gốc repo. Mỗi script vừa xuất PNG vừa xuất sheet duyệt (file bắt đầu bằng `_`).

| Script | Lệnh | Đầu ra |
|---|---|---|
| `pixkit.py` | (thư viện) | Bộ vẽ dùng chung: `Layer`, `rbox`/`ell`/`poly`/`stroke`/`along`, sel-out tự động, `RAMPS`, `contact()` |
| `generate_core_entities.py` | `python Design/tools/generate_core_entities.py Design/sprites/core Design/previews/core_entities.png` | 11 sheet 48 frame (`<tên>.png`) + GIF xem trước (`_gif/`) |
| `generate_world_kit.py` | `python Design/tools/generate_world_kit.py Design/sprites/world` | `tiles/` 27 ô, `props/` 16 đồ vật (nhiều frame = strip ngang) |
| `generate_items.py` | `python Design/tools/generate_items.py Design/sprites/items` | `weapons/` 10, `loot/` 9, `gear/` 6 |
| `generate_vfx.py` | `python Design/tools/generate_vfx.py Design/sprites/vfx` | 10 strip VFX + `_gif/` |
| `generate_ui_kit.py` | `python Design/tools/generate_ui_kit.py Design/sprites/ui` | 40 thành phần UI; vùng 9-slice ghi trong `SLICE` |
| `generate_mockups.py` | `python Design/tools/generate_mockups.py Design/previews/mock` | `mock_room`, `mock_hud`, `mock_inventory` (1280×720) |

Muốn sửa một hình: sửa hàm vẽ tương ứng trong script rồi chạy lại. **Không sửa PNG bằng tay**, vì lần sinh sau sẽ ghi đè.

---

## 3. Nhân vật & quái

### 3.1. Danh sách
| Sheet thiết kế | File trong Unity | Dùng cho |
|---|---|---|
| `knight` | `Characters/Dark/Dark_Knight.png` | `Char_knight` |
| `paladin` | `Characters/Dark/Dark_Paladin.png` | `Char_paladin` |
| `archer` | `Characters/Dark/Dark_Archer.png` | `Char_ranger` |
| `mage` | `Characters/Dark/Dark_Mage.png` | `Char_wizard` |
| `necromancer` | `Characters/Dark/Dark_Necromancer.png` | `Char_necromancer` |
| `priest` | `Characters/Dark/Dark_Priest.png` | *(chưa có nhân vật)* |
| `Enemy_zombie` | `Enemies/Dark/Dark_Zombie.png` | `Enemy_Zombie` |
| `Enemy_skeleton` | `Enemies/Dark/Dark_Skeleton.png` | `Enemy_Skeleton_Warrior`, `Enemy_Skeleton_Spear` |
| `Enemy_vampire` | `Enemies/Dark/Dark_Vampire.png` | `Enemy_Vampire` |
| `Enemy_skull` | `Enemies/Dark/Dark_Skull.png` | `Enemy_Skull` |
| `Enemy_slime` | `Enemies/Dark/Dark_Slime.png` | *(chưa có quái)* |

Boss (`Boss_EliteWarrior`, `Boss_EliteVampire`, `Boss_VampireLord`) vẫn dùng hình cũ phóng to.

### 3.2. Bố cục frame (giống FantasyKnights)
| Index | Animation | Clip tạo trong Unity |
|---|---|---|
| 0–7 | idle | `<sheet>_idle` 8 fps, lặp |
| 8–15 | walk | `<sheet>_run` của **quái** 10 fps |
| 16–23 | run | `<sheet>_run` của **nhân vật** 12 fps |
| 24–31 | skill (tụ lực + hào quang) | `<sheet>_skill` 10 fps |
| 32–39 | attack | `<sheet>_attack` dùng 32–37, 14 fps |
| 40–41 | hurt (40 chớp trắng) | `<sheet>_hurt` 10 fps |
| 42–47 | death (ngã ngửa, linh hồn bay lên) | `<sheet>_death` 8 fps |

`SpriteAnimDriver` tự nhận state theo từ khóa trong tên clip.

### 3.3. Đưa vào Unity *(đã gỡ ngày 2026-11-13, giữ lại làm hướng dẫn cho bản mới)*
Đã xóa: `Characters/Dark/`, `Enemies/Dark/`, `Art/Animations/Characters/dark/`, `Prefabs/Enemies/Animations/Dark/`, `Editor/Data/DarkSpriteSetup.cs`. `CharacterData`, prefab quái, `Lobby.unity`, `DataSeeder.cs`, `EntityPrefabBuilder.Enemies.cs` đã khôi phục về commit `649b7de`. Khi làm bản mới, viết lại công cụ theo các bước dưới.

1. Chạy `generate_core_entities.py`, rồi copy sheet sang `Assets/Project/Art/Sprites/Characters/Dark/` hoặc `.../Enemies/Dark/` theo tên ở §3.1.
2. Menu **Tools ▸ Dungeon Gathering ▸ Art ▸ Setup Dark Sprites** (`Assets/Project/Editor/Data/DarkSpriteSetup.cs`):
   - đặt import: PPU 48, Point, không nén, Multiple, cắt lưới 48;
   - pivot: nhân vật `(0.5, 0.5)` như FantasyKnights; quái `(0.5, 0.4)` để chân trùng con `Shadow` (y −0.35);
   - tạo clip + controller trong `Art/Animations/Characters/dark/` và `Prefabs/Enemies/Animations/Dark/`;
   - gán `portrait` + `animator` vào `CharacterData`, gán controller + sprite vào prefab quái.
3. Chạy **Tools ▸ Dungeon Gathering ▸ Lobby ▸ Build Character Spots** để dựng lại sảnh.

`DataSeeder.Run` và `EntityPrefabBuilder.BuildEnemyBaseAndVariants` đều gọi lại `DarkSpriteSetup`, nên seed lại hoặc dựng lại prefab không bị quay về hình cũ.

**Lưu ý:** quái giờ cao ~0,63 unit (trước đây zombie ~0,94). Collider/hurtbox vẫn giữ bán kính cũ 0,4–0,45 nên hơi to so với hình.

---

## 4. Map, công trình, item, VFX, UI (đã thiết kế, chưa import)

| Nhóm | Thư mục | Nội dung |
|---|---|---|
| Tileset | `Design/sprites/world/tiles/` | Sàn 6 kiểu (`floor_plain/cracked/moss/blood/bones/grate`), mặt tường 5 kiểu + `wall_end_L/R`, đỉnh tường 9-slice `cap_TL…cap_BR` + `cap_inner_*`, `stairs_down` |
| Công trình | `Design/sprites/world/props/` | `torch`, `brazier`, `candles` (3 frame), `chest`, `chest_cursed` (đóng/mở), `barrel`, `crate`, `urn` (lành/vỡ), `bones`, `skull_pile`, `altar`, `spike_trap` (4 frame), `gargoyle`, `banner`, `door` (3 frame), `portal` (4 frame) |
| Vũ khí | `Design/sprites/items/weapons/` | `sword`, `greatsword`, `axe`, `spear`, `mace`, `dagger`, `scythe`, `staff`, `bow`, `wand` |
| Vật phẩm | `Design/sprites/items/loot/`, `gear/` | `coin` (4 frame), `soul_gem`, `potion_hp`, `potion_mana`, `armor_shard`, `key`, `mana_orb`, `scroll`, `bomb`; `helm`, `chestplate`, `ring`, `amulet`, `boots`, `cloak` |
| VFX | `Design/sprites/vfx/` | `slash`, `hit`, `bolt_soul`, `bolt_blood`, `arrow`, `explosion`, `soul_burst`, `holy_nova`, `summon`, `heal` |
| UI | `Design/sprites/ui/` | `panel_9slice` (viền 12), `subpanel_9slice`, `btn_stone_*`, `btn_blood_*`, `btn_close`, `btn_icon_*`, `bar_hp/armor/mana/gold`, `slot_empty` + `slot_F/C/B/A/S`, `tooltip_9slice`, `tab_*`, `ribbon`, `joy_base/knob`, `btn_round_*`, `portrait_frame`, `minimap_frame`, `ico_*` |

### 4.1. Việc cần làm khi import (chưa làm)
1. **Camera**: đổi Pixel Perfect Camera sang PPU 48, khung 640×360. Nếu không đổi, phòng hiện to gấp 3.
2. **Tilemap**: dựng RuleTile mới từ `tiles/` (đỉnh tường 9-slice + góc trong, mặt tường ngẫu nhiên có trọng số) và cập nhật room template.
3. **UI**: `UI_SYSTEM.md` đang dùng tham chiếu 1280×720 với artScale 4; kit mới vẽ cho tham chiếu 640×360 phóng ×2. Cần thống nhất trước khi thay skin.
4. **Font**: `Pixelta.ttf` không có dấu tiếng Việt (nhãn "CHƯA RA MẮT" trong sảnh bị lỗi). `dearpix-2-01.otf` có đủ dấu ở cỡ 12/16.
5. **Vũ khí**: kiểm tra lại `holdOffset`/góc trong `WeaponData` (xem `SYSTEM_02.md`) khi đổi sang sprite 32×32 PPU 48.

---

## 5. Canvas thiết kế (claude.ai)

Canvas **"Dungeon Gathering — UI, Maps & Concept"** là nơi duyệt mọi thiết kế; bản sao nằm trong repo:

| Đường dẫn | Nội dung |
|---|---|
| `Design/canvas/project/canvas.json` | Chỉ mục: các trang, vị trí từng bảng, tiêu đề, ghi chú |
| `Design/canvas/project/*.dc.html` | 21 bảng (UI-*, Map-*, Main, StyleGuide, **Sprites-Core**, **World-Mockups**, **World-Kit**) |
| `Design/canvas/assets/<blob id>.png/.gif` | Ảnh của các bảng Sprites/World (copy từ `Design/sprites`, `Design/previews`) |
| `Design/canvas/blob-map.json` | `local`: blob id → file trong repo · `remote_only`: id chỉ có trên canvas (ảnh của các bảng cũ) |

Các trang trên canvas:
- **Sprites**: GIF animation 6 nhân vật + 5 quái, sheet mẫu, bố cục frame.
- **Map · UI · Item · VFX**: 3 mockup 1280×720 + toàn bộ kit.
- **UI**, **Concept**, **Maps & Level**: thiết kế cũ (vẫn theo bộ hình 16px cũ).

Ảnh trong HTML trỏ tới `/_blob/<id>` (chỉ hiển thị trên canvas). Muốn xem offline, tra id trong `blob-map.json`.

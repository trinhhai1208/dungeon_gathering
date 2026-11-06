# Tài liệu Dungeon Gathering — đọc gì, theo thứ tự nào

Game roguelite pixel 2D (Unity 6.3, URP 2D), phát hành **WebGL trên itch.io**. Tài liệu viết tiếng Việt; giữ phong cách này khi cập nhật.

| File | Nội dung | Đọc khi |
|---|---|---|
| `ARCHITECTURE.md` | Tổng quan hệ thống, scene, dungeon & pathfinding, bẫy, save, EventBus, cây thư mục cũ, lộ trình vertical slice | Luôn đọc đầu tiên |
| `MODULES_ADDRESSABLES_WEBGL.md` | Module hóa, Addressables, hợp đồng service Core (`IAssetService`, `ISceneService`, `IUIService`…), quy tắc async **R1–R14**, thiết lập WebGL/itch.io, lộ trình **M0–M9**, nhật ký bàn giao | Trước khi tạo scene/prefab/asmdef, hoặc làm build |
| `UI_SYSTEM.md` | Scene ↔ UI, `UIRoot`, co giãn **1280×720 + pixel snap**, **bảng neo từng màn**, luồng prefab UI, `ItemTile`, đa ngôn ngữ & font, checklist dựng màn | Trước khi dựng bất kỳ màn/prefab UI nào |
| `ENTITIES_PREFABS.md` | Vật phẩm, người chơi, quái vật/boss: dữ liệu SO, cây prefab, layer va chạm, luồng sinh → chết → pool | Trước khi dựng prefab gameplay |
| `CODE_STRUCTURE.md` | asmdef + thư mục script thực tế, layer/ma trận va chạm, component bắt buộc theo từng prefab, cách chạy thử trước khi có Addressables | Trước khi gắn script vào prefab/scene |
| `DATA.md` | Dữ liệu ScriptableObject sinh bằng `DataSeeder` (Seed/Validate), roster 12 nhân vật + cơ chế "Chưa ra mắt", quy ước số liệu, việc còn thiếu | Trước khi thêm/sửa nhân vật, vũ khí, trang bị, quái, tầng |
| `SYSTEM_01.md` | **Hệ thống Inventory & Sảnh**: `CharacterInventory`, `PlayerInventory`, luật để trống ô vũ khí 0 (bắt buộc có vũ khí mới vào trận), đồng bộ 1 nguồn `runBag`, giữ đồ khi thua / xóa đồ khi thoát | Khi chỉnh sửa hoặc refactor túi đồ, trang bị, kho sảnh |
| `SYSTEM_02.md` | **Hệ thống Vũ khí & Tấn công**: Cấu hình góc xoay & `holdOffset` theo từng `WeaponData` (bảo toàn 17 asset), hỗ trợ song song Prefab View & Auto-Sprite View | Khi chỉnh sửa hoặc refactor vũ khí, đạn, hitbox, tấn công |
| `SYSTEM_03.md` | **Hệ thống Kẻ địch & AI Boss**: Tách `BossEncounterDirector` khỏi `LevelDirector`, bổ sung AI Kiting (`retreatDistance` + `MoveAwayFrom`) trong `EnemyBrain` | Khi chỉnh sửa hoặc refactor quái vật, AI hành vi, Boss multi-phase |
| `SYSTEM_04.md` | **Cốt lõi, Lưu trữ, Tiền tệ & Tái cấu trúc File**: 1 Coin Vàng, `OnApplicationQuit`, luồng `Settle` nhiều tầng vs `Abandon`, tách `MinimapView` & File lớn | Khi chỉnh sửa hoặc refactor hệ thống save, tiền tệ, lifecycle, tách file lớn |

**Nguồn thiết kế** (ngoài `Assets`, Unity không import): `Design/canvas/project/UI-*.dc.html` (mỗi file = 1 màn 1280×720, cũng là canvas trên claude.ai), `Design/localization/strings_src.py` (chuỗi key/en/vi), `Design/tools/` (script cắt sprite, dựng nền, i18n), `Design/cuts/blob-map.json`.

**Quy ước cập nhật tài liệu**
- Sửa đúng file chuyên đề; `ARCHITECTURE.md` chỉ giữ tổng quan + mục trỏ.
- Số mục cũ của `ARCHITECTURE` (vd §5.5, §6) vẫn tra được: mục đã chuyển giữ tiêu đề và trỏ sang file mới; phần nguyên văn trong file mới ghi *(nguyên văn từ ARCHITECTURE §x)*.
- Xong một bước trong lộ trình M0–M9: đánh dấu ở `MODULES_ADDRESSABLES_WEBGL.md` §11 và ghi một dòng ở §13.

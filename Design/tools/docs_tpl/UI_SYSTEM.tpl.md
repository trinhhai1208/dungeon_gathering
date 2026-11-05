# Hệ thống UI — Scene, Canvas, co giãn, neo & luồng prefab

> Tài liệu bàn giao cho agent/session làm UI. Đọc trước khi dựng bất kỳ prefab UI nào.
> Cập nhật: 2026-11-04 · Unity 6.3 · uGUI + TextMeshPro · Input System · Localization 1.5 · Đích: WebGL trên itch.io.
> Liên quan: `docs/ARCHITECTURE.md` (tổng quan), `docs/MODULES_ADDRESSABLES_WEBGL.md` (module, Addressables, `IUIService`/`ISceneService`, quy tắc async R1–R14), `docs/ENTITIES_PREFABS.md` (prefab vật phẩm/người chơi/quái). Bản thiết kế: `Design/canvas/project/UI-*.dc.html` (mỗi file = 1 màn, 1280×720).

---

## 0. TL;DR cho agent

- **Khung tham chiếu 1280×720**, `CanvasScaler` = **Scale With Screen Size + Expand**, kèm **làm tròn tỉ lệ để pixel luôn nguyên** (`PixelSnapCanvasScaler`, §3.2). Không đổi sang 1920×1080 (kết quả như nhau, xem §3.1). Không dùng Constant Pixel Size.
- **1 đơn vị UI = 1 px trên bản thiết kế.** Sprite pixel vẽ ở **×4** (1 pixel gốc = 4 đơn vị). Số đo trên file `.dc.html` chép thẳng vào RectTransform.
- **Chỉ có 1 Canvas overlay toàn game** (`UIRoot`, nằm trong Bootstrap, sống suốt game) với các layer cố định (§2). Scene module **không tự tạo Canvas overlay**; màn hình là prefab được `IUIService.Open` gắn vào layer.
- **Neo theo bảng §4.** Quy tắc chung: cụm điều khiển neo theo góc màn hình; khối nội dung (3 cột, panel giữa) neo giữa với kích thước cố định; nền phủ kín. Màn rộng/hẹp → các cụm góc dạt theo mép, khối giữa giữ nguyên.
- Mọi chữ qua **key i18n** (`LocalizedTMPText`), font pixel theo ngôn ngữ (§7). Mọi ô vật phẩm là **`ItemTile`** (§6.1).
- Màn hiển thị trạng thái game **đọc trạng thái hiện tại trong `OnOpen`** rồi mới nghe `EventBus` (R7). Nút gây chuyển scene/kinh tế **tự khóa khi bấm** (R1, R11).
- Làm một màn mới: theo checklist §9.

---

## 1. Scene ↔ UI

| Scene | Camera | UI của scene | Ghi chú |
|---|---|---|---|
| **Bootstrap** (Build Settings, sống suốt game) | `UICamera`? **Không** — `UIRoot` là Canvas *Screen Space – Overlay*, không cần camera. Có 1 `Main Camera` tạm để clear màu `#1b1420` khi chưa có scene nào. | `UIRoot` (§2) gồm `Backdrop`, `LoadingOverlay`, mọi layer | `EventSystem` + `InputSystemUIInputModule` cũng ở đây (duy nhất). |
| **Menu** (Addressables `scene/menu`) | Không cần camera thế giới (dùng camera clear của Bootstrap) | Mở `ui/screen/menu` | `ModuleDefinition.showBackdrop = true`. |
| **Lobby** (`scene/lobby`) | `Main Camera` + `Pixel Perfect Camera` (ref 320×180, PPU 16) nhìn sảnh tilemap | `ui/hud/lobby` (Back, Coin, gợi ý), `ui/panel/character` khi bấm nhân vật; nhãn "THƯƠNG NHÂN" là **world-space** trên NPC | `showBackdrop = false`. Nhân vật trong sảnh là object thế giới, bấm qua `Physics2DRaycaster`. |
| **Play** (`scene/play`) | `Main Camera` + Cinemachine + Pixel Perfect | `ui/hud/play`, `ui/screen/pause`, `ui/screen/shop` (run), `ui/screen/victory|defeat` | Số sát thương, thanh máu quái thường = world-space (pool), không vào UIRoot. |

Vì `UIRoot` là overlay nên **luôn vẽ trên mọi camera thế giới** — không cần chỉnh sorting giữa scene.

---

## 2. `UIRoot.prefab` (Core)

```
UIRoot  (Canvas: Screen Space – Overlay, Pixel Perfect ✔, sortingOrder 0)
│        PixelSnapCanvasScaler (ref 1280×720, Expand, artScale 4)   ← §3.2
│        GraphicRaycaster
├─ Backdrop        (sibling 0)  Image #1b1420 stretch + BackdropArt (1600×720, giữa) — §5.1
├─ SafeArea        (SafeAreaFitter — §3.3; mọi layer bên dưới là con của SafeArea)
│   ├─ Layer_Hud       (stretch, không chặn raycast khi trống)
│   ├─ Layer_Screen    (stretch)
│   ├─ Layer_Panel     (stretch)
│   ├─ Layer_Popup     (stretch + Dimmer Image #000 40% bật khi có popup)
│   └─ Layer_Toast     (stretch, raycastTarget off)
├─ Layer_Overlay   (stretch, NGOÀI SafeArea) → LoadingOverlay (sibling cuối, chặn input khi hiện)
└─ InputBlocker    (Image trong suốt, bật trong lúc Open/Go — R1/R5)
```

- `UILayer` enum (MODULES §7.5) ↔ các RectTransform trên. `IUIService` thêm `SetBackdrop(bool)`; `SceneService.Go` gọi theo `ModuleDefinition.showBackdrop`, `LoadingOverlay` luôn bật Backdrop khi hiện.
- Layer là RectTransform stretch (anchor 0–1, offset 0). Screen prefab gắn vào layer với RectTransform **stretch** của chính nó (§4 quy tắc A).
- `Layer_Overlay` nằm **ngoài** SafeArea: overlay/backdrop phủ cả vùng tai thỏ.

---

## 3. Co giãn theo màn hình (phương án đã chốt)

### 3.1. Vì sao 1280×720 chứ không 1920×1080

Với Scale With Screen Size, tỉ lệ thật = `chiều cao màn ÷ chiều cao tham chiếu` → khung 1280×720 (art ×4) và 1920×1080 (art ×6) cho **cùng** số pixel màn hình trên mỗi pixel gốc ở mọi độ phân giải (720p ×4, 1080p ×6, 1440p ×8, 4K ×12). Hiệu năng như nhau (số vertex, texture, draw call không phụ thuộc khung). 1280×720 giữ số liệu bản thiết kế 1:1 và khớp viewport mặc định của itch (1280×720 → tỉ lệ 1). **Không dùng Constant Pixel Size** (UI cố định pixel: to tràn ở 720p, bé ở 4K).

### 3.2. `PixelSnapCanvasScaler` — tỉ lệ nguyên cho pixel art

Mục tiêu: `artScale × scaleFactor` (số pixel màn hình cho 1 pixel gốc) luôn là **số nguyên** → không răng cưa/không lệch pixel. Phần màn hình dư được Expand dồn thành lề, các phần tử neo góc tự dạt ra.

```csharp
// Assets/Project/Core/Scripts/UI/PixelSnapCanvasScaler.cs
[RequireComponent(typeof(Canvas))]
public sealed class PixelSnapCanvasScaler : CanvasScaler
{
    [Tooltip("Số đơn vị UI cho 1 pixel gốc của sprite (bản thiết kế vẽ ×4).")]
    [SerializeField] int artScale = 4;
    [Tooltip("Dưới mức này (màn rất nhỏ) cho phép tỉ lệ lẻ để UI không bị quá to.")]
    [SerializeField] int minWholePixels = 2;

    protected override void HandleScaleWithScreenSize()
    {
        Vector2 screen = new(Screen.width, Screen.height);
        // Expand: cả khung tham chiếu luôn nằm trọn trong màn hình.
        float s = Mathf.Min(screen.x / m_ReferenceResolution.x, screen.y / m_ReferenceResolution.y);
        float px = s * artScale;                       // pixel màn hình / pixel gốc
        if (px >= minWholePixels) s = Mathf.Floor(px) / artScale;   // làm tròn XUỐNG để vẫn vừa màn
        SetScaleFactor(s);
        SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
    }
}
```

| Màn (px thật) | `s` Expand | px/pixel gốc | `s` sau snap | Kích thước canvas (đơn vị UI) |
|---|---|---|---|---|
| 1280×720 (embed itch) | 1.0 | 4 | 1.0 | 1280×720 |
| 1920×1080 | 1.5 | 6 | 1.5 | 1280×720 |
| 2532×1170 (iPhone ngang, DPR cap 2 → ~1688×780) | 1.083 | 4.33 | 1.0 | 1688×780 |
| 2560×1440 | 2.0 | 8 | 2.0 | 1280×720 |
| 1024×768 (4:3) | 0.8 | 3.2 | 0.75 | 1365×1024 |
| 2400×1080 (20:9) | 1.5 | 6 | 1.5 | 1600×720 |

- **Bội số sprite (bắt buộc):** `scaleFactor` được snap theo bước 0.25 (vì `artScale = 4`), nên một sprite đặt ở **×k đơn vị trên mỗi pixel gốc** chỉ nét hoàn toàn khi `k × scaleFactor` nguyên:

  | Bội số sprite | 720p (s=1) | 1080p (s=1.5) | 1440p (s=2) | s=1.25 / 1.75 (hiếm) | Dùng cho |
  |---|---|---|---|---|---|
  | **×4** | ✔ | ✔ | ✔ | ✔ | mặc định: frame 9-slice, nút, icon, ItemTile, chữ pixel lớn |
  | ×2 | ✔ | ✔ | ✔ | ✘ (lệch ½ px) | danh sách nhỏ không bấm |
  | ×3 | ✔ | ✘ (4.5) | ✘ | ✘ | **không dùng** |
  | ×8 | ✔ | ✔ | ✔ | ✔ | logo, ảnh lớn |

- Cài đặt Inspector: `Reference Resolution 1280×720`, `Screen Match Mode` (bị override, để Expand cho dễ đọc), `Reference Pixels Per Unit 16`.
- Canvas `Pixel Perfect ✔` để snap vị trí vertex; `Image.pixelsPerUnitMultiplier` của 9-slice tính theo artScale (frame 6px nguồn → border 24 đơn vị: multiplier = `sprite.PPU / 100 × …` — dùng giá trị sinh bởi tool, xem `ItemTilePrefabBuilder` làm mẫu).
- Cỡ chữ TMP: dùng bội của 4 (12, 16, 20, 24, 28, 32…) và font raster (§7) để chữ không nhòe khi snap.

### 3.3. Safe area

```csharp
// Assets/Project/Core/Scripts/UI/SafeAreaFitter.cs — đặt trên node SafeArea của UIRoot
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    Rect last; Vector2Int lastScreen;
    void OnEnable() => Apply();
    void Update() { if (Screen.safeArea != last || lastScreen != new Vector2Int(Screen.width, Screen.height)) Apply(); }
    void Apply()
    {
        last = Screen.safeArea; lastScreen = new Vector2Int(Screen.width, Screen.height);
        var rt = (RectTransform)transform;
        Vector2 min = last.position, max = last.position + last.size;
        min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
```

- Trên **WebGL**, `Screen.safeArea` thường = toàn màn (trình duyệt tự né tai thỏ khi không bật `viewport-fit=cover`). WebGL template **không** bật `viewport-fit=cover` → không cần xử lý thêm; component vẫn giữ để build native sau này đúng ngay.

### 3.4. Độ phân giải vẽ WebGL (hiệu năng thật)

- Template `Assets/WebGLTemplates/DungeonPixel/index.html`: truyền `devicePixelRatio: Math.min(window.devicePixelRatio || 1, 2)` vào config của `createUnityInstance` → điện thoại DPR 3 không vẽ gấp 9 lần pixel. `matchWebGLToCanvasSize: true`.
- Canvas HTML `width:100%; height:100%; background:#1b1420; image-rendering: pixelated`. Khung embed itch 1280×720 (MODULES §10.1).
- Theo dõi FPS trên Chrome Android; nếu < 50 FPS mới cân nhắc cap DPR 1.5.

### 3.5. Kiểm tra

Game view presets bắt buộc trước khi merge một màn: **1280×720**, **1920×1080**, **2400×1080 (20:9)**, **1688×780 (iPhone ngang, DPR 2)**, **1024×768 (4:3)**, **1152×720 (16:10)**. Tiêu chí: không phần tử nào bị cắt, cụm góc bám đúng góc, khối giữa không méo, pixel đều (zoom Game view ×2 để soi).

---

## 4. Neo (anchor) — quy tắc & bảng từng màn

Ký hiệu: anchor/pivot viết `(x,y)` theo Unity (0,0 = dưới-trái, 1,1 = trên-phải). `pos` = `anchoredPosition` trong đơn vị UI (bằng px trên bản thiết kế). Kích thước lấy từ file `.dc.html` tương ứng.

**Quy tắc chung**

| Mã | Loại phần tử | Anchor / Pivot | Ví dụ |
|---|---|---|---|
| A | Root của screen prefab | stretch (0,0)–(1,1), offset 0 | mọi screen |
| B | Nền phủ (dimmer, ảnh nền) | stretch, hoặc giữa + kích thước cố định lớn hơn khung (backdrop 1600×720) | Pause dimmer, Backdrop |
| C | Cụm điều khiển ở góc | anchor = pivot = góc tương ứng, `pos` = khoảng cách tới mép trên bản thiết kế | HUD, nút Back, Coin |
| D | Khối nội dung chính | anchor = pivot = (0.5,0.5), kích thước cố định theo thiết kế, `pos` = độ lệch tâm | 3 cột Equipment, panel Pause |
| E | Thanh ngang bám mép | anchor (0,y)–(1,y) stretch ngang, pivot (0.5,y) | thanh mẹo Loading (chiều ngang co theo màn, lề 220) |
| F | Phần tử bên trong khối D | anchor (0,1) pivot (0,1), `pos` = tọa độ trong khối (copy từ thiết kế, đổi dấu y) | các ô, nhãn trong panel |

Đổi tọa độ thiết kế (gốc trên-trái, y xuống) sang Unity: với anchor (0,1)/pivot (0,1) → `pos = (left, -top)`; anchor (1,1) → `pos = (-(1280 - left - width), -top)` tức `(-right, -top)`; anchor (0,0) → `(left, bottom)`; anchor (1,0) → `(-right, bottom)`.

### 4.1. Loading (`UI-Loading`, `LoadingOverlay`, layer Overlay)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Backdrop | B (dùng `Backdrop` của UIRoot) | — | — | — |
| Logo (DUNGEON + GATHERING + 2 đuốc) | D | (0.5,1)/(0.5,1) | (0, -108) | 640×180 |
| Knight chạy | trong ProgressGroup | (0,0)/(0.5,0) | x = 656·p | 64×68 |
| ProgressGroup (thanh + chữ bước + % + MB) | D | (0.5,0.5)/(0.5,0.5) | (0, -110) | 720×164 |
| Thanh tải | F trong ProgressGroup | stretch ngang | — | 720×44 |
| Ô MẸO | E | (0,0)–(1,0)/(0.5,0) | bottom 44, lề trái/phải 220 | cao 48 |
| Lỗi + THỬ LẠI | D (thay ProgressGroup) | (0.5,0.5) | (0, -90) | 720×160 |

### 4.2. Menu (`UI-Menu`, `MenuScreen`, layer Screen)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Coin | C | (0,1)/(0,1) | (24, -24) | auto×64 |
| Nút Settings | C | (1,1)/(1,1) | (-24, -24) | 66×66 |
| Logo | D | (0.5,1)/(0.5,1) | (0, -112) | 720×220 |
| Hàng nhân vật | D | (0.5,0.5) | (0, -72) | 620×100 |
| Nút PLAY + "TAP TO PLAY" | D | (0.5,0)/(0.5,0) | (0, 100) | 300×150 |
| Sound / Music | C | (1,0)/(1,0) | (-24, 20) | 122×56 |

### 4.3. Lobby (`UI-Lobby`, `LobbyHud` layer Hud + `CharacterPanel` layer Panel)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Back (về Menu / đóng panel) | C | (0,1)/(0,1) | (20, -14) | 52×52 |
| Coin | C | (1,1)/(1,1) | (-20, -14) | auto×52 |
| "CHỌN NHÂN VẬT ĐỂ BẮT ĐẦU" | C | (0.5,0)/(0.5,0) | (0, 22) | auto×48 |
| CharacterPanel | D-cạnh | trái: (0,0.5)/(0,0.5) pos (24, -30) · phải: (1,0.5)/(1,0.5) pos (-24, -30) | — | 470×620 |
| Nhãn THƯƠNG NHÂN, mũi tên, giá mở khóa | **world-space** trên NPC/nhân vật | — | — | — |

- Phía panel: `CharacterPanel.OnOpen(characterId)` lấy vị trí màn hình của nhân vật (`Camera.WorldToScreenPoint`) — nhân vật ở nửa trái → panel bên phải và ngược lại (giống thiết kế).
- Sảnh rộng hơn 16:9: camera Pixel Perfect giữ ref 320×180 với **crop frame = None** (xem thêm hai bên). Tilemap sảnh phải vẽ dư mỗi bên ≥ 3 tile (≈ 20:9) để màn rộng không lộ khoảng trống.

### 4.4. Equipment / Weapon (`UI-Equipment`, `UI-Weapon`, layer Screen)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Nền (dimmer #1b1420 100%) | B | stretch | — | — |
| Back | C | (0,1)/(0,1) | (24, -16) | 56×56 |
| Tiêu đề + chip nhân vật | C | (0,1)/(0,1) | (100, -18) | auto×48 |
| Chip "Kích hoạt n/2", "Role Synergy n/6" | C | (1,1)/(1,1) | (-244, -18), (-24, -18) | auto×48 |
| Khối 3 cột | D | (0.5,0.5)/(0.5,0.5) | (0, -34) | 1240×620 |
| Cột Chi tiết | F | (0,1)/(0,1) trong khối | (0, 0) | 372×620 |
| Cột Nhân vật + ô + chỉ số | F | (0,1)/(0,1) | (384, 0) | 472×620 |
| Cột Túi / Kho | F | (0,1)/(0,1) | (868, 0) | 372×620 |

- Màn 4:3 (canvas 1365×1024): khối 1240 vẫn vừa ngang; dư chiều cao → khối ở giữa, lề trên/dưới tăng.
- Lưới túi/kho: `GridLayoutGroup` cell **104×108** (ItemTile ×4 — §6.1), spacing 8, **3 cột**; nội dung dài hơn → `ScrollRect` dọc (chỉ kéo dọc). 6 ô mặc ở cột giữa: 104×108, chia 3-3 hai bên nhân vật (cột giữa cao 620 đủ cho 3 ô + nhãn: 3 × (108 + 16) = 372).
- Ô vũ khí (màn Vũ khí): ItemTile ×4 (104×108) thay cho ô 96 trên bản thiết kế.

### 4.5. Shop (`UI-Shop`, layer Screen)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Nền (ảnh phòng Shop mờ + dimmer) | B | stretch | — | — |
| Đóng | C | (0,1)/(0,1) | (24, -18) | 56×56 |
| Tiêu đề + chip ngữ cảnh | C | (0,1)/(0,1) | (100, -18) | auto×56 |
| Coin | C | (1,1)/(1,1) | (-24, -18) | auto×56 |
| Khối lưới hàng + chi tiết | D | (0.5,0.5) | (0, -34) | 1232×600 |

### 4.6. HUD (`UI-HUD`, layer Hud)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Khung chỉ số (avatar + Máu/Giáp/Mana) | C | (0,1)/(0,1) | (10, -10) | 252×96 |
| Coin + Tầng | C | (0,1)/(0,1) | (10, -112) | auto×34 |
| Minimap | C | (1,1)/(1,1) | (-78, -10) | 176×130 |
| Pause | C | (1,1)/(1,1) | (-10, -10) | 60×60 |
| Thanh máu Boss (khi có) | C | (0.5,1)/(0.5,1) | (0, -16) | 480×40 |
| Joystick | C | (0,0)/(0,0) | (48, 40) | 160×160 · vùng chạm = nửa trái màn (`RectTransform` stretch (0,0)–(0.5,0.6) trong suốt) |
| Cụm hành động | C | (1,0)/(1,0) | (-28, 28) | 330×250 |
| ↳ Tấn công | F trong cụm | (1,0)/(1,0) | (0, 0) | 132×132 |
| ↳ Ô vũ khí (đổi) | F | (1,0)/(1,0) | (-138, 112) | 72×75 (+ icon đổi 32×32 ở góc trên-trái) |
| ↳ Kích hoạt 1 | F | (1,0)/(1,0) | (-142, 0) | 104×108 (ItemTile ×4, không chữ) |
| ↳ Kích hoạt 2 | F | (1,0)/(1,0) | (-26, 150) | 104×108 (ItemTile ×4, không chữ) |

### 4.7. Pause + Settings (`UI-Pause`, layer Screen; `SettingsPanel` từ Shared)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Dimmer | B | stretch | — | — |
| Khối 2 panel | D | (0.5,0.5) | (0, 0) | 1020×600 |
| ↳ Panel TẠM DỪNG | F | (0,1)/(0,1) | (0, 0) | 470×600 |
| ↳ `SettingsPanel` | F | (0,1)/(0,1) | (510, 0) | 510×600 |

`SettingsPanel` dùng lại ở Menu (nút bánh răng) dưới dạng popup: layer Popup, anchor (0.5,0.5), size 510×600.

### 4.8. Kết thúc — Thắng / Thua (`UI-Victory`, `UI-RunEnd`, layer Screen)

| Phần tử | Quy tắc | Anchor/Pivot | pos | Size |
|---|---|---|---|---|
| Nền (ảnh arena boss mờ) | B | stretch | — | — |
| Tiêu đề (dòng phụ + chữ lớn + dòng "Đã hạ…") | D | (0.5,1)/(0.5,1) | (0, -26) | 900×160 |
| Khối 2 panel (thống kê + loot / coin) | D | (0.5,0.5) | (0, -14) | 1080×320 |
| Hàng nút (Về sảnh / Tiếp tục hoặc Chơi lại) | C | (0.5,0)/(0.5,0) | (0, 30) | auto×76 |

---

## 5. Luồng prefab UI

### 5.1. Backdrop & Loading lúc mở game

| Lúc | Ai vẽ | Backdrop | Lớp trên |
|---|---|---|---|
| 1. Trình duyệt tải `.wasm/.data` | WebGL template (HTML/CSS) | ảnh `bg_title` làm CSS background, `image-rendering: pixelated` | Logo + thanh HTML (`onProgress`) |
| 2. Bootstrap: `core`, `shared-ui`, `menu` | `UIRoot/Backdrop` | bật | `LoadingOverlay` |
| 3. Menu sẵn sàng | `Backdrop` giữ nguyên | **không vẽ lại** | Overlay tắt, `MenuScreen` mở |
| 4. Vào Lobby / Play | `SceneService.Go` | bật trong lúc tải, tắt khi scene mới `Begin` | `LoadingOverlay` |
| 5. Về Menu | `SceneService.Go("scene/menu")` | bật | `MenuScreen` |

- Ảnh: `bg_title` gốc **400×180** (bản rộng, sinh bởi `Design/tools/backdrop.py` — mở rộng hai bên cho màn 20:9; bản 320×180 hiện tại dùng cho canvas thiết kế). Import: Point, None compression, PPU 16. Nằm trong `Core/Art/` (vào bản build — ~10 KB), **không** qua Addressables; Menu không tham chiếu ảnh này.
- `BackdropArt`: Image native size ×4 = **1600×720**, anchor (0.5,0.5). Canvas cao hơn 720 (4:3) → nền `#1b1420` phía sau lấp trên/dưới (trần và mép đã tối sẵn nên liền mạch).

### 5.2. Mở một screen

```mermaid
sequenceDiagram
    participant Btn as Nút (vd TRANG BỊ)
    participant UI as IUIService
    participant Scope as ModuleScope (Lobby)
    participant Scr as EquipmentScreen
    Btn->>Btn: interactable = false (chống bấm đôi)
    Btn->>UI: Open<EquipmentScreen>("ui/screen/equipment", characterId)
    UI->>UI: InputBlocker bật; nếu address đang mở → trả về instance cũ
    UI->>Scope: Instantiate(address) (tải bundle mod_loadout nếu chưa có)
    Scope-->>UI: GameObject
    UI->>UI: owner còn sống? → gắn vào Layer_Screen, stretch
    UI->>Scr: await OnOpen(args)
    Scr->>Scr: đọc Inventory/Loadout/SessionData hiện tại → dựng ItemTile
    Scr->>Scr: Subscribe EquipmentChangedEvent (OnEnable)
    UI->>UI: InputBlocker tắt
    Btn->>Btn: interactable = true
```

### 5.3. Bảng screen

| Screen (prefab) | Address | Layer | Mở bởi | `args` | Đọc khi `OnOpen` | Nghe sự kiện | `releaseOnClose` |
|---|---|---|---|---|---|---|---|
| `LoadingOverlay` | (trong Bootstrap) | Overlay | Bootstrap, `SceneService.Go` | bước + progress | — | — | — |
| `MenuScreen` | `ui/screen/menu` | Screen | Menu `IModuleEntry.Begin` | — | `SaveData.coins` | — | ✘ |
| `SettingsPanel` | `ui/popup/settings` | Popup / nhúng trong Pause | nút bánh răng, Pause | — | `SettingsData`, `GameLanguage.Current` | `GameLanguage.Changed` | ✘ |
| `LobbyHud` | `ui/hud/lobby` | Hud | Lobby `Begin` | — | coin | coin đổi | ✘ |
| `CharacterPanel` | `ui/panel/character` | Panel | bấm nhân vật | `characterId` | `CharacterData`, `Loadout`, trạng thái khóa | `EquipmentChangedEvent` | ✘ |
| `EquipmentScreen` | `ui/screen/equipment` | Screen | CharacterPanel "TRANG BỊ" | `characterId` | `Inventory`, `Loadout`, `LoadoutRules` | `EquipmentChangedEvent` | ✔ |
| `WeaponScreen` | `ui/screen/weapon` | Screen | CharacterPanel "VŨ KHÍ" | `characterId` | như trên | `WeaponSwappedEvent` | ✔ |
| `ShopScreen` | `ui/screen/shop` | Screen | NPC Thương nhân / `ShopStand` | `ShopContext.Lobby|Run` | ví tiền theo ngữ cảnh, danh sách hàng | — | ✔ |
| `Hud` | `ui/hud/play` | Hud | Play `Begin` | — | `PlayerStats`, `WeaponHolder`, `PlayerSkillController`, `SessionData` | `PlayerStatsChangedEvent`, `WeaponSwappedEvent`, `ActiveSkillUsedEvent`, `BossHealthChangedEvent` | ✘ |
| `PauseScreen` | `ui/screen/pause` | Screen | nút Pause / mất focus tab | — | thống kê run, loadout | — | ✘ |
| `VictoryScreen` | `ui/screen/victory` | Screen | `RunFinishedEvent{Victory}` | kết quả run | `SessionData` (đã chốt) | — | ✔ |
| `DefeatScreen` | `ui/screen/defeat` | Screen | `RunFinishedEvent{!Victory}` | kết quả run | `SessionData` | — | ✔ |

Đóng: nút Back/Esc → `IUIService.Back()` đóng screen trên cùng; screen gọi `OnClose` → hủy subscribe (OnDisable) → nếu `releaseOnClose` thì dispose scope con của nó.

---

## 6. Thành phần UI dùng chung (Shared)

### 6.1. `ItemTile`

{{A54}}

- **Cỡ chuẩn (theo quy tắc bội số §3.2):** prefab hiện dựng ở ×3 (78×81) → **phải đổi trong M5** sang 2 biến thể:
  - `ItemTile` **×4 = 104×108** (icon 64×64, chữ 36×40): ô mặc trang bị, ô vũ khí, ô chi tiết, 2 nút Kích hoạt trên HUD, lưới túi/kho (**3 cột** × 104 + khe 8 = 328, vừa cột 372 — bản thiết kế 4 cột × 64 đổi thành 3 cột khi dựng).
  - `ItemTileSmall` **×2 = 52×54**: chỉ dùng cho danh sách **không bấm** (hàng loadout ở Pause, loot ở màn Kết thúc, panel nhân vật ở Lobby).
  - Đổi bằng hằng `Scale` trong `Editor/ItemTilePrefabBuilder.cs` (build 2 lần ra 2 prefab).
- Vùng chạm tối thiểu cho thứ bấm được: **≥ 88 đơn vị** (trên điện thoại DPR 2, 1 đơn vị ≈ 0.5 px CSS → 88 ≈ 44 px CSS, ngưỡng chạm dễ). Vì thế ô bấm dùng ×4, không dùng ×2.
- Trong trận (`Hud`, `PauseScreen`): `SetShowRarityLetter(false)`.

### 6.2. Khác

| Prefab | Dùng ở | Ghi chú |
|---|---|---|
| `SettingsPanel` | Menu (popup), Pause (nhúng) | Âm lượng 10 nấc (ô vuông `sq_white/sq_grey`), Auto-Aim, Rung, Ngôn ngữ (2 `LanguageButton` cờ). Lưu khi thả tay/đổi (debounce 0.5 s). |
| `VirtualJoystick` | HUD | Vùng chạm nửa trái; tâm joystick nhảy tới điểm chạm (floating), trả về `Vector2` cho `PlayerInput`. Sprite `joy_base`/`joy_knob`. |
| `ConfirmDialog` | Lỗi tải, bỏ run | layer Popup, 2 nút, key i18n truyền vào. |
| `Toast` | "Đã mua", "Đủ 2 kỹ năng" | layer Toast, tự ẩn 2 s, pool 3 cái. |
| `PixelButton` | mọi nút | `Button` + 9-slice frame (`fr_btn`/`fr_light`/`fr_dark`) + `LocalizedTMPText`; trạng thái nhấn = frame tối hơn, không tween scale (giữ pixel). |

---

## 7. Đa ngôn ngữ & font trong UI

{{A13}}

---

## 8. Input trên UI

- `EventSystem` duy nhất trong Bootstrap, `InputSystemUIInputModule`. Scene module không tự thêm EventSystem.
- Nhân vật/NPC trong Lobby là object thế giới → `Physics2DRaycaster` trên camera Lobby.
- Game chạy trong iframe itch: Esc có thể bị trang cha giữ khi fullscreen; Pause luôn có nút trên HUD. Mất focus tab (`visibilitychange`) → tự mở Pause.
- Không dùng `TMP_InputField` (bàn phím ảo WebGL mobile không bật).

---

## 9. Checklist dựng một màn

1. Mở `Design/canvas/project/UI-<Màn>.dc.html`: lấy kích thước/vị trí, sprite (blob → file gốc theo `Design/cuts/blob-map.json` và `Design/tools/cut*.py`), key i18n (`STR` trong file).
2. Tạo prefab trong `Modules/<Module>/Prefabs/UI/` (hoặc `Shared/`), root theo quy tắc A + component kế thừa `UIScreen` (layer, `releaseOnClose`).
3. Đặt phần tử theo bảng §4 của màn đó (anchor/pivot/pos/size). Khối nội dung = quy tắc D, cụm góc = C.
4. Chữ: `TextMeshProUGUI` + `LocalizedTMPText` (key), font asset EN mặc định, cỡ chữ bội 4. Ô vật phẩm: `ItemTile`.
5. Logic: đọc trạng thái trong `OnOpen`, subscribe trong `OnEnable`, nút tự khóa khi gây chuyển scene/kinh tế, `destroyCancellationToken` sau mỗi `await`.
6. Đánh address + group theo MODULES §5/§6 (chạy `Apply Module Rules`).
7. Kiểm tra 6 preset §3.5, đổi EN/VI, kiểm tra không rò handle khi mở/đóng 10 lần.
8. Cập nhật bảng §5.3 nếu thêm screen mới.

{{A11}}

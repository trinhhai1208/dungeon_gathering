using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Sinh <c>docs/SO_REFERENCE.md</c>: mọi lớp ScriptableObject của game (namespace DungeonGathering) — mô tả lấy từ
    /// chú thích <c>///</c> trong code, từng field (kiểu, mặc định, tooltip / chú thích), kiểu nhúng, enum, asset đang có
    /// và nơi tham chiếu. Chạy lại sau mỗi lần sửa SO: <c>Tools ▸ Dungeon Gathering ▸ Docs ▸ Generate SO Reference</c>.
    /// </summary>
    public static class SOReferenceGenerator
    {
        const string OutPath = "docs/SO_REFERENCE.md";
        const string Root = "Assets/Project/";
        const BindingFlags InstFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        // Nhóm theo thư mục script (thứ tự = thứ tự trong tài liệu)
        static readonly (string prefix, string title)[] Groups =
        {
            ("Scripts/Catalog/ItemCatalog", "Gốc nối dữ liệu"),
            ("Scripts/Play/PlayPrefabSet", "Gốc nối dữ liệu"),
            ("Scripts/Catalog/Stats", "Chỉ số"),
            ("Scripts/Catalog/Status", "Hiệu ứng trạng thái"),
            ("Scripts/Catalog/Weapons", "Vũ khí"),
            ("Scripts/Catalog/WeaponData", "Vũ khí"),
            ("Scripts/Catalog/Skills", "Skill"),
            ("Scripts/Catalog/ActiveSkillSO", "Skill"),
            ("Scripts/Combat/Skills", "Skill"),
            ("Scripts/Enemy/EnemyAttackSO", "Skill"),
            ("Scripts/Enemy/Attacks", "Skill"),
            ("Scripts/Catalog/HeroDefinitionSO", "Hero và quái"),
            ("Scripts/Enemy/MonsterDefinitionSO", "Hero và quái"),
            ("Scripts/Catalog/Loot", "Rơi đồ"),
            ("Scripts/Catalog/Levels", "Level và map"),
            ("Scripts/Play/Dungeon/Traps", "Bẫy"),
            ("Scripts/Play/Dungeon", "Level và map"),
            ("Scripts/Catalog/ItemPassiveSO", "Passive"),
            ("Scripts/Catalog/WeaponPassiveSO", "Passive"),
            ("Scripts/Catalog/EquipmentPassiveSO", "Passive"),
            ("Scripts/Combat/Passives", "Passive"),
            ("Scripts/Catalog/ItemData", "Vật phẩm, trang bị, buff"),
            ("Scripts/Catalog/Equipment", "Vật phẩm, trang bị, buff"),
            ("Scripts/Catalog/RunBuffSO", "Vật phẩm, trang bị, buff"),
            ("Core/", "Hạ tầng (Core)"),
        };

        sealed class TypeDoc
        {
            public Type type;
            public string scriptPath;
            public string group;
            public string summary;
        }

        static Dictionary<string, string> sources;   // đường dẫn → nội dung các file .cs
        static readonly HashSet<Type> nestedTypes = new();
        static readonly HashSet<Type> enums = new();

        [MenuItem("Tools/Dungeon Gathering/Docs/Generate SO Reference")]
        public static void GenerateMenu()
        {
            var path = Generate(out int count);
            Debug.Log($"[SO Reference] {count} lớp SO → {path}");
        }

        public static string Generate(out int typeCount)
        {
            nestedTypes.Clear(); enums.Clear();
            sources = Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Replace('\\', '/').Contains("/Editor/") && !p.Replace('\\', '/').Contains("/Tests/"))
                .ToDictionary(p => p.Replace('\\', '/'), File.ReadAllText);

            var soTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(t => t.Namespace != null && t.Namespace.StartsWith("DungeonGathering") && !t.IsGenericType)
                .Where(t => FindDeclaringFile(t) != null)
                .ToList();

            var docs = soTypes.Select(t =>
            {
                var file = FindDeclaringFile(t);
                string rel = file.StartsWith(Root) ? file.Substring(Root.Length) : file;
                return new TypeDoc { type = t, scriptPath = file, group = GroupOf(rel), summary = Summary(t) };
            }).ToList();

            // Asset theo kiểu (chính xác kiểu, không tính lớp con)
            var assets = new Dictionary<Type, List<string>>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Project" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var o = AssetDatabase.LoadMainAssetAtPath(p);
                if (o == null) continue;
                if (!assets.TryGetValue(o.GetType(), out var list)) assets[o.GetType()] = list = new List<string>();
                list.Add(p);
            }

            // Ai tham chiếu kiểu nào (field trên SO hoặc kiểu nhúng)
            var refBy = new Dictionary<Type, SortedSet<string>>();
            void AddRefs(Type owner, string ownerName)
            {
                foreach (var f in SerializedFields(owner))
                {
                    foreach (var target in ElementTypes(f.FieldType))
                    {
                        if (!refBy.TryGetValue(target, out var set)) refBy[target] = set = new SortedSet<string>();
                        set.Add($"{ownerName}.{f.Name}");
                    }
                }
            }

            var sb = new StringBuilder();
            WriteIntro(sb, docs, assets);

            string currentGroup = null;
            foreach (var d in docs.OrderBy(d => GroupIndex(d.group)).ThenBy(d => Depth(d.type)).ThenBy(d => d.type.Name))
            {
                if (d.group != currentGroup)
                {
                    currentGroup = d.group;
                    sb.AppendLine().AppendLine($"## {currentGroup}").AppendLine();
                }
                WriteType(sb, d, assets);
                AddRefs(d.type, d.type.Name);
            }

            // Kiểu nhúng (class/struct [Serializable]) — thu thập lặp vì kiểu nhúng có thể chứa kiểu nhúng khác
            var written = new HashSet<Type>();
            sb.AppendLine().AppendLine("## Kiểu nhúng (class / struct dùng trong field)").AppendLine();
            sb.AppendLine("Không phải asset riêng — nằm bên trong field của SO. Sửa trong Inspector của SO chứa nó.").AppendLine();
            while (true)
            {
                var next = nestedTypes.Where(t => !written.Contains(t)).OrderBy(t => t.Name).FirstOrDefault();
                if (next == null) break;
                written.Add(next);
                WriteNested(sb, next);
                AddRefs(next, Pretty(next));
            }

            // Enum
            sb.AppendLine().AppendLine("## Enum dùng trong field").AppendLine();
            foreach (var e in enums.OrderBy(t => t.Name))
            {
                var names = Enum.GetNames(e);
                var vals = names.Select(n => $"`{n}`" + (IsFlags(e) ? $" ({Convert.ToInt64(Enum.Parse(e, n))})" : ""));
                var sum = Summary(e);
                sb.AppendLine($"- **{e.Name}**{(IsFlags(e) ? " (cờ, chọn nhiều)" : "")}: {string.Join(", ", vals)}" + (string.IsNullOrEmpty(sum) ? "" : $" — {sum}"));
            }

            // Bảng "được tham chiếu bởi"
            sb.AppendLine().AppendLine("## Được tham chiếu bởi").AppendLine();
            sb.AppendLine("Field nào trỏ tới mỗi lớp SO (kể cả qua List / kiểu nhúng). Dùng khi đổi tên / xóa một lớp.").AppendLine();
            foreach (var d in docs.OrderBy(d => d.type.Name))
            {
                var users = new SortedSet<string>();
                for (var t = d.type; t != null && t != typeof(ScriptableObject); t = t.BaseType)
                    if (refBy.TryGetValue(t, out var s)) foreach (var u in s) users.Add(t == d.type ? u : $"{u} (qua {t.Name})");
                if (users.Count > 0) sb.AppendLine($"- **{d.type.Name}** ← {string.Join(", ", users.Select(u => $"`{u}`"))}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutPath)));
            File.WriteAllText(OutPath, sb.ToString(), new UTF8Encoding(false));
            typeCount = docs.Count;
            return OutPath;
        }

        // ------------------------------------------------------------------ viết từng phần

        static void WriteIntro(StringBuilder sb, List<TypeDoc> docs, Dictionary<Type, List<string>> assets)
        {
            int assetCount = docs.Sum(d => assets.TryGetValue(d.type, out var l) ? l.Count : 0);
            sb.AppendLine("# Tham chiếu ScriptableObject — Dungeon Gathering");
            sb.AppendLine();
            sb.AppendLine("> **File sinh tự động** bởi `Tools ▸ Dungeon Gathering ▸ Docs ▸ Generate SO Reference`");
            sb.AppendLine("> (`Assets/Project/Editor/Docs/SOReferenceGenerator.cs`). Đừng sửa tay — sửa chú thích `///` / `[Tooltip]` trong");
            sb.AppendLine("> code rồi chạy lại. Tổng quan kiến trúc: [`ARCHITECTURE.md`](ARCHITECTURE.md); số liệu đang cấu hình và việc còn");
            sb.AppendLine("> lại: [`HANDOFF.md`](HANDOFF.md).");
            sb.AppendLine();
            sb.AppendLine($"**{docs.Count} lớp SO** · **{assetCount} asset**. Cập nhật: {DateTime.Now:yyyy-MM-dd HH:mm}.");
            sb.AppendLine();
            sb.AppendLine("Cách đọc:");
            sb.AppendLine("- **Mặc định** = giá trị khi tạo asset mới (`Create ▸ Dungeon Gathering ▸ …`), không phải giá trị trong asset đang có.");
            sb.AppendLine("- `Overridable<T>`: checkbox + giá trị. Bỏ tick = dùng giá trị của SO gốc; tick = dùng giá trị này.");
            sb.AppendLine("- *(abstract)*: lớp gốc, không tạo asset trực tiếp — field của nó có trong mọi lớp con.");
            sb.AppendLine("- Runtime **không bao giờ ghi vào SO**; sau khi sửa dữ liệu chạy `Tools ▸ Dungeon Gathering ▸ Validate All`.");
            sb.AppendLine();
            sb.AppendLine("## Mục lục").AppendLine();
            foreach (var g in docs.GroupBy(d => d.group).OrderBy(g => GroupIndex(g.Key)))
                sb.AppendLine($"- **{g.Key}**: " + string.Join(", ", g.OrderBy(d => Depth(d.type)).ThenBy(d => d.type.Name)
                    .Select(d => $"[{d.type.Name}](#{Anchor(d.type.Name)})")));
        }

        static void WriteType(StringBuilder sb, TypeDoc d, Dictionary<Type, List<string>> assets)
        {
            var t = d.type;
            string kind = t.IsAbstract ? " *(abstract)*" : "";
            string baseName = t.BaseType != null && t.BaseType != typeof(ScriptableObject) ? $" : `{t.BaseType.Name}`" : "";
            sb.AppendLine($"### {t.Name}").AppendLine();
            var menu = t.GetCustomAttribute<CreateAssetMenuAttribute>();
            string create = menu != null && !string.IsNullOrEmpty(menu.menuName) ? $" · tạo: `Create ▸ {menu.menuName.Replace("/", " ▸ ")}`" : "";
            sb.AppendLine($"`{t.Name}`{baseName}{kind} — [`{d.scriptPath.Replace("Assets/Project/", "")}`](../{d.scriptPath}){create}");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(d.summary)) sb.AppendLine(d.summary).AppendLine();

            var fields = SerializedFields(t).ToList();
            if (fields.Count == 0) sb.AppendLine("*Không có field riêng (dùng field của lớp cha).*").AppendLine();
            else
            {
                var inst = t.IsAbstract ? null : SafeCreate(t);
                WriteFieldTable(sb, t, fields, inst);
                if (inst is Object o && o != null) Object.DestroyImmediate(o);
            }
            var parentFields = new List<string>();
            for (var b = t.BaseType; b != null && b != typeof(ScriptableObject); b = b.BaseType)
                if (SerializedFields(b).Any()) parentFields.Add($"[{b.Name}](#{Anchor(b.Name)})");
            if (parentFields.Count > 0 && fields.Count > 0) sb.AppendLine($"*Cộng thêm field của: {string.Join(", ", parentFields)}.*").AppendLine();

            if (assets.TryGetValue(t, out var list) && list.Count > 0)
            {
                var byDir = list.GroupBy(p => Path.GetDirectoryName(p).Replace('\\', '/').Replace("Assets/Project/", ""));
                sb.AppendLine($"**Asset ({list.Count})**: " + string.Join(" · ", byDir.Select(g =>
                    $"`{g.Key}/` " + string.Join(", ", g.Select(p => Path.GetFileNameWithoutExtension(p)).OrderBy(n => n)))));
                sb.AppendLine();
            }
            else if (!t.IsAbstract) sb.AppendLine("**Asset**: chưa có.").AppendLine();
        }

        static void WriteNested(StringBuilder sb, Type t)
        {
            sb.AppendLine($"### {Pretty(t)}").AppendLine();
            var file = FindDeclaringFile(t);
            sb.AppendLine($"{(t.IsValueType ? "struct" : "class")} `{Pretty(t)}`" + (file != null ? $" — [`{file.Replace("Assets/Project/", "")}`](../{file})" : ""));
            sb.AppendLine();
            var sum = Summary(t);
            if (!string.IsNullOrEmpty(sum)) sb.AppendLine(sum).AppendLine();
            var fields = SerializedFields(t).ToList();
            object inst = null;
            try { inst = Activator.CreateInstance(t); } catch { /* không có ctor rỗng */ }
            if (fields.Count > 0) WriteFieldTable(sb, t, fields, inst);
        }

        static void WriteFieldTable(StringBuilder sb, Type owner, List<FieldInfo> fields, object instance)
        {
            sb.AppendLine("| Field | Kiểu | Mặc định | Ý nghĩa |");
            sb.AppendLine("|---|---|---|---|");
            string header = null;
            foreach (var f in fields)
            {
                var h = f.GetCustomAttribute<HeaderAttribute>();
                if (h != null) header = h.header;
                string desc = FieldDescription(owner, f);
                if (h != null) desc = $"*[{header}]* " + desc;
                var range = f.GetCustomAttribute<RangeAttribute>();
                if (range != null) desc += $" (khoảng {Num(range.min)}–{Num(range.max)})";
                var min = f.GetCustomAttribute<MinAttribute>();
                if (min != null) desc += $" (≥ {Num(min.min)})";
                string def = "";
                if (instance != null) { try { def = Format(f.GetValue(instance)); } catch { def = ""; } }
                CollectNested(f.FieldType);
                sb.AppendLine($"| `{f.Name}` | {TypeCell(f.FieldType)} | {Esc(def)} | {Esc(desc)} |");
            }
            sb.AppendLine();
        }

        // ------------------------------------------------------------------ reflection

        static IEnumerable<FieldInfo> SerializedFields(Type t)
        {
            foreach (var f in t.GetFields(InstFlags))
            {
                if (f.IsStatic || f.IsInitOnly || f.IsLiteral) continue;
                if (f.GetCustomAttribute<NonSerializedAttribute>() != null) continue;
                if (f.Name.Contains("k__BackingField")) continue;
                bool ser = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null || f.GetCustomAttribute<SerializeReference>() != null;
                if (ser) yield return f;
            }
        }

        static IEnumerable<Type> ElementTypes(Type t)
        {
            if (t.IsArray) { foreach (var x in ElementTypes(t.GetElementType())) yield return x; yield break; }
            if (t.IsGenericType) { foreach (var a in t.GetGenericArguments()) foreach (var x in ElementTypes(a)) yield return x; yield break; }
            yield return t;
        }

        static void CollectNested(Type t)
        {
            foreach (var e in ElementTypes(t))
            {
                if (e.IsEnum) { if (e.Namespace != null && e.Namespace.StartsWith("DungeonGathering")) enums.Add(e); continue; }
                if (typeof(Object).IsAssignableFrom(e) || e.IsPrimitive || e == typeof(string)) continue;
                if (e.Namespace == null || !e.Namespace.StartsWith("DungeonGathering")) continue;
                if (e.IsGenericType) continue;
                if (e.GetCustomAttribute<SerializableAttribute>() == null && !e.IsValueType) continue;
                if (nestedTypes.Add(e)) foreach (var f in SerializedFields(e)) CollectNested(f.FieldType);
            }
        }

        static object SafeCreate(Type t)
        {
            try
            {
                var o = ScriptableObject.CreateInstance(t);
                if (o != null) o.hideFlags = HideFlags.HideAndDontSave;
                return o;
            }
            catch { return null; }
        }

        static int Depth(Type t) { int d = 0; for (var b = t; b != null && b != typeof(ScriptableObject); b = b.BaseType) d++; return d; }

        static bool IsFlags(Type e) => e.GetCustomAttribute<FlagsAttribute>() != null;

        // ------------------------------------------------------------------ nguồn code: tìm file, chú thích

        static string FindDeclaringFile(Type t)
        {
            var name = t.IsGenericType ? t.Name.Split('`')[0] : t.Name;
            var rx = new Regex($@"\b(class|struct|enum)\s+{Regex.Escape(name)}\b(?!\s*\()");
            string nsDecl = t.Namespace;
            foreach (var kv in sources)
                if (rx.IsMatch(kv.Value) && (nsDecl == null || kv.Value.Contains("namespace " + nsDecl)))
                    return kv.Key;
            return null;
        }

        static string Summary(Type t)
        {
            var file = FindDeclaringFile(t);
            if (file == null) return null;
            var name = t.IsGenericType ? t.Name.Split('`')[0] : t.Name;
            var m = new Regex($@"\b(class|struct|enum)\s+{Regex.Escape(name)}\b").Match(sources[file]);
            return m.Success ? DocCommentBefore(sources[file], m.Index) : null;
        }

        static string FieldDescription(Type owner, FieldInfo f)
        {
            var tip = f.GetCustomAttribute<TooltipAttribute>();
            if (tip != null && !string.IsNullOrWhiteSpace(tip.tooltip)) return tip.tooltip.Trim();
            var file = FindDeclaringFile(f.DeclaringType);
            if (file == null) return "";
            var text = sources[file];
            // Dòng khai báo field: "... TênKiểu tênField;" hoặc "= ..."
            var rx = new Regex($@"^[^\r\n]*\b{Regex.Escape(f.Name)}\s*(=|;)[^\r\n]*$", RegexOptions.Multiline);
            foreach (Match m in rx.Matches(text))
            {
                var line = m.Value;
                if (line.TrimStart().StartsWith("//")) continue;
                int c = line.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0 && !line.Substring(0, c).Contains("\"")) return line.Substring(c + 2).Trim();
                var doc = DocCommentBefore(text, m.Index);
                if (!string.IsNullOrEmpty(doc)) return doc;
                break;
            }
            return "";
        }

        /// <summary>Gom các dòng /// ngay trên vị trí (bỏ qua dòng attribute).</summary>
        static string DocCommentBefore(string text, int index)
        {
            int lineStart = text.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
            var lines = new List<string>();
            int pos = lineStart - 1;
            while (pos > 0)
            {
                int prevStart = text.LastIndexOf('\n', pos - 1) + 1;
                var line = text.Substring(prevStart, pos - prevStart).Trim();
                if (line.StartsWith("///")) lines.Insert(0, line.Substring(3).Trim());
                else if (line.StartsWith("[") && lines.Count == 0) { }
                else break;
                pos = prevStart - 1;
            }
            if (lines.Count == 0) return null;
            var s = string.Join(" ", lines);
            s = Regex.Replace(s, @"<see\s+cref=""([^""]+)""\s*/>", m => "`" + m.Groups[1].Value.Split('.').Last() + "`");
            s = Regex.Replace(s, @"<paramref\s+name=""([^""]+)""\s*/>", "`$1`");
            s = Regex.Replace(s, @"<c>(.*?)</c>", "`$1`");
            s = Regex.Replace(s, @"</?(summary|remarks|para|b|i)>", " ");
            s = Regex.Replace(s, @"<[^>]+>", "");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        // ------------------------------------------------------------------ định dạng

        static string GroupOf(string rel)
        {
            foreach (var g in Groups) if (rel.StartsWith(g.prefix)) return g.title;
            return "Khác";
        }

        static int GroupIndex(string title)
        {
            for (int i = 0; i < Groups.Length; i++) if (Groups[i].title == title) return i;
            return 999;
        }

        static string Pretty(Type t)
        {
            if (t == typeof(float)) return "float";
            if (t == typeof(int)) return "int";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(long)) return "long";
            if (t.IsArray) return Pretty(t.GetElementType()) + "[]";
            if (t.IsGenericType)
                return t.Name.Split('`')[0] + "<" + string.Join(", ", t.GetGenericArguments().Select(Pretty)) + ">";
            return t.DeclaringType != null && !t.IsNested ? t.Name : t.Name;
        }

        static string TypeCell(Type t)
        {
            // Liên kết tới mục của kiểu SO / kiểu nhúng trong tài liệu
            var parts = Pretty(t);
            foreach (var e in ElementTypes(t).Distinct())
            {
                bool linkable = (typeof(ScriptableObject).IsAssignableFrom(e) || (!e.IsPrimitive && !e.IsEnum && e != typeof(string)))
                                && e.Namespace != null && e.Namespace.StartsWith("DungeonGathering");
                if (linkable) parts = Regex.Replace(parts, $@"\b{Regex.Escape(e.Name)}\b", $"[{e.Name}](#{Anchor(e.Name)})");
            }
            return parts.Contains("[") ? parts : $"`{parts}`";
        }

        static string Anchor(string name) => name.ToLowerInvariant();

        static string Format(object v)
        {
            switch (v)
            {
                case null: return "";
                case Object o: return o ? o.name : "";
                case string s: return s.Length == 0 ? "" : $"\"{s}\"";
                case float f: return Num(f);
                case double d: return Num((float)d);
                case bool b: return b ? "✓" : "✗";
                case Enum e: return e.ToString();
                case Vector2 v2: return $"({Num(v2.x)}, {Num(v2.y)})";
                case Vector3 v3: return $"({Num(v3.x)}, {Num(v3.y)}, {Num(v3.z)})";
                case Vector2Int vi: return $"({vi.x}, {vi.y})";
                case Color c: return c.a <= 0f ? "trong suốt" : "#" + ColorUtility.ToHtmlStringRGBA(c);
                case IList list: return list.Count == 0 ? "[]" : $"{list.Count} phần tử";
            }
            var t = v.GetType();
            if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("Overridable"))
            {
                bool on = (bool)t.GetField("enabled").GetValue(v);
                return on ? "bật: " + Format(t.GetField("value").GetValue(v)) : "tắt";
            }
            if (t.IsPrimitive) return Convert.ToString(v, CultureInfo.InvariantCulture);
            return "";
        }

        static string Num(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

        static string Esc(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}

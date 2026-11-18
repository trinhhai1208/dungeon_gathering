using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Enemies;
using DungeonGathering.VFX;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    public static class DarkFantasyVfxBuilder
    {
        const string SpriteRoot = "Assets/Project/Art/Sprites/VFX/DarkFantasy";
        const string VfxOutRoot = "Assets/Project/Prefabs/VFX/DarkFantasy";
        const string ProjOutRoot = "Assets/Project/Prefabs/Projectiles";

        [MenuItem("Tools/Dungeon Gathering/VFX/Build & Assign Dark Fantasy VFX")]
        public static void BuildAndAssignAll()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(
                AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));

            // 1. Slashes (6 frames, 84x68, 30fps, one-shot)
            var slashNames = new[]
            {
                ("slash_iron",   "VFX_Slash_Iron"),
                ("slash_twin",   "VFX_Slash_Twin"),
                ("slash_spear",  "VFX_Slash_Spear"),
                ("slash_gold",   "VFX_Slash_Gold"),
                ("slash_fire",   "VFX_Slash_Fire"),
                ("slash_ice",    "VFX_Slash_Ice"),
                ("slash_thunder","VFX_Slash_Thunder"),
                ("slash_venom",  "VFX_Slash_Venom"),
                ("slash_thorn",  "VFX_Slash_Thorn"),
                ("slash_void",   "VFX_Slash_Void"),
                ("slash_holy",   "VFX_Slash_Holy"),
            };

            var slashes = new Dictionary<string, GameObject>();
            foreach (var (texName, prefabName) in slashNames)
            {
                var sprites = SliceStrip($"{SpriteRoot}/{texName}.png", texName, 6, 84, 68, new Vector2(0.38f, 0.5f));
                var pf = BuildOneShotVfx($"{VfxOutRoot}/Slashes", prefabName, sprites, 30f, 0.22f, mat, "Items", 15);
                slashes[prefabName] = pf;
            }

            // 2. Hit Impacts (5 frames, 20x20, 24fps, one-shot)
            var projSpecs = new (string texSuffix, string projName, string impactName, float colliderRadius)[]
            {
                ("plasma_blue",      "Projectile_Plasma_Blue",      "VFX_Impact_Plasma_Blue",      0.32f),
                ("plasma_red",       "Projectile_Plasma_Red",       "VFX_Impact_Plasma_Red",       0.32f),
                ("rocket",           "Projectile_Rocket",           "VFX_Impact_Rocket",           0.40f),
                ("sparks_blast",     "Projectile_Sparks_Blast",     "VFX_Impact_Sparks_Blast",     0.30f),
                ("hunter_arrow",     "Projectile_Hunter_Arrow",     "VFX_Impact_Hunter_Arrow",     0.30f),
                ("rune_orb",         "Projectile_Rune_Orb",         "VFX_Impact_Rune_Orb",         0.34f),
                ("wave_holy",        "Projectile_Wave_Holy",        "VFX_Impact_Wave_Holy",        0.42f),
                ("wave_void",        "Projectile_Wave_Void",        "VFX_Impact_Wave_Void",        0.42f),
                ("enemy_blood",      "Projectile_Enemy_Blood",      "VFX_Impact_Enemy_Blood",      0.32f),
                ("enemy_void_frost", "Projectile_Enemy_VoidFrost",  "VFX_Impact_Enemy_VoidFrost",  0.34f),
            };

            var impacts = new Dictionary<string, GameObject>();
            var projectiles = new Dictionary<string, GameObject>();

            foreach (var (suffix, projName, impactName, radius) in projSpecs)
            {
                var hitSprites = SliceStrip($"{SpriteRoot}/hit_{suffix}.png", $"hit_{suffix}", 5, 20, 20, new Vector2(0.5f, 0.5f));
                impacts[impactName] = BuildOneShotVfx($"{VfxOutRoot}/Impacts", impactName, hitSprites, 24f, 0.22f, mat, "Items", 16);

                var projSprites = SliceStrip($"{SpriteRoot}/proj_{suffix}.png", $"proj_{suffix}", 4, 24, 16, new Vector2(0.5f, 0.5f));
                projectiles[projName] = BuildOrUpdateProjectile(projName, projSprites, 12f, radius, mat);
            }

            // 3. Assign to ProjectileLibrary, SwordLibrary, RangedLibrary, WeaponBase_Sword, Skill_arrow-rain, EnemyAttacks
            AssignProjectileLibrary(projectiles, impacts);
            AssignSwords(slashes);
            AssignRanged(impacts);
            AssignSkillAndEnemies(projectiles, impacts);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkFantasyVfx] Built and assigned 11 Slashes, 10 Projectiles, and 10 Hit Impacts!");
        }

        static List<Sprite> SliceStrip(string path, string prefix, int count, int frameW, int frameH, Vector2 pivot)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) throw new FileNotFoundException($"Missing texture: {path}");

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(ti);
            dataProvider.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{prefix}_{i}",
                    rect = new Rect(i * frameW, 0, frameW, frameH),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = GUID.Generate()
                };
            }
            dataProvider.SetSpriteRects(rects);
            dataProvider.Apply();
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s =>
                {
                    var parts = s.name.Split('_');
                    return int.TryParse(parts[^1], out int idx) ? idx : 0;
                })
                .ToList();
        }

        static AnimationClip BuildSpriteClip(string clipPath, string clipName, List<Sprite> sprites, float fps, bool loop)
        {
            EnsureDir(Path.GetDirectoryName(clipPath).Replace("\\", "/"));
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = fps;
            float dt = 1f / fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i * dt, value = sprites[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AnimatorController BuildController(string ctrlPath, AnimationClip clip)
        {
            EnsureDir(Path.GetDirectoryName(ctrlPath).Replace("\\", "/"));
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var st in sm.states.ToArray()) sm.RemoveState(st.state);
            var play = sm.AddState("Play");
            play.motion = clip;
            sm.defaultState = play;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static GameObject BuildOneShotVfx(string dir, string prefabName, List<Sprite> sprites, float fps, float duration, Material mat, string sortingLayer, int sortingOrder)
        {
            EnsureDir(dir);
            var clip = BuildSpriteClip($"{dir}/Animations/{prefabName}.anim", prefabName, sprites, fps, false);
            var ctrl = BuildController($"{dir}/Animations/{prefabName}.controller", clip);

            string pfPath = $"{dir}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(pfPath);
            var go = existing != null ? PrefabUtility.LoadPrefabContents(pfPath) : new GameObject(prefabName);

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;

            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            var ade = go.GetComponent<AutoDestroyEffect>();
            if (ade == null) ade = go.AddComponent<AutoDestroyEffect>();
            ade.Duration = duration;

            if (existing != null)
            {
                PrefabUtility.SaveAsPrefabAsset(go, pfPath);
                PrefabUtility.UnloadPrefabContents(go);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(go, pfPath);
                UnityEngine.Object.DestroyImmediate(go);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(pfPath);
        }

        static GameObject BuildOrUpdateProjectile(string projName, List<Sprite> sprites, float fps, float colliderRadius, Material mat)
        {
            EnsureDir(ProjOutRoot);
            var clip = BuildSpriteClip($"{ProjOutRoot}/Animations/{projName}.anim", projName, sprites, fps, true);
            var ctrl = BuildController($"{ProjOutRoot}/Animations/{projName}.controller", clip);

            string pfPath = $"{ProjOutRoot}/{projName}.prefab";
            GameObject go;
            bool loadedExisting = AssetDatabase.LoadAssetAtPath<GameObject>(pfPath) != null;
            if (loadedExisting)
            {
                go = PrefabUtility.LoadPrefabContents(pfPath);
            }
            else
            {
                string templatePath = $"{ProjOutRoot}/Projectile_Plasma_Blue.prefab";
                var tpl = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
                go = (GameObject)PrefabUtility.InstantiatePrefab(tpl);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                go.name = projName;
            }

            go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprites[0];
                if (mat != null) sr.sharedMaterial = mat;
                sr.sortingLayerName = "Characters";
                sr.sortingOrder = 10;
            }
            var anim = go.GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = ctrl;

            var col = go.GetComponent<CircleCollider2D>();
            if (col != null) col.radius = colliderRadius;

            PrefabUtility.SaveAsPrefabAsset(go, pfPath);
            if (loadedExisting) PrefabUtility.UnloadPrefabContents(go);
            else UnityEngine.Object.DestroyImmediate(go);

            return AssetDatabase.LoadAssetAtPath<GameObject>(pfPath);
        }

        static void AssignProjectileLibrary(Dictionary<string, GameObject> projectiles, Dictionary<string, GameObject> impacts)
        {
            var lib = AssetDatabase.LoadAssetAtPath<ProjectileLibrarySO>("Assets/Project/Data/WeaponLibrary/ProjectileLibrary.asset");
            if (lib == null) return;

            void Upsert(string id, string projName, string impactName, float speed, int pierce, float explosionRadius)
            {
                var entry = lib.projectiles.Find(e => e != null && e.id == id);
                if (entry == null)
                {
                    entry = new ProjectileEntry { id = id };
                    lib.projectiles.Add(entry);
                }
                entry.prefab = projectiles[projName];
                entry.hitVfx = impacts[impactName];
                entry.speed = speed;
                entry.pierce = pierce;
                entry.explosionRadius = explosionRadius;
            }

            Upsert("plasma-blue",  "Projectile_Plasma_Blue",  "VFX_Impact_Plasma_Blue",  15f, 0, 0f);
            Upsert("plasma-red",   "Projectile_Plasma_Red",   "VFX_Impact_Plasma_Red",   16f, 0, 0f);
            Upsert("rocket",       "Projectile_Rocket",       "VFX_Impact_Rocket",       11f, 0, 0f);
            Upsert("sparks-blast", "Projectile_Sparks_Blast", "VFX_Impact_Sparks_Blast", 13f, 0, 0f);
            Upsert("hunter-arrow", "Projectile_Hunter_Arrow", "VFX_Impact_Hunter_Arrow", 16f, 0, 0f);
            Upsert("rune-orb",     "Projectile_Rune_Orb",     "VFX_Impact_Rune_Orb",     12f, 0, 0f);
            Upsert("wave-holy",    "Projectile_Wave_Holy",    "VFX_Impact_Wave_Holy",    14f, 0, 0f);
            Upsert("wave-void",    "Projectile_Wave_Void",    "VFX_Impact_Wave_Void",    14f, 0, 0f);

            EditorUtility.SetDirty(lib);
        }

        static void AssignSwords(Dictionary<string, GameObject> slashes)
        {
            var baseSword = AssetDatabase.LoadAssetAtPath<SwordBaseSO>("Assets/Project/Data/WeaponLibrary/WeaponBase_Sword.asset");
            if (baseSword != null)
            {
                baseSword.slashVfx = slashes["VFX_Slash_Iron"];
                EditorUtility.SetDirty(baseSword);
            }

            var lib = AssetDatabase.LoadAssetAtPath<SwordLibrarySO>("Assets/Project/Data/WeaponLibrary/SwordLibrary.asset");
            if (lib == null) return;

            var map = new Dictionary<string, string>
            {
                ["dagger-twin"]   = "VFX_Slash_Twin",
                ["spear-iron"]    = "VFX_Slash_Spear",
                ["sword-iron"]    = "VFX_Slash_Iron",
                ["sword-old"]     = "VFX_Slash_Iron",
                ["sword-gold"]    = "VFX_Slash_Gold",
                ["sword-fire"]    = "VFX_Slash_Fire",
                ["sword-lava"]    = "VFX_Slash_Fire",
                ["sword-ice"]     = "VFX_Slash_Ice",
                ["sword-thunder"] = "VFX_Slash_Thunder",
                ["sword-venom"]   = "VFX_Slash_Venom",
                ["sword-thorn"]   = "VFX_Slash_Thorn",
                ["sword-dark"]    = "VFX_Slash_Void",
                ["sword-void"]    = "VFX_Slash_Void",
                ["sword-purple"]  = "VFX_Slash_Void",
                ["sword-holy"]    = "VFX_Slash_Holy",
            };

            foreach (var s in lib.swords)
            {
                if (s == null) continue;
                if (map.TryGetValue(s.id, out var vfxName) && slashes.TryGetValue(vfxName, out var pf))
                    s.slashVfx = pf;
                if (s.id == "sword-holy") s.waveProjectileId = "wave-holy";
                else if (s.id == "sword-purple") s.waveProjectileId = "wave-void";
            }
            EditorUtility.SetDirty(lib);
        }

        static void AssignRanged(Dictionary<string, GameObject> impacts)
        {
            var lib = AssetDatabase.LoadAssetAtPath<RangedLibrarySO>("Assets/Project/Data/WeaponLibrary/RangedLibrary.asset");
            if (lib == null) return;

            foreach (var w in lib.weapons)
            {
                if (w == null) continue;
                switch (w.id)
                {
                    case "bow-wood":
                        w.projectileId = "hunter-arrow";
                        w.hitVfx = impacts["VFX_Impact_Hunter_Arrow"];
                        break;
                    case "gun-pistol":
                        w.projectileId = "plasma-blue";
                        w.hitVfx = impacts["VFX_Impact_Plasma_Blue"];
                        break;
                    case "gun-rifle":
                        w.projectileId = "plasma-red";
                        w.hitVfx = impacts["VFX_Impact_Plasma_Red"];
                        break;
                    case "gun-shotgun":
                        w.projectileId = "sparks-blast";
                        w.hitVfx = impacts["VFX_Impact_Sparks_Blast"];
                        break;
                    case "staff-apprentice":
                        w.projectileId = "rune-orb";
                        w.hitVfx = impacts["VFX_Impact_Rune_Orb"];
                        break;
                }
            }
            EditorUtility.SetDirty(lib);
        }

        static void AssignSkillAndEnemies(Dictionary<string, GameObject> projectiles, Dictionary<string, GameObject> impacts)
        {
            var arrowSkill = AssetDatabase.LoadAssetAtPath<ProjectileVolleySkill>("Assets/Project/Data/Skills/Skill_arrow-rain.asset");
            if (arrowSkill != null)
            {
                var so = new SerializedObject(arrowSkill);
                so.FindProperty("projectilePrefab").objectReferenceValue = projectiles["Projectile_Hunter_Arrow"];
                so.FindProperty("impactVfx").objectReferenceValue = impacts["VFX_Impact_Hunter_Arrow"];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var bloodMap = new HashSet<string> { "Atk_blood-bolt", "Atk_bolt-fan", "Atk_lord-fan" };
            var voidFrostMap = new HashSet<string> { "Atk_frost-fan", "Atk_frost-orb", "Atk_void-fan", "Atk_lord-spiral", "Atk_skull-radial" };

            foreach (var guid in AssetDatabase.FindAssets("t:ShootAttackSO", new[] { "Assets/Project/Data/EnemyAttacks" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var atk = AssetDatabase.LoadAssetAtPath<ShootAttackSO>(path);
                if (atk == null) continue;
                var so = new SerializedObject(atk);
                if (bloodMap.Contains(atk.name))
                {
                    so.FindProperty("projectilePrefab").objectReferenceValue = projectiles["Projectile_Enemy_Blood"];
                    so.FindProperty("impactVfx").objectReferenceValue = impacts["VFX_Impact_Enemy_Blood"];
                }
                else if (voidFrostMap.Contains(atk.name))
                {
                    so.FindProperty("projectilePrefab").objectReferenceValue = projectiles["Projectile_Enemy_VoidFrost"];
                    so.FindProperty("impactVfx").objectReferenceValue = impacts["VFX_Impact_Enemy_VoidFrost"];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void EnsureDir(string dir)
        {
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;
            var parent = Path.GetDirectoryName(dir).Replace("\\", "/");
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(dir));
        }
    }
}

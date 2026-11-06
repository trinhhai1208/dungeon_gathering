using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    public static partial class EntityPrefabBuilder
    {
        // ==========================================
        // 4. ENEMIES, SKULL & BOSS VARIANTS (ENTITIES §3.1 & §3.5.2)
        // ==========================================
        public static void BuildEnemyBaseAndVariants()
        {
            // 4.1 Build Enemy_Base
            var root = new GameObject("Enemy_Base");
            root.tag = "Enemy";
            root.layer = LayerMask.NameToLayer("Enemy");

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            root.AddComponent<EnemyHealth>();
            // NavMeshAgent (AI Navigation): chỉ tìm đường + né nhau, vị trí do Rigidbody2D — EnemyMotor cấu hình & bật lúc spawn.
            var navAgent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;
            navAgent.radius = EnemyNavigation.AgentRadius;
            navAgent.height = EnemyNavigation.AgentHeight;
            navAgent.enabled = false;
            root.AddComponent<EnemyMotor>();
            root.AddComponent<EnemyBrain>();
            root.AddComponent<EnemyAttackRunner>();
            root.AddComponent<StatusEffectController>();
            root.AddComponent<PooledObject>();
            var enemy = root.AddComponent<Enemy>();

            // Visual
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(root.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 0;
            var anim = visualGo.AddComponent<Animator>();
            var animDriver = visualGo.AddComponent<SpriteAnimDriver>();
            var soDriver = new SerializedObject(animDriver);
            soDriver.FindProperty("animator").objectReferenceValue = anim;
            soDriver.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soDriver.FindProperty("facesRight").boolValue = true;
            soDriver.ApplyModifiedPropertiesWithoutUndo();

            // Shadow
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sortingLayerName = "Shadows";
            shadowSr.color = new Color(0f, 0f, 0f, 0.4f);

            // Hurtbox
            var hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = LayerMask.NameToLayer("EnemyHurtbox");
            var hurtCol = hurtGo.AddComponent<CircleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.radius = 0.45f;

            // AttackOrigin & StatusFx
            var attackOriginGo = new GameObject("AttackOrigin");
            attackOriginGo.transform.SetParent(root.transform, false);

            var statusFxGo = new GameObject("StatusFx");
            statusFxGo.transform.SetParent(root.transform, false);
            statusFxGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            // Link Enemy
            var soEnemy = new SerializedObject(enemy);
            soEnemy.FindProperty("attackOrigin").objectReferenceValue = attackOriginGo.transform;
            soEnemy.FindProperty("anim").objectReferenceValue = animDriver;
            var disableArray = soEnemy.FindProperty("disableOnDeath");
            disableArray.arraySize = 2;
            disableArray.GetArrayElementAtIndex(0).objectReferenceValue = col;
            disableArray.GetArrayElementAtIndex(1).objectReferenceValue = hurtCol;
            soEnemy.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, EnemyBasePath);
            Object.DestroyImmediate(root);

            // 4.2 Tạo Animator Controller cho Skull (từ skull_v1_1..4 và skull_v2_1..4)
            string skullCtrlPath = BuildSkullAnimatorController();

            // 4.3 Cập nhật 5 Enemy Prefabs thường
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Skeleton_Spear.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-skeleton1.controller", "skeleton-spear", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Skeleton_Warrior.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-skeleton2.controller", "skeleton-warrior", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Vampire.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-vampire.controller", "vampire", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Zombie.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/1Zombie.controller", "zombie", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Skull.prefab",
                skullCtrlPath, "skull", 1f, false);

            // 4.4 Tạo 3 Boss Prefabs (Boss_EliteWarrior, Boss_EliteVampire, Boss_VampireLord)
            ConfigureBossVariant("Assets/Project/Prefabs/Enemies/Boss_EliteWarrior.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-skeleton2.controller",
                "elite-warrior", 1.65f, new Color(1f, 0.85f, 0.75f, 1f),
                "Assets/Project/Prefabs/VFX/Explosions/VFX_Explosions_FireNova.prefab");

            ConfigureBossVariant("Assets/Project/Prefabs/Enemies/Boss_EliteVampire.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-vampire.controller",
                "elite-vampire", 1.65f, new Color(0.9f, 0.75f, 1f, 1f),
                "Assets/Project/Prefabs/VFX/Shadow/VFX_Shadow_VoidImplosion.prefab");

            ConfigureBossVariant("Assets/Project/Prefabs/Enemies/Boss_VampireLord.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-vampire.controller",
                "vampire-lord", 2.1f, new Color(1f, 0.65f, 0.7f, 1f),
                "Assets/Project/Prefabs/VFX/Explosions/VFX_Explosions_ArcaneBurst.prefab");
        }

        static string BuildSkullAnimatorController()
        {
            const string dir = "Assets/Project/Prefabs/Enemies/Animations/";
            const string ctrlPath = dir + "enemies-skull.controller";
            var v1 = new List<Sprite>();
            var v2 = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                var s1 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Art/Sprites/Enemies/DungeonMonsters/skull/v1/skull_v1_{i}.png");
                var s2 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Art/Sprites/Enemies/DungeonMonsters/skull/v2/skull_v2_{i}.png");
                if (s1 != null) v1.Add(s1);
                if (s2 != null) v2.Add(s2);
            }
            if (v1.Count == 0) return ctrlPath;
            if (v2.Count == 0) v2.AddRange(v1);

            AnimationClip Make(string name, List<Sprite> frames, float fps, bool loop)
            {
                string p = dir + name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                bool isNew = clip == null;
                if (isNew) clip = new AnimationClip { name = name };
                clip.frameRate = fps;
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var keys = new ObjectReferenceKeyframe[frames.Count + 1];
                for (int i = 0; i < frames.Count; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
                keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / fps, value = frames[^1] };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                if (isNew) AssetDatabase.CreateAsset(clip, p);
                else EditorUtility.SetDirty(clip);
                return clip;
            }

            var idle = Make("enemies-skull_idle", v1, 8f, true);
            var move = Make("enemies-skull_movement", v1, 10f, true);
            var atk = Make("enemies-skull_attack", v2, 12f, false);
            var hurt = Make("enemies-skull_take_damage", v2.Take(2).ToList(), 10f, false);
            var death = Make("enemies-skull_death", v2, 8f, false);

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                var sm = ctrl.layers[0].stateMachine;
                foreach (var c in new[] { idle, move, atk, hurt, death })
                {
                    var st = sm.AddState(c.name);
                    st.motion = c;
                    if (c == idle) sm.defaultState = st;
                }
            }
            return ctrlPath;
        }

        static void ConfigureEnemyVariant(string prefabPath, string controllerPath, string enemyId, float scale, bool isBoss)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePath);
            if (basePrefab == null) return;

            var go = Object.Instantiate(basePrefab);
            go.name = Path.GetFileNameWithoutExtension(prefabPath);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var anim = go.GetComponentInChildren<Animator>();
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (anim != null && ctrl != null) anim.runtimeAnimatorController = ctrl;

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (ctrl != null && ctrl.animationClips.Length > 0 && sr != null)
            {
                var clip = ctrl.animationClips[0];
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                if (bindings.Length > 0)
                {
                    var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                    if (keyframes != null && keyframes.Length > 0 && keyframes[0].value is Sprite s)
                        sr.sprite = s;
                }
            }

            // Gán EnemyData nếu đã có trong Assets/Project/Data/Enemies/
            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/Enemy_{enemyId}.asset");
            var enemyComp = go.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                var so = new SerializedObject(enemyComp);
                so.FindProperty("isBoss").boolValue = isBoss;
                if (enemyData != null) so.FindProperty("data").objectReferenceValue = enemyData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static void ConfigureBossVariant(string prefabPath, string controllerPath, string bossId, float scale, Color tint, string vfxPath)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePath);
            if (basePrefab == null) return;

            var go = Object.Instantiate(basePrefab);
            go.name = Path.GetFileNameWithoutExtension(prefabPath);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // Boss không qua PoolManager (ENTITIES §3.1)
            var pooled = go.GetComponent<PooledObject>();
            if (pooled != null) Object.DestroyImmediate(pooled);

            var anim = go.GetComponentInChildren<Animator>();
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (anim != null && ctrl != null) anim.runtimeAnimatorController = ctrl;

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = tint;
                if (ctrl != null && ctrl.animationClips.Length > 0)
                {
                    var clip = ctrl.animationClips[0];
                    var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    if (bindings.Length > 0)
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                        if (keyframes != null && keyframes.Length > 0 && keyframes[0].value is Sprite s)
                            sr.sprite = s;
                    }
                }
            }

            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/Enemy_{bossId}.asset");
            var enemyComp = go.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                var so = new SerializedObject(enemyComp);
                so.FindProperty("isBoss").boolValue = true;
                if (enemyData != null) so.FindProperty("data").objectReferenceValue = enemyData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var phaseCtrl = go.AddComponent<BossPhaseController>();
            var transVfx = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
            var soPhase = new SerializedObject(phaseCtrl);
            soPhase.FindProperty("bossId").stringValue = bossId;
            var phasesProp = soPhase.FindProperty("phases");
            phasesProp.arraySize = 2;

            var p0 = phasesProp.GetArrayElementAtIndex(0);
            p0.FindPropertyRelative("hpThreshold").floatValue = 0.6f;
            p0.FindPropertyRelative("speedMultiplier").floatValue = 1.25f;
            p0.FindPropertyRelative("transitionInvulnerable").floatValue = 0.8f;
            if (transVfx != null) p0.FindPropertyRelative("transitionVfx").objectReferenceValue = transVfx;

            var p1 = phasesProp.GetArrayElementAtIndex(1);
            p1.FindPropertyRelative("hpThreshold").floatValue = 0.3f;
            p1.FindPropertyRelative("speedMultiplier").floatValue = 1.5f;
            p1.FindPropertyRelative("transitionInvulnerable").floatValue = 1.0f;
            if (transVfx != null) p1.FindPropertyRelative("transitionVfx").objectReferenceValue = transVfx;

            // Nạp đòn tấn công nâng cao cho phase 2 của Vampire Lord nếu đã có
            if (bossId == "vampire-lord")
            {
                var spiral = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-spiral.asset");
                var summon = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-summon.asset");
                var fan = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-fan.asset");
                var charge = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-charge.asset");
                if (fan != null && charge != null && summon != null)
                {
                    var a0 = p0.FindPropertyRelative("attacks");
                    a0.arraySize = 3;
                    a0.GetArrayElementAtIndex(0).objectReferenceValue = fan;
                    a0.GetArrayElementAtIndex(1).objectReferenceValue = charge;
                    a0.GetArrayElementAtIndex(2).objectReferenceValue = summon;
                }
                if (spiral != null && fan != null && charge != null && summon != null)
                {
                    var a1 = p1.FindPropertyRelative("attacks");
                    a1.arraySize = 4;
                    a1.GetArrayElementAtIndex(0).objectReferenceValue = spiral;
                    a1.GetArrayElementAtIndex(1).objectReferenceValue = fan;
                    a1.GetArrayElementAtIndex(2).objectReferenceValue = charge;
                    a1.GetArrayElementAtIndex(3).objectReferenceValue = summon;
                }
            }

            soPhase.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }
    }
}

using DungeonGathering.Play;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Dựng camera của Play.unity (CODE_STRUCTURE §3): Main Camera + CinemachineBrain; CameraRig → VirtualCamera
    /// (CinemachineCamera + PositionComposer bám Player + Confiner2D theo CameraBounds của phòng). Nối vào LevelDirector.cameraRig.
    /// Chạy lại được: thành phần đã có thì chỉ cập nhật.
    /// </summary>
    public static class PlayCameraRigSetup
    {
        const string ScenePath = "Assets/Project/Scenes/Play.unity";

        [MenuItem("Tools/Dungeon Gathering/Play/Setup Camera Rig")]
        public static void Setup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (cam == null) { Debug.LogError("[Camera] Play.unity không có Camera."); return; }
            if (!cam.TryGetComponent<CinemachineBrain>(out _)) cam.gameObject.AddComponent<CinemachineBrain>();

            var rig = Object.FindFirstObjectByType<CameraRig>();
            if (rig == null) rig = new GameObject("CameraRig").AddComponent<CameraRig>();

            var vcamT = rig.transform.Find("VirtualCamera");
            if (vcamT == null) { vcamT = new GameObject("VirtualCamera").transform; vcamT.SetParent(rig.transform, false); }
            vcamT.localPosition = new Vector3(0f, 0f, -10f);

            if (!vcamT.TryGetComponent<CinemachineCamera>(out var vcam)) vcam = vcamT.gameObject.AddComponent<CinemachineCamera>();
            var lens = vcam.Lens;
            lens.OrthographicSize = cam.orthographic ? cam.orthographicSize : 5f;
            lens.NearClipPlane = cam.nearClipPlane;
            lens.FarClipPlane = cam.farClipPlane;
            vcam.Lens = lens;

            if (!vcamT.TryGetComponent<CinemachinePositionComposer>(out var composer)) composer = vcamT.gameObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.25f, 0.25f, 0f);

            if (!vcamT.TryGetComponent<CinemachineConfiner2D>(out var confiner)) confiner = vcamT.gameObject.AddComponent<CinemachineConfiner2D>();

            var rso = new SerializedObject(rig);
            rso.FindProperty("virtualCamera").objectReferenceValue = vcam;
            rso.FindProperty("confiner").objectReferenceValue = confiner;
            rso.FindProperty("mainCamera").objectReferenceValue = cam;
            rso.ApplyModifiedPropertiesWithoutUndo();

            var director = Object.FindFirstObjectByType<LevelDirector>();
            if (director != null)
            {
                var dso = new SerializedObject(director);
                dso.FindProperty("cameraRig").objectReferenceValue = rig;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Camera] Play.unity không có LevelDirector — chưa nối cameraRig.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Camera] Đã dựng CameraRig (Cinemachine) cho Play.unity và nối vào LevelDirector.");
        }
    }
}

// Положить в Assets/Editor/
// Runner -> Setup Full Project

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Animations;
using UnityEngine.EventSystems;

public class ProjectAutoSetup : EditorWindow
{
    [MenuItem("Runner/Setup Full Project")]
    static void SetupAll()
    {
        if (!EditorUtility.DisplayDialog("Auto Setup",
            "Это создаст папки, префабы, ScriptableObjects, 2 сцены и настроит Build Settings.\n\nПродолжить?",
            "Да", "Отмена"))
            return;

        CreateFolders();
        SetupTagsAndLayers();

        // префабы
        var obstacleLowPrefab = CreateObstaclePrefab("Obstacle_Low", new Vector3(3f, 0.5f, 0.5f),
            new Color(0.9f, 0.2f, 0.2f));
        var obstacleHighPrefab = CreateObstaclePrefab("Obstacle_High", new Vector3(1.5f, 2f, 1.5f),
            new Color(0.6f, 0.1f, 0.1f));
        var bonusPrefab = CreateBonusPrefab();

        // ScriptableObjects
        var obstDataLow = CreateObstacleData("ObstacleData_Low", obstacleLowPrefab, 10);
        var obstDataHigh = CreateObstacleData("ObstacleData_High", obstacleHighPrefab, 25);
        var bonusHeal = CreateBonusData("BonusData_Heal", BonusType.Heal, 30f, 0f, Color.green);
        var bonusSpeed = CreateBonusData("BonusData_SpeedBoost", BonusType.SpeedBoost, 1.5f, 4f, Color.yellow);
        var bonusInv = CreateBonusData("BonusData_Invincibility", BonusType.Invincibility, 0f, 5f, Color.cyan);

        // Animator
        var animController = CreatePlayerAnimator();

        // Ground material (понадобится в spawner тоже)
        var groundMat = CreateMat("Mat_Ground", new Color(0.35f, 0.35f, 0.35f));

        SetupGameScene(obstDataLow, obstDataHigh, bonusHeal, bonusSpeed, bonusInv,
            bonusPrefab, animController, groundMat);

        SetupMainMenuScene();
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Готово!",
            "Проект собран.\n\n" +
            "1. Откройте сцену MainMenu\n" +
            "2. Нажмите Play\n\n" +
            "Или откройте Game для тестирования геймплея напрямую.", "OK");
    }

    static void CreateFolders()
    {
        string[] folders = {
            "Assets/Scripts",
            "Assets/Scripts/Editor",
            "Assets/Prefabs",
            "Assets/ScriptableObjects",
            "Assets/Scenes",
            "Assets/Materials",
            "Assets/Animation"
        };
        foreach (var f in folders)
        {
            if (!AssetDatabase.IsValidFolder(f))
            {
                var parts = f.Split('/');
                var parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var full = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = full;
                }
            }
        }
    }

    static void SetupTagsAndLayers()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");

        bool groundExists = false;
        for (int i = 6; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == "Ground")
            {
                groundExists = true;
                break;
            }
        }
        if (!groundExists)
        {
            for (int i = 6; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = "Ground";
                    break;
                }
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    static int GetGroundLayer()
    {
        for (int i = 6; i < 32; i++)
        {
            if (LayerMask.LayerToName(i) == "Ground")
                return i;
        }
        return 6;
    }

    static Material CreateMat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ============ Animator Controller для игрока ============

    static AnimatorController CreatePlayerAnimator()
    {
        string path = "Assets/Animation/PlayerAnimator.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);

        // параметры
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Bonus", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Invincible", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("SpeedBoosted", AnimatorControllerParameterType.Bool);

        // базовый слой уже есть, добавляем состояния
        var rootSM = ctrl.layers[0].stateMachine;

        var idle = rootSM.AddState("Run");
        rootSM.defaultState = idle;

        var hitState = rootSM.AddState("Hit");
        var bonusState = rootSM.AddState("BonusPickup");

        // переход в Hit
        var toHit = idle.AddTransition(hitState);
        toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        toHit.duration = 0.1f;
        toHit.hasExitTime = false;

        // обратно
        var fromHit = hitState.AddTransition(idle);
        fromHit.hasExitTime = true;
        fromHit.exitTime = 1f;
        fromHit.duration = 0.1f;

        // в BonusPickup
        var toBonus = idle.AddTransition(bonusState);
        toBonus.AddCondition(AnimatorConditionMode.If, 0, "Bonus");
        toBonus.duration = 0.1f;
        toBonus.hasExitTime = false;

        var fromBonus = bonusState.AddTransition(idle);
        fromBonus.hasExitTime = true;
        fromBonus.exitTime = 1f;
        fromBonus.duration = 0.1f;

        return ctrl;
    }

    // ============ Prefabs ============

    static GameObject CreateObstaclePrefab(string name, Vector3 scale, Color color)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.localScale = scale;

        var mat = CreateMat("Mat_" + name, color);
        obj.GetComponent<Renderer>().sharedMaterial = mat;

        obj.AddComponent<Obstacle>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateBonusPrefab()
    {
        string path = "Assets/Prefabs/BonusPickup.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = "BonusPickup";
        obj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        obj.GetComponent<SphereCollider>().isTrigger = true;
        obj.AddComponent<BonusPickup>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    // ============ ScriptableObjects ============

    static ObstacleData CreateObstacleData(string name, GameObject prefab, int damage)
    {
        string path = "Assets/ScriptableObjects/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<ObstacleData>();
        data.prefab = prefab;
        data.damage = damage;
        data.spd = 0f;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    static BonusData CreateBonusData(string name, BonusType type, float value, float duration, Color color)
    {
        string path = "Assets/ScriptableObjects/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<BonusData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<BonusData>();
        data.type = type;
        data.value = value;
        data.duration = duration;
        data.color = color;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    // ========================= GAME SCENE =========================

    static void SetupGameScene(ObstacleData obstLow, ObstacleData obstHigh,
        BonusData bonusHeal, BonusData bonusSpeed, BonusData bonusInv,
        GameObject bonusPrefab, AnimatorController animCtrl, Material groundMat)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        int groundLayer = GetGroundLayer();

        // --- Ground (два стартовых сегмента) ---
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, 0, 200);
        ground.transform.localScale = new Vector3(1, 1, 100);
        ground.layer = groundLayer;
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        var ground2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground2.name = "Ground_Far";
        ground2.transform.position = new Vector3(0, 0, 1200);
        ground2.transform.localScale = new Vector3(1, 1, 100);
        ground2.layer = groundLayer;
        ground2.GetComponent<Renderer>().sharedMaterial = groundMat;

        // --- Стены (невидимые) ---
        for (int side = -1; side <= 1; side += 2)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall_" + (side < 0 ? "Left" : "Right");
            wall.transform.position = new Vector3(side * 5.5f, 1f, 600);
            wall.transform.localScale = new Vector3(0.5f, 3f, 2500f);
            wall.GetComponent<Renderer>().enabled = false;
        }

        // --- Player ---
        var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 0.5f, 0);

        var playerMat = CreateMat("Mat_Player", new Color(0.2f, 0.6f, 1f));
        player.GetComponent<Renderer>().sharedMaterial = playerMat;

        var rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var pc = player.AddComponent<PlayerController>();
        var pcSo = new SerializedObject(pc);
        var glProp = pcSo.FindProperty("groundLayer");
        if (glProp != null)
        {
            glProp.intValue = 1 << groundLayer;
            pcSo.ApplyModifiedProperties();
        }

        // вешаем аниматор
        var animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = animCtrl;

        // --- Camera ---
        var cam = Camera.main;
        if (cam != null)
        {
            var cf = cam.gameObject.AddComponent<CameraFollow>();
            cf.target = player.transform;
        }

        // --- GameManager ---
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // --- ScoreManager ---
        var smObj = new GameObject("ScoreManager");
        smObj.AddComponent<ScoreManager>();

        // --- Spawner ---
        var spawner = new GameObject("Spawner");
        var os = spawner.AddComponent<ObstacleSpawner>();

        var osSo = new SerializedObject(os);

        var obstTypes = osSo.FindProperty("obstacleTypes");
        obstTypes.arraySize = 2;
        obstTypes.GetArrayElementAtIndex(0).objectReferenceValue = obstLow;
        obstTypes.GetArrayElementAtIndex(1).objectReferenceValue = obstHigh;

        var bonTypes = osSo.FindProperty("bonusTypes");
        bonTypes.arraySize = 3;
        bonTypes.GetArrayElementAtIndex(0).objectReferenceValue = bonusHeal;
        bonTypes.GetArrayElementAtIndex(1).objectReferenceValue = bonusSpeed;
        bonTypes.GetArrayElementAtIndex(2).objectReferenceValue = bonusInv;

        var bpProp = osSo.FindProperty("bonusPrefab");
        bpProp.objectReferenceValue = bonusPrefab;

        // ground material для динамического спавна
        var gmProp = osSo.FindProperty("groundMaterial");
        if (gmProp != null)
            gmProp.objectReferenceValue = groundMat;

        osSo.ApplyModifiedProperties();

        // --- Directional Light ---
        var light = GameObject.Find("Directional Light");
        if (light != null)
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ========================= UI =========================

        CreateEventSystem();

        // --- HUD Canvas ---
        var hudCanvasObj = new GameObject("HUDCanvas");
        var hudCanvas = hudCanvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 0;
        hudCanvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        hudCanvasObj.AddComponent<GraphicRaycaster>();

        var hpBarObj = CreateUISlider(hudCanvasObj.transform, "HPBar",
            new Vector2(150, -30), new Vector2(200, 20), Color.green);

        var scoreTextObj = CreateUIText(hudCanvasObj.transform, "ScoreText",
            "Score: 0", 22, TextAnchor.UpperLeft, new Vector2(160, -55));

        var hsTextObj = CreateUIText(hudCanvasObj.transform, "HighScoreText",
            "Best: 0", 17, TextAnchor.UpperLeft, new Vector2(160, -80));

        var hud = hudCanvasObj.AddComponent<HUDController>();
        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("hpBar").objectReferenceValue = hpBarObj.GetComponent<Slider>();
        hudSo.FindProperty("scoreText").objectReferenceValue = scoreTextObj.GetComponent<Text>();
        hudSo.FindProperty("highScoreText").objectReferenceValue = hsTextObj.GetComponent<Text>();
        hudSo.ApplyModifiedProperties();

        // --- Pause Menu ---
        var pauseManagerObj = new GameObject("PauseManager");
        pauseManagerObj.transform.SetParent(hudCanvasObj.transform, false);

        var pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(pauseManagerObj.transform, false);
        var panelRT = pausePanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        var panelImg = pausePanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);

        CreateUIText(pausePanel.transform, "PauseTitle", "ПАУЗА", 40,
            TextAnchor.MiddleCenter, new Vector2(0, 60));

        var contBtn = CreateUIButton(pausePanel.transform, "ContinueBtn", "Продолжить",
            new Vector2(0, -10), new Vector2(200, 40));

        var menuBtn = CreateUIButton(pausePanel.transform, "MenuBtn", "В меню",
            new Vector2(0, -60), new Vector2(200, 40));

        var pm = pauseManagerObj.AddComponent<PauseMenu>();
        var pmSo = new SerializedObject(pm);
        pmSo.FindProperty("panel").objectReferenceValue = pausePanel;
        pmSo.FindProperty("continueBtn").objectReferenceValue = contBtn.GetComponent<Button>();
        pmSo.FindProperty("menuBtn").objectReferenceValue = menuBtn.GetComponent<Button>();
        pmSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    // ========================= MAIN MENU =========================

    static void SetupMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        CreateEventSystem();
        var canvasObj = new GameObject("MenuCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // фон
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        CreateUIText(canvasObj.transform, "TitleText", "CUBE RUNNER", 50,
            TextAnchor.MiddleCenter, new Vector2(0, 120));

        CreateUIText(canvasObj.transform, "AuthorText", "Чеботарь Игорь Иванович", 24,
            TextAnchor.MiddleCenter, new Vector2(0, 70));

        var hsText = CreateUIText(canvasObj.transform, "HighScoreText", "Рекорд: 0", 28,
            TextAnchor.MiddleCenter, new Vector2(0, 20));

        var playBtn = CreateUIButton(canvasObj.transform, "PlayBtn", "Играть",
            new Vector2(0, -40), new Vector2(200, 50));

        var quitBtn = CreateUIButton(canvasObj.transform, "QuitBtn", "Выход",
            new Vector2(0, -100), new Vector2(200, 50));

        var mmObj = new GameObject("MenuManager");
        var mm = mmObj.AddComponent<MainMenuUI>();
        var mmSo = new SerializedObject(mm);
        mmSo.FindProperty("highScoreText").objectReferenceValue = hsText.GetComponent<Text>();
        mmSo.FindProperty("playBtn").objectReferenceValue = playBtn.GetComponent<Button>();
        mmSo.FindProperty("quitBtn").objectReferenceValue = quitBtn.GetComponent<Button>();
        mmSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // ========================= BUILD SETTINGS =========================

    static void SetupBuildSettings()
    {
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
    }

    // ========================= HELPERS =========================

    static void CreateEventSystem()
    {
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();
    }

    static GameObject CreateUIText(Transform parent, string name, string text,
        int fontSize, TextAnchor anchor, Vector2 pos)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 60);

        var t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return obj;
    }

    static GameObject CreateUIButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f, 1f);
        obj.AddComponent<Button>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        var textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var t = textObj.AddComponent<Text>();
        t.text = label;
        t.fontSize = 22;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return obj;
    }

    static GameObject CreateUISlider(Transform parent, string name,
        Vector2 pos, Vector2 size, Color fillColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);

        var slider = obj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;
        slider.interactable = false;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(obj.transform, false);
        var bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;

        slider.fillRect = fillRT;

        return obj;
    }
}
#endif

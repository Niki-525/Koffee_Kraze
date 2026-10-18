// Scripts/Editor/CoffeeSceneBuilder.cs
// Menu: Coffee > Build Test Scene
// Creates placeholder icons, sample data assets, two prefabs, and a fully wired
// test scene (Assets/Scenes/CoffeeTest.unity). Safe to run repeatedly.
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class CoffeeSceneBuilder
{
    const string Root = "Assets/Coffee";
    const string DataDir = Root + "/Data";
    const string IconDir = Root + "/Generated/Icons";
    const string PrefabDir = Root + "/Generated/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/CoffeeTest.unity";

    static readonly Color PanelDark = new Color(0.16f, 0.11f, 0.09f, 0.96f);
    static readonly Color PanelMid = new Color(0.26f, 0.18f, 0.14f, 1f);
    static readonly Color Cream = new Color(0.93f, 0.86f, 0.74f, 1f);
    static readonly Color ButtonBrown = new Color(0.45f, 0.30f, 0.20f, 1f);
    static readonly Color ButtonGreen = new Color(0.25f, 0.55f, 0.30f, 1f);
    static readonly Color ButtonRed = new Color(0.65f, 0.25f, 0.22f, 1f);
    static readonly Color SelectedAccent = new Color(0.95f, 0.72f, 0.25f, 1f);

    static Sprite UiSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    // ------------------------------------------------------------------
    [MenuItem("Coffee/Delete Save File")]
    public static void DeleteSave()
    {
        SaveSystem.Delete();
        EditorUtility.DisplayDialog("Coffee", "Save file deleted. The next Play starts at Day 1.", "OK");
    }

    [MenuItem("Coffee/Report Ingredient Icons")]
    public static void ReportIcons()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:IngredientSO"))
        {
            var ing = AssetDatabase.LoadAssetAtPath<IngredientSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (ing == null) continue;
            string where = ing.icon == null ? "NONE" : AssetDatabase.GetAssetPath(ing.icon);
            string size = ing.icon == null ? "" : $" ({ing.icon.texture.width}x{ing.icon.texture.height})";
            Debug.Log($"[Icons] {ing.id}: {where}{size}", ing);
        }
    }

    [MenuItem("Coffee/Build Test Scene")]
    public static void Build()
    {
        if (Resources.Load("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("Coffee",
                "TextMeshPro essentials aren't imported yet. Unity will open the import window now. " +
                "Click \"Import TMP Essentials\", then run Coffee > Build Test Scene again.", "OK");
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolders();

        // ---------- data ----------
        var espresso = Ing("espresso", "Espresso", IngredientKind.Base, new Color(0.30f, 0.17f, 0.10f), 40, 1);
        var milk = Ing("milk", "Milk", IngredientKind.Milk, new Color(0.45f, 0.52f, 0.62f), 20, 1);
        var vanilla = Ing("vanilla", "Vanilla", IngredientKind.Syrup, new Color(0.70f, 0.58f, 0.25f), 15, 1);
        var caramel = Ing("caramel", "Caramel", IngredientKind.Syrup, new Color(0.72f, 0.42f, 0.12f), 15, 1);
        var chocolate = Ing("chocolate", "Chocolate", IngredientKind.Syrup, new Color(0.45f, 0.25f, 0.18f), 15, 1);
        var oat = Ing("oat_milk", "Oat Milk", IngredientKind.Milk, new Color(0.62f, 0.50f, 0.30f), 30, 2);
        var whipped = Ing("whipped_cream", "Whipped Cream", IngredientKind.Topping, new Color(0.50f, 0.62f, 0.70f), 10, 2);
        var cinnamon = Ing("cinnamon", "Cinnamon", IngredientKind.Topping, new Color(0.65f, 0.30f, 0.18f), 5, 2);

        var allIngredients = new List<IngredientSO>
            { espresso, milk, vanilla, caramel, chocolate, oat, whipped, cinnamon };

        foreach (var ing in allIngredients)
            if (ing == null || ing.icon == null)
                Debug.LogError($"[CoffeeSceneBuilder] Ingredient \"{(ing != null ? ing.id : "?")}\" has no icon sprite. " +
                               "Check that Assets/Coffee/Generated/Icons/icon_<id>.png exists and is imported as a Sprite.");

        var recipes = new List<DrinkRecipeSO>
        {
            Recipe("espresso", "Espresso", 300, espresso, null, new IngredientSO[0], new IngredientSO[0], 1),
            Recipe("latte", "Latte", 450, espresso, milk, new IngredientSO[0], new IngredientSO[0], 1),
            Recipe("vanilla_latte", "Vanilla Latte", 500, espresso, milk, new[] { vanilla }, new IngredientSO[0], 1),
            Recipe("caramel_latte", "Caramel Latte", 500, espresso, milk, new[] { caramel }, new IngredientSO[0], 1),
            Recipe("mocha", "Mocha", 525, espresso, milk, new[] { chocolate }, new IngredientSO[0], 1),
            Recipe("oat_latte", "Oat Latte", 525, espresso, oat, new IngredientSO[0], new IngredientSO[0], 2),
            Recipe("cappuccino", "Cappuccino", 475, espresso, milk, new IngredientSO[0], new[] { cinnamon }, 2),
            Recipe("mocha_deluxe", "Mocha Deluxe", 625, espresso, milk, new[] { chocolate }, new[] { whipped }, 2),
        };

        var customers = new List<CustomerSO>
        {
            Customer("maya", "Maya", 30f, 1.0f, "Friendly regular, always in a good mood.",
                new Color(0.35f, 0.55f, 0.80f)),
            Customer("okafor", "Mr. Okafor", 37.5f, 1.2f, "Patient retired teacher who tips well.",
                new Color(0.55f, 0.45f, 0.75f)),
            Customer("jun", "Jun", 25f, 1.0f, "Late for work and in a hurry.",
                new Color(0.30f, 0.65f, 0.55f)),
            Customer("priya", "Priya", 45f, 0.8f, "Chatty student, orders complicated drinks.",
                new Color(0.80f, 0.55f, 0.35f)),
        };

        // ---------- prefabs ----------
        var trayPrefab = MakeTrayItemPrefab();
        var cupIconPrefab = MakeCupIconPrefab();

        AssetDatabase.SaveAssets();

        // ---------- scene ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.10f, 0.07f, 0.06f);
        }

        // Event system (picks the right input module for the project).
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var module = es.AddComponent<InputSystemUIInputModule>();
        // Newer Input System versions throw from AssignDefaultActions() in the editor, so prefer the
        // project-wide actions asset (Edit > Project Settings > Input System Package) and only fall
        // back to the old call, inside a try/catch so the rest of the scene still builds.
        var projectActions = UnityEngine.InputSystem.InputSystem.actions;
        if (projectActions != null)
        {
            module.actionsAsset = projectActions;
        }
        else
        {
            try { module.AssignDefaultActions(); }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CoffeeSceneBuilder] Could not assign UI input actions (" + e.Message +
                    "). Select EventSystem in the Hierarchy and set Actions Asset on the Input System UI Input Module, " +
                    "or create project-wide actions in Project Settings > Input System Package.");
            }
        }
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        // Managers
        var managers = new GameObject("Managers");
        var economy = Child(managers, "EconomyManager").AddComponent<EconomyManager>();
        var takeover = Child(managers, "TakeoverManager").AddComponent<TakeoverManager>();
        var orders = Child(managers, "OrderManager").AddComponent<OrderManager>();
        var register = Child(managers, "RegisterManager").AddComponent<RegisterManager>();
        var gm = Child(managers, "GameManager").AddComponent<GameManager>();

        orders.recipes = recipes;
        orders.customers = customers;
        register.mode = RegisterManager.Mode.KeyboardOnly;   // flip to SerialAndKeyboard when you have the hardware

        // Canvas
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var ct = canvasGo.transform;

        var bg = Panel(ct, "Background", new Color(0.12f, 0.09f, 0.07f), 0, 0, 1, 1);
        bg.raycastTarget = false;

        var hud = canvasGo.AddComponent<HudController>();
        BuildHud(ct, hud);

        var area = NewUI("StationArea", ct);
        Place(area, 0f, 0.09f, 1f, 0.91f);

        var assembly = BuildAssembly(area.transform, allIngredients, trayPrefab, cupIconPrefab);
        var checkout = BuildCheckout(area.transform);

        BuildOverlays(ct, hud);

        // Wire the game manager.
        gm.stations = new List<Station> { assembly, checkout };
        gm.orders = orders;
        gm.takeover = takeover;
        gm.economy = economy;
        gm.useLlmStrategist = false;

        // Stations start hidden; GameManager shows them in turn.
        assembly.gameObject.SetActive(false);
        checkout.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = gm.gameObject;

        EditorUtility.DisplayDialog("Coffee",
            "Test scene built and saved to " + ScenePath + ".\n\n" +
            "Press Play. Drag ingredients into the cup, press Serve, then type the change on the keypad " +
            "(on-screen or keyboard: digits, Enter) in cents, e.g. 5 5 0 = $5.50.\n\n" +
            "If you want a fresh start, use Coffee > Delete Save File.", "OK");
    }

    // ==================================================================
    // HUD
    // ==================================================================
    static void BuildHud(Transform canvas, HudController hud)
    {
        var top = Panel(canvas, "TopBar", PanelDark, 0f, 0.91f, 1f, 1f);
        var t = top.transform;

        hud.dayText = Label(t, "DayText", "Day 1", 44, Cream, TextAlignmentOptions.MidlineLeft, 0.01f, 0, 0.12f, 1);
        hud.moneyText = Label(t, "MoneyText", "$0.00", 44, new Color(0.6f, 0.9f, 0.5f), TextAlignmentOptions.Midline, 0.13f, 0, 0.28f, 1);
        hud.pointsText = Label(t, "PointsText", "0 pts", 40, Cream, TextAlignmentOptions.Midline, 0.29f, 0, 0.42f, 1);
        hud.dayTimerText = Label(t, "DayTimerText", "180s left today", 32, Cream, TextAlignmentOptions.Midline, 0.43f, 0, 0.62f, 1);
        Label(t, "TakeoverLabel", "Takeover risk", 30, new Color(1f, 0.6f, 0.5f), TextAlignmentOptions.MidlineRight, 0.63f, 0, 0.75f, 1);
        hud.takeoverSlider = MakeSlider(t, "TakeoverSlider", 0.76f, 0.25f, 0.99f, 0.75f);

        var bottom = Panel(canvas, "BottomBar", PanelDark, 0f, 0f, 1f, 0.09f);
        hud.rivalSpeechText = Label(bottom.transform, "RivalSpeech", "Rival: ...", 30, new Color(1f, 0.7f, 0.6f), TextAlignmentOptions.MidlineLeft, 0.01f, 0, 0.60f, 1);
        hud.lastResultText = Label(bottom.transform, "LastResult", "", 30, Cream, TextAlignmentOptions.MidlineRight, 0.60f, 0, 0.99f, 1);
    }

    static Slider MakeSlider(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var res = new DefaultControls.Resources
        {
            standard = UiSprite,
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };
        var go = DefaultControls.CreateSlider(res);
        go.name = name;
        go.transform.SetParent(parent, false);
        Place(go, x0, y0, x1, y1);

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 0.3f;

        // Display-only: remove the handle and fill the full height.
        var handle = go.transform.Find("Handle Slide Area");
        if (handle != null) Object.DestroyImmediate(handle.gameObject);
        slider.handleRect = null;
        slider.targetGraphic = null;
        slider.interactable = false;

        var bg = go.transform.Find("Background") as RectTransform;
        if (bg != null)
        {
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f);
        }
        var fillArea = go.transform.Find("Fill Area") as RectTransform;
        if (fillArea != null)
        {
            fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;
        }
        var fill = go.transform.Find("Fill Area/Fill") as RectTransform;
        if (fill != null)
        {
            fill.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.20f);
        }
        return slider;
    }

    // ==================================================================
    // Assembly station
    // ==================================================================
    static AssemblyStation BuildAssembly(Transform parent, List<IngredientSO> all,
        DraggableIngredient trayPrefab, Image cupIconPrefab)
    {
        var root = NewUI("AssemblyPanel", parent);
        Stretch(root);
        var station = root.AddComponent<AssemblyStation>();
        var rt = root.transform;

        // Order ticket
        var ticket = Panel(rt, "OrderTicket", PanelMid, 0.02f, 0.06f, 0.30f, 0.96f);
        station.orderText = Label(ticket.transform, "OrderText", "Order", 30, Cream,
            TextAlignmentOptions.TopLeft, 0.04f, 0.32f, 0.96f, 0.97f);
        station.timerText = Label(ticket.transform, "TimerText", "60s", 72, SelectedAccent,
            TextAlignmentOptions.Center, 0.04f, 0.04f, 0.96f, 0.30f);

        // Cup
        var cupPanel = NewUI("CupArea", rt);
        Place(cupPanel, 0.33f, 0.06f, 0.67f, 0.96f);
        var cpt = cupPanel.transform;

        var cupImg = Panel(cpt, "CupZone", Cream, 0.03f, 0.26f, 0.97f, 1.0f);
        var zone = cupImg.gameObject.AddComponent<DropZone>();
        station.cupZone = zone;
        Label(cupImg.transform, "DropHint", "Drop ingredients here", 28, new Color(0.45f, 0.35f, 0.25f, 0.6f),
            TextAlignmentOptions.Center, 0, 0, 1, 1);

        var contents = NewUI("CupContents", cupImg.transform, typeof(GridLayoutGroup));
        Stretch(contents, 12f);
        var grid = contents.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 100);
        grid.spacing = new Vector2(8, 8);
        grid.childAlignment = TextAnchor.UpperCenter;
        station.cupContentsRoot = contents.transform;
        station.cupIconPrefab = cupIconPrefab;

        // Size buttons (Small, Medium, Large)
        station.sizeButtons = new Button[3];
        string[] sizeNames = { "Small", "Medium", "Large" };
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.03f + i * 0.325f;
            station.sizeButtons[i] = MakeButton(cpt, "Size_" + sizeNames[i], sizeNames[i], 30,
                ButtonBrown, x0, 0.14f, x0 + 0.30f, 0.24f);
        }

        station.undoButton = MakeButton(cpt, "UndoButton", "Undo", 30, ButtonBrown, 0.03f, 0.01f, 0.30f, 0.12f);
        station.clearButton = MakeButton(cpt, "ClearButton", "Clear", 30, ButtonRed, 0.33f, 0.01f, 0.60f, 0.12f);
        station.serveButton = MakeButton(cpt, "ServeButton", "Serve", 34, ButtonGreen, 0.63f, 0.01f, 0.97f, 0.12f);

        // Tray
        var tray = Panel(rt, "TrayPanel", PanelMid, 0.70f, 0.06f, 0.98f, 0.96f);
        Label(tray.transform, "TrayTitle", "Ingredients", 34, Cream, TextAlignmentOptions.Center, 0, 0.90f, 1, 1f);
        var trayRoot = NewUI("TrayRoot", tray.transform, typeof(GridLayoutGroup));
        Place(trayRoot, 0.03f, 0.02f, 0.97f, 0.89f);
        var tg = trayRoot.GetComponent<GridLayoutGroup>();
        tg.cellSize = new Vector2(150, 150);
        tg.spacing = new Vector2(12, 12);
        tg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tg.constraintCount = 3;
        tg.childAlignment = TextAnchor.UpperCenter;
        station.trayRoot = trayRoot.transform;
        station.trayItemPrefab = trayPrefab;
        station.allIngredients = all;

        return station;
    }

    // ==================================================================
    // Checkout station
    // ==================================================================
    static CheckoutStation BuildCheckout(Transform parent)
    {
        var root = NewUI("CheckoutPanel", parent);
        Stretch(root);
        var station = root.AddComponent<CheckoutStation>();
        var rt = root.transform;

        var info = Panel(rt, "InfoPanel", PanelMid, 0.04f, 0.58f, 0.96f, 0.96f);
        station.infoText = Label(info.transform, "InfoText", "Total", 44, Cream, TextAlignmentOptions.Center, 0, 0, 1, 1);

        var entry = Panel(rt, "EntryDisplay", new Color(0.05f, 0.08f, 0.05f, 1f), 0.04f, 0.34f, 0.46f, 0.52f);
        station.entryText = Label(entry.transform, "EntryText", "$0.00", 84, new Color(0.5f, 1f, 0.5f),
            TextAlignmentOptions.MidlineRight, 0, 0, 1, 1);

        station.feedbackText = Label(rt, "FeedbackText", "", 34, SelectedAccent,
            TextAlignmentOptions.TopLeft, 0.04f, 0.12f, 0.46f, 0.32f);

        // Keypad
        var padGo = NewUI("Keypad", rt, typeof(GridLayoutGroup), typeof(RegisterUIKeypad));
        Place(padGo, 0.52f, 0.02f, 0.96f, 0.54f);
        var pad = padGo.GetComponent<RegisterUIKeypad>();
        var pg = padGo.GetComponent<GridLayoutGroup>();
        pg.cellSize = new Vector2(240, 95);
        pg.spacing = new Vector2(12, 10);
        pg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        pg.constraintCount = 3;
        pg.childAlignment = TextAnchor.MiddleCenter;

        string[] keys = { "7", "8", "9", "4", "5", "6", "1", "2", "3", "C", "0", "E" };
        foreach (var k in keys)
        {
            Color c = k == "C" ? ButtonRed : (k == "E" ? ButtonGreen : ButtonBrown);
            string text = k == "E" ? "ENTER" : (k == "C" ? "CLEAR" : k);
            var b = MakeButton(padGo.transform, "Key_" + k, text, 40, c, 0, 0, 1, 1);
            // The grid layout controls the size; anchors set above are ignored.
            UnityEventTools.AddStringPersistentListener(b.onClick, pad.Press, k);
        }

        var drawer = MakeButton(rt, "DrawerButton", "Open drawer (Space)", 30, ButtonBrown, 0.04f, 0.02f, 0.46f, 0.11f);
        UnityEventTools.AddPersistentListener(drawer.onClick, pad.PressDrawer);

        return station;
    }

    // ==================================================================
    // Overlays: day summary, buyout offer, game over
    // ==================================================================
    static void BuildOverlays(Transform canvas, HudController hud)
    {
        // Day summary
        var dim = Panel(canvas, "DaySummaryPanel", new Color(0f, 0f, 0f, 0.65f), 0, 0, 1, 1);
        var box = Panel(dim.transform, "Box", PanelMid, 0.30f, 0.22f, 0.70f, 0.78f);
        hud.daySummaryText = Label(box.transform, "SummaryText", "Day complete", 44, Cream,
            TextAlignmentOptions.Center, 0.03f, 0.25f, 0.97f, 0.97f);
        hud.continueButton = MakeButton(box.transform, "ContinueButton", "Next day", 40, ButtonGreen,
            0.25f, 0.06f, 0.75f, 0.20f);
        hud.daySummaryPanel = dim.gameObject;

        // Buyout offer (non-blocking box, dismissable)
        var buy = Panel(canvas, "BuyoutPanel", new Color(0.35f, 0.10f, 0.10f, 0.97f), 0.30f, 0.30f, 0.70f, 0.70f);
        Label(buy.transform, "BuyoutText", "BUYOUT OFFER\n\nThe AI chain is offering to buy your cafe.\nKeep serving well to push them back.",
            36, Cream, TextAlignmentOptions.Center, 0.03f, 0.30f, 0.97f, 0.97f);
        var dismiss = MakeButton(buy.transform, "DismissButton", "Never", 36, ButtonRed, 0.30f, 0.06f, 0.70f, 0.24f);
        UnityEventTools.AddBoolPersistentListener(dismiss.onClick, buy.gameObject.SetActive, false);
        hud.buyoutPanel = buy.gameObject;

        // Game over
        var over = Panel(canvas, "GameOverPanel", new Color(0f, 0f, 0f, 0.85f), 0, 0, 1, 1);
        Label(over.transform, "GameOverText",
            "BOUGHT OUT\n\nThe last human-run cafe in town is gone.\n\nStop Play. Use Coffee > Delete Save File to start over.",
            52, Cream, TextAlignmentOptions.Center, 0.1f, 0.2f, 0.9f, 0.8f);
        hud.gameOverPanel = over.gameObject;
    }

    // ==================================================================
    // Prefabs
    // ==================================================================
    static DraggableIngredient MakeTrayItemPrefab()
    {
        // Tile background + icon on top + name on a dark strip underneath, so names stay readable on any art.
        var go = NewUI("TrayItem", null, typeof(Image), typeof(DraggableIngredient));
        ((RectTransform)go.transform).sizeDelta = new Vector2(150, 150);
        var bg = go.GetComponent<Image>();
        bg.sprite = UiSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.20f, 0.14f, 0.11f, 1f);
        bg.raycastTarget = true;               // the tile receives the drag

        var iconGo = NewUI("Icon", go.transform, typeof(Image));
        Place(iconGo, 0.06f, 0.26f, 0.94f, 0.96f);
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var strip = NewUI("LabelBg", go.transform, typeof(Image));
        Place(strip, 0f, 0f, 1f, 0.26f);
        var stripImg = strip.GetComponent<Image>();
        stripImg.sprite = UiSprite;
        stripImg.type = Image.Type.Sliced;
        stripImg.color = new Color(0f, 0f, 0f, 0.70f);
        stripImg.raycastTarget = false;

        var label = Label(strip.transform, "Label", "Item", 22, Color.white, TextAlignmentOptions.Center, 0, 0, 1, 1);
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 22;

        var drag = go.GetComponent<DraggableIngredient>();
        drag.label = label;
        drag.iconImage = icon;
        return SavePrefab<DraggableIngredient>(go, PrefabDir + "/TrayItem.prefab");
    }

    static Image MakeCupIconPrefab()
    {
        var go = NewUI("CupIcon", null, typeof(Image));
        ((RectTransform)go.transform).sizeDelta = new Vector2(100, 100);
        var img = go.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;

        var strip = NewUI("LabelBg", go.transform, typeof(Image));
        Place(strip, 0f, 0f, 1f, 0.24f);
        var stripImg = strip.GetComponent<Image>();
        stripImg.sprite = UiSprite;
        stripImg.type = Image.Type.Sliced;
        stripImg.color = new Color(0f, 0f, 0f, 0.70f);
        stripImg.raycastTarget = false;

        var label = Label(strip.transform, "Label", "Item", 18, Color.white, TextAlignmentOptions.Center, 0, 0, 1, 1);
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9;
        label.fontSizeMax = 18;
        return SavePrefab<Image>(go, PrefabDir + "/CupIcon.prefab");
    }

    static T SavePrefab<T>(GameObject go, string path) where T : Component
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<T>();
    }

    // ==================================================================
    // Data assets
    // ==================================================================
    static IngredientSO Ing(string id, string name, IngredientKind kind, Color color, int cost, int unlockDay)
    {
        string path = $"{DataDir}/Ingredients/{id}.asset";
        var icon = MakeSprite("icon_" + id, color);          // import textures before touching the asset
        var so = GetOrCreate<IngredientSO>(path);
        so.id = id;
        so.displayName = name;
        so.kind = kind;
        // Art in Assets/Coffee/Generated/Icons/icon_<id>.png always wins. (Placeholders are 64px; anything
        // bigger is your art.) A sprite you dragged in by hand is kept only while that file is still a placeholder.
        string current = so.icon != null ? AssetDatabase.GetAssetPath(so.icon) : "";
        bool pngIsRealArt = icon != null && icon.texture != null && icon.texture.width > 64;
        if (so.icon == null || current.StartsWith(IconDir) || pngIsRealArt) so.icon = icon;
        so.cost = cost;
        so.unlockDay = unlockDay;
        return SaveAndReload(so, path);
    }

    static DrinkRecipeSO Recipe(string id, string name, int price, IngredientSO baseIng, IngredientSO milk,
        IngredientSO[] syrups, IngredientSO[] toppings, int unlockDay)
    {
        string path = $"{DataDir}/Recipes/{id}.asset";
        var so = GetOrCreate<DrinkRecipeSO>(path);
        so.id = id;
        so.displayName = name;
        so.basePriceCents = price;
        so.baseIngredient = baseIng;
        so.milk = milk;
        so.syrups = new List<IngredientSO>(syrups);
        so.toppings = new List<IngredientSO>(toppings);
        so.unlockDay = unlockDay;
        if (baseIng == null) Debug.LogError($"[CoffeeSceneBuilder] Recipe {id} has no base ingredient.");
        return SaveAndReload(so, path);
    }

    static CustomerSO Customer(string id, string name, float patience, float tip, string personality, Color color)
    {
        string path = $"{DataDir}/Customers/{id}.asset";
        var neutral = MakeSprite($"cust_{id}_neutral", color);
        var happy = MakeSprite($"cust_{id}_happy", Color.Lerp(color, new Color(0.4f, 0.9f, 0.4f), 0.5f));
        var angry = MakeSprite($"cust_{id}_angry", Color.Lerp(color, new Color(0.9f, 0.2f, 0.2f), 0.5f));
        var so = GetOrCreate<CustomerSO>(path);
        so.id = id;
        so.displayName = name;
        so.patienceSeconds = patience;
        so.tipMultiplier = tip;
        so.personality = personality;
        so.neutral = neutral;
        so.happy = happy;
        so.angry = angry;
        return SaveAndReload(so, path);
    }

    /// <summary>Writes the asset to disk and returns the instance Unity loads from that file.</summary>
    static T SaveAndReload<T>(T so, string path) where T : ScriptableObject
    {
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        var loaded = AssetDatabase.LoadAssetAtPath<T>(path);
        if (loaded == null) Debug.LogError($"[CoffeeSceneBuilder] Could not load {path} after saving.");
        return loaded;
    }

    static T GetOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    // ==================================================================
    // Placeholder sprites
    // ==================================================================
    static Sprite MakeSprite(string fileName, Color fill)
    {
        string path = $"{IconDir}/{fileName}.png";
        if (!File.Exists(path))
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var border = Color.Lerp(fill, Color.black, 0.45f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < 4 || y < 4 || x >= size - 4 || y >= size - 4;
                    tex.SetPixel(x, y, edge ? border : fill);
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // Runs for new placeholders AND for art you dropped in by hand (a PNG with no .meta
        // file imports as a plain texture, which would make the sprite load as null).
        AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite
                                 || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ==================================================================
    // UI helpers
    // ==================================================================
    static void EnsureFolders()
    {
        Directory.CreateDirectory($"{DataDir}/Ingredients");
        Directory.CreateDirectory($"{DataDir}/Recipes");
        Directory.CreateDirectory($"{DataDir}/Customers");
        Directory.CreateDirectory(IconDir);
        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(SceneDir);
        AssetDatabase.Refresh();
    }

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject NewUI(string name, Transform parent, params System.Type[] extra)
    {
        var types = new System.Type[extra.Length + 1];
        types[0] = typeof(RectTransform);
        for (int i = 0; i < extra.Length; i++) types[i + 1] = extra[i];
        var go = new GameObject(name, types);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform Place(GameObject go, float x0, float y0, float x1, float y1, float inset = 0f)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    static void Stretch(GameObject go, float inset = 0f) => Place(go, 0, 0, 1, 1, inset);

    static Image Panel(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
    {
        var go = NewUI(name, parent, typeof(Image));
        Place(go, x0, y0, x1, y1);
        var img = go.GetComponent<Image>();
        img.sprite = UiSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, float x0, float y0, float x1, float y1)
    {
        var go = NewUI(name, parent, typeof(TextMeshProUGUI));
        Place(go, x0, y0, x1, y1, 6f);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string text, float fontSize, Color baseColor,
        float x0, float y0, float x1, float y1)
    {
        var go = NewUI(name, parent, typeof(Image), typeof(Button));
        Place(go, x0, y0, x1, y1);
        var img = go.GetComponent<Image>();
        img.sprite = UiSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.2f);
        cb.pressedColor = Color.Lerp(baseColor, Color.black, 0.25f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = SelectedAccent;      // used as the "selected size" look
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.05f;
        btn.colors = cb;

        var label = Label(go.transform, "Text", text, fontSize, Color.white, TextAlignmentOptions.Center, 0, 0, 1, 1);
        label.fontStyle = FontStyles.Bold;
        return btn;
    }
}

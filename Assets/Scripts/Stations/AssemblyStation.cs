// Scripts/Stations/AssemblyStation.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The build station: shows the order ticket, a tray of ingredients to drag,
/// and a cup to drop them into. Player presses Serve when finished.
/// </summary>
public class AssemblyStation : Station
{
    [Header("Data")]
    public List<IngredientSO> allIngredients = new();
    public int maxIngredients = 10;

    [Header("UI")]
    public DropZone cupZone;
    public Transform trayRoot;
    public DraggableIngredient trayItemPrefab;
    public Transform cupContentsRoot;
    public Image cupIconPrefab;
    public TMP_Text orderText;
    public TMP_Text timerText;
    public Button serveButton, undoButton, clearButton;
    [Tooltip("In order: Small, Medium, Large")]
    public Button[] sizeButtons;

    readonly List<GameObject> _trayItems = new();
    readonly List<GameObject> _cupIcons = new();
    bool _wired;

    void Awake() => WireButtons();

    void WireButtons()
    {
        if (_wired) return;
        _wired = true;

        if (serveButton) serveButton.onClick.AddListener(Complete);
        if (undoButton) undoButton.onClick.AddListener(Undo);
        if (clearButton) clearButton.onClick.AddListener(ClearAll);
        for (int i = 0; i < sizeButtons.Length; i++)
        {
            int idx = i;
            if (sizeButtons[i]) sizeButtons[i].onClick.AddListener(() => SetSize((CupSize)idx));
        }
    }

    protected override void OnEnter()
    {
        WireButtons();
        cupZone.IngredientDropped += OnIngredientDropped;

        BuildTray();
        RefreshCup();
        ShowTicket();
    }

    protected override void OnExit()
    {
        cupZone.IngredientDropped -= OnIngredientDropped;
    }

    void Update()
    {
        if (timerText != null && GameManager.Instance != null)
            timerText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, GameManager.Instance.OrderTimeRemaining))}s";
    }

    // ------------------------------------------------------------------
    void ShowTicket()
    {
        if (orderText == null) return;
        var names = Order.Expected().Select(i => i.displayName);
        orderText.text =
            $"<b>{Order.customer.displayName}</b>: \"{Order.dialogue}\"\n" +
            $"{Order.size} {Order.recipe.displayName}\n" +
            $"<size=80%>{string.Join(", ", names)}</size>";
    }

    void BuildTray()
    {
        foreach (var go in _trayItems) if (go) Destroy(go);
        _trayItems.Clear();

        int day = GameManager.Instance != null ? GameManager.Instance.Day : 1;
        foreach (var ing in allIngredients.Where(i => i != null && i.unlockDay <= day))
        {
            var item = Instantiate(trayItemPrefab, trayRoot);
            item.Setup(ing);
            _trayItems.Add(item.gameObject);
        }
    }

    void OnIngredientDropped(IngredientSO ing)
    {
        if (Drink.added.Count >= maxIngredients) return;
        Drink.added.Add(ing);
        RefreshCup();
    }

    void Undo()
    {
        if (Drink.added.Count == 0) return;
        Drink.added.RemoveAt(Drink.added.Count - 1);
        RefreshCup();
    }

    void ClearAll()
    {
        Drink.added.Clear();
        RefreshCup();
    }

    void SetSize(CupSize size)
    {
        Drink.size = size;
        for (int i = 0; i < sizeButtons.Length; i++)
            if (sizeButtons[i]) sizeButtons[i].interactable = i != (int)size;   // selected = disabled look
    }

    void RefreshCup()
    {
        foreach (var go in _cupIcons) if (go) Destroy(go);
        _cupIcons.Clear();

        foreach (var ing in Drink.added)
        {
            var icon = Instantiate(cupIconPrefab, cupContentsRoot);
            icon.sprite = ing.icon;
            icon.preserveAspect = true;
            var cupLabel = icon.GetComponentInChildren<TMP_Text>();
            if (cupLabel) cupLabel.text = ing.displayName;
            _cupIcons.Add(icon.gameObject);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject panel;          // Seçim paneli (aktif/pasif)
    public Transform gridParent;      // Grid Layout Group olan objenin Transform’u
    public Button buttonPrefab;       // Basit bir Button prefab (Text içeren)

    readonly List<Button> _buttons = new();

    void Start()
    {
        panel.SetActive(false);
        BuildButtons();
        RefreshButtonsInteractable();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            TogglePanel();
        }
        // ESC ile kapat
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            int target = LevelManager.I.maxLevelReached;
            LevelManager.I.ShowLevel(target);
            ClosePanel();
        }

    }

    void TogglePanel()
    {
        bool willOpen = !panel.activeSelf;
        panel.SetActive(willOpen);
        if (willOpen)
        {
            RefreshButtonsInteractable();
            // Ýstersen oyunu durdur:
            // Time.timeScale = 0f;
        }
        else
        {
            // Time.timeScale = 1f;
        }
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        // Time.timeScale = 1f;
    }

    void BuildButtons()
    {
        // Eski butonlarý temizle
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        _buttons.Clear();

        int levelCount = LevelManager.I.levels.Count;
        for (int i = 0; i < levelCount; i++)
        {
            int idx = i; // closure
            var btn = Instantiate(buttonPrefab, gridParent);
            _buttons.Add(btn);

            // Buton üzerindeki yazýyý ayarla
            var label = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = $"Level {idx + 1}";
            else
            {
                var legacy = btn.GetComponentInChildren<Text>();
                if (legacy != null) legacy.text = $"Level {idx + 1}";
            }

            btn.onClick.AddListener(() =>
            {
                LevelManager.I.ShowLevel(idx);
                ClosePanel();
            });
        }
    }

    void RefreshButtonsInteractable()
    {
        int unlockedMax = LevelManager.I.maxLevelReached; // 0-based
        for (int i = 0; i < _buttons.Count; i++)
        {
            bool unlocked = i <= unlockedMax;
            _buttons[i].interactable = unlocked;

            // Kilit görseli istiyorsan burada ekleyebilirsin:
            // _buttons[i].GetComponentInChildren<CanvasGroup>().alpha = unlocked ? 1f : 0.5f;
        }
    }

    // Ýlerleme deðiþince dýþarýdan çaðýrmak için:
    public void OnProgressChanged() => RefreshButtonsInteractable();
}

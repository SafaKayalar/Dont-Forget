using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class TutorialZone : MonoBehaviour
{
    [Header("Metin")]
    [TextArea(2, 5)] public string tutorialText;  // Inspector’dan gireceksin

    [Header("UI")]
    public GameObject panel;       // Panel objesi (içinde text var)
    public Text textLabel;         // Panelin child’ı olan Text

    [Header("Davranış")]
    [Tooltip("0 = bölgede kaldıkça açık; >0 = bu kadar sn sonra otomatik kapanır")]
    public float autoHideSeconds = 0f;
    public bool hideOnExit = true;
    public bool onlyOnce = false;

    float hideAt = -1f;
    bool used = false;
    bool isOpen = false;

    void Awake()
    {
        if (panel != null) panel.SetActive(false); // başta kapalı
    }

    void Update()
    {
        if (isOpen && autoHideSeconds > 0f && hideAt > 0f && Time.time >= hideAt)
            Close();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (used && onlyOnce) return;

        if (textLabel != null) textLabel.text = tutorialText;
        Open();

        if (onlyOnce) used = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (hideOnExit && autoHideSeconds <= 0f) Close();
    }

    void Open()
    {
        isOpen = true;
        if (panel != null) panel.SetActive(true);
        hideAt = (autoHideSeconds > 0f) ? Time.time + autoHideSeconds : -1f;
    }

    void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        hideAt = -1f;
    }
}

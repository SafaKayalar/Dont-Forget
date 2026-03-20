using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
// TMPro kullanýyorsan aç: using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Oyun sahnesi adý")]
    public string gameplaySceneName = "Level-1";

    [Header("UI")]
    [Tooltip("Continue butonun (interactable ve görünüm için)")]
    public Button continueButton;

    // Kayýt dosya ad(lar)ýn — projene göre artýrabilirsin
    readonly string[] _saveFileNames = { "gamestate.json" };

    void Start()
    {
        // Açýlýþta Continue durumunu güncelle
        UpdateContinueButtonState();
    }

    // === PLAY === (kaydýn hepsini sil + Level-1’e geç)
    public void PlayNewGame()
    {
        DeleteAnySave();
        SceneManager.LoadScene(gameplaySceneName);
    }

    // === CONTINUE === (kayýt varsa Level-1’e geç)
    public void ContinueGame()
    {
        if (!HasAnySave()) return;
        SceneManager.LoadScene(gameplaySceneName);
    }

    // === EXIT ===
    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ------ Helpers ------
    bool HasAnySave()
    {
        string root = Application.persistentDataPath;
        foreach (var name in _saveFileNames)
        {
            string p = Path.Combine(root, name);
            if (File.Exists(p)) return true;
        }
        return false;
    }

    void DeleteAnySave()
    {
        string root = Application.persistentDataPath;
        foreach (var name in _saveFileNames)
        {
            string p = Path.Combine(root, name);
            if (File.Exists(p))
            {
                try { File.Delete(p); } catch { /* yoksay */ }
            }
        }
    }

    void UpdateContinueButtonState()
    {
        bool hasSave = HasAnySave();

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
            SetButtonVisual(continueButton, hasSave); // yoksa biraz silik yap
        }
    }

    // Buton görselini hafif soldur (CanvasGroup varsa onu; yoksa Image/Text alpha’sýný ayarla)
    void SetButtonVisual(Button btn, bool enabled)
    {
        float a = enabled ? 1f : 0.5f;

        // CanvasGroup varsa
        if (btn.TryGetComponent<CanvasGroup>(out var cg))
        {
            cg.alpha = a;
        }
        else
        {
            // Arkaplan görüntüsü
            if (btn.image != null)
            {
                var c = btn.image.color; c.a = a; btn.image.color = c;
            }

            // Çocuk yazýlar (hem Text hem TMP_Text destekle)
            var text = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (text != null) { var c = text.color; c.a = a; text.color = c; }

#if TMP_PRESENT
            var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null) { var c2 = tmp.color; c2.a = a; tmp.color = c2; }
#endif
        }
    }
}

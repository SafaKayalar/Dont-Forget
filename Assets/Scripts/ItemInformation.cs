using UnityEngine;

[DisallowMultipleComponent]
public class ItemInformation : MonoBehaviour
{
    [Header("Kimlik Bilgisi")]
    public string itemId;   // benzersiz id

    [Header("Durum (debug için)")]
    public bool taken;

    void Start()
    {
        ApplyState();
    }

    // Aktif level adýný al (LevelManager yoksa boþ döner)
    private string GetLevelId()
    {
        return (LevelManager.I != null) ? LevelManager.I.CurrentLevelId : "";
    }

    // Kayýtlý level kökünün altýna al
    private void ReparentUnderLevel(string levelId)
    {
        if (LevelManager.I == null || string.IsNullOrEmpty(levelId)) return;
        int idx = LevelManager.I.FindIndexByName(levelId);
        if (idx >= 0 && idx < LevelManager.I.levels.Count && LevelManager.I.levels[idx] != null)
        {
            Transform levelRoot = LevelManager.I.levels[idx].transform;
            transform.SetParent(levelRoot, true); // world pos/rot koru
        }
    }

    private void ApplyState()
    {
        if (string.IsNullOrEmpty(itemId) || GameState.I == null) return;

        // --- JSON'dan tek seferde oku ---
        var st = GameState.I.GetState(itemId); // st.sceneName = levelId gibi kullanýlýyor
        string activeLevelId = GetLevelId();

        // 1) Kayýt yoksa: prefab haliyle devam
        if (st == null)
        {
            return;
        }

        // 2) Envanterdeyse gizle
        if (st.taken)
        {
            taken = true;
            gameObject.SetActive(false);
            return;
        }

        // 3) Kayýtlý level'in altýna reparent et ve poz/rot uygula
        string savedLevelId = st.sceneName; // burada levelId tutuluyor
        ReparentUnderLevel(savedLevelId);
        transform.SetPositionAndRotation(st.position, st.rotation);
        taken = false;

        // 4) Görünürlük: sadece aktif level kayýtlý level ise görünür
        bool shouldBeActive = (activeLevelId == savedLevelId);
        gameObject.SetActive(shouldBeActive);
        // Debug.Log($"Item {itemId}: Kayýt={savedLevelId}, Aktif={activeLevelId}, Active={shouldBeActive}");
    }

    public void MarkTaken()
    {
        if (GameState.I == null || string.IsNullOrEmpty(itemId)) return;

        GameState.I.MarkTaken(itemId);
        taken = true;
        gameObject.SetActive(false);
    }

    private void ReparentToCurrentLevel()
    {
        if (LevelManager.I == null) return;
        ReparentUnderLevel(LevelManager.I.CurrentLevelId);
    }

    public void MarkDropped(Vector3 pos, Quaternion rot)
    {
        if (GameState.I == null || string.IsNullOrEmpty(itemId)) return;

        string levelId = (LevelManager.I != null) ? LevelManager.I.CurrentLevelId : "";

        // Aktif level'in altýna al
        ReparentToCurrentLevel();

        // Konum/rotasyon + görünürlük
        transform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);

        // JSON'a yaz
        GameState.I.MarkDropped(itemId, levelId, pos, rot);
        taken = false;
    }


    private void OnDisable()
    {
        if (!Application.isPlaying || GameState.I == null) return;
        if (string.IsNullOrEmpty(itemId) || taken) return;

        // Parent bir level kökü ise onun adýný kullan; deðilse aktif level'i kullan
        string levelId = GetLevelId();
        if (LevelManager.I != null && transform.parent != null)
        {
            int pIdx = LevelManager.I.FindIndexByName(transform.parent.name);
            if (pIdx != -1) levelId = transform.parent.name;
        }

        GameState.I.MarkDropped(itemId, levelId, transform.position, transform.rotation);
    }

    // Level deðiþince çaðýr: sadece aktif/pasif kararýný tazeler
    public void RefreshForLevelChange()
    {
        if (GameState.I == null || string.IsNullOrEmpty(itemId)) return;

        var st = GameState.I.GetState(itemId); // st.sceneName = levelId gibi kullanýlýyor
        if (st == null)
        {
            // kayýt yoksa prefab hali: parent level açýk/kapalý hali zaten parent'tan miras alýr
            return;
        }

        if (st.taken)
        {
            gameObject.SetActive(false);
            return;
        }

        string currentLevelId = (LevelManager.I != null) ? LevelManager.I.CurrentLevelId : "";
        bool shouldBeActive = (currentLevelId == st.sceneName);
        gameObject.SetActive(shouldBeActive);
    }

}

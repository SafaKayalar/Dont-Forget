using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // ---- Singleton ----
    public static LevelManager I;

    [Header("Level Objeleri (Inspector’dan ekle)")]
    public List<GameObject> levels = new List<GameObject>();

    [Header("Level Bilgileri")]
    [Tooltip("Başlangıçta açılacak level indeksi")]
    public int currentLevelIndex = 0;          // aktif level
    [Tooltip("Oyuncunun ulaştığı en yüksek level indeksi")]
    public int maxLevelReached = 0;            // en fazla ulaşılan level

    // Level değişince Player spawn'a gitsin diye
    public event Action<int> OnLevelChanged;

    // Aktif level adı (GameObject adı)
    public string CurrentLevelId
    {
        get
        {
            if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count && levels[currentLevelIndex] != null)
                return levels[currentLevelIndex].name;
            return string.Empty;
        }
    }

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // Tek sahnede kalıcı istiyorsan aç:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // GameState’ten kayıtlı ilerlemeyi oku
        if (GameState.I != null && GameState.I.levelProgress != null)
        {
            currentLevelIndex = GameState.I.levelProgress.currentLevelIndex;
            maxLevelReached = GameState.I.levelProgress.maxLevelReached;
        }

        ClampIndices();
        ShowLevel(currentLevelIndex); // sahnede yalnızca tek level aktif kalsın
    }

    private void Update()
    {
        // Emniyet: maxLevel hep güncel kalsın
        if (currentLevelIndex > maxLevelReached)
            maxLevelReached = currentLevelIndex;
    }

    /// <summary>
    /// Verilen indeksteki leveli AÇAR, diğerlerini KAPATIR.
    /// Kamera noktası varsa kamerayı taşır, item görünürlüklerini yeniler,
    /// GameState’e ilerlemeyi kaydeder ve OnLevelChanged event’ını tetikler.
    /// </summary>
    public void ShowLevel(int index)
    {
        if (levels == null || levels.Count == 0) return;
        if (index < 0 || index >= levels.Count) return;

        // --- Tüm levelleri KAPAT ---
        for (int i = 0; i < levels.Count; i++)
        {
            var lv = levels[i];
            if (lv != null && lv.activeSelf) lv.SetActive(false);
        }

        // --- İstenen leveli AÇ ---
        var go = levels[index];
        if (go != null)
        {
            go.SetActive(true);
            currentLevelIndex = index;
            if (currentLevelIndex > maxLevelReached)
                maxLevelReached = currentLevelIndex;

            // Kamera noktası: aktif level altında "CameraPoint"
            var camPoint = GetCameraPoint();
            if (camPoint != null)
                MoveCameraToPoint(camPoint);
        }

        // --- Item görünürlüklerini güncelle ---
        var items = GameObject.FindObjectsOfType<ItemInformation>(true); // inaktif dahil
        foreach (var it in items) it.RefreshForLevelChange();

        // --- İlerlemeni GameState'e kaydet ---
        SaveProgressToGameState();

        // --- Oyuncuya haber ver (spawn'a yerleşsin) ---
        OnLevelChanged?.Invoke(currentLevelIndex);
    }

    /// <summary>Bir sonraki levele gider (varsa).</summary>
    public void NextLevel()
    {
        int next = currentLevelIndex + 1;
        if (next < levels.Count)
            ShowLevel(next);
    }

    /// <summary>Bir önceki levele gider (varsa).</summary>
    public void PrevLevel()
    {
        int prev = currentLevelIndex - 1;
        if (prev >= 0)
            ShowLevel(prev);
    }

    /// <summary>İndeks ile atla (kilit kontrolüyle). Kilitliyse false döner.</summary>
    public bool GoToLevel(int index, bool ignoreLock = false)
    {
        if (index < 0 || index >= levels.Count) return false;
        if (!ignoreLock && index > maxLevelReached) return false; // kilitli
        ShowLevel(index);
        return true;
    }

    /// <summary>Level GameObject adıyla geçiş yap.</summary>
    public bool SetLevelByName(string levelId, bool ignoreLock = false)
    {
        int idx = FindIndexByName(levelId);
        if (idx == -1) return false;
        return GoToLevel(idx, ignoreLock);
    }

    /// <summary>Level GameObject adından indeks bul.</summary>
    public int FindIndexByName(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return -1;
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null && levels[i].name == levelId)
                return i;
        }
        return -1;
    }

    // ----------------- Yardımcılar -----------------

    /// <summary>Aktif level GameObject’ini döner.</summary>
    public GameObject GetCurrentLevelGO()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count) return null;
        return levels[currentLevelIndex];
    }

    /// <summary>Aktif level altında "SpawnPoint" child’ını döner.</summary>
    public Transform GetCurrentSpawn()
    {
        var lvl = GetCurrentLevelGO();
        if (lvl == null) return null;

        // Önce child adına bak
        var t = lvl.transform.Find("SpawnPoint");
        if (t != null) return t;

        // Yedek: sahnede "SpawnPoint" tag’li varsa ve aktif level altındaysa onu al
        var tagged = GameObject.FindWithTag("SpawnPoint");
        if (tagged != null && tagged.transform.IsChildOf(lvl.transform))
            return tagged.transform;

        return null;
    }

    /// <summary>Aktif level altında "CameraPoint" child’ını döner.</summary>
    public Transform GetCameraPoint()
    {
        var lvl = GetCurrentLevelGO();
        if (lvl == null) return null;
        return lvl.transform.Find("CameraPoint");
    }

    private void MoveCameraToPoint(Transform p)
    {
        var cam = Camera.main;
        if (cam == null || p == null) return;

        var pos = p.position;
        // 2D ise Z’i koru
        pos.z = cam.transform.position.z;
        cam.transform.position = pos;
    }

    private void ClampIndices()
    {
        if (levels == null || levels.Count == 0)
        {
            currentLevelIndex = 0;
            maxLevelReached = 0;
            return;
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
        maxLevelReached = Mathf.Clamp(maxLevelReached, 0, Mathf.Max(currentLevelIndex, levels.Count - 1));
    }

    private void SaveProgressToGameState()
    {
        if (GameState.I == null || GameState.I.levelProgress == null) return;

        GameState.I.levelProgress.currentLevelIndex = currentLevelIndex;
        GameState.I.levelProgress.maxLevelReached = maxLevelReached;

        // Anında JSON’a yaz (istersen daha seyrek çağırabilirsin)
        GameState.I.SaveToFile();
    }
}

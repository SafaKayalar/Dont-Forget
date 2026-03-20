using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ItemStateV2
{
    public string itemId;
    public bool taken;          // true -> envanterde
    public string sceneName;    // taken == false ise: hangi sahnede?
    public Vector3 position;    // taken == false ise: son konum
    public Quaternion rotation; // taken == false ise: son rotasyon
}

[Serializable]
public class DoorState
{
    public string doorId;
    public bool isOpen;
}

[Serializable]
public class ButonnedDoor // butonun basýlý olup olmamasý (isPressed)
{
    public string doorId;
    public bool isPressed;
}

[Serializable]
public class LevelProgress
{
    public int currentLevelIndex;
    public int maxLevelReached;
}

/* -------- NEW: JewelryDoorState --------
 * Bir mücevher kapýsý için:
 * - isOpen       : Kapý açýk mý?
 * - stoneItemId  : Kapýnýn altýnda duracak taþýn itemId’si (envanter/sahne item sistemiyle ayný id)
 */
[Serializable]
public class JewelryDoorState
{
    public string doorId;
    public bool isOpen;
    public string stoneItemId; // boþ/ null olabilir
}

public class GameState : MonoBehaviour
{
    public static GameState I;

    // ---- RUNTIME HAFIZA ----
    private readonly Dictionary<string, ItemStateV2> _itemStates = new Dictionary<string, ItemStateV2>();
    private readonly Dictionary<string, DoorState> _doorStates = new Dictionary<string, DoorState>();
    private readonly List<ButonnedDoor> _butonnedDoorList = new List<ButonnedDoor>(); // JsonUtility uyumu için List

    // NEW: JewelryDoorState saklama (JsonUtility list seviyor, runtime’da dictionary’ye mapliyoruz)
    private readonly Dictionary<string, JewelryDoorState> _jewelryDoorStates = new Dictionary<string, JewelryDoorState>();

    // ---- LEVEL PROGRESS ----
    public LevelProgress levelProgress = new LevelProgress();

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "gamestate.json");

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        LoadFromFile();
    }

    // =========================================================
    //                       ITEM API
    // =========================================================
    public void MarkTaken(string itemId)
    {
        var st = GetOrCreateItem(itemId);
        st.taken = true;
        st.sceneName = null;
        SaveToFile();
    }

    public void MarkDropped(string itemId, string sceneName, Vector3 pos, Quaternion rot)
    {
        var st = GetOrCreateItem(itemId);
        st.taken = false;
        st.sceneName = sceneName;
        st.position = pos;
        st.rotation = rot;
        SaveToFile();
    }

    public bool IsTaken(string itemId)
        => _itemStates.TryGetValue(itemId, out var st) && st.taken;

    public bool ShouldExistInScene(string itemId, string sceneName, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = default;
        if (_itemStates.TryGetValue(itemId, out var st) && !st.taken && st.sceneName == sceneName)
        {
            pos = st.position;
            rot = st.rotation;
            return true;
        }
        return false;
    }

    public ItemStateV2 GetItemState(string itemId)
    {
        _itemStates.TryGetValue(itemId, out var st);
        return st;
    }

    // Eski çaðrýlar kýrýlmasýn diye alias
    public ItemStateV2 GetState(string itemId) => GetItemState(itemId);

    private ItemStateV2 GetOrCreateItem(string itemId)
    {
        if (!_itemStates.TryGetValue(itemId, out var st))
        {
            st = new ItemStateV2 { itemId = itemId, taken = false };
            _itemStates[itemId] = st;
        }
        return st;
    }

    // =========================================================
    //                      DOOR (isOpen) API
    // =========================================================
    public bool IsDoorOpen(string doorId)
        => _doorStates.TryGetValue(doorId, out var st) && st.isOpen;

    public void SetDoorOpen(string doorId, bool open)
    {
        if (!_doorStates.TryGetValue(doorId, out var st))
        {
            st = new DoorState { doorId = doorId, isOpen = open };
            _doorStates[doorId] = st;
        }
        else
        {
            st.isOpen = open;
        }
        SaveToFile();
    }

    // Eski kullaným uyumu:
    public void MarkDoorOpen(string doorId) => SetDoorOpen(doorId, true);

    // =========================================================
    //               BUTTONNED DOOR (isPressed) API
    // =========================================================
    public bool GetButonnedDoor(string doorId)
    {
        var st = _butonnedDoorList.Find(d => d.doorId == doorId);
        return st != null && st.isPressed;
    }

    public void SetButonnedDoor(string doorId, bool pressed)
    {
        var st = _butonnedDoorList.Find(d => d.doorId == doorId);
        if (st == null)
        {
            st = new ButonnedDoor { doorId = doorId, isPressed = pressed };
            _butonnedDoorList.Add(st);
        }
        else
        {
            st.isPressed = pressed;
        }
        SaveToFile();
    }

    // =========================================================
    //                 NEW: JEWELRY DOOR (open + stone) API
    // =========================================================
    private JewelryDoorState GetOrCreateJewelryDoor(string doorId)
    {
        if (!_jewelryDoorStates.TryGetValue(doorId, out var st))
        {
            st = new JewelryDoorState { doorId = doorId, isOpen = false, stoneItemId = null };
            _jewelryDoorStates[doorId] = st;
        }
        return st;
    }

    public bool IsJewelryDoorOpen(string doorId)
        => _jewelryDoorStates.TryGetValue(doorId, out var st) && st.isOpen;

    public void SetJewelryDoorOpen(string doorId, bool open, bool saveNow = true)
    {
        var st = GetOrCreateJewelryDoor(doorId);
        st.isOpen = open;
        if (saveNow) SaveToFile();
    }

    /// <summary>
    /// Kapýyla iliþkilendirilen taþýn itemId’sini sakla (kapý altýndaki taþ).
    /// Bunu kapýyý açtýðýn anda ya da editor/baþlangýçta set edebilirsin.
    /// </summary>
    public void SetJewelryDoorStone(string doorId, string stoneItemId, bool saveNow = true)
    {
        var st = GetOrCreateJewelryDoor(doorId);
        st.stoneItemId = stoneItemId;
        if (saveNow) SaveToFile();
    }

    /// <summary>
    /// Kapý altýnda hangi taþýn (itemId) beklenmesi gerektiðini öðren.
    /// </summary>
    public bool TryGetJewelryDoorStone(string doorId, out string stoneItemId)
    {
        stoneItemId = null;
        if (_jewelryDoorStates.TryGetValue(doorId, out var st))
        {
            stoneItemId = st.stoneItemId;
            return !string.IsNullOrEmpty(stoneItemId);
        }
        return false;
    }

    /// <summary>
    /// Convenience: Hem taþ id’si ayarla hem kapýyý aç.
    /// </summary>
    public void OpenJewelryDoorWithStone(string doorId, string stoneItemId)
    {
        var st = GetOrCreateJewelryDoor(doorId);
        st.isOpen = true;
        st.stoneItemId = stoneItemId;
        SaveToFile();
    }

    // =========================================================
    //                       LEVEL API
    // =========================================================
    public void SetLevelProgress(int currentIndex, int maxReached, bool saveNow = true)
    {
        levelProgress.currentLevelIndex = currentIndex;
        levelProgress.maxLevelReached = Mathf.Max(maxReached, currentIndex);
        if (saveNow) SaveToFile();
    }

    public void UpdateCurrentLevel(int currentIndex, bool saveNow = true)
    {
        levelProgress.currentLevelIndex = currentIndex;
        levelProgress.maxLevelReached = Mathf.Max(levelProgress.maxLevelReached, currentIndex);
        if (saveNow) SaveToFile();
    }

    // =========================================================
    //                    PERSISTENCE (JSON)
    // =========================================================
    [Serializable]
    private class Bag
    {
        public List<ItemStateV2> items = new List<ItemStateV2>();
        public List<DoorState> doors = new List<DoorState>();
        public List<ButonnedDoor> buttonedDoors = new List<ButonnedDoor>(); // isPressed kayýtlarý
        public LevelProgress level = new LevelProgress();

        // NEW: Jewelry doors list
        public List<JewelryDoorState> jewelryDoors = new List<JewelryDoorState>();
    }

    public void SaveToFile()
    {
        try
        {
            var bag = new Bag();

            foreach (var kv in _itemStates) bag.items.Add(kv.Value);
            foreach (var kv in _doorStates) bag.doors.Add(kv.Value);
            bag.buttonedDoors = new List<ButonnedDoor>(_butonnedDoorList);
            bag.level = levelProgress;

            // NEW: jewelry doors
            foreach (var kv in _jewelryDoorStates) bag.jewelryDoors.Add(kv.Value);

            var json = JsonUtility.ToJson(bag, true);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (Exception)
        {
            // sessizce yut
        }
    }

    public void SaveNow() => SaveToFile();

    private void LoadFromFile()
    {
        _itemStates.Clear();
        _doorStates.Clear();
        _butonnedDoorList.Clear();
        _jewelryDoorStates.Clear();

        if (!File.Exists(SaveFilePath))
        {
            levelProgress = new LevelProgress();
            return;
        }

        try
        {
            var json = File.ReadAllText(SaveFilePath);
            var bag = JsonUtility.FromJson<Bag>(json);

            if (bag != null)
            {
                if (bag.items != null)
                    foreach (var s in bag.items) _itemStates[s.itemId] = s;

                if (bag.doors != null)
                    foreach (var d in bag.doors) _doorStates[d.doorId] = d;

                if (bag.buttonedDoors != null)
                    _butonnedDoorList.AddRange(bag.buttonedDoors);

                // NEW: jewelry doors
                if (bag.jewelryDoors != null)
                    foreach (var jd in bag.jewelryDoors) _jewelryDoorStates[jd.doorId] = jd;

                levelProgress = bag.level ?? new LevelProgress();
            }
            else
            {
                levelProgress = new LevelProgress();
            }
        }
        catch (Exception)
        {
            levelProgress = new LevelProgress();
        }
    }

    // Ýsteðe baðlý: Kaydý silmek için
    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
        }
        catch (Exception)
        {
            // sessizce yut
        }
    }
}

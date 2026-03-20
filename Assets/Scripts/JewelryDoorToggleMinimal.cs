using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class JewelryDoorToggleMinimal : MonoBehaviour
{
    [Header("Kimlik")]
    public string doorId = "jewelerydoor-1"; // GameState JSON'daki ile aynı olmalı

    [Header("Kapı Görseli")]
    public GameObject closedDoor;             // Kapalı görünüm (aktif = kapalı)

    [Header("Oyuncu")]
    public Player player;                     // Boş ise Start'ta bulunur
    public AudioSource doorOpenedVoice;

    [Header("Ayarlar")]
    [Tooltip("Taş rengi. Ör: green, blue, red... (eldeki taş adında bu renk aranır)")]
    public string doorColor = "green";

    // Dahili
    private bool isTouchingPlayer;
    public bool isOpenedDoor = false;        // true = kapı açık
    private GameObject cachedOwnedStone;      // Kapı altında tutulan taş
    private string savedStoneItemId;          // JSON'dan gelen veya F ile yerleştirilince kaydedilen id

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Player bul
        if (player == null)
        {
            var p = FindObjectOfType<Player>();
            if (p != null) player = p;
        }

        // --- BAŞLANGIÇ: JSON'dan oku ---
        if (GameState.I != null)
        {
            // Kapının ilk durumu (JSON: jewelryDoors[].isOpen)
            isOpenedDoor = GameState.I.IsJewelryDoorOpen(doorId);

            // JSON'dan taş id'sini çek (varsa)
            if (GameState.I.TryGetJewelryDoorStone(doorId, out var stoneId) && !string.IsNullOrEmpty(stoneId))
                savedStoneItemId = stoneId;

            // Tüm Start'lar bitsin, sonra taşı kapıya bağla (yarış durumlarına karşı)
            StartCoroutine(RebindSavedStoneAtEndOfFrame());
        }
        else
        {
            // GameState yoksa emniyet: kapalı başlat
            isOpenedDoor = false;
        }

        ApplyDoorVisuals(isOpenedDoor);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahne yeniden yüklendiğinde/aktif olduğunda da bağlamayı dene
        StartCoroutine(RebindSavedStoneAtEndOfFrame());
    }

    IEnumerator RebindSavedStoneAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        if (!isOpenedDoor)
        {
            ApplyDoorVisuals(false);
            yield break;
        }

        // Kapı açık → taş mutlaka kapı altında olmalı
        if (string.IsNullOrEmpty(savedStoneItemId))
        {
            // JSON'da taş id yoksa ama elde doğru renkli taş varsa onu kapıya atayıp kaydedebiliriz (opsiyonel davranış)
            if (player != null && player.ownedItem != null && HasCorrectStone(player.ownedItem.name))
            {
                var info = player.ownedItem.GetComponent<ItemInformation>();
                if (info != null && GameState.I != null)
                {
                    savedStoneItemId = info.itemId;
                    GameState.I.OpenJewelryDoorWithStone(doorId, savedStoneItemId);
                    GameState.I.MarkDropped(savedStoneItemId, SceneManager.GetActiveScene().name, transform.position, Quaternion.identity);
                    GameState.I.SaveNow();

                    cachedOwnedStone = player.ownedItem;
                    BindUnderDoor(cachedOwnedStone);
                    player.ownedItem = null;
                }
            }
        }
        else
        {
            // JSON'da id var → sahnede (inaktif dahil) bul, kapının altına al
            if (cachedOwnedStone == null)
            {
                var all = GameObject.FindObjectsOfType<ItemInformation>(true);
                foreach (var it in all)
                {
                    if (it != null && it.itemId == savedStoneItemId)
                    {
                        cachedOwnedStone = it.gameObject;
                        break;
                    }
                }
            }

            if (cachedOwnedStone != null)
            {
                // Emniyet: envanterde görünüyorsa sahneye düşürülmüş say
                if (GameState.I != null && GameState.I.IsTaken(savedStoneItemId))
                {
                    GameState.I.MarkDropped(savedStoneItemId, SceneManager.GetActiveScene().name, transform.position, Quaternion.identity);
                    GameState.I.SaveNow();
                }

                BindUnderDoor(cachedOwnedStone);
            }
        }

        // Kapı görseli kesinlikle açık kalsın
        ApplyDoorVisuals(true);
        isOpenedDoor = true;
    }

    void Update()
    {
        // --- F: Taşı kapıya koy (kapı kapalıyken) ---
        if (Input.GetKeyDown(KeyCode.F) && isTouchingPlayer)
        {
            if (!isOpenedDoor && player != null && player.ownedItem != null && HasCorrectStone(player.ownedItem.name))
            {
                var info = player.ownedItem.GetComponent<ItemInformation>();
                if (info != null && GameState.I != null)
                {
                    savedStoneItemId = info.itemId;

                    // Kayda al ve kapıyı aç
                    GameState.I.OpenJewelryDoorWithStone(doorId, savedStoneItemId);
                    GameState.I.MarkDropped(savedStoneItemId, SceneManager.GetActiveScene().name, transform.position, Quaternion.identity);
                    GameState.I.SaveNow();

                    isOpenedDoor = true;
                    ApplyDoorVisuals(true);

                    cachedOwnedStone = player.ownedItem;
                    BindUnderDoor(cachedOwnedStone);

                    player.ownedItem = null;
                    doorOpenedVoice.Play();
                }
            }
        }

        // --- E: Taşı geri al (kapı açıkken) ---
        if (Input.GetKeyDown(KeyCode.E) && isTouchingPlayer)
        {
            if (isOpenedDoor && cachedOwnedStone != null && player != null && player.ownedItem == null)
            {
                player.ownedItem = cachedOwnedStone;
                player.ownedItem.SetActive(false);

                if (GameState.I != null)
                {
                    var info = cachedOwnedStone.GetComponent<ItemInformation>();
                    if (info != null)
                    {
                        GameState.I.MarkTaken(info.itemId);
                        GameState.I.SetJewelryDoorOpen(doorId, false, saveNow: false);
                        GameState.I.SetJewelryDoorStone(doorId, null, saveNow: false);
                        GameState.I.SaveNow();
                    }
                }

                cachedOwnedStone = null;
                savedStoneItemId = null;
                isOpenedDoor = false;
                ApplyDoorVisuals(false);
                doorOpenedVoice.Play();
            }
        }

        // Güvenlik: kapı açıksa ve child referans düşmüşse yeniden bağla
        if (isOpenedDoor && cachedOwnedStone == null && !string.IsNullOrEmpty(savedStoneItemId))
        {
            // Rebind tek seferlik dene
            var all = GameObject.FindObjectsOfType<ItemInformation>(true);
            foreach (var it in all)
            {
                if (it != null && it.itemId == savedStoneItemId)
                {
                    cachedOwnedStone = it.gameObject;
                    BindUnderDoor(cachedOwnedStone);
                    ApplyDoorVisuals(true);
                    break;
                }
            }
        }
    }

    // --- Helpers ---

    // Elde doğru renkte taş var mı?
    bool HasCorrectStone(string itemName)
    {
        string n = Normalize(itemName);
        string color = Normalize(doorColor);
        if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(color)) return false;

        if (n.Contains(color + "stone")) return true;   // greenstone
        if (n.Contains("stone" + color)) return true;   // stonegreen
        if (n.Contains(color)) return true;             // green
        return false;
    }

    // Taşı kapının altına child edip gizle (kapı süsü gibi dursun)
    void BindUnderDoor(GameObject stone)
    {
        if (stone == null) return;
        stone.transform.SetParent(transform);
        stone.transform.position = transform.position;
        stone.transform.rotation = Quaternion.identity;
        stone.SetActive(false);
    }

    // Kapı görsellerini uygula
    void ApplyDoorVisuals(bool open)
    {
        if (closedDoor != null) closedDoor.SetActive(!open);
    }

    // Trigger alanı: Player var mı?
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Player>() != null) isTouchingPlayer = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Player>() != null) isTouchingPlayer = false;
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
}

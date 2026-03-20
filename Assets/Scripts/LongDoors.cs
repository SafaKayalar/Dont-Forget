using System.Collections.Generic;
using UnityEngine;

public class LongDoors : MonoBehaviour
{
    public Player player;
    public AudioSource doorOpenedVoice;
    private GameObject unPressed;
    public bool isPressed;          // true = buton basýlý
    private bool isTouchingPlayer;

    [Tooltip("Buton/Kapý için benzersiz ID. Boþsa GameObject.name kullanýlacak.")]
    public string doorID;

    [Header("Çubuklar")]
    public List<GameObject> sticks = new List<GameObject>();

    [Tooltip("Basýlýyken kaydýrma miktarý (dünya birimi)")]
    public float moveAmount = 1f;

    // Dahili
    private readonly List<Vector3> originalPositions = new List<Vector3>();

    void Start()
    {
        if (transform.childCount > 0)
            unPressed = transform.GetChild(0).gameObject;

        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<Player>();
        }

        if (string.IsNullOrEmpty(doorID))
            doorID = gameObject.name;

        originalPositions.Clear();
        for (int i = 0; i < sticks.Count; i++)
            originalPositions.Add(sticks[i] != null ? sticks[i].transform.position : Vector3.zero);

        bool defaultPressed = false;
        isPressed = GameState.I != null ? GameState.I.GetButonnedDoor(doorID) : defaultPressed;

        ApplyStateVisuals();
    }

    void Update()
    {
        if (!isTouchingPlayer) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            isPressed = !isPressed;
            ApplyStateVisuals();
            if (GameState.I != null) GameState.I.SetButonnedDoor(doorID, isPressed);
            doorOpenedVoice.Play();
        }
    }

    private void ApplyStateVisuals()
    {
        if (unPressed != null) unPressed.SetActive(!isPressed);

        for (int i = 0; i < sticks.Count; i++)
        {
            var stick = sticks[i];
            if (stick == null) continue;

            var basePos = (i < originalPositions.Count) ? originalPositions[i] : stick.transform.position;
            Vector3 offset = Vector3.zero;

            if (isPressed)
            {
                Vector3 tipDir = GetTipDirection(stick); // çubuðun “ucu” yönü
                offset = tipDir * moveAmount;
            }

            stick.transform.position = basePos + offset;
        }
    }

    // Çubuðun uzun eksenini bul: Sprite varsa local sprite boyutlarýndan,
    // yoksa localScale'den; yönü local eksenden al (up veya right).
    private Vector3 GetTipDirection(GameObject stick)
    {
        Transform t = stick.transform;

        // 1) Sprite boyutlarýný (local) kullan
        var sr = stick.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 s = sr.sprite.bounds.size; // local space
            bool tall = s.y >= s.x;            // uzun eksen hangisi?
            return tall ? t.up : t.right;      // uç yönü: +up veya +right
        }

        // 2) Yedek: localScale’e göre tahmin
        Vector3 sc = t.lossyScale;
        bool tallByScale = Mathf.Abs(sc.y) >= Mathf.Abs(sc.x);
        return tallByScale ? t.up : t.right;
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (c.gameObject.CompareTag("Player")) isTouchingPlayer = true;
    }

    private void OnTriggerExit2D(Collider2D c)
    {
        if (c.gameObject.CompareTag("Player")) isTouchingPlayer = false;
    }
}

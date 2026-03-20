using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Kimlik")]
    public string doorId = "door-level1-to-2"; // Her kapýya benzersiz ID ver

    [Header("Referanslar")]
    public AudioSource doorOpenVoice;
    public Player player; // Inspector'dan atayabilir ya da tag ile buluruz
    private GameObject closedSprite; // "Closed Door" child'ý
    private bool isTouchingPlayer;

    private void Awake()
    {
        // Child otomatik bul (Inspector'dan atanmadýysa)
        if (closedSprite == null)
            closedSprite = transform.Find("Closed Door")?.gameObject;

        // Player referansý yoksa tag ile bul
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponent<Player>();
        }
    }

    private void Start()
    {
        // JSON'dan kapý durumu oku ve uygula
        bool open = (GameState.I != null) && GameState.I.IsDoorOpen(doorId);
        SetOpen(open);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            // Güvenli anahtar kontrolü
            if (player != null && player.ownedItem != null && player.ownedItem.name.StartsWith("Key") && isTouchingPlayer == true)
            {
                Destroy(closedSprite);
                // Zaten açýksa tekrar iþlem yapma
                if (!GameState.I.IsDoorOpen(doorId))
                {
                    GameState.I.MarkDoorOpen(doorId); // JSON'a yazar + kaydeder
                    SetOpen(true);
                    doorOpenVoice.Play();
                }
            }
        }
    }

    private void SetOpen(bool open)
    {
        if (closedSprite != null)
            closedSprite.SetActive(!open);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
        }
    }
}

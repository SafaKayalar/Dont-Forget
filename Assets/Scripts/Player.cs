using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    // ---- References / Components ----
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Transform handAnchor;          // elde gizli tutmak istersen
    public GameObject ownedItem;          // tek slot envanter
    public AudioSource walkVoice;
    public AudioSource handleVoice;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sprite;

    // ---- State ----
    private bool isTouchingGround;
    private GameObject touchingItem;      // trigger içindeki toplanabilir
    private int _lastKnownLevel = -1;     // LevelManager ile senkron emniyeti

    // ---- Unity ----
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        // Sahne yüklenince spawn'a yerleştir
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        // LevelManager event'ine abone ol (tek sahnede level değişiminde spawn’a koymak için)
        var lm = LevelManager.I != null ? LevelManager.I : FindFirstObjectByType<LevelManager>();
        if (lm != null)
        {
            lm.OnLevelChanged -= HandleLevelChanged; // çifte aboneliği önle
            lm.OnLevelChanged += HandleLevelChanged;
            _lastKnownLevel = lm.currentLevelIndex;
        }
    }

    private void OnDisable()
    {
        var lm = LevelManager.I != null ? LevelManager.I : FindFirstObjectByType<LevelManager>();
        if (lm != null) lm.OnLevelChanged -= HandleLevelChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        var lm = LevelManager.I != null ? LevelManager.I : FindFirstObjectByType<LevelManager>();
        if (lm != null) lm.OnLevelChanged -= HandleLevelChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Envanteri geri yükle (JSON'da taken=true olan item)
        StartCoroutine(RestoreOwnedItemAtStart());
    }

    private void Update()
    {
        HandleMovement();
        HandleUse();
        UpdateAnim();
    }

    // Event → bir sonraki frame’de spawn’a yerleştir
    private void HandleLevelChanged(int newIndex)
    {
        _lastKnownLevel = newIndex;
        StartCoroutine(PlaceAtSpawnNextFrame());
    }

    // Emniyet: event kaçarsa yakala
    private void LateUpdate()
    {
        var lm = LevelManager.I;
        if (lm != null && lm.currentLevelIndex != _lastKnownLevel)
        {
            _lastKnownLevel = lm.currentLevelIndex;
            StartCoroutine(PlaceAtSpawnNextFrame());
        }
    }

    // ---- Movement ----
    private void HandleMovement()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.A)) h = -1f;
        else if (Input.GetKey(KeyCode.D)) h = 1f;

        // yatay hız
        Vector2 v = rb.linearVelocity;
        v.x = h * moveSpeed;
        rb.linearVelocity = v;

        // yüz çevir
        if (h != 0) sprite.flipX = (h < 0);

        // zıpla
        if (Input.GetKeyDown(KeyCode.W) && isTouchingGround)
        {
            v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;
        }
    }

    // ---- Interact (E/Q) ----
    private void HandleUse()
    {
        // E: al
        if (Input.GetKeyDown(KeyCode.E) && ownedItem == null && touchingItem != null)
        {
            ownedItem = touchingItem;

            var info = ownedItem.GetComponent<ItemInformation>();
            if (info != null)
                info.MarkTaken(); // JSON: taken=true, objeyi sahnede gizler

            // Elde gizli tut (handAnchor varsa ona, yoksa player'a)
            Transform parent = handAnchor != null ? handAnchor : transform;
            ownedItem.transform.SetParent(parent);
            handleVoice.Play();
        }

        // Q: bırak
        if (Input.GetKeyDown(KeyCode.Q) && ownedItem != null)
        {
            var info = ownedItem.GetComponent<ItemInformation>();
            if (info != null)
            {
                // oyuncunun önünde biraz ileri bırak
                float dir = sprite.flipX ? -1f : 1f;
                Vector3 dropPos = transform.position;
                info.MarkDropped(dropPos, Quaternion.identity); // aktif level altına al + görünür + JSON yaz
            }

            ownedItem = null; // envanter boş
        }
    }

    // ---- Animation flags ----
    private void UpdateAnim()
    {
        bool isWalking = Mathf.Abs(rb.linearVelocity.x) > 0.01f;
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isJumping", !isTouchingGround);
    }

    // ---- Collisions / Triggers ----
    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Ground"))
            isTouchingGround = true;
    }

    private void OnCollisionExit2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Ground"))
            isTouchingGround = false;
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (c.gameObject.CompareTag("Collectable"))
            touchingItem = c.gameObject;
    }

    private void OnTriggerExit2D(Collider2D c)
    {
        if (c.gameObject.CompareTag("Collectable") && touchingItem == c.gameObject)
            touchingItem = null;
    }

    // ---- Spawn yerleştirme ----
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(PlaceAtSpawnNextFrame()); // objeler aktifleşsin
    }

    private IEnumerator PlaceAtSpawnNextFrame()
    {
        yield return null;
        PlaceAtSpawn();
    }

    private void PlaceAtSpawn()
    {
        Transform spawn = null;

        // 1) LevelManager varsa oradan
        var lm = LevelManager.I != null ? LevelManager.I : FindFirstObjectByType<LevelManager>();
        if (lm != null) spawn = lm.GetCurrentSpawn();

        // 2) Yedek: sahnede "SpawnPoint" tag’li
        if (spawn == null)
        {
            var go = GameObject.FindWithTag("SpawnPoint");
            if (go != null) spawn = go.transform;
        }

        if (spawn == null)
        {
            Debug.LogWarning("SpawnPoint bulunamadı! Aktif level altında 'SpawnPoint' adlı child var mı / tag doğru mu?");
            return;
        }

        rb.linearVelocity = Vector2.zero;
        rb.position = spawn.position;

    }

    // ---- Envanteri geri yükleme ----
    private IEnumerator RestoreOwnedItemAtStart()
    {
        yield return null; // GameState yüklensin

        ItemInformation[] items = FindObjectsOfType<ItemInformation>(true); // include inactive
        foreach (var info in items)
        {
            if (info == null || string.IsNullOrEmpty(info.itemId)) continue;

            if (GameState.I != null && GameState.I.IsTaken(info.itemId))
            {
                ownedItem = info.gameObject;
                if (ownedItem.activeSelf) ownedItem.SetActive(false); // envanterdeyken görünmesin

                Transform parent = handAnchor != null ? handAnchor : transform;
                ownedItem.transform.SetParent(parent);

                break; // tek slot
            }
        }
    }


    void PlayWalkSound()
    {
        walkVoice.Play();
    }


}

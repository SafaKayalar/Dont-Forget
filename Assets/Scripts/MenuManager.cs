using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Player player;
    public Image ownedItemImage;

    void Start()
    {
        // Baþlangýçta gizle
        ownedItemImage.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player.ownedItem != null) // Oyuncunun bir itemi varsa
        {
            // Item’in SpriteRenderer’ýndan sprite’ý al
            Sprite itemSprite = player.ownedItem.GetComponent<SpriteRenderer>().sprite;

            // UI Image’i aç ve sprite’ý ata
            ownedItemImage.gameObject.SetActive(true);
            ownedItemImage.sprite = itemSprite;
        }
        else
        {
            // Oyuncunun itemi yoksa resmi gizle
            ownedItemImage.gameObject.SetActive(false);
        }
    }
}

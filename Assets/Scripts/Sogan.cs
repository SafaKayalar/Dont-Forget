using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Sogan : MonoBehaviour
{
    bool isTouchingPlayer = false;
    public GameObject soganTextPanel;
    public Text soganText;

    // Silmek istediðin dosyanýn yolu (tam path veya Application.persistentDataPath içinde)
    string filePath;

    void Start()
    {
        soganTextPanel.SetActive(false);

        // Örnek: GameState.json dosyasý
        filePath = Path.Combine(Application.persistentDataPath, "GameState.json");
    }

    void Update()
    {
        if (isTouchingPlayer)
        {
            soganTextPanel.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                Application.Quit();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        isTouchingPlayer = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isTouchingPlayer = false;
        soganTextPanel.SetActive(false);
    }
}

using UnityEngine;

public class LevelUpdater : MonoBehaviour
{
    public Player player;
    public LevelManager levelManager;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            levelManager.NextLevel();
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over Settings")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private float restartDelay = 2f;

    public bool IsGameOver { get; private set; }

    public System.Action OnGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    public void TriggerGameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log("[GameManager] GAME OVER!");

        // Tüm düşmanları yok et
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }

        // Oyuncuyu durdur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        // UI göster
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        OnGameOver?.Invoke();

        // Otomatik restart
        Invoke(nameof(RestartGame), restartDelay);
    }

    public void RestartGame()
    {
        IsGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        IsGameOver = false;
        SceneManager.LoadScene(0); // Main menu scene index
    }
}

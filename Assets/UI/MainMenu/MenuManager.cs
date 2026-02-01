using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Start butonuna basınca yüklenecek sahnenin TAM adı")]
    [SerializeField] private string gameSceneName = "village_complete"; 

    private void Start()
    {
        // Menü açıldığında her ihtimale karşı zamanı oynat
        Time.timeScale = 1f;
        Cursor.visible = true; // Mouse'u görünür yap
        Cursor.lockState = CursorLockMode.None; // Mouse'u serbest bırak
    }

    public void StartGame()
    {
        Debug.Log("StartGame butonu tıklandı!");
        
        // Oyuna başlarken zamanın aktığından emin ol
        Time.timeScale = 1f;
        
        Debug.Log($"Yüklenen sahne: {gameSceneName}");
        // Belirtilen isme sahip sahneyi yükler
        // Not: File -> Build Settings'e bu sahneyi eklemeyi unutma!
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan çıkıldı (Sadece Build alınınca çalışır)!");
        Application.Quit();
    }
}
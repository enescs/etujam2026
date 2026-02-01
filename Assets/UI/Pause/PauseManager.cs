using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel; // Hiyerarşideki paneli buraya sürükleyeceğiz
    private bool isPaused = false;

    void Update()
    {
        // New Input System kullanımı (P tuşu)
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true); // Kararma panelini aç
        Time.timeScale = 0f;        // Unity'nin zamanını durdur (fizik/zaman donar)
        isPaused = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false); // Paneli kapat
        Time.timeScale = 1f;         // Zamanı normale döndür
        isPaused = false; 
    }
}
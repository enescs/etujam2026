using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel; // Hiyerarşideki paneli buraya sürükleyeceğiz
    private bool isPaused = false;

    void Update()
    {
        // P tuşuna basıldığında durumu değiştir
        if (Input.GetKeyDown(KeyCode.P))
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
        isPaused = true; // Hata olmaması için false yapıyoruz
        isPaused = false; 
    }
}
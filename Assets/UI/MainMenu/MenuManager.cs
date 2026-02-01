using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi kütüphanesi

public class MenuManager : MonoBehaviour
{
    public void OyunuBaslat()
    {
        // Build Settings'te 1 numaralı sahneye (oyuna) geçiş yapar
        SceneManager.LoadScene(1); 
    }
}
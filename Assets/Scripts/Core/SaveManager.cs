using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameData
{
    public int sceneIndex;
    public Vector3 playerPosition;
    // Buraya sağlık, altın gibi başka veriler ekleyebilirsin
    // public int health;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private string saveFilePath;
    private GameData dataToLoad; // Sahne yüklendikten sonra kullanılacak veri

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Sahneler arası yok olmasını engeller
        
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SaveGame()
    {
        GameData data = new GameData();
        
        // 1. Sahne verisini kaydet
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 2. Oyuncu pozisyonunu kaydet
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;
        }
        else
        {
            Debug.LogWarning("[SaveManager] Player bulunamadı, pozisyon kaydedilemedi!");
        }

        // JSON'a çevir ve dosyaya yaz
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        
        Debug.Log("Oyun kaydedildi: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("Kayıt dosyası bulunamadı.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        GameData data = JsonUtility.FromJson<GameData>(json);
        
        dataToLoad = data;

        // Eğer kayıtlı sahne şu anki sahne değilse, o sahneyi yükle
        if (SceneManager.GetActiveScene().buildIndex != data.sceneIndex)
        {
            SceneManager.LoadScene(data.sceneIndex);
        }
        else
        {
            // Zaten aynı sahnedeyiz, direkt oyuncuyu yerleştir
            ApplyLoadData();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahne yüklendiğinde, eğer yüklenecek bir veri varsa uygula
        if (dataToLoad != null && scene.buildIndex == dataToLoad.sceneIndex)
        {
            ApplyLoadData();
        }
    }

    private void ApplyLoadData()
    {
        if (dataToLoad == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Pozisyonu ayarla
            player.transform.position = dataToLoad.playerPosition;
            
            // Eğer fizik motoru kullanıyorsan hızı sıfırlamak iyi olabilir
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            Debug.Log("Oyun yüklendi!");
        }
        
        dataToLoad = null; // Veriyi temizle
    }
    
    // Test için Update içinde tuş ataması (İstersen silebilirsin)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame();
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame();
        }
    }
}

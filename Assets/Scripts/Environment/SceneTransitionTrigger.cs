using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Player bu alana girince belirtilen sahneye geçiş yapar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Geçilecek Sahne")]
    [Tooltip("Geçilecek sahnenin adı (Build Settings'te olmalı)")]
    [SerializeField] private string targetSceneName;
    
    [Header("Alternatif: Index ile geçiş")]
    [Tooltip("Eğer sahne adı boşsa, bu index kullanılır")]
    [SerializeField] private int targetSceneIndex = -1;
    
    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    private void Awake()
    {
        // Collider'ın trigger olduğundan emin ol
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        
        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        Debug.Log($"[SceneTransition] Loading scene: {targetSceneName}");
        
        // Sahne adı varsa onu kullan
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        // Yoksa index kullan
        else if (targetSceneIndex >= 0)
        {
            SceneManager.LoadScene(targetSceneIndex);
        }
        else
        {
            Debug.LogError("[SceneTransition] Sahne adı veya index belirtilmedi!");
        }
    }

    private void OnDrawGizmos()
    {
        // Editor'de görünür yap
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        var col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
    }
}

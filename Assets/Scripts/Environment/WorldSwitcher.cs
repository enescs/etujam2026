using UnityEngine;

/// <summary>
/// Normal dünya ve ruhlar alemi arasında geçiş yapar.
/// Birden fazla objeyi aynı anda aktif/pasif yapabilir.
/// </summary>
public class WorldSwitcher : MonoBehaviour
{
    [Header("Normal Dünya Objeleri")]
    [Tooltip("Normal dünyada aktif olacak objeler (Grid, normalDunya vs.)")]
    [SerializeField] private GameObject[] normalWorldObjects;
    
    [Header("Ruhlar Alemi Objeleri")]
    [Tooltip("Ruhlar aleminde aktif olacak objeler (ruhlarGrid, ruhlarAlemi vs.)")]
    [SerializeField] private GameObject[] spiritWorldObjects;

    private void Start()
    {
        // Event'lere abone ol
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn += SwitchToSpiritWorld;
            MaskSystem.Instance.OnMaskOff += SwitchToNormalWorld;
        }
        
        // Başlangıçta normal dünya aktif
        SwitchToNormalWorld();
    }

    private void OnDestroy()
    {
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn -= SwitchToSpiritWorld;
            MaskSystem.Instance.OnMaskOff -= SwitchToNormalWorld;
        }
    }

    private void SwitchToNormalWorld()
    {
        // Normal dünya objelerini aç
        SetObjectsActive(normalWorldObjects, true);
        
        // Ruhlar alemi objelerini kapat
        SetObjectsActive(spiritWorldObjects, false);
        
        Debug.Log("[WorldSwitcher] Normal Dünya aktif");
    }

    private void SwitchToSpiritWorld()
    {
        // Normal dünya objelerini kapat
        SetObjectsActive(normalWorldObjects, false);
        
        // Ruhlar alemi objelerini aç
        SetObjectsActive(spiritWorldObjects, true);
        
        Debug.Log("[WorldSwitcher] Ruhlar Alemi aktif");
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        
        foreach (var obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}


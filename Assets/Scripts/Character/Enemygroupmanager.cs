using UnityEngine;
using System.Collections.Generic;

public class EnemyGroupManager : MonoBehaviour
{
    public static EnemyGroupManager Instance { get; private set; }

    private Dictionary<int, List<EnemyAI>> groups = new Dictionary<int, List<EnemyAI>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to detection bar's full detection event
        // This assumes DetectionBar is initialized before this (use Script Execution Order or Awake timing)
    }

    private void Start()
    {
        if (DetectionBar.Instance != null)
            DetectionBar.Instance.OnFullDetection += HandleFullDetection;
    }

    private void OnDestroy()
    {
        if (DetectionBar.Instance != null)
            DetectionBar.Instance.OnFullDetection -= HandleFullDetection;
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!groups.ContainsKey(enemy.GroupId))
            groups[enemy.GroupId] = new List<EnemyAI>();

        groups[enemy.GroupId].Add(enemy);
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (groups.ContainsKey(enemy.GroupId))
            groups[enemy.GroupId].Remove(enemy);
    }

    /// <summary>
    /// When detection bar fills, alert the entire group of the enemy that triggered it.
    /// </summary>
    private void HandleFullDetection(EnemyAI triggeringEnemy)
    {
        AlertGroup(triggeringEnemy.GroupId);
    }

    /// <summary>
    /// Trigger chase state on all enemies in a group.
    /// </summary>
    public void AlertGroup(int groupId)
    {
        if (!groups.ContainsKey(groupId)) return;

        foreach (EnemyAI enemy in groups[groupId])
        {
            enemy.TriggerChase();
        }
    }

    /// <summary>
    /// Get all enemies in a specific group.
    /// </summary>
    public List<EnemyAI> GetGroup(int groupId)
    {
        if (groups.ContainsKey(groupId))
            return groups[groupId];
        return new List<EnemyAI>();
    }
}
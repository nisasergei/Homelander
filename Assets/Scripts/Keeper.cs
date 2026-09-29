using System.Collections.Generic;
using UnityEngine;

// Трон игрока. HP и защита — в компоненте Health на этом же объекте.
// Когда Keeper разрушен, игрок выбывает: его казармы рушатся, герой больше не воскресает, доход останавливается.
[RequireComponent(typeof(Health))]
public class Keeper : MonoBehaviour
{
    public int ownerPlayerId = 1;

    static readonly HashSet<int> eliminated = new HashSet<int>();
    static readonly HashSet<int> registered = new HashSet<int>();

    public static bool IsEliminated(int teamId) => eliminated.Contains(teamId);
    public static int WinnerTeam { get; private set; }

    Health health;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        eliminated.Clear();
        registered.Clear();
        WinnerTeam = 0;
    }

    void Awake()
    {
        health = GetComponent<Health>();
        health.unitKind = UnitKind.Building;
        if (ownerPlayerId <= 0) ownerPlayerId = health.teamId;
        registered.Add(ownerPlayerId);
        health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    void HandleDeath(Health self, Health killer)
    {
        eliminated.Add(ownerPlayerId);
        string by = killer != null ? $" (добил игрок {killer.teamId})" : "";
        Debug.Log($"Трон игрока {ownerPlayerId} разрушен{by}. Игрок {ownerPlayerId} ВЫБЫЛ ИЗ ИГРЫ!");
        
        foreach (Health h in Health.All.ToArray())
        {
            if (h == null || h == self || h.teamId != ownerPlayerId || !h.IsAlive) continue;
            if (h.unitKind == UnitKind.Building || h.unitKind == UnitKind.Hero) h.Kill(killer);
        }

        CheckWinner();
    }

    static void CheckWinner()
    {
        int alive = 0, last = 0;
        foreach (int team in registered)
        {
            if (eliminated.Contains(team)) continue;
            alive++;
            last = team;
        }
        if (alive == 1)
        {
            WinnerTeam = last;
            Debug.Log($"ПОБЕДА игрока {last}!");
        }
    }
}

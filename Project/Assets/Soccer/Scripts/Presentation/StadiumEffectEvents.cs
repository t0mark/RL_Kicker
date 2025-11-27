using System;
using UnityEngine;

/// <summary>
/// Centralized event bus for neon stadium presentation effects.
/// Prevents gameplay scripts from referencing presentation-specific components directly.
/// </summary>
public static class StadiumEffectEvents
{
    public static event Action<Vector3, Team> BallKicked;
    public static event Action<Team, Vector3> GoalScored;

    public static void RaiseBallKicked(Vector3 position, Team team)
    {
        BallKicked?.Invoke(position, team);
    }

    public static void RaiseGoalScored(Team scoringTeam, Vector3 goalPosition)
    {
        GoalScored?.Invoke(scoringTeam, goalPosition);
    }
}

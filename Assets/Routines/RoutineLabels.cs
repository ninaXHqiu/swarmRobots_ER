using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// How the routine lists show a routine: its own number, its name (which keeps the stage label,
// v0 / v1 / v2) and, under it, its note saying which kind it is. The number and note are written
// in each routine's own file (SwarmRoutine.Number and Note), so there is no separate table.
//   Number: tens digit = the family, ones digit = the variant (0 natural, 1 about 3 min, 2 about 5 min)
//    1 -  9  Line up and the v0 / v1 routines      20s  v2 Vibration       50s  v2 Social Dance
//   10 - 14  v2 Messenger and the older v2 attempts 30s  v2 Heartbeat       60s  v2 tests (Piano Keys, Maze, ...)
//                                                   40s  v2 Kaleidoscope
public static class RoutineLabels
{
    // "21  v2 Vibration (3 min)" and under it the note in smaller type (rich text)
    public static string Label(SwarmRoutine r)
    {
        string number = r.Number > 0 ? r.Number.ToString("00") : "--";
        string note = string.IsNullOrEmpty(r.Note) ? "(no note yet)" : r.Note;
        return "<b>" + number + "</b>  " + r.Name + "\n<size=11>" + note + "</size>";
    }

    // Routines in number order; the ones without a number last, by name. Warns about repeats.
    public static List<SwarmRoutine> Sort(List<SwarmRoutine> routines)
    {
        foreach (var r in routines.Where(r => r.Number == 0)) { Debug.LogWarning("RoutineLabels: '" + r.Name + "' has no number"); }
        foreach (var g in routines.Where(r => r.Number > 0).GroupBy(r => r.Number).Where(g => g.Count() > 1))
        {
            Debug.LogWarning("RoutineLabels: number " + g.Key + " is used by " + string.Join(", ", g.Select(r => r.Name).ToArray()));
        }
        return routines.OrderBy(r => r.Number == 0 ? int.MaxValue : r.Number).ThenBy(r => r.Name).ToList();
    }
}

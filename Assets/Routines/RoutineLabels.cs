using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// How the routine lists show a routine: its short label (a letter for the stage, two digits, and
// n / 3 / 5 for a routine with lengths), its name, and under it the note of at most five words.
// The label and note are written in each routine's own file (SwarmRoutine.Code and Note).
//   A01 A02 ...  v0 stage       B01 B02 ...  v1 stage       C01 C02 ...  v2 stage
//   C063 = routine C06 in its 3 minute version (C06n natural, C065 five minutes)
// To add a routine: take the next free number of its stage and never renumber the existing ones.
public static class RoutineLabels
{
    // "C063  v2 Vibration (3 min)" and under it the note in smaller type (rich text)
    public static string Label(SwarmRoutine r)
    {
        string code = string.IsNullOrEmpty(r.Code) ? "---" : r.Code;
        string note = string.IsNullOrEmpty(r.Note) ? "(no note yet)" : r.Note;
        return "<b>" + code + "</b>  " + r.Name + "\n<size=11>" + note + "</size>";
    }

    // Routines in label order (A, then B, then C; by number; n, 3, 5); unlabelled ones last. Warns about repeats.
    public static List<SwarmRoutine> Sort(List<SwarmRoutine> routines)
    {
        foreach (var r in routines.Where(r => string.IsNullOrEmpty(r.Code))) { Debug.LogWarning("RoutineLabels: '" + r.Name + "' has no label"); }
        foreach (var g in routines.Where(r => !string.IsNullOrEmpty(r.Code)).GroupBy(r => r.Code).Where(g => g.Count() > 1))
        {
            Debug.LogWarning("RoutineLabels: label " + g.Key + " is used by " + string.Join(", ", g.Select(r => r.Name).ToArray()));
        }
        return routines.OrderBy(r => Key(r)).ThenBy(r => r.Name).ToList();
    }

    // A01 < A02 < ... < B01 < ... ; C06n < C063 < C065
    static string Key(SwarmRoutine r)
    {
        if (string.IsNullOrEmpty(r.Code)) { return "~"; }
        string code = r.Code.ToUpperInvariant();
        char last = code[code.Length - 1];
        if (code.Length == 4 && (last == 'N' || last == '3' || last == '5')) { return code.Substring(0, 3) + (last == 'N' ? "0" : last.ToString()); }
        return code + "0";
    }

    // The routine whose label is `code` (case does not matter), or null
    public static SwarmRoutine Find(IEnumerable<SwarmRoutine> routines, string code)
    {
        return routines.FirstOrDefault(r => string.Equals(r.Code, code, System.StringComparison.OrdinalIgnoreCase));
    }
}

using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Database", menuName = "Database")]
public class PartsDatabase : ScriptableObject
{
    public PathPart[] GetParts(bool straight, bool turn, bool others)
    {
        List<PathPart> parts = new();

        if (straight) parts.AddRange(straightParts);
        if (turn) parts.AddRange(turnParts);
        if (others) parts.AddRange(otherParts);

        return parts.ToArray();
    }

    [SerializeField] private PathPart[] straightParts;
    [SerializeField] private PathPart[] turnParts;
    [SerializeField] private PathPart[] otherParts;
}

using UnityEngine;

public class Checkpoint : CarCollidable
{
    public bool visible = true;

    public Transform[] points;

    public float GetDistance(Vector2 pos)
    {
        float closestDist = float.MaxValue;

        for (int i = 0; i < points.Length; i++)
        {
            float d = Vector2.Distance(points[i].position, pos);
            if (d < closestDist) closestDist = d;
        }

        return closestDist;
    }

    [Header("Auto assigned")]
    public int id;
    public Checkpoint next;

    public float totalScore;
    public float localScore; // score from previous checkpoint to this one, if checkpoints is first (before spawnpoint) value is negative

    protected override void OnTriggerWithCarEnter(Car car)
    {
        car.OnCheckpointEnter(this);
        GetComponentInParent<PathPart>().OnCarEnter();
    }

    public void Show (bool show) { GetComponent<SpriteRenderer>().enabled = show; }
}

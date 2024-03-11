public class Finish : CarCollidable
{
    public bool temporary = false;
    public float targetTime;

    public bool final;

    protected override void OnTriggerWithCarEnter(Car car) => car.OnTriggerWithFinish(this);
}

using System;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Read only")]
    public float avgSpeed;
    public float o_speed, o_rotation;
    public bool o_brake;

    protected float CurrentSpeed
    {
        get { return _currentSpeed; }
        set
        {
            if (float.IsNaN(value)) throw new Exception("Speed can't be NaN!");
            _currentSpeed = value;
        }
    }

    private float _currentSpeed;

    protected float currentSteer = 0;

    private static readonly float speedMultiplayer = 10f;
    private static readonly float steerMultiplayer = 250f;

    private float totSpeed;
    private int count;

    /// <param name="speed"> between 0 and 1 </param>
    /// <param name="steer"> between 0 and 1 </param>
    protected void TranslateCar(float speed, float steer, bool brake)
    {
        if (float.IsNaN(speed)) throw new Exception("Speed can't be NaN");

        if (speed < 0 || speed > 1) throw new ArgumentOutOfRangeException($"Speed out of range! ({speed})");
        if (steer < 0 || steer > 1) throw new ArgumentOutOfRangeException($"Steer out of range! ({steer})");

        steer = InvertValue(steer);

        if (brake) speed = .5f;

        o_brake = brake;

        CurrentSpeed = LerpFloat(CurrentSpeed, o_speed = NormalizeValue(speed), GetSpeedMultiplier(speed, brake));
        currentSteer = LerpFloat(currentSteer, o_rotation = NormalizeValue(steer), steerMult);

        totSpeed += CurrentSpeed;
        count++;

        avgSpeed = totSpeed / count;

        TranslateCar(CurrentSpeed);

        int dirMult = CurrentSpeed > 0 ? 1 : -1; // fixes direction of steer
        float rMutl = CalculateRotMult(Mathf.Abs(CurrentSpeed));

        RotateCar(currentSteer * rMutl * dirMult);
    }

    public static float NormalizeValue(float v) { return v - .5f; }

    /// <param name="x"> 0 <= x <= 1 </param>
    private float InvertValue(float x) => Math.Abs(x - 1);

    /// <param name="x"> 0 <= x <= 1 /param>
    private float CalculateRotMult(float speed)
    {
        return (speed - Mathf.Pow(speed, 2)) / (.3f + Mathf.Pow(speed, 2));
    }

    private void TranslateCar(float speed)
    {
        if (float.IsNaN(speed)) throw new Exception("Speed is NAN!");
        transform.Translate(speed * speedMultiplayer * Time.deltaTime * Vector2.up);
    }

    private void RotateCar(float speed) => transform.Rotate(speed * steerMultiplayer * Time.deltaTime * transform.forward);

    private static readonly float noAccelerationMult = .0125f;
    private static readonly float accelerationMult = .6f;
    private static readonly float brakeMult = .8f;

    private static readonly float steerMult = 2.3f;

    private float GetSpeedMultiplier(float speed, bool brake)
    {
        if (brake) return brakeMult;

        bool accelerating = Mathf.Abs(speed) > Mathf.Abs(CurrentSpeed);
        bool sameDir = (CurrentSpeed > 0 && speed > 0) || (CurrentSpeed < 0 && speed < 0);

        return sameDir && !accelerating ? noAccelerationMult : accelerationMult;
    }

    /// <param name="f1"> current </param>
    /// <param name="f2"> target </param>
    /// <returns> lerped float </returns>
    private static float LerpFloat(float f1, float f2, float m = 1)
    {
        if (float.IsNaN(f1)) throw new Exception($"{nameof(f1)} can't be NaN");
        if (float.IsNaN(f2)) throw new Exception($"{nameof(f2)} can't be NaN");

        float t = Time.deltaTime * m;

        if (f1 + t < f2) return f1 + t;
        else if (f1 - t > f2) return f1 - t;
        else return f2;
    }

}

using System;
using UnityEngine;

public class CarCollidable : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        Console.WriteLine($"Trigger {name}");

        Car car = collision.GetComponentInParent<Car>();
        if (car) OnTriggerWithCarEnter(car);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Console.WriteLine($"Collision {name}");

        Car car = collision.collider.GetComponentInParent<Car>();
        if (car) OnCollisionWithCarEnter(car);
    }

    protected virtual void OnTriggerWithCarEnter(Car car) { }

    protected virtual void OnCollisionWithCarEnter(Car car) { }
}

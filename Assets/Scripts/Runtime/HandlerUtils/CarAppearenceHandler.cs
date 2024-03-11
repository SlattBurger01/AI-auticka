using UnityEngine;

public class CarAppearenceHandler : MonoBehaviour
{
    private static readonly Color nC = Color.white;
    private static Color bC => Handler.customBestCarCols ? Color.green : nC;
    private static Color sBC => Handler.customBestCarCols ? Color.yellow : nC;

    public static void UpdateDefaultCar(Car car)
    {
        if (!car) return;

        int order = car.enabled ? Handler.normalLayerOrder : Handler.disabledLayerOrder;

        UpdateCarF(car, order, nC, GetCarName(car, "Car"), 1);
    }

    private static string GetCarName(Car car, string defName) { return car.enabled ? $"{defName} ({car.id})" : $"Disabled{defName} ({car.id})"; }

    public static void UpdateBestCar(Car car) => UpdateCarF(car, Handler.bestCarLayerOrder, bC, GetCarName(car, "BestCar"), 4.5f);
    public static void UpdateSecondBestCar(Car car) => UpdateCarF(car, Handler.sbestCarLayerOrder, sBC, GetCarName(car, "SecondBestCar"), 4);

    private static void UpdateCarF(Car car, int order, Color col, string name, float oMult)
    {
        car.SetOrderInLayer(order);
        car.SetMainColor(col, oMult);
        car.name = name;
    }
}

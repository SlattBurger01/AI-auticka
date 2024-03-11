using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [SerializeField] private bool follow = true;
    private Camera cam;

    //private void Awake() => GetComponents();

    private void GetComponents()
    {
        cam = GetComponent<Camera>();
    }

    public void Enable(bool enable)
    {
        if (!cam) GetComponents();
        cam.enabled = enable;
    }

    public void UpdatePosition(Vector2 targetPosition)
    {
        if (!follow) return;
        UpdatePosF(targetPosition);
    }

    public void SetStaticPosition(Vector2 position, float size)
    {
        UpdatePosF(position);
        cam.orthographicSize = size;
    }

    private void UpdatePosF(Vector2 p) { transform.position = new Vector3(p.x, p.y, transform.position.z); }
}

using UnityEngine;

public class TreasureSpin : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 75f;
    [SerializeField] private float fixedXRotation = -90f;
    [SerializeField] private float fixedZRotation = 0f;

    private float yaw;
    private float worldY;

    public void Configure(float speed, float spawnY, float xRotation, float zRotation)
    {
        spinSpeed = speed;
        worldY = spawnY;
        fixedXRotation = xRotation;
        fixedZRotation = zRotation;
        yaw = transform.eulerAngles.y;
    }

    private void Awake()
    {
        worldY = transform.position.y;
        yaw = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        yaw = Mathf.Repeat(yaw + spinSpeed * Time.deltaTime, 360f);
        var pos = transform.position;
        pos.y = worldY;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(fixedXRotation, yaw, fixedZRotation);
    }
}

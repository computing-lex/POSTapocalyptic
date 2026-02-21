using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    [SerializeField] private float distanceFromUser = 5f;
    [SerializeField] private float smoothTime = 5f;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool invertDirection;
    [SerializeField] private bool smoothLookAtCameraOnStart = true;

    public bool SmoothLookingAtCamera { get; set; }
    public float DistanceFromUser { get => distanceFromUser; set => distanceFromUser = value; }
    public float SmoothTime { get => smoothTime; set => smoothTime = value; }
    public Vector3 Offset { get => offset; set => offset = value; }
    public bool InvertDirection { get => invertDirection; set => invertDirection = value; }

    private Vector3 currentVelocity = Vector3.zero;
    private Transform mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.transform;
        SmoothLookingAtCamera = smoothLookAtCameraOnStart;
    }

    // Update is called once per frame
    void Update()
    {
        if (SmoothLookingAtCamera)
        {
            Vector3 cameraPos = mainCamera.position;
            transform.forward = (transform.position - cameraPos) * (invertDirection ? -1f : 1f);
            transform.position = Vector3.SmoothDamp(transform.position, cameraPos + mainCamera.TransformDirection(new Vector3(0, 0, distanceFromUser) + offset), ref currentVelocity, smoothTime);
        }
    }
}
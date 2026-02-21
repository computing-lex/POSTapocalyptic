using UnityEngine;
// Thanks Joeseph !
public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool invertDirection;
    [SerializeField] private bool lookAtCameraOnStart = true;

    public bool LookingAtCamera { get; set; }
    public Vector3 Offset { get => offset; set => offset = value; }
    public bool InvertDirection { get => invertDirection; set => invertDirection = value; }

    private Transform mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.transform;
        LookingAtCamera = lookAtCameraOnStart;
    }

    // Update is called once per frame
    void Update()
    {
        if (LookingAtCamera)
        {
            Vector3 cameraPos = mainCamera.position;
            cameraPos -= offset;
            transform.forward = (transform.position - cameraPos) * (invertDirection ? -1f : 1f);
        }
    }
}
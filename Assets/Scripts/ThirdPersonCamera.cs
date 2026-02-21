//using UnityEngine;
//using UnityEngine.InputSystem;
//using Unity.Cinemachine; // Required for Unity 6 Cinemachine

//public class CinemachineInputLink : MonoBehaviour
//{
//    public CinemachineCamera vCam;
//    public InputActionAsset lookAction;

    //void Start()
    //{
    //    // Link to your specific Action Map setup
    //    var actions = new PlayerInput(); // Replace with your generated C# class name if different
    //    lookAction = actions.FindActionMap("Player").FindAction("Look");
    //    lookAction.Enable();
    //}

    // bruh why this not work i followed unity docs u mfs!!!
    //void Update()
    //{
    //    if (vCam == null) return;

    //    // Read the Vector2 from Look action
    //    Vector2 lookInput = lookAction.ReadValue<Vector2>();

    //    // Unity 6 uses the Input Axis Controller to drive camera movement
    //    var controller = vCam.GetComponent<CinemachineInputAxisController>();

    //    if (controller != null)
    //    {
    //        // "Horizontal" and "Vertical" are the default axis names in the 
    //        // Cinemachine Input Axis Controller inspector list.
    //        //controller.TryGetInputAxis("Horizontal").Value = lookInput.x;
    //        //controller.TryGetInputAxis("Vertical").Value = lookInput.y;
    //    }
    //}
//}
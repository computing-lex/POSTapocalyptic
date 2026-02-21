using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{

    public static PlayerController Instance { get; private set; }

    // Input Action Configuration
    public InputActionAsset actions;

    private InputAction move;
    private InputAction CatMode;
    private InputAction BatMode;
    private InputAction GekkoMode;
    private InputAction jump;
    private InputAction look;

    // General
    private string form;
    private Vector2 moveVector;
    private bool isGrounded;

    // Camera (and some UI ideas)
    //[Header("UI Reference")]
    //public UnityEngine.UI.Image crosshair;          // assign in Inspector
    //public Sprite crosshairIdle;       // assign in Inspector
    //public Sprite crosshairActive;     // assign in Inspector

    [Header("Smoothing")]
    float pitch = 0f;
    public Transform cameraTransform;
    public Camera playerCamera;
    private float speed = 5f;
    private float lookSensitivity = 10f;
    public float lookSmoothing = 0.1f; // Higher = weightier/slower
    private Vector2 currentLookInput;
    private Vector2 lookInputVelocity;

    // Gekko
    private float gekkoMoveSpeed = 0.5f; // move speed
    private float turnSpeed = 90; // turning speed (degrees/second)
    private float lerpSpeed = 10; // smoothing speed
    private float gravity = 10; // gravity acceleration

    private float deltaGround = 0.2f; // character is grounded up to this distance
    private float jumpSpeed = 10; // vertical jump initial speed
    private float jumpRange = 10; // range to detect target wall
    private Vector3 surfaceNormal; // current surface normal
    private Vector3 myNormal; // character normal
    private float distGround; // distance from character position to ground
    private bool jumping = false; // flag "I'm jumping to wall"
    private float vertSpeed = 0; // vertical jump current speed

    private Transform myTransform;
    public BoxCollider boxCollider; // drag BoxCollider ref in editor


    // Cat


    // Bat



    void Awake()
    {
        // Find Action
        move = actions.FindActionMap("Player").FindAction("Move");
        jump = actions.FindActionMap("Player").FindAction("Jump");
        look = actions.FindActionMap("Player").FindAction("Look");
       
        actions.FindActionMap("Player").FindAction("CatMode").performed += OnCat;
        actions.FindActionMap("Player").FindAction("BatMode").performed += OnBat;
        actions.FindActionMap("Player").FindAction("GekkoMode").performed += OnGekko;
    }

    // Enable InputActions
    void OnEnable()
    {
        //actions.FindActionMap("gameplay").Enable();
    }

    // Disable InputActions
    void OnDisable()
    {
        //actions.FindActionMap("gameplay").Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);

        form = "gekko";

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;


        //crosshair.sprite = crosshairIdle;
    }

    void Update()
    {
        HandleLook();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        // our update loop polls the "move" action value each frame
        moveVector = move.ReadValue<Vector2>();

        // look!!


        switch (form)
        {
            case "cat":
                
                // cat methods
                Debug.Log("CAT TIME...");
                break;

            case "bat":
                
                // bat stuff
                Debug.Log("BAT TIME...");
                
                break;
            case "gekko":
                
                // gekko stuff
                //Debug.Log("GEKKO TIME...");

                myNormal = transform.up; // normal starts as character up direction
                myTransform = transform;
                GetComponent<Rigidbody>().freezeRotation = true; // disable physics rotation
                // distance from transform.position to ground
                distGround = boxCollider.bounds.extents.y - boxCollider.center.y;

                gekkoMove();    

                break;

            default:
                Debug.Log("NO FORM...");
                break;
        }
    }

    #region Look
    private void HandleLook()
    {
        //if (forcedWalk) return;

        Vector2 rawLookInput = look.ReadValue<Vector2>();

        // Interpolate the input
        currentLookInput = Vector2.SmoothDamp(
            currentLookInput,
            rawLookInput,
            ref lookInputVelocity,
            lookSmoothing
        );

        // Rotation (Yaw)
        transform.Rotate(Vector3.up * currentLookInput.x * lookSensitivity * Time.deltaTime);

        // Pitch (Look up/down)
        pitch -= currentLookInput.y * lookSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -50f, 50f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
    #endregion

    #region GEKKO Movement

    /// C# translation from http://answers.unity3d.com/questions/155907/basic-movement-walking-on-walls.html
    /// Author: UA @aldonaletto, then Ryan Ferguson

    // Prequisites: create an empty GameObject, attach to it a Rigidbody w/ UseGravity unchecked
    // To empty GO also add BoxCollider and this script. Makes this the parent of the Player
    // Size BoxCollider to fit around Player model.

    private void gekkoMove()
    {
        // apply constant weight force according to character normal:
        GetComponent<Rigidbody>().AddForce(-gravity * GetComponent<Rigidbody>().mass * myNormal);

        // jump code - jump to wall or simple jump
        if (jumping) return; // abort Update while jumping to a wall

        Ray ray;
        RaycastHit hit;

        if (jump.WasPressedThisFrame()) // EDIT LATER
        { // jump pressed:

            Debug.Log("Jump.Read.");

            ray = new Ray(myTransform.position, myTransform.forward);
            if (Physics.Raycast(ray, out hit, jumpRange))
            { // wall ahead?
                JumpToWall(hit.point, hit.normal); // yes: jump to the wall
            }
            else if (isGrounded)
            { // no: if grounded, jump up
                GetComponent<Rigidbody>().linearVelocity += jumpSpeed * myNormal;
            }
        }

        // movement code - turn left/right with Horizontal axis:
        //myTransform.Rotate(0, moveVector.x * turnSpeed, 0);
        
        // update surface normal and isGrounded:
        ray = new Ray(myTransform.position, -myNormal); // cast ray downwards
        
        if (Physics.Raycast(ray, out hit))
        { // use it to update myNormal and isGrounded
            isGrounded = hit.distance <= distGround + deltaGround;
            surfaceNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            // assume usual ground normal to avoid "falling forever"
            surfaceNormal = Vector3.up;
        }
        
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, lerpSpeed);
        // find forward direction with new myNormal:
        Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
        // align character to the new myNormal while keeping the forward direction:
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, lerpSpeed);
        // move the character forth/back with Vertical axis:
        myTransform.Translate(moveVector.x * gekkoMoveSpeed, 0, moveVector.y * gekkoMoveSpeed);
    }

    private void JumpToWall(Vector3 point, Vector3 normal)
    {
        // jump to wall
        jumping = true; // signal it's jumping to wall
        GetComponent<Rigidbody>().isKinematic = true; // disable physics while jumping
        Vector3 orgPos = myTransform.position;
        Quaternion orgRot = myTransform.rotation;
        Vector3 dstPos = point + normal * (distGround + 0.5f); // will jump to 0.5 above wall
        Vector3 myForward = Vector3.Cross(myTransform.right, normal);
        Quaternion dstRot = Quaternion.LookRotation(myForward, normal);

        StartCoroutine(jumpTime(orgPos, orgRot, dstPos, dstRot, normal));
        //jumptime
    }

    private IEnumerator jumpTime(Vector3 orgPos, Quaternion orgRot, Vector3 dstPos, Quaternion dstRot, Vector3 normal)
    {
        for (float t = 0.0f; t < 1.0f;)
        {
            t += Time.deltaTime;
            myTransform.position = Vector3.Lerp(orgPos, dstPos, t);
            myTransform.rotation = Quaternion.Slerp(orgRot, dstRot, t);
            yield return null; // return here next frame
        }
        myNormal = normal; // update myNormal
        GetComponent<Rigidbody>().isKinematic = false; // enable physics
        jumping = false; // jumping to wall finished

    }

    #endregion

    #region CAT Movement

    private void CatMove()
    {
        // haha move u fucking cat
    }

    #endregion


    #region BAT Movement

    private void BatMove()
    {
        // haha move u stupid bat
    }

    #endregion

    private void OnCat(InputAction.CallbackContext context)
    {
        // this is the "cat" action callback method
        Debug.Log("CAT");
    }

    private void OnBat(InputAction.CallbackContext context)
    {
        // this is the "bat" action callback method
        Debug.Log("BAT");
    }

    private void OnGekko(InputAction.CallbackContext context)
    {
        // this is the "gekko" action callback method
        Debug.Log("GEKKO");
    }
}

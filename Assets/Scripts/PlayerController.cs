using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{

    // Input Action Configuration
    public InputActionAsset actions;

    private InputAction move;
    private InputAction CatMode;
    private InputAction BatMode;
    private InputAction GekkoMode;

    // General
    private string form;
    private Vector2 moveVector;
    private bool isGrounded;


    // Gekko
    private float moveSpeed = 6; // move speed
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
        jump = actions.FindActionMap("Player").FindAction("Jump")
       
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
        form = "gekko";
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // our update loop polls the "move" action value each frame
        moveVector = moveAction.ReadValue<Vector2>();

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
                Debug.Log("GEKKO TIME...");

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

        if (Input.GetButtonDown("Jump")) // EDIT LATER
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
        myTransform.Rotate(0, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0);
        
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
        
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, lerpSpeed * Time.deltaTime);
        // find forward direction with new myNormal:
        Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
        // align character to the new myNormal while keeping the forward direction:
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, lerpSpeed * Time.deltaTime);
        // move the character forth/back with Vertical axis:
        myTransform.Translate(0, 0, Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
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

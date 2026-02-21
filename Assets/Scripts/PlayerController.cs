using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    // ─── Enums ────────────────────────────────────────────────────────────────
    public enum Form { Cat, Bat, Gekko }

    // ─── Input ────────────────────────────────────────────────────────────────
    public InputActionAsset actions;
    private InputAction move;
    private InputAction jump;
    private InputAction look;

    // ─── General ──────────────────────────────────────────────────────────────
    private Form currentForm;
    private Vector2 moveVector;
    private bool isGrounded;

    private float jumpBufferTime = 0.15f; // seconds the jump input is remembered
    private float jumpBufferCounter = 0f;
    private float coyoteTime = 0.12f; // seconds after walking off a ledge you can still jump
    private float coyoteCounter = 0f;

    // ─── Camera / Look ────────────────────────────────────────────────────────
    [Header("Camera")]
    public Transform cameraPivot; // child of player
    private CinemachineCamera virtualCamera; // cinemachin

    [Header("Look Settings")]
    public float lookSensitivity = 10f;
    public float lookSmoothing = 0.1f;
    private float currentYaw;
    private float currentPitch;

    private float pitch;
    private Vector2 currentLookInput;
    private Vector2 lookInputVelocity;

    // ─── Cached Components ────────────────────────────────────────────────────
    private Rigidbody rb;
    private Transform myTransform;
    public BoxCollider boxCollider; // assign in Inspector

    // ─── Gekko Settings ───────────────────────────────────────────────────────
    [Header("Gekko")]
    public float gekkoMoveSpeed = 0.5f;
    public float turnSpeed = 90f;
    public float lerpSpeed = 10f;
    public float gekkoGravity = 10f;
    public float deltaGround = 0.2f;
    public float jumpSpeed = 10f;
    public float jumpRange = 10f;

    private Vector3 surfaceNormal;
    private Vector3 myNormal;
    private float distGround;
    private bool jumping;
    private float vertSpeed;

    // ─── Cat Settings ─────────────────────────────────────────────────────────
    [Header("Cat")]
    public float catMoveSpeed = 8f;
    public float catJumpForce = 12f;
    public float catFallDamping = 0.85f; // multiplier applied to fall velocity on land

    // ─── Bat Settings ─────────────────────────────────────────────────────────
    [Header("Bat")]
    public float batMoveSpeed = 7f;
    public float batFlySpeed = 5f;   // vertical flight speed
    private bool batIsFlying = false;

    // ─────────────────────────────────────────────────────────────────────────
    #region Lifecycle

    void Awake()
    {
        // Singleton — must be in Awake so other scripts can access it in their Awake
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Cache components
        rb = GetComponent<Rigidbody>();
        myTransform = transform;

        // Bind input actions
        var playerMap = actions.FindActionMap("Player");
        move = playerMap.FindAction("Move");
        jump = playerMap.FindAction("Jump");
        look = playerMap.FindAction("Look");

        playerMap.FindAction("CatMode").performed += OnCat;
        playerMap.FindAction("BatMode").performed += OnBat;
        playerMap.FindAction("GekkoMode").performed += OnGekko;
    }

    void OnEnable() => actions.FindActionMap("Player").Enable();
    void OnDisable() => actions.FindActionMap("Player").Disable();

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        EnterForm(Form.Gekko); // default form

        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        virtualCamera.Follow = cameraPivot;
        virtualCamera.LookAt = cameraPivot;
    }

    //void Update()
    //{
    //    HandleLook();
    //}

    void LateUpdate()
    {
        HandleLook();
    }

    void FixedUpdate()
    {
        moveVector = move.ReadValue<Vector2>();

        switch (currentForm)
        {
            case Form.Cat: CatMove(); break;
            case Form.Bat: BatMove(); break;
            case Form.Gekko: GekkoMove(); break;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Form Transitions

    private void EnterForm(Form next)
    {
        ExitCurrentForm();
        currentForm = next;

        switch (currentForm)
        {
            case Form.Gekko:
                rb.useGravity = false;     // gekko uses its own gravity
                rb.freezeRotation = true;
                myNormal = myTransform.up;
                distGround = boxCollider.bounds.extents.y - boxCollider.center.y;
                break;

            case Form.Cat:
                rb.useGravity = true;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                myNormal = Vector3.up;  // cat always walks on normal ground
                break;

            case Form.Bat:
                rb.useGravity = false;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                batIsFlying = false;
                break;
        }

        Debug.Log($"Entered form: {currentForm}");
    }

    private void ExitCurrentForm()
    {
        // Clean up state from previous form
        switch (currentForm)
        {
            case Form.Gekko:
                if (jumping)
                {
                    StopAllCoroutines();
                    rb.isKinematic = false;
                    jumping = false;
                }
                rb.freezeRotation = false;
                break;

            case Form.Cat:
                break;

            case Form.Bat:
                rb.useGravity = false;
                break;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Look

    private void HandleLook()
    {
        Vector2 rawLookInput = look.ReadValue<Vector2>();

        currentYaw += rawLookInput.x * lookSensitivity * Time.deltaTime;
        currentPitch -= rawLookInput.y * lookSensitivity * Time.deltaTime;

        float pitchMin = (currentForm == Form.Bat) ? -90f : -50f;
        float pitchMax = (currentForm == Form.Bat) ? 90f : 50f;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

        Vector3 yawAxis = (currentForm == Form.Gekko) ? myNormal : Vector3.up;
        myTransform.rotation = Quaternion.AngleAxis(currentYaw, yawAxis);
        cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Gekko Movement

    // Original wall-walking logic: http://answers.unity3d.com/questions/155907/basic-movement-walking-on-walls.html
    // Author: UA @aldonaletto — adapted here

    private void GekkoMove()
    {
        // Apply surface gravity
        rb.AddForce(-gekkoGravity * rb.mass * myNormal);

        if (jumping) return;

        Ray ray;
        RaycastHit hit;

        // Jump input
        if (jump.WasPressedThisFrame())
        {
            ray = new Ray(myTransform.position, myTransform.forward);
            if (Physics.Raycast(ray, out hit, jumpRange))
                JumpToWall(hit.point, hit.normal);
            else if (isGrounded)
                rb.linearVelocity += jumpSpeed * myNormal;
        }

        // Update surface normal and grounded state
        ray = new Ray(myTransform.position, -myNormal);
        if (Physics.Raycast(ray, out hit))
        {
            isGrounded = hit.distance <= distGround + deltaGround;
            surfaceNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            surfaceNormal = Vector3.up;
        }

        // Smoothly align to surface — frame-rate independent
        float t = lerpSpeed * Time.fixedDeltaTime;
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, t);

        Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, t);

        myTransform.Translate(moveVector.x * gekkoMoveSpeed, 0, moveVector.y * gekkoMoveSpeed);
    }

    private void JumpToWall(Vector3 point, Vector3 normal)
    {
        jumping = true;
        rb.isKinematic = true;

        Vector3 orgPos = myTransform.position;
        Quaternion orgRot = myTransform.rotation;
        Vector3 dstPos = point + normal * (distGround + 0.5f);
        Vector3 myFwd = Vector3.Cross(myTransform.right, normal);
        Quaternion dstRot = Quaternion.LookRotation(myFwd, normal);

        StartCoroutine(JumpToWallRoutine(orgPos, orgRot, dstPos, dstRot, normal));
    }

    private IEnumerator JumpToWallRoutine(
        Vector3 orgPos, Quaternion orgRot,
        Vector3 dstPos, Quaternion dstRot,
        Vector3 normal)
    {
        for (float t = 0f; t < 1f;)
        {
            t += Time.deltaTime;
            myTransform.position = Vector3.Lerp(orgPos, dstPos, t);
            myTransform.rotation = Quaternion.Slerp(orgRot, dstRot, t);
            yield return null;
        }

        myNormal = normal;
        rb.isKinematic = false;
        jumping = false;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Cat Movement

    private void CatMove()
    {
        // Grounded check — offset ray origin to bottom of collider
        Vector3 rayOrigin = myTransform.position;
        float rayLength = distGround + deltaGround;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, rayLength);

        // Coyote time — counts down after leaving ground
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        // Jump buffer — counts down after pressing jump
        if (jump.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.fixedDeltaTime;

        // Horizontal movement
        Vector3 moveDir = myTransform.right * moveVector.x
                               + myTransform.forward * moveVector.y;
        Vector3 targetVelocity = moveDir * catMoveSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        // Jump — consume buffer if we have ground (or coyote time)
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, catJumpForce, rb.linearVelocity.z);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // Reduced fall damage
        if (isGrounded && rb.linearVelocity.y < -1f)
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                rb.linearVelocity.y * catFallDamping,
                rb.linearVelocity.z
            );
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Bat Movement

    private void BatMove()
    {
        // Horizontal movement
        Vector3 horizontalMove = (myTransform.right * moveVector.x
                                + myTransform.forward * moveVector.y) * batMoveSpeed;

        // Vertical: jump input controls ascent while held; gravity pulls down otherwise
        float verticalVelocity = rb.linearVelocity.y;
        if (jump.IsPressed())
            verticalVelocity = batFlySpeed;
        else
            verticalVelocity -= 9.8f * Time.fixedDeltaTime; // manual gravity

        rb.linearVelocity = new Vector3(horizontalMove.x, verticalVelocity, horizontalMove.z);

        // Bat can only land upside-down or on walls — orient accordingly
        Ray ray;
        RaycastHit hit;

        // Check ceiling
        ray = new Ray(myTransform.position, Vector3.up);
        if (Physics.Raycast(ray, out hit, distGround + deltaGround))
        {
            isGrounded = true;
            surfaceNormal = hit.normal; // points downward from ceiling
            Debug.Log("BAT: Ceiling found");
        }
        // Check wall
        else
        {
            ray = new Ray(myTransform.position, -myTransform.right);
            if (Physics.Raycast(ray, out hit, distGround + deltaGround) ||
                Physics.Raycast(new Ray(myTransform.position, myTransform.right), out hit, distGround + deltaGround))
            {
                isGrounded = true;
                surfaceNormal = hit.normal;
                Debug.Log("BAT: Wall found");
            }
            else
            {
                isGrounded = false;
                surfaceNormal = Vector3.up; // fallback: no surface nearby
                Debug.Log("BAT: No surface found");
            }
        }

        if (isGrounded)
        {
            // Align bat to surface
            float t = lerpSpeed * Time.fixedDeltaTime;
            Vector3 batFwd = Vector3.Cross(myTransform.right, surfaceNormal);
            Quaternion targetRot = Quaternion.LookRotation(batFwd, surfaceNormal);
            myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, t);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Input Callbacks

    private void OnCat(InputAction.CallbackContext ctx) => EnterForm(Form.Cat);
    private void OnBat(InputAction.CallbackContext ctx) => EnterForm(Form.Bat);
    private void OnGekko(InputAction.CallbackContext ctx) => EnterForm(Form.Gekko);

    #endregion
}
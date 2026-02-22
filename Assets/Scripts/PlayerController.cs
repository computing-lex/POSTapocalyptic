using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    #region Variables

    public static PlayerController Instance { get; private set; }

    // ─── Enums ────────────────────────────────────────────────────────────────
    public enum Form { Cat, Bat, Gekko, Rat }

    // ─── Crosshair/UI ───────────────────────────────────────────────────────────────

    public Image crosshair;
    public Sprite intCrosshair;
    public Sprite catCrosshair;
    public Sprite ratCrosshair;
    public Sprite batCrosshair;

    // ─── Input ────────────────────────────────────────────────────────────────
    public InputActionAsset actions;
    private InputAction move;
    private InputAction jump;
    private InputAction look;

    // ─── General ──────────────────────────────────────────────────────────────
    private Form currentForm;
    private Vector2 moveVector;
    private bool isGrounded;
    private bool jumpPressedThisUpdate = false;

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
    public BoxCollider boxCollider;
    private CharacterController cc; 

    // ─── Mesh / Animator Settings ───────────────────────────────────────────────────────
    public GameObject ratMesh;
    public GameObject catMesh;
    public GameObject batMesh;

    public RuntimeAnimatorController ratController;
    public RuntimeAnimatorController catController;
    public RuntimeAnimatorController batController;

    public Animator ratAnimation;
    public Animator catAnimation;
    public Animator batAnimation;

    // ─── Gekko Settings ───────────────────────────────────────────────────────

    [Header("Gekko")]
    public Transform gekkoMesh;
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

    private float gekkoVerticalVelocity = 0f;

    // ─── Rat Settings ─────────────────────────────────────────────────────────
    [Header("Rat")]
    public float ratMoveSpeed = 4f;
    public float ratJumpForce = 5f;      // shorter jump than cat
    public float ratCapsuleRadius = 0.2f;  // smaller collider radius for squeezing
    private float ratVerticalVelocity = 0f;


    // ─── Cat Settings ─────────────────────────────────────────────────────────
    [Header("Cat")]
    public float catMoveSpeed = 8f;
    public float catJumpForce = 12f;
    public float catFallDamping = 0.85f; // multiplier applied to fall velocity on land
    private float catVerticalVelocity = 0f; 


    // ─── Bat Settings ─────────────────────────────────────────────────────────
    [Header("Bat")]
    public float batMoveSpeed = 7f;
    public float batFlySpeed = 5f;   // vertical flight speed
    private bool batIsFlying = false;

    private float batVerticalVelocity = 0f;

    #endregion

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

        cc = GetComponent<CharacterController>();
        cc.enabled = false;
    }

    void OnEnable() => actions.FindActionMap("Player").Enable();
    void OnDisable() => actions.FindActionMap("Player").Disable();

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        EnterForm(Form.Rat); // default form
        ratMesh.SetActive(true);
        catMesh.SetActive(false);
        batMesh.SetActive(false);

        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        virtualCamera.Follow = cameraPivot;
        virtualCamera.LookAt = cameraPivot;
    }

    void Update()
    {
        if (jump.WasPressedThisFrame())
            jumpPressedThisUpdate = true;
    }

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
            case Form.Rat: RatMove(); break;
            case Form.Gekko: RatMove(); break;
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
                rb.useGravity = false;
                rb.freezeRotation = true;
                myNormal = myTransform.up;
                distGround = boxCollider.bounds.extents.y - boxCollider.center.y;

                //crosshair.sprite
                // gameobject.setActive()
                break;
            case Form.Cat:
                rb.isKinematic = true;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                myNormal = Vector3.up;
                cc.enabled = true;
                cc.radius = 0.5f;
                cc.height = 2f;
                catVerticalVelocity = 0f;

                // mesh & animation
                catMesh.SetActive(true);
                crosshair.sprite = catCrosshair;
                break;

            case Form.Bat:
                rb.useGravity = false;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                batIsFlying = false;

                // mesh & animation
                batMesh.SetActive(true);
                crosshair.sprite = batCrosshair;
                break;

            case Form.Rat:
                rb.isKinematic = true;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                myNormal = Vector3.up;
                cc.enabled = true;
                cc.radius = 0.2f;  // smaller — can squeeze through gaps or something HAHAHAHAHAH
                cc.height = 0.8f;
                ratVerticalVelocity = 0f;
                distGround = cc.height / 2f;

                // mesh & animation
                ratMesh.SetActive(true);
                crosshair.sprite = ratCrosshair;
                break;
        }
        Debug.Log($"Entered form: {currentForm}");
    }

    private void ExitCurrentForm()
    {
        // Reset shared state
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        jumpPressedThisUpdate = false;
        isGrounded = false;

        switch (currentForm)
        {
            case Form.Gekko:
                if (jumping)
                {
                    StopAllCoroutines();
                    rb.isKinematic = false;
                    jumping = false;
                }
                gekkoVerticalVelocity = 0f;
                rb.freezeRotation = false;
                break;
            case Form.Cat:
                catVerticalVelocity = 0f;
                cc.enabled = false;
                rb.isKinematic = false;
                catMesh.SetActive(false);
                break;
            case Form.Bat:
                batVerticalVelocity = 0f;
                rb.useGravity = false;
                batMesh.SetActive(false);
                break;
            case Form.Rat:
                cc.enabled = false;
                rb.isKinematic = false;
                ratVerticalVelocity = 0f;
                boxCollider.size = new Vector3(1f, 1f, 1f);
                ratMesh.SetActive(false);
                break;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Look

    private void HandleLook()
    {
        Vector2 rawLookInput = look.ReadValue<Vector2>();

        float yawDelta = rawLookInput.x * lookSensitivity * Time.deltaTime;
        currentPitch -= rawLookInput.y * lookSensitivity * Time.deltaTime;

        float pitchMin = (currentForm == Form.Bat) ? -90f : -50f;
        float pitchMax = (currentForm == Form.Bat) ? 90f : 50f;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

        Vector3 yawAxis = (currentForm == Form.Gekko) ? myNormal : Vector3.up;
        Quaternion yawDeltaQ = Quaternion.AngleAxis(yawDelta, yawAxis);

        if (cc.enabled)
            myTransform.rotation = myTransform.rotation * yawDeltaQ;
        else
            rb.MoveRotation(rb.rotation * yawDeltaQ);

        cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Gekko Movement

    private void GekkoMove()
    {
        Ray ray;
        RaycastHit hit;

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

        Vector3 surfaceRight = Vector3.Cross(myNormal, myTransform.forward).normalized;
        Vector3 surfaceForward = Vector3.Cross(surfaceRight, myNormal).normalized;
        Vector3 moveDir = surfaceRight * moveVector.x
                               + surfaceForward * moveVector.y;

        if (moveDir.magnitude > 0.01f)
        {
            Vector3 moveDirNorm = moveDir.normalized;
            bool foundSurface = false;

            // Priority 1 — diagonal into surface (floor->wall, wall->ceiling)
            ray = new Ray(myTransform.position, (moveDirNorm - myNormal).normalized);
            if (Physics.Raycast(ray, out hit, distGround + 1.0f))
            {
                surfaceNormal = hit.normal;
                foundSurface = true;
            }

            // Priority 2 — forward along surface (walls directly ahead)
            if (!foundSurface)
            {
                ray = new Ray(myTransform.position, moveDirNorm);
                if (Physics.Raycast(ray, out hit, distGround + 1.0f))
                {
                    surfaceNormal = hit.normal;
                    foundSurface = true;
                }
            }

            // Priority 3 — top->side, only on horizontal surfaces
            bool onHorizontalSurface = Vector3.Angle(myNormal, Vector3.up) < 45f;
            if (!foundSurface && onHorizontalSurface)
            {
                Vector3 aheadPos = myTransform.position + moveDirNorm * (distGround + 0.5f);
                ray = new Ray(aheadPos, Vector3.down);
                if (Physics.Raycast(ray, out hit, distGround + 1.5f))
                {
                    if (Vector3.Angle(hit.normal, myNormal) > 45f)
                    {
                        surfaceNormal = hit.normal;
                        foundSurface = true;
                    }
                }
            }

            // Priority 4 — side->bottom, only on vertical surfaces moving "down" in surface space
            bool onVerticalSurface = Vector3.Angle(myNormal, Vector3.up) >= 45f;
            bool movingDownSurface = moveVector.y < -0.1f; // S key = moving down the surface
            if (!foundSurface && onVerticalSurface && movingDownSurface)
            {
                Vector3 aheadPos = myTransform.position + moveDirNorm * (distGround + 0.5f);
                ray = new Ray(aheadPos, Vector3.down);
                if (Physics.Raycast(ray, out hit, distGround + 2.0f))
                {
                    if (Vector3.Angle(hit.normal, myNormal) > 45f)
                    {
                        surfaceNormal = hit.normal;
                        foundSurface = true;
                    }
                }
            }

            // Priority 5 — ahead + negative normal fallback
            if (!foundSurface)
            {
                Vector3 aheadPos = myTransform.position + moveDirNorm * (distGround + 0.5f);
                ray = new Ray(aheadPos, -myNormal);
                if (Physics.Raycast(ray, out hit, distGround + 1.5f))
                    surfaceNormal = hit.normal;
            }
        }

        // Smoothly align surface normal
        float t = lerpSpeed * Time.deltaTime;
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, t);

        // Rotate only the mesh to align with surface
        Vector3 meshForward = Vector3.Cross(myTransform.right, myNormal);
        Quaternion meshTarget = Quaternion.LookRotation(meshForward, myNormal);
        gekkoMesh.rotation = Quaternion.Lerp(gekkoMesh.rotation, meshTarget, t);

        // Gravity when airborne
        if (!isGrounded)
            gekkoVerticalVelocity -= gekkoGravity * Time.deltaTime;
        else
            gekkoVerticalVelocity = 0f;

        Vector3 displacement = moveDir * gekkoMoveSpeed * Time.deltaTime;
        if (!isGrounded)
            displacement += myNormal * gekkoVerticalVelocity * Time.deltaTime;

        rb.MovePosition(rb.position + displacement);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Rat Movement

    private void RatMove()
    {
        ratAnimation.SetBool("isMoving", moveVector.magnitude > 0.1f);
        
        isGrounded = cc.isGrounded;

        // Coyote time
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpPressedThisUpdate)
        {
            jumpBufferCounter = jumpBufferTime;
            jumpPressedThisUpdate = false;
        }
        else
            jumpBufferCounter -= Time.deltaTime;

        // Gravity
        if (!isGrounded)
            ratVerticalVelocity -= 9.8f * Time.deltaTime;

        // Jump
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            ratVerticalVelocity = ratJumpForce;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // Landing
        if (isGrounded && ratVerticalVelocity < 0f)
            ratVerticalVelocity = 0f;

        Vector3 moveDir = myTransform.right * moveVector.x
                             + myTransform.forward * moveVector.y;
        Vector3 displacement = (moveDir * ratMoveSpeed + Vector3.up * ratVerticalVelocity)
                             * Time.deltaTime;

        cc.Move(displacement);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Cat Movement

    private void CatMove()
    {
        catAnimation.SetBool("isMoving", moveVector.magnitude > 0.1f);

        isGrounded = cc.isGrounded;

        // Coyote time
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpPressedThisUpdate)
        {
            jumpBufferCounter = jumpBufferTime;
            jumpPressedThisUpdate = false;
        }
        else
            jumpBufferCounter -= Time.deltaTime;

        // Gravity
        if (!isGrounded)
            catVerticalVelocity -= 9.8f * Time.deltaTime;

        // Jump
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            catVerticalVelocity = catJumpForce;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // Landing
        if (isGrounded && catVerticalVelocity < 0f)
        {
            if (catVerticalVelocity < -1f)
                catVerticalVelocity *= catFallDamping;
            else
                catVerticalVelocity = 0f;
        }

        Vector3 moveDir = myTransform.right * moveVector.x
                             + myTransform.forward * moveVector.y;
        Vector3 displacement = (moveDir * catMoveSpeed + Vector3.up * catVerticalVelocity)
                             * Time.deltaTime;

        cc.Move(displacement);
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
            batAnimation.SetBool("isMoving", false);
            //Quaternion targetRot = Quaternion.LookRotation(batFwd, surfaceNormal);
            //myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, t);
        }
        else
        {
            batAnimation.SetBool("isMoving", true);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Input Callbacks

    private void OnCat(InputAction.CallbackContext ctx) => EnterForm(Form.Cat);
    private void OnBat(InputAction.CallbackContext ctx) => EnterForm(Form.Bat);
    private void OnGekko(InputAction.CallbackContext ctx) => EnterForm(Form.Rat); // got lazy, sry LOL FUCKT HE GEKCKO I HATE THAT GUYUS

    #endregion
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
//using UnityEngine.Windows;

public enum playerStates
{
    Grounded,
    Airborne,
    Vaulting,
    Recovering,
    Sliding,
    Clinging,
    Sneaking,
    Hiding
}

public class StealthMovement : MonoBehaviour
{
    [Header("Maneuverability")]
    public float acceleration;
    public float airAcceleration;
    public float maxHorizontalSpeed;
    public float maxVerticalSpeed;
    public float globalGravity;
    public float fastFallGravity;
    public float fastFallSpeedCap;

    [Header("Jump")]
    public float jumpHeight;

    [Header("Buffer and timing")]
    public float bufferWindow;

    [Header("Vault")]
    public float vaultAnimationTime;
    public float vaultRecoveryAnimationTime;

    public float forwardVaultSpeedIncrease; //added on to maxspeed
    public float forwardVaultVerticalHeight;
    public float boostLength; //any boost to player speed (such as forward vault) will go away after this amount of time

    public float bigVaultHorizontalSpeed; //not increased over normal top speed
    public float bigVaultVerticalHeight;

    public float upVaultVerticalHeight;

    public float backVaultVerticalHeight;
    public float backVaultHorizontalSpeed; //not increased over normal top speed

    public GameObject[] vaultParticles;
    private Coroutine vaultCoroutine;

    [Header("Slide")]
    public float slideSpeedIncrease;
    public float slideAnimationTime;

    [Header("Wall Jump")]
    public float wallJumpVerticalHeight;
    public float wallJumpHorizontalSpeed;
    public float wallSlideGravity;
    public float wallSlideSpeedCap;

    [Header("Sneak")]
    public float sneakSpeed;
    public float sneakNoiseRadius;

    [Header("Other Script Requirements")]
    public LayerMask levelGeometry; //don't put more than one layer in here
    public float floorDetectionMargin; //Needs to be above zero
    public float wallDetectionMargin; //Needs to be above zero

    [Header("Arnold's janky public variables")]
    public AnimationController controller;
    public AnimationController2 controller2;
    public Animator animator;
    Transform space;


    
    [SerializeField]
    public playerStates state = playerStates.Grounded;
    enum movementTech
    {
        longJump,
        bigJump,
        highJump,
        backFlip,
        slide,
        nothing
    }

    private class buttonInput
    {
        public float downLast; //the last time the button was pressed
        public bool down; //is the button currently being pressed?
        public bool justPressed; //if the button was pressed last frame
        public bool read; //past tense
        public bool updatable; //needed this to fix a bug for some reason
        public bool fixedUpdatable;
    }

    private KeyCode[] buttons =
        {KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.LeftShift,
        KeyCode.RightShift, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.Space};

    
    private Dictionary<KeyCode, buttonInput> buffer = new Dictionary<KeyCode, buttonInput>();
    private Rigidbody2D rb;
    private BoxCollider2D bc;
    private List<TileMapCorner> corners;
    private List<GameObject> hideableObjects;
    private float boostAmt;
    private Vector3 vaultTarget; //points to where we are climbing up to
    private float height;
    private float width;
    private float moveStartTime;
    private movementTech bufferedAction;
    private float noiseRadius; //maximum generally 1
    private SpriteRenderer sr;
    private GameObject hideObject;
    public bool jumpStart = false;
    public bool slideStart = false;
    public bool slideExit = false;
    public bool wallParticles = false;
    public bool sneak = false;
    private bool sneakyHide = false;
    private bool wallgrab = false;
    [SerializeField] public float facingDir; //-1 means left, 1 means right (or negative positive works)
    public float timescale = 1f;
    private bool landing = false;
    private bool prevGround = false;
    void Awake()
    {
#if UNITY_EDITOR
        //QualitySettings.vSyncCount = 0;  // VSync must be disabled
        //Application.targetFrameRate = 10;
#endif
    }
    // Start is called before the first frame update
    void Start()
    {
        void Awake() {
    #if UNITY_EDITOR
	QualitySettings.vSyncCount = 0;  // VSync must be disabled
	Application.targetFrameRate = 45;
    #endif
}

        controller = GetComponent<AnimationController>();       
        controller2 = GetComponent<AnimationController2>();
        Physics2D.IgnoreLayerCollision(6, 12, true); //move this to game manager later
        corners = new List<TileMapCorner>();
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
        height = bc.bounds.size.y;
        width = bc.bounds.size.x;
        maxHorizontalSpeed = Mathf.Abs(maxHorizontalSpeed);
        facingDir = 1;
        if (boostLength < 0.05f) boostLength = 0.05f;
        bufferedAction = movementTech.nothing;
        sr = GetComponent<SpriteRenderer>();
        noiseRadius = 0;
        Physics2D.gravity = new Vector2(0, -globalGravity);
        hideObject = null; hideableObjects = new List<GameObject>();
        animator = GetComponentInChildren<Animator>();
        space = GetComponent<Transform>();
        sneakyHide = false;
        foreach(KeyCode key in buttons)
        {
            buttonInput b = new buttonInput();
            b.downLast = float.NegativeInfinity;
            b.justPressed = false;
            b.down = false;
            b.read = true;
            b.updatable = true;
            buffer.Add(key, b);
        }
    }

    private void readInputsDown() //called in update
    {
        foreach(KeyValuePair<KeyCode, buttonInput> input in buffer)
        {
            if (Input.GetKeyDown(input.Key))
            {
                if(input.Value.updatable)
                {
                    input.Value.downLast = Time.time;
                    input.Value.justPressed = true;
                }
                else 
                { 
                    input.Value.updatable = true;
                }
            }
        }
    }

    private void readInputsDownBackup() //called in fixed update - for potato laptops that run less than 50 frames I think
    {
        foreach (KeyValuePair<KeyCode, buttonInput> input in buffer)
        {
            if (Input.GetKeyDown(input.Key) && input.Value.updatable)
            {
                input.Value.downLast = Time.time;
                input.Value.justPressed = true;
                input.Value.updatable = false;
                input.Value.fixedUpdatable = false;
            }
            else if(!Input.GetKey(input.Key))
            {
                input.Value.fixedUpdatable = true;
            }
            else if (input.Value.fixedUpdatable)
            {
                input.Value.updatable = true;
            }
            if (input.Value.justPressed)
            {
                //Debug.Log("Button pressed: " + input.Key + " Time: " + Time.time);
                input.Value.read = false;
            }
        }
    }

    private void readInputs() //called in fixedupdate
    {
        foreach (KeyValuePair<KeyCode, buttonInput> input in buffer)
        {
            if (Input.GetKey(input.Key))
            {
                input.Value.down = true;
            }
            else
            {
                input.Value.down = false;
                input.Value.justPressed = false;
            }
            
        }
    }
    private void resetInputs() //maybe uneeded
    {
        foreach (KeyValuePair<KeyCode, buttonInput> input in buffer)
        {

            input.Value.justPressed = false;
        }
    }
    private bool getInput(KeyCode key)
    {
        try
        {
            return buffer[key].down;
        }
        catch
        {
            return false;
        }
    }
    private bool getInputDown(KeyCode key)
    {
        try
        {
            if ((Time.time - buffer[key].downLast <= bufferWindow || buffer[key].justPressed) && !buffer[key].read)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
        
    }
    private void useInput(KeyCode key)
    {
        if (key == KeyCode.Space)
        {
            Debug.Log("Space read");
        }

        try
        {
            buffer[key].read = true;
        }
        catch
        {
            Debug.Log("Input no exist");
        }
    }

    public Vector3 position()
    {
        return transform.TransformPoint(bc.offset);
    }

    public float getNoiseRadius()
    {
        return noiseRadius;
    }

    public bool isHiding()
    {
        return state == playerStates.Hiding;
    }
    public void setHideableObject(GameObject hide)
    {
        hideableObjects.Add(hide);
    }
    public void unsetHideableObject(GameObject hide)
    {
        if (hideableObjects.Contains(hide))
        {
            hideableObjects.Remove(hide);
        }
    }

    private void updateAnimationValue()
    {
        //controller.xVelocity = Mathf.Abs(rb.velocity.x);
        //bool prevGround = controller.isGrounded;
        //controller.isGrounded = isGrounded();
        //if (!prevGround && controller.isGrounded)
        //    controller.landing = true;
        //else 
        //    controller.landing = false;
        //if (jumpStart)
        //    controller.jumpStart = true;
        //else
        //    controller.jumpStart = false;
        //if(isWalled(facingDir))
        //    controller.walled = true;
        //else 
        //    controller.walled = false;
        //if (slideStart)
        //    controller.slideStart = true;
        //else
        //    controller.slideStart = false;
        //if (slideExit)
        //    controller.slideExit = true;
        //else
        //    controller.slideExit = false;
        //if (Input.GetKeyDown(KeyCode.Space))
        //    controller.space = true;
        //else
        //    controller.space = false;

        //controller.sneaking = (state == playerStates.Sneaking);
        //controller.wallParticles = wallParticles;
        //if(wallgrab)
        //    controller.wallGrab = true;
        //else 
        //    controller.wallGrab = false;
        //print(state == playerStates.Sneaking);

        //slideStart = false;
        //slideExit = false;
        //jumpStart = false;
        //wallParticles = false;
        landing = false;
        if (isGrounded())
        {
            if (!prevGround)
            {
                landing = true;
            }
            prevGround = true;
        }
        else
            prevGround = false;

        controller2.landing = landing;
        controller2.jumpStart = jumpStart;
        controller2.slideStart = slideStart;
        controller2.slideStop = slideExit;
        jumpStart = false;
        slideStart = false;
        slideExit = false;
    }
    private bool isGrounded()
    {
        Vector2 pos = transform.TransformPoint(bc.offset);
        float width = bc.bounds.size.x;
        float height = bc.bounds.size.y;

        RaycastHit2D leftCorner = Physics2D.Raycast(new Vector2(pos.x - width / 2f,
                                                        pos.y - height / 2f),
                                                        -Vector3.up, floorDetectionMargin, levelGeometry);
        RaycastHit2D rightCorner = Physics2D.Raycast(new Vector2(pos.x + width / 2f,
                                                        pos.y - height / 2f),
                                                        -Vector3.up, floorDetectionMargin, levelGeometry);
        return (leftCorner.collider != null || rightCorner.collider != null);
    }
    public Collider2D isWalled(float dir, out RaycastHit2D hit)
    {
        Vector2 pos = transform.TransformPoint(bc.offset);
        float width = bc.bounds.size.x;
        float height = bc.bounds.size.y;
        RaycastHit2D topCorner = Physics2D.Raycast(new Vector2(pos.x + (width / 2f - wallDetectionMargin) * dir,
                                                       pos.y + (0.5f) * height / 2f),
                                                       Vector3.right * dir, wallDetectionMargin * 2, levelGeometry);
        RaycastHit2D bottomCorner = Physics2D.Raycast(new Vector2(pos.x + (width / 2f - wallDetectionMargin) * dir,
                                                        pos.y - (0) * height / 2f),
                                                        Vector3.right * dir, wallDetectionMargin*2, levelGeometry);
        if (topCorner.collider != null || bottomCorner.collider != null)
        {
            hit = topCorner;
            return topCorner.collider;
        }
        hit = topCorner;
        return null;
    }
    public Collider2D isWalled(float dir)
    {
        Vector2 pos = transform.TransformPoint(bc.offset);
        float width = bc.bounds.size.x;
        float height = bc.bounds.size.y;
        RaycastHit2D topCorner = Physics2D.Raycast(new Vector2(pos.x + (width / 2f - wallDetectionMargin) * dir,
                                                        pos.y + (0.5f) * height / 2f),
                                                        Vector3.right * dir, wallDetectionMargin * 2, levelGeometry);
        RaycastHit2D bottomCorner = Physics2D.Raycast(new Vector2(pos.x + (width / 2f - wallDetectionMargin) * dir,
                                                         pos.y - (0) * height / 2f),
                                                         Vector3.right * dir, wallDetectionMargin * 2, levelGeometry);
        if (topCorner.collider != null || bottomCorner.collider != null)
        {
            return topCorner.collider;
        }
        return null;
    }
    private bool isCeilinged() //not done yet - this will be for detecting if we can come out of slide/crounch
    {
        Vector2 pos = transform.TransformPoint(bc.offset);
        float width = bc.bounds.size.x;
        float height = bc.bounds.size.y;
        RaycastHit2D leftCorner = Physics2D.Raycast(new Vector2(pos.x - width / 2f,
                                                        pos.y),
                                                        Vector3.up, 1f, levelGeometry);
        RaycastHit2D rightCorner = Physics2D.Raycast(new Vector2(pos.x + width / 2f,
                                                        pos.y),
                                                        Vector3.up, 1f, levelGeometry);
        return (leftCorner.collider != null || rightCorner.collider != null);
    }


    private void boost(float amount)
    {
        boostAmt = amount;
        if (Input.GetAxisRaw("Horizontal") > 0)
        {
            rb.velocity = new Vector2(maxHorizontalSpeed + boostAmt, rb.velocity.y);
        }
        if (Input.GetAxisRaw("Horizontal") < 0)
        {
            rb.velocity = new Vector2(-maxHorizontalSpeed - boostAmt, rb.velocity.y);
        }
    }

    private void setFacingDir(float dir)
    {
        if (dir == 0)
        {
            Debug.LogError("0 is not a direction. Try -1 or 1");
        }
        else
        {
            facingDir = Mathf.Sign(dir);
            if (dir < 0)
            {
                //sr.flipX = true;
                space.localScale = new UnityEngine.Vector3(-1, 1, 1);
            }
            else
            {
                space.localScale = new UnityEngine.Vector3(1, 1, 1);

            }
            //TODO: activate animations for facing the other way
        }
    }
    public void setCorner(TileMapCorner corn)
    {
        corners.Add(corn);
    }
    public void unsetCorner(TileMapCorner corn)
    {
        if (corners.Contains(corn))
        {
            corners.Remove(corn);
        }
    }
    private void enterHide(GameObject obj)
    {
        hideObject = obj;
        transform.position = hideObject.transform.position
             - Vector3.up * (hideObject.GetComponent<SpriteRenderer>().bounds.size.y / 2
             - bc.bounds.size.y / 2);
        GetComponent<SortingGroup>().sortingLayerName = "HidingLayer";
        gameObject.layer = 10;
        hideObject.GetComponent<Hideable>().fadeOut();
        if(state == playerStates.Sneaking)
        {
            sneakyHide = true;
        }
        else
        {
            sneakyHide = false;
        }

    }
    private void exitHide()
    {
        hideObject.GetComponent<Hideable>().fadeIn();
        hideObject = null;
        GetComponent<SortingGroup>().sortingLayerName = "Entities";
        gameObject.layer = 6;
    }

    private void exitVault()
    {
        transform.position = vaultTarget;
        rb.gravityScale = 1;
        Physics2D.IgnoreLayerCollision(gameObject.layer, 3, false);
        //bc.enabled = true;
    }
    private void enterVault(TileMapCorner corn)
    {
        controller.vault = true;
        rb.gravityScale = 1.0f;
        vaultTarget = corn.getWorldCoord() + new Vector3(width / 2f * facingDir, height / 2f, 0);
        Vector3 vaultVector = vaultTarget - transform.position;
        rb.gravityScale = 0;
        //bc.enabled = false;
        Physics2D.IgnoreLayerCollision(gameObject.layer, 3, true);
        rb.velocity = vaultVector / vaultAnimationTime;
        moveStartTime = Time.time;

        bufferedAction = movementTech.nothing;
        //TODO - activate vault animation - somehow set it to vaultTime length
    }
    private void enterSlide()
    {
        slideStart = true;
        moveStartTime = Time.time;
        bc.size = new Vector2(width, height/2);
        bc.offset = new Vector2(0, -height/4);
        rb.velocity = Vector2.right * (maxHorizontalSpeed + slideSpeedIncrease) * facingDir;
    }
    private void exitSlide()
    {
        slideExit = true;
        bc.size = new Vector2(width, height);
        bc.offset = Vector2.zero;
    }
    private void enterSneak()
    {
        bc.size = new Vector2(width, height/2);
        bc.offset = new Vector2(0, -height/4);
    }
    private void exitSneak()
    {
        bc.size = new Vector2(width, height);
        bc.offset = Vector2.zero;
    }
    private void bigJump()
    {
        
        rb.velocity = new Vector2(facingDir * bigVaultHorizontalSpeed, jumpSpeedGivenHeight(bigVaultVerticalHeight));
    }
    private void longJump()
    {
        rb.velocity = new Vector2(facingDir * (forwardVaultSpeedIncrease + maxHorizontalSpeed), jumpSpeedGivenHeight(forwardVaultVerticalHeight));
    }
    private void backFlip()
    {
        setFacingDir(-facingDir);
        rb.velocity = new Vector2(backVaultHorizontalSpeed * facingDir, jumpSpeedGivenHeight(backVaultVerticalHeight));
    }
    private void highJump()
    {
        
        rb.velocity = Vector2.up * jumpSpeedGivenHeight(upVaultVerticalHeight);
    }
    private void jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeedGivenHeight(jumpHeight));
    } 
    private void enterWallCling(RaycastHit2D hit, float dir)
    {
        //TODO - Wall cling animation
        //transform.position = new Vector3(wall.transform.position.x - (wall.bounds.size.x / 2f + width / 2f) * dir, transform.position.y, 0); //temp - need to fix for tilemaps
        transform.position = new Vector3(Mathf.Round(hit.point.x) - (width / 2f) * dir, transform.position.y, 0);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (shift)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0;
            wallgrab = true;

        }
        else
        {
            rb.velocity = new Vector2(0, Mathf.Max(rb.velocity.y, -wallSlideSpeedCap));
            rb.gravityScale = -wallSlideGravity / Physics2D.gravity.y;
        }
        wallParticles = true;
        setFacingDir(-dir);
    }
    private void exitWallCling()
    {
        rb.gravityScale = 1f;
        wallgrab = false;
    }
    private void wallJump()
    {
        exitWallCling();
        rb.velocity = new Vector2(wallJumpHorizontalSpeed * facingDir, jumpSpeedGivenHeight(wallJumpVerticalHeight));
    }

    private float jumpSpeedGivenHeight(float height)
    {
        return Mathf.Pow(-2f * Physics2D.gravity.y * height, 0.5f);
    }
    private void callMech()
    {
        //TO-DO: call the transform ability from the player controller script
    }
    // Update is called once per frame

    void StateMachine()
    {
        bool KDown, ShiftDown, WDown, SDown, ADown, LDown, DDown, MDown, SpaceDown;
        bool K = getInput(KeyCode.K);
        bool Shift = getInput(KeyCode.LeftShift) || getInput(KeyCode.RightShift);
        bool W = getInput(KeyCode.W);
        bool S = getInput(KeyCode.S);
        bool A = getInput(KeyCode.A);
        bool D = getInput(KeyCode.D);
        bool L = getInput(KeyCode.L);
        bool M = getInput(KeyCode.M);
        bool Space = getInput(KeyCode.Space);

        KDown = getInputDown(KeyCode.K);
        ShiftDown = getInputDown(KeyCode.LeftShift) || getInputDown(KeyCode.RightShift);
        WDown = getInputDown(KeyCode.W);
        SDown = getInputDown(KeyCode.S);
        ADown = getInputDown(KeyCode.A);
        DDown = getInputDown(KeyCode.D);
        LDown = getInputDown(KeyCode.L);
        MDown = getInputDown(KeyCode.M);
        SpaceDown = getInputDown(KeyCode.Space);

        float HorizInput = Input.GetAxisRaw("Horizontal");

        

        switch (state)
        {
            case playerStates.Grounded:
            {
                
                if (!isGrounded())
                {
                    //TODO - activate airborne animation (taking into account facingDir)
                    state = playerStates.Airborne;
                    break;
                }
                if (LDown)
                {
                    useInput(KeyCode.L);
                    callMech();
                    break;
                }
                if (KDown
                   && hideableObjects.Count > 0)
                {
                    useInput(KeyCode.K);
                    enterHide(hideableObjects[0]);
                    state = playerStates.Hiding; break;
                }

                if (SpaceDown)
                {
                    useInput(KeyCode.Space);
                    jump();
                    state = playerStates.Airborne;
                    jumpStart = true;
                    break;
                }
                if (ShiftDown)
                {
                    useInput(KeyCode.LeftShift);
                    useInput(KeyCode.RightShift);
                    enterSlide();
                    rb.velocity = Vector2.right * (maxHorizontalSpeed + slideSpeedIncrease) * facingDir;
                    state = playerStates.Sliding; break;
                }
                if (SDown)
                {
                    useInput(KeyCode.S);
                    enterSneak();
                    state = playerStates.Sneaking; break;
                }
                if (HorizInput * facingDir < 0)
                {
                    setFacingDir(Mathf.Sign(HorizInput));
                }
                if (HorizInput * rb.velocity.x < 0f) //if we are trying to turn around
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                    //TODO - activate running animation in new direction (maybe do that in setFacingDir())
                }
                else if (Mathf.Abs(HorizInput) < 0.1) //else if we are trying to stop
                {
                    
                    //TODO - activate standing animation
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }

                float speed = rb.velocity.x;
                
                speed += HorizInput * acceleration * Time.fixedDeltaTime;
                if (speed > maxHorizontalSpeed)
                {
                    rb.velocity = new Vector2(maxHorizontalSpeed, rb.velocity.y);
                }
                else if (speed < -maxHorizontalSpeed)
                {
                    rb.velocity = new Vector2(-maxHorizontalSpeed, rb.velocity.y);
                }
                else
                {
                    rb.velocity = new Vector2(speed, rb.velocity.y);
                }
                break;
            }
            case playerStates.Airborne:
            {
                if (isGrounded() && rb.velocity.y < 0.01)
                {
                        Debug.Log("landed at " + (Time.time - buffer[KeyCode.Space].downLast) + " space read: " + buffer[KeyCode.Space].read);
                        rb.gravityScale = 1.0f;
                    state = playerStates.Grounded;
                        //if (SpaceDown)
                        //{
                        //    Debug.Log("We are jump plese");
                        //    useInput(KeyCode.Space);
                        //    StartCoroutine("jump");
                        //    state = playerStates.Airborne;
                        //    jumpStart = true;
                        //    break;
                        //}
                        break;
                }
                if (SDown)
                {
                    useInput(KeyCode.S);
                    rb.gravityScale = -fastFallGravity / Physics2D.gravity.y;
                    if (rb.velocity.y < -fastFallSpeedCap)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, -fastFallSpeedCap);
                    }
                }
                else if (rb.velocity.y < -maxVerticalSpeed)
                {
                    rb.gravityScale = 1;
                    rb.velocity = new Vector2(rb.velocity.x, -maxVerticalSpeed);
                }
                float bigToe = transform.position.x + width / 2 * facingDir;
                if (corners.Count > 0)
                {
                    TileMapCorner corner = corners[corners.Count - 1];
                    if (corner.getDirection() * facingDir < 0 //player input and corner should be facing each other
                    && (corner.getWorldCoord().x - bigToe) * facingDir >= 0
                    && (corner.getWorldCoord().x - bigToe) * HorizInput > 0
                    && Mathf.Abs(HorizInput) >= 0.2)
                    {
                        enterVault(corner);
                        state = playerStates.Vaulting;
                        break;
                    }
                }
                if (SpaceDown)
                {
                    if (isWalled(facingDir))
                    {
                        useInput(KeyCode.Space);
                        setFacingDir(-facingDir);
                        wallJump();
                    }
                    else if (isWalled(-facingDir))
                    {
                        useInput(KeyCode.Space);
                        wallJump();
                    }
                }
                Collider2D wall = isWalled(Mathf.Sign(HorizInput), out RaycastHit2D hit);
                    //Debug.Log((wall != null) + " Player grab? pls " + (Mathf.Sign(HorizInput) * rb.velocity.x >= 0));
                    if (wall != null
                    && (HorizInput != 0 || Shift))
                    //&& Mathf.Sign(HorizInput) * rb.velocity.x >= 0
                    //&& !isGrounded())
                {
                    if (Shift || rb.velocity.y < 0 && !S)
                    {
                        enterWallCling(hit, Mathf.Sign(HorizInput));
                        state = playerStates.Clinging;
                        break;
                    }
                }


                //Airspeed regulation - can maybe be simplified later
                float speed = rb.velocity.x;
                if (speed > maxHorizontalSpeed)
                {
                    speed -= Time.fixedDeltaTime * (boostAmt) / boostLength;
                    rb.velocity = new Vector2(speed, rb.velocity.y);
                    if (HorizInput < 0)
                    {
                        speed += HorizInput * acceleration * Time.fixedDeltaTime;
                    }
                }
                else if (speed < -maxHorizontalSpeed)
                {
                    speed += Time.fixedDeltaTime * (boostAmt) / boostLength;
                    rb.velocity = new Vector2(speed, rb.velocity.y);
                    if (HorizInput > 0)
                    {
                        speed += HorizInput * acceleration * Time.fixedDeltaTime;
                    }
                }
                else
                {
                    speed += HorizInput * airAcceleration * Time.fixedDeltaTime;
                    if (speed > maxHorizontalSpeed)
                    {
                        speed = maxHorizontalSpeed;
                    }
                    else if (speed < -maxHorizontalSpeed)
                    {
                        speed = -maxHorizontalSpeed;
                    }
                }
                rb.velocity = new Vector2(speed, rb.velocity.y);
                break;
            }
            case playerStates.Vaulting:
            {
                if (Time.time - moveStartTime > vaultAnimationTime)
                {
                    Vector2 velocityVector = Vector2.zero;
                    state = playerStates.Airborne;
                    switch (bufferedAction)
                    {
                        case movementTech.slide:
                        {
                            enterSlide();
                            state = playerStates.Sliding;
                            StartVaultParticlesCoroutine();        
                            break;
                        }
                        case movementTech.bigJump:
                        {
                            bigJump();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.longJump:
                        {
                            longJump();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.backFlip:
                        {
                            backFlip();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.highJump:
                        {
                            highJump();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.nothing:
                        {
                            rb.velocity = new Vector2(0, 0);
                            bufferedAction = movementTech.nothing;
                            moveStartTime = Time.time;
                            state = playerStates.Recovering;
                            break;
                        }
                    }
                    exitVault();
                    if (isCeilinged())
                    {
                        if(!(state == playerStates.Sliding))
                        {
                            state = playerStates.Sneaking;
                            rb.velocity = Vector2.zero;
                            enterSneak();
                        }
                    }
                    break;
                }
                if (ShiftDown)
                {
                    bufferedAction = movementTech.slide;
                }
                else if (SpaceDown)
                {
                    useInput(KeyCode.Space);
                    float horiz = Input.GetAxisRaw("Horizontal") * facingDir;
                    float vert = Input.GetAxisRaw("Vertical");
                    if (horiz > 0.5f && vert > 0.5f) { bufferedAction = movementTech.bigJump; }
                    else if (horiz >= 0.5f) { bufferedAction = movementTech.longJump; }
                    else if (horiz <= -0.5f) { bufferedAction = movementTech.backFlip; }
                    else if (vert >= 0.5f) { bufferedAction = movementTech.highJump; }
                    else if (vert <= -0.5f) { bufferedAction = movementTech.nothing; } //player DIs down
                    else { bufferedAction = movementTech.bigJump; } //if the player provides no DI
                }
                break;
            }
            case playerStates.Recovering:
            {
                bool bufferBreak = false;
                if (ShiftDown)
                {
                    bufferedAction = movementTech.slide;
                    bufferBreak = true;
                }
                else if (SpaceDown)
                {
                    float horiz = Input.GetAxisRaw("Horizontal") * facingDir;
                    float vert = Input.GetAxisRaw("Vertical");
                    if (horiz > 0.5f && vert > 0.5f) { bufferedAction = movementTech.bigJump; }
                    else if (horiz >= 0.5f) { bufferedAction = movementTech.longJump; }
                    else if (horiz <= -0.5f) { bufferedAction = movementTech.backFlip; }
                    else if (vert >= 0.5f) { bufferedAction = movementTech.highJump; }
                    else if (vert <= -0.5f) { bufferedAction = movementTech.nothing; } //player DIs down
                    else { bufferedAction = movementTech.bigJump; } //if the player provides no DI
                    bufferBreak = true;
                }
                else if(Time.time - moveStartTime > vaultRecoveryAnimationTime)
                {
                    bufferBreak = true;
                }
                if (bufferBreak)
                {
                    Vector2 velocityVector = Vector2.zero;
                    state = playerStates.Airborne;
                    switch (bufferedAction)
                    {
                        case movementTech.slide:
                        {
                            enterSlide();
                            state = playerStates.Sliding;
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.bigJump:
                        {
                            bigJump();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.longJump:
                        {
                            longJump();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.backFlip:
                        {
                            backFlip();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.highJump:
                        {
                            highJump();
                            StartVaultParticlesCoroutine();
                            break;
                        }
                        case movementTech.nothing:
                        {
                            rb.velocity = new Vector2(rb.velocity.x, 0);
                            bufferedAction = movementTech.nothing;
                            state = playerStates.Grounded;
                            break;
                        }
                    }
                    exitVault();
                    if (isCeilinged())
                    {
                        if (!(state == playerStates.Sliding))
                        {
                            state = playerStates.Sneaking;
                            rb.velocity = Vector2.zero;
                            enterSneak();
                        }
                    }
                    break;
                }
                break;
            }
            case playerStates.Sliding:
            {
                    if (Time.time - moveStartTime > slideAnimationTime
                        && !isCeilinged()) //if slide over or we hit something
                    {
                        exitSlide();
                        if (isGrounded())
                        {
                            if (S)
                            {
                                useInput(KeyCode.S);
                                enterSneak();
                                state = playerStates.Sneaking; break;
                            }
                            else
                            {
                                state = playerStates.Grounded; break;
                            }
                        }
                        else
                        {
                            state = playerStates.Airborne; break;
                        }
                    }
                    else if (Time.time - moveStartTime > slideAnimationTime
                        && isCeilinged() && isGrounded() && S) //if slide over or we hit something
                    {
                        if (S)
                        {
                            useInput(KeyCode.S);
                            exitSlide();
                            enterSneak();
                            state = playerStates.Sneaking; break;
                        }
                    }
                    else if (!isGrounded() && !isCeilinged()) //if player slides off an edge
                    {
                        exitSlide();
                        state = playerStates.Airborne;
                        break;
                    }
                    else if (Mathf.Abs(rb.velocity.x) < 0.8f * (maxHorizontalSpeed + slideSpeedIncrease))
                    {
                        if (isCeilinged())
                        {
                            exitSlide();
                            state = playerStates.Airborne;
                            break;
                        }
                        else if (Mathf.Abs(rb.velocity.x) < 0.8f * (maxHorizontalSpeed + slideSpeedIncrease))
                        {
                            if (isCeilinged())
                            {
                                exitSlide();
                                if (isGrounded())
                                {
                                    if (S)
                                    {
                                        useInput(KeyCode.S);
                                        enterSneak();
                                        state = playerStates.Sneaking; break;
                                    }
                                    else
                                    {
                                        state = playerStates.Grounded; break;
                                    }
                                }
                                else
                                {
                                    state = playerStates.Airborne; break;
                                }
                            }
                            else
                            {
                                rb.velocity = new Vector2(-(maxHorizontalSpeed + slideSpeedIncrease) * facingDir, 0);
                                setFacingDir(-facingDir);
                            }
                        }
                    }
                    else if (SpaceDown && !isCeilinged()) //if player jumps
                    {
                        useInput(KeyCode.Space);
                        exitSlide();
                        longJump();
                        jumpStart = true;
                        state = playerStates.Airborne;
                        break;
                    }
                    if (KDown
                       && hideableObjects.Count > 0)
                    {
                        useInput(KeyCode.K);
                        exitSlide();
                        enterHide(hideableObjects[0]);
                        state = playerStates.Hiding; break;
                    }
                    break;
            }
            case playerStates.Clinging:
            {
                if (!isWalled(-facingDir))
                {
                    exitWallCling();
                    state = playerStates.Airborne;
                    break;
                }
                if (Shift)
                {
                    rb.velocity = Vector2.zero;
                    rb.gravityScale = 0;
                }
                else
                {
                    rb.velocity = new Vector2(0, Mathf.Max(rb.velocity.y, -wallSlideSpeedCap));
                    rb.gravityScale = -wallSlideGravity / Physics2D.gravity.y;
                }
                if (SpaceDown)
                {
                    wallJump();
                    useInput(KeyCode.Space);
                    state = playerStates.Airborne;
                }
                if (HorizInput * facingDir > 0 || S && HorizInput * facingDir >= 0) //maybe add condition |input| > 0.2 for joystick?
                {
                    exitWallCling();
                    state = playerStates.Airborne;
                }   
                break;
            }
            case playerStates.Sneaking:
            {
                if (!isGrounded() && !isCeilinged())
                {
                    exitSneak();
                    state = playerStates.Airborne;
                    break;
                }
                if (SpaceDown && !isCeilinged())
                {
                    useInput(KeyCode.Space);
                    exitSneak();
                    jumpStart = true;
                    StartCoroutine("jump");
                    state = playerStates.Airborne;
                    break;
                }
                if (ShiftDown)
                {
                    enterSlide();
                    rb.velocity = Vector2.right * (maxHorizontalSpeed + slideSpeedIncrease) * facingDir;
                    state = playerStates.Sliding; break;
                }
                if (SDown && !isCeilinged())
                {
                    useInput(KeyCode.S);
                    exitSneak();
                    state = playerStates.Grounded; break;
                }
                if (KDown
                   && hideableObjects.Count > 0)
                {
                    useInput(KeyCode.K);
                    exitSneak();
                    enterHide(hideableObjects[0]);
                    state = playerStates.Hiding; break;
                }
                if (HorizInput   * facingDir < 0)
                {
                    setFacingDir(Mathf.Sign(HorizInput));
                }
                rb.velocity = new Vector2(sneakSpeed * HorizInput, rb.velocity.y);
                noiseRadius = Mathf.Abs(rb.velocity.x) / sneakSpeed;
                break;
            }
            case playerStates.Hiding:
            {
                rb.velocity = Vector2.zero;
                if (SDown)
                {
                    sneakyHide = !sneakyHide;
                    //todo - sneak idle animation toggle
                }
                if (SpaceDown || KDown)
                {
                    useInput(KeyCode.Space);
                    useInput(KeyCode.K);
                    exitHide();
                    if (sneakyHide)
                    {
                        state = playerStates.Sneaking;
                    }
                    else
                    {
                        state = playerStates.Grounded;
                    }
                    break;
                    
                }
                break;
            }
        }
    }
    void Update()
    {
        //if player dies or goes into hitstun that logic goes here
        //if interrupt
        //ie. state = playerStates.airborne
        readInputsDown();
        
    }

    void FixedUpdate()
    {
        readInputs();
        readInputsDownBackup();
        StateMachine();
        updateAnimationValue();
        resetInputs();
    }

    public void StartVaultParticlesCoroutine()
    {
        if (vaultCoroutine != null)
            StopCoroutine(vaultCoroutine);

        vaultCoroutine = StartCoroutine(startVaultParticles());
    }

    private IEnumerator startVaultParticles()
    {
        print("start dash particles");

        foreach (GameObject particles in vaultParticles)
        {
            particles.SetActive(true);
        }

        yield return new WaitForSeconds(1);

        foreach (GameObject particles in vaultParticles)
        {
            particles.SetActive(false);
        }
    }
}

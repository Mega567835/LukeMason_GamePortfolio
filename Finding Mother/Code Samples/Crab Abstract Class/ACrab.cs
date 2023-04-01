using System;
using System.Collections;
using System.Timers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public abstract class ACrab : MonoBehaviour
{
    public float power;
    public float maxPowerMultiplier;

    public Rigidbody2D rb;
    public BoxCollider2D bc;
    public LineRenderer lr;
    public LayerMask jumpableLayers;
    protected float startTime;
    public Image loadingBar;

    public LineRenderer angryBirdsLR;
    public int angryBirdsLength;

    protected Camera cam;

    protected Vector3 startPoint;
    protected Vector3 endPoint;

    protected enum crabState {
        grounded,
        inAir,
        held,
        draged,
    }

    protected crabState state = crabState.grounded;

    protected Animator anim;
    protected GameMaster gm;

    //GOOD CODE, Queue lambda expressions to be executed in next fixed update 
    protected Queue<Action> fixedUpdateActions = new Queue<Action>();

    // Utility Functions
    public bool shootBoxOfRays(int raysPerSide, LayerMask layers) {

        List<Vector2> rotatedRayPos = new List<Vector2>();

        if (raysPerSide < 2) raysPerSide = 2;
        Vector2 bcOrgin = bc.transform.position;
        float bcx = bc.size.x;
        float bcy = bc.size.y;
        Quaternion rotation = bc.transform.rotation;



        List<Vector2> rayPos = new List<Vector2>();
        //Add Corners
        rayPos.Add(new Vector2(bcOrgin.x - (bcx / 2), bcOrgin.y - (bcy / 2)));
        rayPos.Add(new Vector2(bcOrgin.x + (bcx / 2), bcOrgin.y - (bcy / 2)));
        rayPos.Add(new Vector2(bcOrgin.x - (bcx / 2), bcOrgin.y + (bcy / 2)));
        rayPos.Add(new Vector2(bcOrgin.x + (bcx / 2), bcOrgin.y + (bcy / 2)));
        //Draw Vertical Sides
        float yStep = bc.size.y / (raysPerSide - 1);
        for (int i = 1; i <= raysPerSide - 1; i++) {
            rayPos.Add(new Vector2(bcOrgin.x - (bcx / 2), ((bcOrgin.y - (bcy / 2)) + yStep * i)));
            rayPos.Add(new Vector2(bcOrgin.x + (bcx / 2), ((bcOrgin.y - (bcy / 2)) + yStep * i)));
        }
        //Draw Horizontal Sides
        float xStep = bc.size.x / (raysPerSide - 1);
        for (int i = 1; i <= raysPerSide - 1; i++) {
            rayPos.Add(new Vector2((bcOrgin.x - (bcx / 2)) + xStep * i, bcOrgin.y - (bcy / 2)));
            rayPos.Add(new Vector2((bcOrgin.x - (bcx / 2)) + xStep * i, bcOrgin.y + (bcy / 2)));
        }

        foreach (Vector2 pos in rayPos) {
            Vector2 displacement = pos - bcOrgin;
            Vector2 direction = rotation * displacement;
            rotatedRayPos.Add(bcOrgin + direction);
        }

        bool rayHit = false;
        foreach (Vector2 pos in rotatedRayPos) {
            ContactFilter2D rayFilter = new ContactFilter2D();
            RaycastHit2D[] results = new RaycastHit2D[64];
            rayFilter.layerMask = (jumpableLayers);
            rayFilter.useLayerMask = true;
            Physics2D.Raycast(pos, new Vector2(0, -1), rayFilter, results, yStep);
            List<RaycastHit2D> resultsList = new List<RaycastHit2D>();
            foreach (RaycastHit2D col in results) {
                if (col.collider == null) continue;
                if (col.collider.gameObject != this.gameObject) {
                    resultsList.Add(col);
                }
            }
            if (resultsList.Count > 0) rayHit = true;
        }
        return rayHit;

    }

    protected bool smallDistance(Vector3 endPoint, Vector3 startPoint) {
        return (startPoint - endPoint).magnitude < 1;
    }

    //Start
    public virtual void Start() {
        cam = Camera.main;
        anim = gameObject.GetComponent<Animator>();
        gm = FindObjectOfType<GameMaster>();
    }

    //Sounds on collision 
    protected virtual void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.layer == 11 && gameObject.GetComponent<OneHeartyBoi>().checkCooldown()) {
            FindObjectOfType<audioManager>().Play("Crab Hit Enemy");
        }
        if (collision.gameObject.layer == 8) {
            FindObjectOfType<audioManager>().Play("Crab Hit Ground");
        }
    }

    //State machine flow functions
    bool wasClicked = false;

    private void camFollow() {
        if (Input.GetMouseButtonDown(0) && bc.OverlapPoint(cam.ScreenToWorldPoint(Input.mousePosition))) {
            wasClicked = true;
        }
        if (wasClicked && Input.GetMouseButtonUp(0)) { 
            FindObjectOfType<Camer>().mainCrab = gameObject.transform;
            wasClicked = false;
        }
    }

    protected virtual void every_time() {
        return;
    }

    protected virtual void grounded_anim() {
        anim.SetInteger("AnimationInt", 0);
    }
    protected virtual void grounded_genStates() {
        if (!bc.IsTouchingLayers(jumpableLayers)) {
            state = crabState.inAir;
            return;
        }

        if (Input.GetMouseButtonDown(0) && bc.OverlapPoint(cam.ScreenToWorldPoint(Input.mousePosition))) {
            startPoint = cam.ScreenToWorldPoint(Input.mousePosition);
            startPoint.z = 15;

            startTime = Time.time;
            state = crabState.held;
            return;
        }
        return;
    }
    protected virtual void grounded_other() {
        return;
    }

    protected virtual void held_genStates() {
        if (!bc.IsTouchingLayers(jumpableLayers)) {
            loadingBar.fillAmount = 0;
            state = crabState.inAir;
            return;
        }

        if (!Input.GetMouseButton(0)) {
            loadingBar.fillAmount = 0;
            state = crabState.grounded;
            return;
        }

        endPoint = cam.ScreenToWorldPoint(Input.mousePosition);
        endPoint.z = 15;

        if (!smallDistance(endPoint, startPoint)) {
            loadingBar.fillAmount = 0;
            state = crabState.draged;
            return;
        }
    }

    protected abstract bool held_startConditions();

    protected virtual void held_inProgress() {
        anim.SetInteger("AnimationInt", 1);
    }
    protected abstract void held_complete();

    protected virtual void drag_anim() {
        anim.SetInteger("AnimationInt", 1);
        transform.localRotation = Quaternion.identity;
    }
    protected virtual void drag_genStates() {
        if (!bc.IsTouchingLayers(jumpableLayers)) {
            lr.positionCount = 0;
            angryBirdsLR.positionCount = 0;
            state = crabState.inAir;
            return;
        }

        if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0)) {
            state = crabState.grounded;
            lr.positionCount = 0;
            angryBirdsLR.positionCount = 0;
            return;
        }
    }
    protected virtual void drag_launch(Vector2 force, int xSign, int ySign) {
        CrabStacker stacker;
        if (TryGetComponent<CrabStacker>(out stacker)) GetComponent<CrabStacker>().lastOneLaunched();
        lr.positionCount = 0;
        angryBirdsLR.positionCount = 0;
        transform.localRotation = Quaternion.identity;

        fixedUpdateActions.Enqueue( () => rb.AddForce(force, ForceMode2D.Impulse) );
        state = crabState.inAir;
        gm.incJumps();
    }

    protected virtual void inAir_anim() {
        anim.SetInteger("AnimationInt", 2);
    }
    protected virtual void inAir_genStates() {
        lr.positionCount = 0;
        angryBirdsLR.positionCount = 0;

        if (bc.IsTouchingLayers(jumpableLayers) && shootBoxOfRays(7, jumpableLayers) && Mathf.Abs(rb.velocity.y) < .05) {
            state = crabState.grounded;
            return;
        }
    }



    void Update() {

        //camFollow();
        every_time();

        switch (state) {
            case crabState.grounded:
                grounded_other();

                grounded_anim();

                grounded_genStates();

                break;
            case crabState.held:

                held_genStates();

                if (held_startConditions()) {
                    if ((Time.time - startTime) > .33) {
                        loadingBar.fillAmount = ((Time.time - startTime) - .33f) / .33f;

                        held_inProgress();
                    }

                    if ((Time.time - startTime > .66) && (Time.time - startTime < .76)) {
                        loadingBar.fillAmount = 0;

                        held_complete();
                    }
                }
                break;
            case crabState.draged:

                drag_anim();

                loadingBar.fillAmount = 0;


                drag_genStates();


                startPoint = transform.position;
                endPoint = cam.ScreenToWorldPoint(Input.mousePosition);
                endPoint.z = 15;

                float angle = Mathf.Atan(Mathf.Abs(startPoint.y - endPoint.y) / Mathf.Abs(startPoint.x - endPoint.x));
                float distance = Mathf.Clamp(Mathf.Pow(Mathf.Pow(startPoint.y - endPoint.y, 2f) + Mathf.Pow(startPoint.x - endPoint.x, 2f), 0.5f), 0, maxPowerMultiplier);
                int xSign = 1;
                int ySign = 1;
                if (endPoint.x > startPoint.x) xSign = -1;
                if (endPoint.y > startPoint.y) ySign = -1;

                lr.positionCount = 2;
                lr.SetPositions(new Vector3[] { startPoint, new Vector3(startPoint.x - (xSign * distance * Mathf.Cos(angle)), startPoint.y - (ySign * distance * Mathf.Sin(angle)), 0) });

                lr.SetColors(new Color(1, 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 255f), new Color(1, 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 0f));

                Vector2 force = new Vector2(xSign * distance * power * Mathf.Cos(angle), ySign * distance * power * Mathf.Sin(angle));

                Vector3 quadraticOffset(float t) {
                    float x = (force.x * t);
                    float y = (force.y * t) + (Physics2D.gravity.y / 2) * t * t;
                    return new Vector3(x / rb.mass, y / rb.mass, transform.position.z);
                }

                angryBirdsLR.positionCount = angryBirdsLength;
                for (float i = 0; i < angryBirdsLength; i++) {
                    angryBirdsLR.SetPosition((int)i, transform.position + quadraticOffset(i / 20));
                }
                angryBirdsLR.SetColors(new Color(1, 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 255f), new Color(1, 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 1 - Mathf.Pow((distance / maxPowerMultiplier), 2), 0f));


                if (Input.GetMouseButtonUp(0)) {

                    drag_launch(force, xSign, ySign);

                }
                break;
            case crabState.inAir:
                inAir_anim();

                inAir_genStates();

                break;
        }
    }
    private void FixedUpdate() {
        for(int i = 0; i<fixedUpdateActions.Count; i++) {
            fixedUpdateActions.Dequeue().Invoke();
        }
    }


}

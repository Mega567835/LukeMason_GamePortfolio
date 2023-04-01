using System.Collections;
using System.Collections.Generic;
using System.Timers;
using System;
using UnityEngine;

public class Ump : MonoBehaviour {

    //crab layer for collision
    public LayerMask layersToKill;
    private Collider2D[] collisionResults;
    private Collider2D[] collisionResults2;
    public CircleCollider2D circ;

    //attack cycle of umpire
    public float onTime;
    public float offTime;
    private float elapsedStart;

    //Animation
    private Animator anim;
    private bool firing = false;
    private bool canFire = false;
    private Vector2 umpPos;
    
    //initialize animation and cycle
    void Start() {
        anim = gameObject.GetComponent<Animator>();
        elapsedStart = Time.time;
    }

    // Update is called once per frame
    void Update() {

        //handles cycle times and setting attack animation
        if (Time.time - elapsedStart > offTime && !fireing && canFire) {
            firing = true;
            elapsedStart = Time.time;

            anim.SetInteger("AnimationInt", 1);
        }

        //this loop handles colllisions
        if (firing) {
            if (Time.time - elapsedStart > .3) { //flexibility for close calls :)
                //Play the fire sound effect
                if (!FindObjectOfType<audioManager>().IsPlaying("UmpFire")) {
                    FindObjectOfType<audioManager>().Play("UmpFire");
                }
                anim.SetInteger("AnimationInt", 2);
                umpPos = this.gameObject.GetComponent<Transform>().position;
                collisionResults = Physics2D.OverlapBoxAll(new Vector2(umpPos.x + 2.25f, umpPos.y - .5f), new Vector2(3f, 2f), 1f, layersToKill);
                collisionResults2 = Physics2D.OverlapBoxAll(new Vector2(umpPos.x - 2.25f, umpPos.y - .5f), new Vector2(3f, 2f), 1f, layersToKill);

                //Check all collisions that the fire made with crabs
                foreach (Collider2D gameObjectCollider in collisionResults) {
                    GameObject parentObject = gameObjectCollider.gameObject;

                    OneHeartyBoi healthScript;
                    if (parentObject.TryGetComponent<OneHeartyBoi>(out healthScript) && parentObject != this.gameObject) {
                        healthScript.health -= 2;
                    }
                }
                //He shoots fire in both directions, so we check the other side too
                foreach (Collider2D gameObjectCollider in collisionResults2) {
                    GameObject parentObject = gameObjectCollider.gameObject;

                    OneHeartyBoi healthScript;
                    if (parentObject.TryGetComponent<OneHeartyBoi>(out healthScript) && parentObject != this.gameObject) {
                        healthScript.health -= 2;
                    }
                }
            }
            //Stop firing
            if (Time.time - elapsedStart > onTime) {
                firing = false;
                elapsedStart = Time.time;
                anim.SetInteger("AnimationInt", 0);
                FindObjectOfType<audioManager>().Stop("UmpFire");
            }


        }
    }
    //don't attack if no crabs are near
    private void OnTriggerStay2D(Collider2D col) {
        if (col.gameObject.tag == "StackerCrab")
           canFire = true;
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.tag == "StackerCrab")
            canFire = false;
    }
}

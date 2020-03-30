﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Throwable heldObject;
    public Transform holdPoint;
    public LayerMask grabbableObjects;
    public LayerMask pushableWalls;
    public float grabDistance = 1f;
    public float maxForce = 1000f;
    public float maxVel = 5f;
    public float pullbackDistance = 100000f;
    public float thrustForce = 1f;

    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 mousePos2D;
    private Vector2 mouseClickPos;
    private Vector2 aimDir = new Vector2();
    private Vector2 dragDirection = Vector2.zero;
    private float dragDistRaw = 0; // distance from mouse to click point, can be negative
    private float dragDist = 0; // clamped distance from mouse to click point, 0 - dragInputScale
    private float dragMult = 0; // 0 - 1, how close is mouse to dragInputScale?


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // rotate item in hand
        float s = Input.GetAxis("RotateHeld");
        holdPoint.rotation = Quaternion.Euler(holdPoint.rotation.eulerAngles + new Vector3(0f, 0f, s));


        // Microthrusters
        Vector2 thrusters = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        rb.AddForce(thrusters.normalized * thrustForce);

        mousePos2D = Input.mousePosition;
        if (Input.GetButtonDown("Grab"))  // grab object if left clicked
        {
            if(heldObject == null)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, aimDir, grabDistance, grabbableObjects);
                if(hit.collider != null && hit.collider.GetComponent<Throwable>() != null)
                {
                    Grab(hit.collider.gameObject.GetComponent<Throwable>());
                }
            }
        }


        if(Input.GetButtonDown("Throw")) // keep track of click location
        {
            mouseClickPos = mousePos2D;
        }


        if (Input.GetButtonUp("Throw")) // initiate throw
        {
            if (heldObject != null)
                Throw();
            else
                PushOffWall();
        }


        if (Input.GetButton("Throw")) // charge throw
        {
            Charge();
        }


        else {          // unless charging a throw, face the mouse.
            FaceMouse();
            animator.SetBool("isDragging", false);
        }
    }
    void Charge()
    {
        dragDirection = Camera.main.WorldToScreenPoint(transform.position);
        dragDirection = dragDirection - mouseClickPos;
        dragDistRaw = Vector2.Dot(dragDirection, mousePos2D - mouseClickPos);
        dragDist = Mathf.Clamp(dragDistRaw, 0, pullbackDistance);
        dragMult = dragDist / pullbackDistance;
        animator.SetBool("isDragging", true);
        animator.SetFloat("Blend", dragMult);
    }

    void FaceMouse()
    {
        aimDir = Camera.main.ScreenToWorldPoint(mousePos2D) - transform.position;
        transform.up = aimDir;
    }

    void Grab(Throwable obj)
    {
        holdPoint.rotation = Quaternion.identity;


        // calculate velocity
        Vector2 collisionVel = (obj.rb.mass * obj.rb.velocity + rb.mass * rb.velocity) / (obj.rb.mass + rb.mass);
        rb.velocity = collisionVel;

        heldObject = obj;
        Collider2D c = heldObject.GetComponent<Collider2D>();
        c.enabled = false;
        heldObject.rb.isKinematic = true;
        heldObject.rb.angularVelocity = 0f;

        heldObject.transform.parent = holdPoint;
        heldObject.rb.velocity = Vector3.zero;

        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;
    }


    void Throw()
    {

        heldObject.rb.isKinematic = false;
        heldObject.rb.velocity = rb.velocity;
        heldObject.transform.parent = null;
        heldObject.GetComponent<Collider2D>().enabled = true;
        heldObject.rb.AddForce(aimDir.normalized * maxForce * dragMult, ForceMode2D.Impulse);
        rb.AddForce(-1 * aimDir.normalized * maxForce * dragMult, ForceMode2D.Impulse);
        holdPoint.rotation = Quaternion.identity;

        heldObject = null;
    }

    void PushOffWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, aimDir, grabDistance, pushableWalls);
        if (hit.collider != null)
        {
            rb.AddForce(-1 * aimDir.normalized * maxForce * dragMult, ForceMode2D.Impulse);
        }
    }
}

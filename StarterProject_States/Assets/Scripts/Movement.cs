﻿// Base taken from Mix and Jam: https://www.youtube.com/watch?v=STyY26a_dPY

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Added for class 
public enum PlayerState 
{
    IDLE,
    RUNNING,
    CLIMBING,
    DASHING,
    WALL_JUMP,
    WALL_SLIDE,
    JUMPING,
    GROUNDED
}

// Other states to consider: ON_WALL, JUMPING, FALLING, DASHING, WALL_JUMPING
// You may also need to move code into the states I've already made

// An alternative idea would be to make a few larger states like GROUNDED, AIRBORN, ON_WALL
// Then each state has a larger chunk of code that deals with each area

// How you choose to implement the states is up to you
// The goal is to make the code easier to understand and easier to expand on

public class Movement : MonoBehaviour
{
    // Use this to check the state
    private PlayerState currentState = PlayerState.IDLE;

    // Custom collision script
    private Collision coll;
    
    [HideInInspector]
    public Rigidbody2D rb;
    private AnimationScript anim;

    [Space] // Adds some space in the inspector
    [Header("Stats")] // Adds a header in the inspector 
    public float speed = 10;
    public float jumpForce = 50;
    public float slideSpeed = 5;
    public float wallJumpLerp = 10;
    public float dashSpeed = 20;

    [Space]
    [Header("Booleans")]

    // These were originally used to switch between movement
    // They also control the animation system in unity
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;

    [Space]

    private bool groundTouch;
    private bool hasDashed;

    public int side = 1;

    // Input Variables
    private float xInput;
    private float yInput;
    private float xRaw;
    private float yRaw;
    private Vector2 inputDirection;

    private void SetInputVariables()
    {
        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");
        xRaw = Input.GetAxisRaw("Horizontal");
        yRaw = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector2(xInput, yInput);
    }
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimationScript>();

        SetInputVariables();
    }

    void Update()
    {
        // Set input data for easy access
        SetInputVariables();

        // Reset Gravity
        rb.gravityScale = 3;

        // Use the statemachine
        StateMachine(currentState);

        //CLIMBING =====================================================================================================
        if (coll.onWall && Input.GetButton("Fire2") && canMove)
        {
            // Change state
            currentState = PlayerState.CLIMBING;

            // Flips sprite based on which wall
            if(side != coll.wallSide)
                anim.Flip(side*-1);
            
            // Bools for movement and animation
            wallGrab = true;
            wallSlide = false;
        }
        //=======================================================================================================

        // Used when no longer on a wall
        if (Input.GetButtonUp("Fire2") || !coll.onWall || !canMove)
        {
            wallGrab = false;
            wallSlide = false;
        }

        // When on the ground and not dashing
        // You might want to move these to a state
        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            GetComponent<BetterJumping>().enabled = true;
        }

        //WALL_SLIDE ==================================================================================================
        if(coll.onWall && !coll.onGround)
        {
            // If the player is moving towards the wall
            if (xInput != 0 && !wallGrab)
            {   
                // Slide down the wall
                wallSlide = true;
                currentState = PlayerState.WALL_SLIDE;
            }
        }
        //======================================================================================================

        //JUMPING & WALL_JUMP =========================================================================================
        if (Input.GetButtonDown("Jump"))
        {
            // Sets the jump animation
            anim.SetTrigger("jump");

            //JUMPING STATE
            if (coll.onGround)
                currentState = PlayerState.JUMPING;

            //WALL_JUMP STATE
            if (coll.onWall && !coll.onGround)
                currentState = PlayerState.WALL_JUMP;
            
        }
        //======================================================================================================

        //DASHING =====================================================================================================
        if (Input.GetButtonDown("Fire1") && !hasDashed)
        {
            currentState = PlayerState.DASHING;
        }
        //======================================================================================================

        //GROUNDED ====================================================================================================
        if (coll.onGround && !groundTouch)
        {
            // GroundTouch() resets the dash, as you can only dash once per jump
            currentState = PlayerState.GROUNDED;
            groundTouch = true;
        }
        if(!coll.onGround && groundTouch)
        {
            groundTouch = false;
        }
        //======================================================================================================

        // Return if on a wall
        if (wallGrab || wallSlide || !canMove)
            return;

        // Otherwise use the horizontal input to flip the sprite
        if(xInput > 0)
        {
            side = 1;
            anim.Flip(side);
        }
        if (xInput < 0)
        {
            side = -1;
            anim.Flip(side);
        }
    }

    private void StateMachine(PlayerState state)
    {
        // This is where the code for each state goes
        switch (state)
        {
            case PlayerState.IDLE:
                if(xInput > 0.01f || xInput < -0.01f)
                {
                    currentState = PlayerState.RUNNING;
                }
            break;

            case PlayerState.RUNNING:
                Walk(inputDirection);
                anim.SetHorizontalMovement(xInput, yInput, rb.velocity.y);
            
                // Condition: No horizontal input, go to IDLE state
                if(xInput <= 0.01f || xInput >= 0.01f)
                {
                    currentState = PlayerState.IDLE;
                }
            break;
            
            case PlayerState.CLIMBING:
                // Stop gravity
                rb.gravityScale = 0;

                // Limit horizontal movement
                if(xInput > .2f || xInput < -.2f)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                }
            
                // Vertical Movement, slower when climbing
                float speedModifier = yInput > 0 ? .5f : 1;
                rb.velocity = new Vector2(rb.velocity.x, yInput * (speed * speedModifier));

                // Leave Condition:
                if (!coll.onWall || !Input.GetButton("Fire2"))
                {
                    // Change state to default
                    currentState = PlayerState.IDLE;
            
                    // Reset Gravity
                    rb.gravityScale = 3;
                }        
            break;
            
                // More states here pls
            case PlayerState.DASHING:
                if (xRaw != 0 || yRaw != 0)
                    
                Camera.main.transform.DOComplete();
                Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
                FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

                // Put dash on cooldown
                hasDashed = true;

                anim.SetTrigger("dash");


                rb.velocity = Vector2.zero;
                Vector2 dir = new Vector2(xRaw, yRaw);

                rb.velocity += dir.normalized * dashSpeed;
                StartCoroutine(DashWait());
                currentState = PlayerState.IDLE;
            break;

            case PlayerState.WALL_JUMP:
                // Flip sprite if needed
                if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
                {
                    side *= -1;
                    anim.Flip(side);
                }

                // Disable movement while wall jumping
                StopCoroutine(DisableMovement(0));
                StartCoroutine(DisableMovement(.1f));

                // Set direction based on which wall
                Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;

                // Jump using the direction
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.velocity += (Vector2.up / 1.5f + wallDir / 1.5f) * jumpForce;

                wallJumped = true;
                currentState = PlayerState.IDLE;
            break;

            case PlayerState.WALL_SLIDE:
                // Flip if needed
                if (coll.wallSide != side)
                    anim.Flip(side * -1);

                if (!canMove)
                    return;

                // If the player is holding towards the wall...
                bool pushingWall = false;
                if ((rb.velocity.x > 0 && coll.onRightWall) || (rb.velocity.x < 0 && coll.onLeftWall))
                {
                    pushingWall = true;
                }
                float push = pushingWall ? 0 : rb.velocity.x;

                // Move down
                rb.velocity = new Vector2(push, -slideSpeed);

                if (!coll.onWall || coll.onGround)
                {
                    currentState = PlayerState.IDLE;
                    wallSlide = false;
                }

            break;

            case PlayerState.JUMPING:
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.velocity += Vector2.up * jumpForce;
                currentState = PlayerState.IDLE;
            break;

            case PlayerState.GROUNDED:
                hasDashed = false;
                isDashing = false;

                side = anim.sr.flipX ? -1 : 1;
                currentState = PlayerState.IDLE;
            break;
        }
    }

    IEnumerator DashWait()
    {   
        // Graphics effect for trail
        FindObjectOfType<GhostTrail>().ShowGhost();
        
        // Resets dash right away if on ground 
        StartCoroutine(GroundDash());

        // Changes drag over time
        DOVirtual.Float(14, 0, .8f, SetRigidbodyDrag);

        // Stop gravity
        rb.gravityScale = 0;

        // Disable better jumping script
        GetComponent<BetterJumping>().enabled = false;

        wallJumped = true;
        isDashing = true;

        // Wait for dash to end
        yield return new WaitForSeconds(.3f);

        // Reset gravity
        rb.gravityScale = 3;

        // Turn better jumping back on
        GetComponent<BetterJumping>().enabled = true;

        wallJumped = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {   
        // Resets dash right away
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    private void Walk(Vector2 dir)
    {   
        // Do we need these if statements anymore?
        if (!canMove)
            return;
        if (wallGrab)
            return;
        
        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)), wallJumpLerp * Time.deltaTime);
        }
    }

    IEnumerator DisableMovement(float time)
    {  
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void SetRigidbodyDrag(float x)
    {
        rb.drag = x;
    }
}

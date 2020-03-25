﻿using System.Collections;
using UnityEngine;


public class NinjaPlayerMovement : PlayerMovement
{
    private Vector3 normal;

    [SerializeField] float wallJumpForce = 20f;

    public bool wallRunning;
    private bool doubleJumped;
    private bool canWallJump;
    private bool wallJumping;

    protected override void FixedUpdate()
    {
        if (PauseMenu.GameIsPaused)
            {
                return;
            }

        if (edgeHanging)
        {
            //turns off gravity while hanging on edge
            velocity.y = 0f;
            fallVelocity = 0f;
        }
        else
        {
            //sets vertical pull
            velocity.y += gravity * Time.deltaTime;
        }

        Move();
        MoveAudio();
        Fall();
        Crouch();

        if (!edgeClimbing)
        {
            //applies gravity
            controller.Move(velocity * Time.deltaTime);
        }

        if (wallRunning)
        {
            if (!canWallJump)
            {
                //sets vertical pull when starting wall running
                velocity.y = 10f;
            }

            //sets vertical pull attributes for wall running
            gravity = 0f;
            velocity.y -= 0.25f;
            fallDecrease = 0.1f;
            canWallJump = true;
        }
        else
        {
            //sets vertical pull attributes back to default
            gravity = -25f;
            fallDecrease = 0.4f;
            canWallJump = false;
        }

        //restricts the max vertical speed
        if(!isGrounded)
        {
            velocity.y = Mathf.Clamp(velocity.y, -40f, 15f);
        }
        else
        {
            velocity.y = Mathf.Clamp(velocity.y, 0f, 15f); 
        }

        if (velocity.x > 0f)
        {
            velocity.x -= wallJumpForce * Time.deltaTime;
        }
        else if (velocity.x < 0f)
        {
            velocity.x += wallJumpForce * Time.deltaTime;
        }

        if (velocity.z > 0f)
        {
            velocity.z -= wallJumpForce * Time.deltaTime;
        }
        else if (velocity.z < 0f)
        {
            velocity.z += wallJumpForce * Time.deltaTime;
        }
    }

    //moves the player according to its state and attributes
    protected override void Move()
    {
        if (isGrounded && !wallJumping)
        {
            doubleJumped = false;
            move = (transform.right * horizontal + transform.forward * vertical) * speed;
            lastMove = move;
        }
        else if (!wallJumping)
        {
            if (!edgeClimbing && !edgeHanging)
            {
                controller.Move(lastMove * 0.3f * Time.deltaTime);
                move = (transform.right * horizontal + transform.forward * vertical) * speed * 0.8f;
            }

            if (edgeHanging && !edgeClimbing)
            {
                vertical = Mathf.Clamp(vertical, -1f, 0f);
                move = (transform.right * horizontal + transform.forward * vertical) * speed * 0.8f;
            }

            if (!edgeClimbing)
            {
                move = (transform.right * horizontal + transform.forward * vertical) * speed * 0.8f;
            }
        }
        controller.Move(move * Time.deltaTime);
    }

    public override void Jump()
    {
        //Cant jump while crouching
        if (isCrouched)
        {
            return;
        }

        if (canWallJump && !Physics.Raycast(transform.position, transform.forward, 1.5f))
        {
            //if able to wall jump and parallel t the wall, start wall jump
            StartCoroutine(WallJump());
        }
        else if (isGrounded)
        {
            //if grounded play jump sound and move upwards
            audioManager.Play("Jump");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            fallVelocity = 10f;
        }
        else if (!isGrounded && !doubleJumped && !edgeClimbing && !wallRunning)
        {
            //if in air and didn't double jump yet, jump again
            audioManager.Play("Jump");
            doubleJumped = true;
            resetFall = true;
            velocity.y = Mathf.Sqrt(jumpHeight * -2.4f * gravity);
            fallVelocity = 10f;
        }
    }

    protected override void MoveAudio()
    {
        if (moveInput != Vector2.zero && isGrounded)
        {
            if (!audioManager.IsPlaying("Walking"))
            {
                audioManager.Play("Walking");
            }
        }
        else
        {
            audioManager.Stop("Walking");
        }

        if (moveInput != Vector2.zero && wallRunning)
        {
            if (!audioManager.IsPlaying("Wallrun"))
            {
                audioManager.Play("Wallrun");
            }
        }
        else
        {
            audioManager.Stop("Wallrun");
        }
    }

    IEnumerator WallJump()
    {
        if(!doubleJumped)
        {
            doubleJumped = true;
        }

        wallJumping = true;
        velocity.x = wallJumpForce * normal.x;
        velocity.z = wallJumpForce * normal.z;
        velocity.y = Mathf.Sqrt(jumpHeight * 50f);
        move.x = lastMove.x;
        fallVelocity = 0f;
        audioManager.Play("Jump");

        yield return new WaitForSeconds(0.5f);

        wallJumping = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        normal = hit.normal;
    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform groundCheck;
    public LayerMask groundMask;
    private Vector3 velocity;
    private Vector3 move;

    public float jumpHeight;
    public float speed;
    private float horizontal;
    private float vertical;
    private float gravity = -11f;
    private float fallMultiplier = 1.25f;
    private float groungDistance = 0.4f;

    public bool isGrounded;
    public bool doubleJumped;
    public bool resetFall;

    private void Start()
    {
        move = new Vector3();
        velocity = new Vector3();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groungDistance, groundMask);

        if (isGrounded)
        {
            doubleJumped = false;
            move = (transform.right * horizontal + transform.forward * vertical) * speed;
        }

        controller.Move(move * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;

        if (velocity.y < 0 && !isGrounded && !resetFall)
        {
            velocity.y *= Mathf.Lerp(1.05f, fallMultiplier, 0f);
        }
        else
        {
            resetFall = false;
        }

        velocity.y = Mathf.Clamp(velocity.y, -10f, 10f);
        controller.Move(velocity * Time.deltaTime);

        print(velocity);
    }

    public void Move(InputAction.CallbackContext context)
    {
        Vector2 move = context.ReadValue<Vector2>();
        horizontal = move.x;
        vertical = move.y;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.action.phase == InputActionPhase.Performed)
        {
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else if (!isGrounded && !doubleJumped)
            {
                doubleJumped = true;
                resetFall = true;
                velocity.y = Mathf.Sqrt(jumpHeight * -2.4f * gravity);
            }
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (vertical > 0)
        {
            if (context.action.phase == InputActionPhase.Started)
            {
                speed *= 1.6f;
            }
            else if (context.action.phase == InputActionPhase.Canceled)
            {
                speed /= 1.6f;
            }
        }
    }
}
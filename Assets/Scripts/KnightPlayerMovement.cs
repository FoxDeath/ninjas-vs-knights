﻿using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


//TO DO: Refactor(Radu)
public class KnightPlayerMovement : MonoBehaviour
{
    [SerializeField] LayerMask groundMask;
    private CharacterController controller;
    private GameObject groundCheck;
    private Camera fpsCamera;
    private AudioManager audioManager;

    private Vector3 velocity;
    private Vector3 movement;
    private Vector3 lastMove;
    private Vector2 movementInput;

    [SerializeField] float jumpHeight = 3f;
    [SerializeField] float speed = 7f;
    [SerializeField] float dashForce = 10f;
    [SerializeField] float jetpackForce = 100f;
    [SerializeField] float chargeForce = 5f;
    [SerializeField] float chargePushForce = 100f;
    [SerializeField] float upwardsForce = 50f;
    [SerializeField] float chargeDamage = 30f;
    [SerializeField] float maxJetpackFuel = 5f; 
    [SerializeField] float fallDecrease = 0.8f;
    private float defaultSpeed;
    private float horizontal;
    private float vertical;
    private float gravity = -25f;
    private float fallMultiplier = 2.5f;
    private float groungDistance = 0.4f;
    private float currentForce = 0f;
    private float jetpackFuel;

    private bool isGrounded;
    private bool crouching;
    private bool sprinting;
    private bool resetFall;
    public bool jetpackOn;
    private bool canCharge = true;
    private bool canDash = true;
    private bool dashing;
    private bool charging;
    private bool jetpacking;
    
    #region Getters and Setters
    public bool GetCrouching()
    {
        return crouching;
    }
    public void SetCrouching(bool crouching)
    {
        this.crouching = crouching;
    }
    public bool GetSprinting()
    {
        return sprinting;
    }
    public float GetVertical()
    {
        return vertical;
    }
    #endregion
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        groundCheck = GameObject.Find("GroundCheck");
        fpsCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        Application.targetFrameRate = 60;
        movement = new Vector3();
        velocity = new Vector3();
        jetpackFuel = maxJetpackFuel;
        defaultSpeed = speed;

        audioManager = FindObjectOfType<AudioManager>();
    }

    void Update()
    {
        //Checks if the player is grounded
        if(!isGrounded)
        {
            isGrounded = Physics.CheckSphere(groundCheck.transform.position, groungDistance, groundMask);

            //If it isn't grounded and becomes grounded plays falling sound
            if(isGrounded)
            {
                audioManager.Play("Falling");
            }
        }
        isGrounded = Physics.CheckSphere(groundCheck.transform.position, groungDistance, groundMask);

        //if sprinting and moving backwards
        if(sprinting && vertical <= 0)
        {
            Sprint(false);
        }
    }

    void FixedUpdate()
    {
        if(PauseMenu.GameIsPaused)
        {
            return;
        }

        velocity.y += gravity * Time.deltaTime;

        Move();
        MoveAudio();
        Jetpack();

        Fall();

        velocity.y = Mathf.Clamp(velocity.y, -25f, 10f);
        controller.Move(velocity * Time.deltaTime);
    }

    //Sets the move input from the PlayerInput
    public void SetMoveInput(Vector2 movementInput)
    {
        if (!charging)
        {
            this.movementInput = movementInput;
            horizontal = movementInput.x;
            vertical = movementInput.y;
        }
    }

    //Moves the player acording to the "horizontal" and "vertical" atributes
    private void Move()
    {
        if (isGrounded)
        {
            movement = (transform.right * horizontal + transform.forward * vertical) * speed;
            lastMove = movement;
        }
        else
        {
            controller.Move(lastMove * 0.3f * Time.deltaTime);
            movement = (transform.right * horizontal + transform.forward * vertical) * speed * 0.8f;
        }

        controller.Move(movement * Time.deltaTime);
    }

    //Plays the moving sound
    private void MoveAudio()
    {
        if(movement != Vector3.zero && isGrounded)
        {
            if(!audioManager.IsPlaying("Walking"))
            {
                audioManager.Play("Walking");
            }
        }
        else
        {
            if(audioManager.IsPlaying("Walking"))
            {
                audioManager.Stop("Walking");
            }
        }
    }

    public void Jump()
    {   
        //Cant jump while crouching
        if(crouching)
        {
            return;
        }
        
        if (isGrounded)
        {
            audioManager.Play("Jump");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    public void Jetpack()
    {

        // Observer pattern
        SendMessage("SetSliderValue", jetpackFuel/maxJetpackFuel);
        //If the jetpack is off stops the sound and recharges the jetpack
        if(!jetpackOn)
        {
            audioManager.Stop("Jetpack");
            if(isGrounded && jetpackFuel < maxJetpackFuel)
            {
                jetpackFuel += Time.deltaTime * 2;
            }
            currentForce -= Time.deltaTime/5f;    
            if(currentForce < 0f)
            {
                currentForce = 0f;
            }

            if(jetpacking)
            {
                speed = speed / 1.5f;
            }
            
            jetpacking = false;

            return;
        }
        //If the jetpack is on and it has fuel the player flies
        else if(jetpackFuel > 0f)
        {
            if(!jetpacking)
            {
                speed = speed * 1.5f;
            }

            jetpacking = true;

            if(!audioManager.IsPlaying("Jetpack"))
            {
                audioManager.Play("Jetpack");
            }

            jetpackFuel -= Time.deltaTime;
            currentForce += Time.deltaTime/10f;

            if(currentForce > 1f)
            {
                currentForce = 1f;
            }

            if(!dashing)
            {
                velocity.y = Mathf.Sqrt(jetpackForce * -2f * gravity * currentForce);
            }
        }
        //If the jetpack is on and it has no fuel it stops
        else if(jetpackFuel <= 0f)
        {
            if(jetpacking)
            {
                speed = speed / 1.5f;
            }
            
            jetpacking = false;
            if(audioManager.IsPlaying("Jetpack"))
            {
                audioManager.Stop("Jetpack");
            }
        }
    }

    public void Sprint(bool state)
    {
        if (state && !sprinting)
        {
            audioManager.SetPitch("Walking", audioManager.GetPitch("Walking") * 2f);
            sprinting = true;
            speed *= 1.6f;
        }
        else if(!state && sprinting)
        {
            audioManager.SetPitch("Walking", audioManager.GetPitch("Walking") / 2f);
            sprinting = false;
            speed /= 1.6f;
        }
    }

    public void Dash()
    {
        if(canDash)
        {
            StartCoroutine(DashBehaviour());
        }
    }

    IEnumerator DashBehaviour()
    {
        //If the player is not on the ground and you dash it dashes in the direction he is looking at
        if(!isGrounded)
        {
            canDash = false;
            dashing = true;

            SendMessage("SetSliderColour", Color.red);

            audioManager.Play("Jetpack Dash");

            float oldVerticalX = this.vertical;
            float oldVerticalY = this.horizontal;
            float oldHorizontal = this.velocity.y;
            float oldGravity = this.gravity;

            Vector3 localForward = transform.worldToLocalMatrix.MultiplyVector(fpsCamera.transform.forward);

            vertical = dashForce * localForward.z;
            horizontal = dashForce * localForward.x;

            gravity = 0f;
            velocity.y = dashForce * localForward.y;

            yield return new WaitForSeconds(0.4f);
            
            dashing = false;

            vertical = oldVerticalX;
            horizontal = oldVerticalY;
            velocity.y = oldHorizontal;
            gravity = oldGravity;

            yield return new WaitForSeconds(2f);

            canDash = true;
            SendMessage("SetSliderColour", Color.green);   
        }
    }

    public void Charge()
    {
        if(canCharge)
        {
            StartCoroutine(ChargeBehaviour());
        }
    }

    //Charging attack
    IEnumerator ChargeBehaviour()
    {
        if (!jetpackOn && isGrounded && vertical > 0)
        {
            canCharge = false;
            charging = true;
            
            //audioManager.Play("");
            
            GetComponentInChildren<MouseLook>().mouseSensitivity /= 10;

            vertical = chargeForce * movementInput.y;

            yield return new WaitForSeconds(2.5f);

            charging = false;

            GetComponentInChildren<MouseLook>().mouseSensitivity *= 10;
            
            vertical = 0;

            yield return new WaitForSeconds(10f);

            canCharge = true;
        }
    }

    private void Fall()
    {
        if (velocity.y < 0 && !isGrounded && !resetFall)
        {
            velocity.y -= fallDecrease;
        }
        else
        {
            resetFall = false;
        }
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (charging)
        {
            Target target = hit.transform.GetComponent<Target>();
            if (target)
            {
                target.TakeDamage(chargeDamage);
            }

            if (hit.rigidbody)
            {
                Vector3 force = -hit.normal * chargePushForce;
                force.y = upwardsForce;
                hit.rigidbody.AddForce(force);
            }
        }
    }
    
    public void Crouch(bool crouching)
    {   
        //You can't crouch if you are not on the ground
        if(!isGrounded)
        {
            return;
        }

        if(crouching)
        {
            this.crouching = true;
            transform.localScale = new Vector3(1f, 0.5f, 1f);
            speed *= 0.6f;
        }
        else
        {
            this.crouching = false;
            transform.localScale = new Vector3(1f, 1f, 1f);
            speed /= 0.6f;
        }
    }
}

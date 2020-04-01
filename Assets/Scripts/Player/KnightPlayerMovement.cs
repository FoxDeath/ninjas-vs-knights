﻿using System.Collections;
using UnityEngine;


public class KnightPlayerMovement : PlayerMovement
{
    private Camera fpsCamera;
    private UIManager uiManager;

    [SerializeField] float dashForce = 10f;
    [SerializeField] float jetpackForce = 100f;
    [SerializeField] float chargeForce = 5f;
    [SerializeField] float chargePushForce = 100f;
    [SerializeField] float upwardsForce = 50f;
    [SerializeField] float chargeDamage = 30f;
    [SerializeField] float maxJetpackFuel = 5f;
    private float currentForce = 0f;
    private float jetpackFuel;

    private bool jetpackOn;
    private bool canCharge = true;
    private bool canDash = true;
    private bool dashing;
    private bool charging;
    private bool jetpacking;

    public bool GetDashing()
    {
        return dashing;
    }
    public void SetJetpackOn(bool jetpackOn)
    {
        this.jetpackOn = jetpackOn;
    }

    protected override void Start()
    {
        controller = GetComponent<CharacterController>();
        groundCheck = transform.Find("Cylinder").Find("GroundCheck");
        groundMask = LayerMask.GetMask("Ground");
        fpsCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        uiManager = FindObjectOfType<UIManager>();
        audioManager = FindObjectOfType<AudioManager>();

        move = new Vector3();
        velocity = new Vector3();
        jetpackFuel = maxJetpackFuel;
        defaultSpeed = speed;
    }

    protected override void FixedUpdate()
    {
        if(PauseMenu.GameIsPaused)
        {
            return;
        }


        if(edgeHanging)
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
        Jetpack();
        Crouch();
        Fall();

        if(!isGrounded && !dashing)
        {
            velocity.y = Mathf.Clamp(velocity.y, -40f, 10f);
        }
        else if(!dashing)
        {
            velocity.y = Mathf.Clamp(velocity.y, 0f, 10f);
        }

        controller.Move(velocity * Time.deltaTime);
    }

    //Calculates the speed depending on the situation
    protected override void SpeedCalculation()
    {
        if(sliding)
        {
            speed = defaultSpeed * 2f;
        }
        else if(!sliding && (sprinting || jetpacking))
        {
            speed = defaultSpeed * 1.6f;
        }
        else
        {
            if (isCrouched && !scoping)
            {
                speed = defaultSpeed * 0.6f;
            }
            else if (scoping)
            {
                speed = defaultSpeed * 0.4f;
            }
            else
            {
                speed = defaultSpeed;
            }
        }
    }
    
    public void Jetpack()
    {
        uiManager.SetKnightSliderValue(jetpackFuel / maxJetpackFuel);

        //If the jetpack is off stops the sound and recharges the jetpack
        if(!jetpackOn)
        {
            audioManager.Stop("Jetpack");
            if(jetpackFuel < maxJetpackFuel)
            {
                jetpackFuel += Time.deltaTime * 2;
                    //If the player is crouching you can't use the jetpack
                    if(isCrouched || sliding)
                    {
                        return;
                    }
            }
            currentForce -= Time.deltaTime/5f;    
            if(currentForce < 0f)
            {            
                currentForce = 0f;
            }
            
            if(jetpacking)
            {
                fallVelocity =  8f;
            }

            jetpacking = false;

            return;
        }
        else
        {   
            //If the jetpack is on and it has fuel the player flies
            if(jetpackFuel > 0f)
            {
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
                jetpacking = false;   
                if(audioManager.IsPlaying("Jetpack"))
                {
                    audioManager.Stop("Jetpack");
                }
            }
        } 
    }

    public void Dash()
    {
        if(canDash && !isCrouched)
        {
            StartCoroutine(DashBehaviour());
        }
    }

    IEnumerator DashBehaviour()
    {
        //If the player is on the ground and it dashes then you dash in the direction you are moving towards
        if(!jetpackOn && isGrounded)
        {
            canDash = false;
            dashing = true;
            uiManager.SetKnightSliderColour(Color.red);

            audioManager.Play("Jetpack Dash");

            float oldVertical = vertical;
            float oldHorizontal = horizontal;

            vertical = dashForce * moveInput.y;
            horizontal = dashForce * moveInput.x;

            yield return new WaitForSeconds(0.4f);
            
            dashing = false;

            vertical = oldVertical;
            horizontal = oldHorizontal;

            yield return new WaitForSeconds(2f);

            canDash = true;
            uiManager.SetKnightSliderColour(Color.green);
        }
        //If the player is not on the ground and you dash it dashes in the direction he is looking at
        if(!isGrounded)
        {
            canDash = false;
            dashing = true;

            uiManager.SetKnightSliderColour(Color.red);

            audioManager.Play("Jetpack Dash");

            float oldVerticalX = this.vertical;
            float oldVerticalY = this.horizontal;
            float oldHorizontal = this.velocity.y;
            float oldGravity = this.gravity;

            Vector3 localForward = transform.worldToLocalMatrix.MultiplyVector(fpsCamera.transform.forward);

            vertical = dashForce * localForward.z;
            horizontal = dashForce * localForward.x;

            gravity = 0f;
            velocity.y = dashForce * 5 * localForward.y;
            yield return new WaitForSeconds(0.4f);
            
            dashing = false;

            vertical = oldVerticalX;
            horizontal = oldVerticalY;
            velocity.y = oldHorizontal;
            gravity = oldGravity;

            yield return new WaitForSeconds(2f);

            canDash = true;
            uiManager.SetKnightSliderColour(Color.green);
        }
    }

    public void Charge()
    {
        if(canCharge && !isCrouched)
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
            
            GetComponentInChildren<MouseLook>().mouseSensitivity /= 10;

            vertical = chargeForce * moveInput.y;

            yield return new WaitForSeconds(2.5f);

            charging = false;

            GetComponentInChildren<MouseLook>().mouseSensitivity *= 10;
            
            vertical = 0;

            yield return new WaitForSeconds(10f);

            canCharge = true;
        }
    }

    public void Slam() 
    {
        velocity.y -= 25f;
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
}
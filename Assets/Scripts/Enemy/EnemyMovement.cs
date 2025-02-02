﻿using UnityEngine;
using Mirror;

public class EnemyMovement : NetworkBehaviour
{
    private Transform player;
    private Target target;

    [SerializeField] float speed = 4f;
    [SerializeField] float lookRadius = 20f;
    [SerializeField] float stoppingDistance = 10f;
    [SerializeField] float retreatDistance = 7f;
    private float retreatSpeed;

    #region Getters and Setters

    public float GetSpeed()
    {
        return speed;
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    #endregion

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        target = GetComponent<Target>();
    }

    void Start()
    {
        retreatSpeed = speed * -1.5f;
    }

    //The Enemy will approach the Player only when the Player enters his line of sight.
    //The Enemy will stop once he is close enough to shoot the Player.
    //The Enemy will start retreating when the Player gets too close to him.
    void FixedUpdate()
    {
        if(!target.GetDead())
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= lookRadius)
            {
                FaceTarget();
            }

            if (distance <= lookRadius && distance >= stoppingDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, player.position, speed * Time.fixedDeltaTime);
            }

            if (distance <= stoppingDistance)
            {
                transform.position = this.transform.position;
            }

            if (distance < retreatDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, player.position, retreatSpeed * Time.fixedDeltaTime);
            }
        }
    }

    //Makes the Enemy face the Player.
    void FaceTarget()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * 5f);
    }
}

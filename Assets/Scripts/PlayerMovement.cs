using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header ("Components")]
    public Rigidbody2D      player_rb;
    public Animator player_anim;
    public Cinemachine.CinemachineVirtualCamera camera;

    [Header("Player Stats")]
    public float        movementSpeed = 10f;
    public float        health;
    public float        stamina;
    public float        jumpM = 6;

    public float timeOnGround = 0;
    private float movement;
    private Vector2 speedF;
    private Vector2 movementDirection;
    private int gravityScale;

    public bool isOnGround = false;
    public bool isGrapleShoot = false;
    public bool isFlying = false;
    public bool isMoving = false;
    public bool isJumping = false;
    public bool isHooked = false;
    private void Awake()
    {
        player_rb = GetComponent<Rigidbody2D>();
        player_anim = GetComponent<Animator>();

        camera = GameObject.Find("CM vcam1").GetComponent<Cinemachine.CinemachineVirtualCamera>();
    }

    private void Update()
    {
        Animate();
        CameraZoomOut();
    }
    private void FixedUpdate()
    {
        Vector2 boxRight = new Vector2(GetComponent<BoxCollider2D>().bounds.max.x + 0.0001f, GetComponent<BoxCollider2D>().bounds.min.y);
        Vector2 boxLeft = new Vector2(GetComponent<BoxCollider2D>().bounds.min.x - 0.0001f, GetComponent<BoxCollider2D>().bounds.min.y);
        Vector2 boxUp = new Vector2(GetComponent<BoxCollider2D>().bounds.center.x, GetComponent<BoxCollider2D>().bounds.max.y + 0.001f);

        if (Input.GetKey(KeyCode.D))
        {
            if (!Physics2D.Raycast(boxRight, Vector2.right, 0.1f) && !isHooked)
            {
                if (player_rb.velocity.x <= 7f)
                    player_rb.velocity += new Vector2(0.25f, 0);

                isMoving = true;
                GetComponent<SpriteRenderer>().flipX = false;
            }
            else if (isHooked)
            {
                if (player_rb.velocity.x <= 7f)
                    player_rb.velocity += new Vector2(0.25f, 0);

                isMoving = true;
                GetComponent<SpriteRenderer>().flipX = false;
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (!Physics2D.Raycast(boxLeft, -Vector2.right, 0.1f)  &&  !isHooked)
            {
                if (player_rb.velocity.x >= -7f)
                    player_rb.velocity -= new Vector2(0.25f, 0);

                isMoving = true;
                GetComponent<SpriteRenderer>().flipX = true;
            }
            else if (isHooked)
            {
                if (player_rb.velocity.x >= -7f)
                    player_rb.velocity -= new Vector2(0.25f, 0);

                isMoving = true;
                GetComponent<SpriteRenderer>().flipX = true;
            }
        }
        else 
        {
            isMoving = false;
        }
        if (Input.GetKey(KeyCode.W) & !isJumping & !isFlying)
        {
            if (!(Physics2D.Raycast(boxUp, Vector2.up, 0.1f) || Physics2D.Raycast(boxLeft, -Vector2.right, 0.1f) || Physics2D.Raycast(boxRight, Vector2.right, 0.1f)) & !isJumping)
            {
                player_rb.velocity = new Vector2(player_rb.velocity.x, jumpM);
                isJumping = true;
            }
        }
    }
    //private void Movement()
    //{
    //    movementDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
    //    movementDirection.Normalize();

    //    if (Input.GetKey(KeyCode.D))
    //    {
    //        if (player_rb.velocity.x <= 7f)
    //            player_rb.velocity += new Vector2(0.25f, 0);

    //        isMoving = true;
    //        GetComponent<SpriteRenderer>().flipX = false;
    //    }
    //    else if (Input.GetKey(KeyCode.A))
    //    {
    //        if (player_rb.velocity.x >= -7f)
    //            player_rb.velocity -= new Vector2(0.25f, 0);

    //        isMoving = true;
    //        GetComponent<SpriteRenderer>().flipX = true;
    //    }
    //    else
    //    {
    //        isMoving = false ;
    //    }
    //    if (Input.GetButton("Jump") && !isJumping)
    //    {
    //        player_rb.velocity = new Vector2(player_rb.velocity.x, jumpM);
    //        isJumping = true;
    //    }
    //}
    private void Animate()
    {
        if (timeOnGround > 0)
            isOnGround = true;
        else if (timeOnGround <= 0)
            isOnGround = false;

        player_anim.SetBool("isFlying", isFlying);
        player_anim.SetBool("isGrapleShoot", isGrapleShoot);
        player_anim.SetBool("isMoving", isMoving);
        player_anim.SetBool("isOnGround", isOnGround);
        player_anim.SetFloat("Blend_x", movementDirection.x);
        player_anim.SetFloat("Blend_y", movementDirection.y);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isJumping)
            isJumping = false;
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        timeOnGround += Time.deltaTime;

        if (isFlying)
            isFlying = false;
        if (timeOnGround >= 3f)
            isJumping = false;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        isFlying = true;
        timeOnGround = 0;
    }
    private void CameraZoomOut()
    {
        if (isHooked)
        {
            if (camera.m_Lens.OrthographicSize <= 8)
                camera.m_Lens.OrthographicSize += 0.01f;
        }
        else
        {
            if (camera.m_Lens.OrthographicSize >= 5)
                camera.m_Lens.OrthographicSize -= 0.01f;
        }
    }
    private void InputProccesing()
    {
        //to do: управление геймадом, настройка управления
        //Input input;
        //to do: switch или else if для проверки устройства ввода
        //ниже: само управление
    }
    //private void Movement()
    //{
    //    movementDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
    //    movement = Mathf.Clamp(movementDirection.magnitude, 0.0f, 2.0f);
    //    movementDirection.Normalize();
    //    speedF = movementDirection;

    //    player_anim.SetFloat("Blend_x", movementDirection.y);
    //    player_anim.SetFloat("Blend_y", movementDirection.x);
    //}
    //private void Move()
    //{
    //    if (!isHooked)
    //        gravityScale = 1;
    //    else
    //        gravityScale = 1;

    //    player_rb.gravityScale = gravityScale;
    //    if (Mathf.Abs(movementDirection.x) > 0 || Mathf.Abs(movementDirection.y) > 0)
    //        player_rb.velocity = speedF * movement * movementSpeed;
    //}
}

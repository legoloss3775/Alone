using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public static class ExtensionMethods
{
    public static float Map(this float value, float fromSource, float toSource, float fromTarget, float toTarget)
    {
        return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
    }
}

public class PlayerMovement : Mirror.NetworkBehaviour
{
    [Header ("Components")]
    public Rigidbody2D      player_rb;
    public Animator player_anim;
    public Cinemachine.CinemachineVirtualCamera camera;
    private SpriteRenderer spriteRenderer;

    [Header("Player Stats")]
    public float        movementSpeed = 10f;
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
    public bool isSliding = false;
    public bool isAttacking = false;
    public bool isGasFlying = false;

    public bool faceRight = false;
    private void Awake()
    {
        player_rb = GetComponent<Rigidbody2D>();
        player_anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        camera = transform.GetChild(5).gameObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();
        camera.m_Lens.OrthographicSize = 6.5f;
    }

    private void Update()
    {
        Animate();
        CameraZoomOut();

        if (player_rb.velocity.x > 0.1f)
            Flip(false);
        else if (player_rb.velocity.x < -0.1f)
            Flip(true);

        if (player_rb.velocity.x >= 50)
            player_rb.velocity = new Vector2(50, player_rb.velocity.y);
        if (player_rb.velocity.y >= 50)
            player_rb.velocity = new Vector2(player_rb.velocity.x, 50);
        if (player_rb.velocity.x <= -50)
            player_rb.velocity = new Vector2(-50, player_rb.velocity.y);
        if (player_rb.velocity.y <= -50)
            player_rb.velocity = new Vector2(player_rb.velocity.x, -50);

        if (Input.GetKeyDown(KeyCode.Space) & !isAttacking)
        {
            isAttacking = true;
            player_rb.velocity = new Vector2(player_rb.velocity.x / 2, player_rb.velocity.y);
            player_anim.SetInteger("Attack", 1);
        }
        else
        {
            isAttacking = false;
            player_anim.SetInteger("Attack", 0);
        }
    }
    private void FixedUpdate()
    {
        FixedPlayerMovement();
    }
    [Client]
    void FixedPlayerMovement()
    {
        Vector2 boxRight = new Vector2(GetComponent<BoxCollider2D>().bounds.max.x + 0.0001f, GetComponent<BoxCollider2D>().bounds.min.y);
        Vector2 boxLeft = new Vector2(GetComponent<BoxCollider2D>().bounds.min.x - 0.0001f, GetComponent<BoxCollider2D>().bounds.min.y);
        Vector2 boxUp = new Vector2(GetComponent<BoxCollider2D>().bounds.center.x, GetComponent<BoxCollider2D>().bounds.max.y + 0.001f);

        if (Input.GetKey(KeyCode.D) & !isSliding)
        {
            if (!Physics2D.Raycast(boxRight, Vector2.right, 0.1f) & !isHooked)
            {
                if (player_rb.velocity.x <= 6.5f)
                    player_rb.velocity += new Vector2(0.5f, 0);

                else if (isFlying & player_rb.velocity.x <= 3.25f)
                    player_rb.velocity += new Vector2(0.1f, 0);

                isMoving = true;
                //Flip(false);
            }
            else if (isHooked)
            {
                if (player_rb.velocity.x <= 4.5f)
                    player_rb.velocity += new Vector2(0.5f, 0);

                isMoving = true;
                //Flip(false);
            }
        }
        else if (Input.GetKey(KeyCode.A) & !isSliding)
        {
            if (!Physics2D.Raycast(boxLeft, -Vector2.right, 0.1f) & !isHooked)
            {
                if (player_rb.velocity.x >= -6.5f)
                    player_rb.velocity -= new Vector2(0.5f, 0);

                else if (isFlying & player_rb.velocity.x >= -3.25f)
                    player_rb.velocity -= new Vector2(0.1f, 0);

                isMoving = true;
                Flip(true);
            }
            else if (isHooked)
            {
                if (player_rb.velocity.x >= -4.5f)
                    player_rb.velocity -= new Vector2(0.5f, 0);

                isMoving = true;
                Flip(true);
            }
        }
        else if (!isSliding)
        {
            var slowSpeed = 0.2f;

            if (Math.Abs(player_rb.velocity.x) >= 0f & Math.Abs(player_rb.velocity.x) <= 0.1f)
                player_rb.velocity = new Vector2(0, player_rb.velocity.y);
            else if (!isFlying && !isSliding & player_rb.velocity.x > 0)
                player_rb.velocity -= new Vector2(slowSpeed, 0);
            else if (!isFlying && !isSliding & player_rb.velocity.x < 0)
                player_rb.velocity += new Vector2(slowSpeed, 0);


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

        if (Input.GetKey(KeyCode.LeftShift) & !isHooked & !isJumping & !isAttacking & isOnGround)
        {
            //var slidePush = player_rb.velocity.x > 0 ? new Vector2(2f, 0) : new Vector2(-2f, 0);
            //if (Input.GetKeyDown(KeyCode.LeftShift))
                //player_rb.velocity += slidePush;
            var slideCharge = player_rb.velocity * 1.005f;
            player_rb.velocity = slideCharge;
            isSliding = true;
        }
        else
        {
            isSliding = false;
        }
        CmdPlayerMovementOnServer(player_rb.velocity, isMoving, isJumping, isSliding); 
    }
    [Command]
    void CmdPlayerMovementOnServer(Vector2 velocity, bool _isMoving, bool _isJumping, bool _isSliding)
    {
        player_rb.velocity = velocity;
        isMoving = _isMoving;
        isJumping = _isJumping;
        isSliding = _isSliding;

        RpcPlayerMovementOnClients(velocity, _isMoving, _isJumping, _isSliding);
    }
    [ClientRpc]
    void RpcPlayerMovementOnClients(Vector2 velocity, bool _isMoving, bool _isJumping, bool _isSliding)
    {
        if (isLocalPlayer) return;

        player_rb.velocity = velocity;
        isMoving = _isMoving;
        isJumping = _isJumping;
        isSliding = _isSliding;
    }
    [Command]
    void CmdProvideFlipStateToServer(bool state)
    {
        spriteRenderer.flipX = state;

        RpcSendFlipState(state);
    }

    [ClientRpc]
    void RpcSendFlipState(bool state)
    {
        if (isLocalPlayer) return;

        spriteRenderer.flipX = state;
    }
    [Client]
    private void Flip(bool state)
    {
        if (!isLocalPlayer) return;

        faceRight = state;
        spriteRenderer.flipX = faceRight;

        CmdProvideFlipStateToServer(spriteRenderer.flipX);
    }
    private void Animate()
    {
        if (timeOnGround > 0)
            isOnGround = true;
        else if (timeOnGround <= 0)
            isOnGround = false;

        player_anim.SetBool("isFlying", isFlying);
        player_anim.SetBool("isGasFlying", isGasFlying);
        player_anim.SetBool("isGrapleShoot", isGrapleShoot);
        player_anim.SetBool("isSliding", isSliding);
        player_anim.SetBool("isMoving", isMoving);
        player_anim.SetBool("isOnGround", isOnGround);
        player_anim.SetFloat("Blend_x", movementDirection.x);
        player_anim.SetFloat("Blend_y", (int)Mathf.Clamp(
            ExtensionMethods.Map(player_rb.velocity.y, jumpM, -jumpM, 0, 4),
            0,
            4));
        player_anim.SetFloat("Slide_x", Mathf.Clamp(ExtensionMethods.Map(player_rb.velocity.x, 7, -7, -1, 1), -1, 1));

        player_anim.SetFloat("fly_x", (int)Mathf.Clamp(Math.Abs(player_rb.velocity.x), -2, 2));
        player_anim.SetFloat("fly_y", (int)Mathf.Clamp(player_rb.velocity.y, -2, 2));
    }
    //public void GetAirSprite()
    //{
    //    int airIndex = (int)Mathf.Clamp(
    //        ExtensionMethods.Map(player_rb.velocity.y, jumpM, -jumpM, 0, airborne_anim.Length),
    //        0,
    //        airborne_anim.Length - 1);
    //    spriteRenderer.sprite = airborne_anim[airIndex];
    //}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isJumping)
            isJumping = false;
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        Vector2 boxRight = new Vector2(GetComponent<BoxCollider2D>().bounds.max.x + 0.0001f, GetComponent<BoxCollider2D>().bounds.min.y);
        Vector2 boxLeft = new Vector2(GetComponent<BoxCollider2D>().bounds.min.x - 0.0001f, GetComponent<BoxCollider2D>().bounds.min.y);
        Vector2 boxUp = new Vector2(GetComponent<BoxCollider2D>().bounds.center.x, GetComponent<BoxCollider2D>().bounds.max.y + 0.001f);

        timeOnGround += Time.deltaTime;

        if (isFlying & !(Physics2D.Raycast(boxUp, Vector2.up, 0.1f) || Physics2D.Raycast(boxLeft, -Vector2.right, 0.1f) || Physics2D.Raycast(boxRight, Vector2.right, 0.1f)))
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
        camera.Follow = transform;
        if (isHooked)
        {
            if (camera.m_Lens.OrthographicSize <= 10.5f)
                camera.m_Lens.OrthographicSize += 0.01f;
        }
        else if (isSliding)
        {
            if (camera.m_Lens.OrthographicSize >= 8f)
                camera.m_Lens.OrthographicSize -= 0.01f;
        }
        else
        {
            if (camera.m_Lens.OrthographicSize >= 8.5f)
                camera.m_Lens.OrthographicSize -= 0.01f;
            if (camera.m_Lens.OrthographicSize <= 8.5f)
                camera.m_Lens.OrthographicSize += 0.01f;
        }
    }
}

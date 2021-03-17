using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookSystem : MonoBehaviour
{
    public PlayerMovement movement;
    public ParticleSystem particles;
    public GameObject[] hook = new GameObject[25];

    public float gasStamina = 3f;
    public bool hookIsShooted = false;
    public bool hookIsConnected = false;
    public float moveSpeed = 2;
    public float hookSpeed = 8;
    public GameObject crossHair;

    private SpriteRenderer[] hookSprite = new SpriteRenderer[25];
    private HingeJoint2D[] hookJoint = new HingeJoint2D[25];
    private Rigidbody2D[] hookRb = new Rigidbody2D[25];
    private List<ParticleSystem> deleteParticles = new List<ParticleSystem>();

    private ParticleSystem playParticles;
    private PolygonCollider2D playerCol;
    private GameObject graple;
    private HingeJoint2D grapleJoint;
    private Rigidbody2D grapleRb;
    private Rigidbody2D thrust;
    private Rigidbody2D playerRb;

    public bool staminaRegen = false;
    private float time;
    
    private void Awake()
    {
        playerCol = GetComponent<PolygonCollider2D>();
        playerRb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        for (int i = 0; i < 25; i++)
        {
            hookJoint[i] = hook[i].GetComponent<HingeJoint2D>();
            hookRb[i] = hook[i].GetComponent<Rigidbody2D>();
            hookSprite[i] = hook[i].GetComponent<SpriteRenderer>();
        }

        graple = hook[24];
        grapleRb = hookRb[24];
        grapleJoint = hookJoint[24]; //12 - индекс части крюка, который прикрепляется к объектам
        thrust = hookRb[0]; //0 - к игроку

        HideHook();
        DisattachHook();
    }
    private void Update()
    {
        ShowCrosshair();
        if (Input.GetMouseButton(0))
        {
            ShootGraple();
            if (Input.GetMouseButtonDown(0))
            {
                hookIsShooted = true;
                ShowHook();
                ShootGraple();
            }
            if (Input.GetMouseButton(1))
            {
                //if (hookIsConnected)
                    //ReturnPlayerToHookPosition();
            }
        }
        else
        {
            hookIsShooted = false;
            DisattachHook();
            HideHook();
            ReturnHookPostion();
        }
        if (Input.GetKey(KeyCode.E))
        {
            MovePlayerToIndicatorPostion();
        }

        CheckParticles();
        CheckHookState();
        CheckGasStamina();
    }

    private void HideHook()
    {
        for (int i = 0; i < 25; i++)
        {
            hookSprite[i].enabled = false;
        }
    }
    private void ShowHook()
    {
        for (int i = 0; i < 25; i++)
        {
            hookSprite[i].enabled = true;
        }
    }
    private void ReturnHookPostion()
    {
        for (int i = 0; i < 25; i++)
        {
            hookRb[i].velocity = Vector2.zero;
            hookRb[i].mass = 5;
            hookRb[i].velocity -= Vector2.Lerp(
                hookRb[i].transform.position,
                hookRb[i].transform.position - transform.position,
                1
            )*10;
        }
        graple.GetComponent<BoxCollider2D>().isTrigger = true;
    }
    private void ReturnPlayerToHookPosition()
    {
        for (int i = 0; i < 25; i++)
        {
            hookRb[i].velocity = Vector2.zero;
            hookRb[i].mass = 5;
            hookRb[i].velocity -= Vector2.Lerp(
                hookRb[i].transform.position, 
                hookRb[i].transform.position - grapleRb.transform.position,
                1
            ) * 2;
        }
        playerRb.velocity -= Vector2.Lerp(
            playerRb.transform.position, 
            playerRb.transform.position - grapleRb.transform.position,
            1
        );
    }
    private void ShootGraple()
    {
        graple.GetComponent<BoxCollider2D>().isTrigger = false;
        if (hookIsShooted)
        {
            time += Time.deltaTime;
            if (time <= 0.5f)
                grapleRb.velocity = Vector2.Lerp(
                    graple.transform.position,
                    crossHair.transform.position - graple.transform.position,
                    1
                ) * hookSpeed;
        }
        if (time >= 0.5f)
        {
            hookIsShooted = false;
            time = 0;
        }
    }

    private void ShowCrosshair()
    {
        Cursor.visible = false;

        Vector2 mouseCursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        crossHair.transform.position = mouseCursorPos;
    }
    private void MovePlayerToIndicatorPostion()
    {
        if (gasStamina >= 0 && movement.isHooked)
        {
            playerRb.velocity = Vector2.Lerp(
                playerRb.transform.position,
                crossHair.transform.position - playerRb.transform.position,
                1
                ) * 2f;
            PlayParticles(1);

            gasStamina -= Time.deltaTime;
            staminaRegen = false;
        }
        if (gasStamina >= 0 && !movement.isHooked)
        {
            playerRb.velocity = Vector2.Lerp(
                playerRb.transform.position,
                crossHair.transform.position - playerRb.transform.position,
                1
                );
            PlayParticles(1);

            gasStamina -= Time.deltaTime * 3;
            staminaRegen = false;
        }
    }
    private void ChangePlayerSpeed()
    {
            //сделать ускорение на shift со стаминой
    }
    private void ChangePlayerMass(float mass)
    {
        playerRb.mass = mass;
    }
    
    private void PlayParticles(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            playParticles = Instantiate(
                particles,
                playerRb.transform.position,
                playerRb.transform.rotation
                );
            deleteParticles.Add(playParticles);
            playParticles.Play();
        }
    }
    private void CheckParticles()
    {
        foreach (var particle in deleteParticles)
        {
            if (!particle.IsAlive())
            {
                Destroy(particle.gameObject);
                deleteParticles.Remove(particle);
            }
        }
    }
    private void CheckHookState()
    {
        movement.isGrapleShoot = hookIsShooted;
    }
    private void CheckGasStamina()
    {
        if (gasStamina <= 3f && staminaRegen)
        {
            gasStamina += Time.deltaTime * 2;
        }
        if (!movement.isFlying && movement.timeOnGround >= 1)
        {
            staminaRegen = true;
        }
    }
    public void DisattachHook()
    {
        grapleJoint.connectedBody = grapleRb;
        grapleJoint.enabled = false;
        hookIsConnected = false;

        ChangePlayerMass(1000);
        playerRb.rotation = 0;
        playerRb.freezeRotation = true;
        movement.isHooked = false;

        graple.GetComponent<BoxCollider2D>().enabled = true;
    }
    public void AttachGraple(Collision2D collision)
    {
        grapleJoint.enabled = true;
        grapleJoint.connectedBody = collision.gameObject.GetComponent<Rigidbody2D>();

        ChangePlayerMass(5f);
        playerRb.freezeRotation = true;
        movement.isHooked = true;
        movement.isFlying = true;

        graple.GetComponent<BoxCollider2D>().enabled = false;
    }
}

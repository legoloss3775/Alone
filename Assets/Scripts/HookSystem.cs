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

    private Ray2D hookRay;
    private Ray2D shootRay;

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
        if (Input.GetMouseButtonDown(0))
        {
            hookIsShooted = true;
            shootRay.origin = transform.position;
            shootRay.direction = crossHair.transform.position - transform.position;
            ShowHook();
            //ShootGraple();
            if (Input.GetMouseButton(0))
            {
                ShootGraple();
            }
        }
        else if (!Input.GetMouseButton(0))
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
            hookRb[i].mass = 1;
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
        //graple.GetComponent<BoxCollider2D>().isTrigger = false;
        if (hookIsShooted)
        {
            time += Time.deltaTime;
            if (time <= 0.5f)
                grapleRb.velocity = Vector2.Lerp(
                    graple.transform.position,
                    new Vector3(shootRay.GetPoint(12).x, shootRay.GetPoint(12).y) - graple.transform.position,
                    1
                ) * hookSpeed;
        }
        if (time >= 0.5f)
        {
            hookIsShooted = false;
            time = 0;
        }
        hookIsShooted = false;
    }

    private void ShowCrosshair()
    {
        Cursor.visible = false;

        var worldMousePosition =
                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));
        var facingDirection = worldMousePosition - transform.position;
        var aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }
        var aimDirection = Quaternion.Euler(0, 0, aimAngle * Mathf.Rad2Deg) * Vector2.right;

        var x = transform.position.x + 2f * Mathf.Cos(aimAngle);
        var y = transform.position.y + 2f * Mathf.Sin(aimAngle);

        var crossHairPosition = new Vector3(x, y, 0);
        crossHair.transform.position = crossHairPosition;

        facingDirection.Normalize();

        float rot_z = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        crossHair.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);
    }
    private void MovePlayerToIndicatorPostion()
    {
        Vector3 directionPos = new Vector3(hookRay.GetPoint(12).x, hookRay.GetPoint(12).y);

        if (gasStamina >= 0 && movement.isHooked)
        {
            playerRb.velocity = Vector2.Lerp(
                playerRb.transform.position,
                directionPos - playerRb.transform.position,
                1
                ) * 1.25f;
            PlayParticles(1);

            gasStamina -= Time.deltaTime;
            staminaRegen = false;
        }
        if (gasStamina >= 0 && !movement.isHooked)
        {
            playerRb.velocity = Vector2.Lerp(
                playerRb.transform.position,
                directionPos - playerRb.transform.position,
                1
                )/1.5f;
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
        hookRay.origin = transform.position;
        hookRay.direction = crossHair.transform.position - transform.position;
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

        ChangePlayerMass(1000f);
        playerRb.rotation = 0;
        playerRb.freezeRotation = true;
        movement.isHooked = false;

        graple.GetComponent<BoxCollider2D>().enabled = true;
    }
    public void AttachGraple(Rigidbody2D collision)
    {
        grapleJoint.enabled = true;
        grapleJoint.connectedBody = collision.gameObject.GetComponent<Rigidbody2D>();

        ChangePlayerMass(25f);
        playerRb.freezeRotation = true;
        movement.isHooked = true;
        movement.isFlying = true;

        graple.GetComponent<BoxCollider2D>().enabled = false;
    }

    private void OnDrawGizmos()
    {
        hookRay.origin = transform.position;
        hookRay.direction = crossHair.transform.position - transform.position;
        Gizmos.DrawLine(transform.position, new Vector3(hookRay.GetPoint(20).x, hookRay.GetPoint(20).y));
    }
}



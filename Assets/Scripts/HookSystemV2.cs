using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookSystemV2 : MonoBehaviour
{
    public PlayerMovement movement;
    public ParticleSystem particles;

    public float gasStamina = 3f;
    public bool hookIsShooted = false;
    public bool hookIsConnected = false;
    public float moveSpeed = 2;
    public float hookSpeed = 8;
    public float hookMaxDistance = 8.5f;
    private float hookCurrentDistance;
    public GameObject crossHair;
    public GameObject hookCrossHair;
    public LineRenderer hookSprite;
    
    private DistanceJoint2D playerJoint;
    private HingeJoint2D grapleJoint;
    private List<ParticleSystem> deleteParticles = new List<ParticleSystem>();

    private Ray2D hookRay;
    private Ray2D shootRay;

    private ParticleSystem playParticles;
    private BoxCollider2D playerCol;
    private Rigidbody2D playerRb;

    private Rigidbody2D grapleRb;
    private GameObject graple;

    public bool staminaRegen = false;
    private float time;

    public Vector2[] hookPoints;
    
    private void Awake()
    {
        playerCol = GetComponent<BoxCollider2D>();
        playerRb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        graple = GameObject.Find("h25");
        grapleRb = graple.GetComponent<Rigidbody2D>();
        grapleJoint = graple.GetComponent<HingeJoint2D>();

        playerJoint = GetComponent<DistanceJoint2D>(); //12 - индекс части крюка, который прикрепляется к объектам

        HookRendererSetStartPoints();
        HideHook();
        DisattachHook();
    }
    private void Update()
    {
        ShowCrosshair();
        ShowSecondCrossHair();
        CheckHookState();

        if (movement.isHooked)
            ShowHook();
        else
            HideHook();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            hookIsShooted = true;
            shootRay.origin = transform.position;
            shootRay.direction = crossHair.transform.position - transform.position;
            ShootGraple();
        }
        else
            hookIsShooted = false;

        if (!Input.GetKey(KeyCode.Space))
        {
            DisattachHook();
            ReturnHookPostion();
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            MakeHookLonger();
        }
        if (Input.GetMouseButton(1))
        {
            MovePlayerToIndicatorPostion();
        }


        CheckParticles();
        CheckGasStamina();
    }

    private void HideHook()
    {
        hookSprite.enabled = false;
    }
    private void ShowHook()
    {
        for (int i = 1; i < hookPoints.Length - 1; i++)
        {
            hookPoints[i] = transform.position;
        }
        hookSprite.enabled = true;
        HookRendererSetPoints(hookPoints);
    }
    private void HookRendererSetStartPoints()
    {
        hookSprite.SetPosition(0, transform.position - new Vector3(-0.1f, 0));
        hookSprite.SetPosition(hookPoints.Length - 1, graple.transform.position);;

        for (int i = 1; i < hookPoints.Length - 1; i++)
        {
            hookPoints[i] = transform.position;
        }
        HookRendererSetPoints(hookPoints);
    }
    public void HookRendererSetPoints(Vector2[] points)
    {

        hookSprite.positionCount = points.Length;
        hookPoints = points;

        hookSprite.SetPosition(0, transform.position - new Vector3(-0.1f, 0));
        hookSprite.SetPosition(hookPoints.Length - 1, graple.transform.position); 

        for (int i = 1; i < hookPoints.Length - 1; i++)
        {

            hookSprite.SetPosition(i, hookPoints[i]);
        }
        
    }
    private void ReturnHookPostion()
    {

    }
    private void MakeHookLonger()
    {
        if (playerJoint.distance <= hookMaxDistance)
        {
            playerJoint.distance = Vector2.Lerp(graple.transform.position, playerRb.transform.position - graple.transform.position, 1).magnitude;
        }
        else
        {
            playerJoint.distance = hookMaxDistance;
        }
    }
    private void ShootGraple()
    {
        Vector3 hitPoint = Physics2D.Raycast(crossHair.transform.position, shootRay.direction).point;
        Rigidbody2D hitObj = Physics2D.Raycast(crossHair.transform.position, shootRay.direction).rigidbody;

        //graple.GetComponent<BoxCollider2D>().isTrigger = false;
        if (Vector2.Lerp(transform.position, hitPoint - transform.position,1).magnitude <= hookMaxDistance)
        {
            graple.transform.position = hitPoint;
            playerJoint.distance = Vector2.Lerp(transform.position, hitPoint - transform.position, 1).magnitude;

            AttachGraple(hitObj);
        }
    }
    private void FastHookFire()
    {
        hookCurrentDistance = playerJoint.distance;
        if (hookCurrentDistance >= 1f)
        {
            playerJoint.distance -= moveSpeed * 15;
            PlayParticles(10);
        }
    }
    private void ShowSecondCrossHair()
    {
        Vector3 hitPoint = Physics2D.Raycast(crossHair.transform.position, hookRay.direction).point;
        Rigidbody2D hitObj = Physics2D.Raycast(crossHair.transform.position, shootRay.direction).rigidbody;

        if (Vector2.Lerp(transform.position, hitPoint - transform.position, 1).magnitude <= hookMaxDistance)
        {
            hookCrossHair.SetActive(true);
            hookCrossHair.transform.position = hitPoint;
        }
        else 
        {
            hookCrossHair.SetActive(false);
        }
        if (!Physics2D.Raycast(crossHair.transform.position, hookRay.direction))
            hookCrossHair.SetActive(false);
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

        if (gasStamina >= 0 & movement.isHooked)
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
        if (gasStamina >= 0 & !movement.isHooked)
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
        if (gasStamina <= 3f & staminaRegen)
        {
            gasStamina += Time.deltaTime * 2;
        }
        if (!movement.isFlying & movement.timeOnGround >= 1)
        {
            staminaRegen = true;
        }
    }
    public void DisattachHook()
    {
        grapleJoint.connectedBody = grapleRb;
        grapleJoint.enabled = false;
        hookIsConnected = false;

        ChangePlayerMass(25f);
        playerRb.rotation = 0;
        playerRb.freezeRotation = true;
        movement.isHooked = false;

        graple.GetComponent<BoxCollider2D>().enabled = true;

        playerJoint.connectedBody = playerRb;
        playerJoint.enabled = false;
    }
    public void AttachGraple(Rigidbody2D collision)
    {
        grapleJoint.enabled = true;
        grapleJoint.connectedBody = collision.gameObject.GetComponent<Rigidbody2D>();

        ChangePlayerMass(5f);
        playerRb.freezeRotation = true;
        movement.isHooked = true;
        movement.isFlying = true;

        graple.GetComponent<BoxCollider2D>().enabled = false;

        playerJoint.connectedBody = grapleRb;
        playerJoint.enabled = true;
    }

    private void OnDrawGizmos()
    {
        hookRay.origin = transform.position;
        hookRay.direction = crossHair.transform.position - transform.position;
        Gizmos.DrawLine(transform.position, new Vector3(hookRay.GetPoint(20).x, hookRay.GetPoint(20).y));

        Gizmos.DrawLine(transform.position, graple.transform.position);

        if (!movement.isHooked)
        {
            Gizmos.DrawWireSphere(transform.position, hookMaxDistance);
        }
        else
        {
            hookCurrentDistance = playerJoint.distance;
            Gizmos.DrawWireSphere(graple.transform.position, hookCurrentDistance);
        }
    }
}



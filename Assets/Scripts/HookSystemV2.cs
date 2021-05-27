using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HookSystemV2 : Mirror.NetworkBehaviour
{
    public PlayerMovement movement;
    public ParticleSystem particles;

    [SyncVar]
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
    private TargetJoint2D grapleJoint;
    private List<ParticleSystem> deleteParticles = new List<ParticleSystem>();

    private Ray2D hookRay;
    private Ray2D shootRay;
    private Vector3 shootPos;

    public RaycastHit2D[] hookHits;
    public Vector2[] temp;

    private ParticleSystem playParticles;
    private BoxCollider2D playerCol;
    private Rigidbody2D playerRb;

    private Rigidbody2D grapleRb;
    public GameObject graple;
    public GameObject targetPoint;

    public bool staminaRegen = false;
    public bool hookFired = false;

    [SyncVar]
    public List<Vector2> hookPoints = new List<Vector2>();

    private float time;


    private void Awake()
    {
        playerCol = GetComponent<BoxCollider2D>();
        playerRb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        graple = transform.GetChild(3).gameObject;
        grapleRb = graple.GetComponent<Rigidbody2D>();
        grapleJoint = graple.GetComponent<TargetJoint2D>();

        playerJoint = GetComponent<DistanceJoint2D>(); //12 - индекс части крюка, который прикрепляется к объектам
        hookSprite = transform.GetChild(4).gameObject.GetComponent<LineRenderer>();
        hookSprite.positionCount = 2;

        graple.transform.position = transform.position;
    }
    private void Start()
    {
        grapleJoint.connectedBody = grapleRb;
        grapleJoint.enabled = false;
        hookIsConnected = false;

        playerRb.mass = 25f;
        playerRb.rotation = 0;
        playerRb.freezeRotation = true;
        movement.isHooked = false;

        graple.GetComponent<BoxCollider2D>().enabled = true;

        playerJoint.connectedBody = playerRb;
        playerJoint.enabled = false;

        HideHook();
        ShowHook();

        if (!isLocalPlayer)
            crossHair.SetActive(false);
        //DisattachHook();
    }
    private void Update()
    {
        ShowCrosshair();
        ShowSecondCrossHair();

        ShowHook();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            hookIsShooted = true;
            ShootGraple();
        }
        else
            hookIsShooted = false;

        MoveGraple();

        if (!Input.GetKey(KeyCode.Space) && movement.isHooked)
        {
            DisattachHook();
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            MakeHookLonger();
        }
        if (Input.GetMouseButton(1))
        {
            MovePlayerToIndicatorPostion();
        }
        else
            movement.isGasFlying = false;
        try
        {
            CheckParticles();
        }
        catch (System.InvalidOperationException)
        {
            
        }

        UpdateTargetPoint();
        CheckHookState();
        CheckGasStamina();
    }
    [Client]
    private void HideHook()
    {
        hookSprite.enabled = false;
        CmdHideHookOnServer(hookSprite.enabled);
    }
    [Command]
    private void CmdHideHookOnServer(bool state)
    {
        hookSprite.enabled = state;
        RpcHideHookOnServer(state);
    }
    [ClientRpc]
    private void RpcHideHookOnServer(bool state)
    {
        if (isLocalPlayer) return;
        hookSprite.enabled = state;
    }
    [Client]
    private void ShowHook()
    {
        hookPoints = new List<Vector2>
        {
            transform.position,
            //(transform.position + (graple.transform.position - transform.position) / 3f) + new Vector3(0.015f, 0),
            //((transform.position + graple.transform.position) + new Vector3(0.015f, 0)) / 2,
            //(transform.position + (graple.transform.position - transform.position) / 1.5f) + new Vector3(0.035f, 0),
            //(transform.position + (graple.transform.position - transform.position) / 1.1f) + new Vector3(0.015f, 0),
            graple.transform.position,
        };
        if (movement.faceRight)
            hookPoints[0] = transform.position + new Vector3(-0.15f, -0.2f);
        else
            hookPoints[0] = transform.position + new Vector3(0.15f, -0.2f);
        hookSprite.positionCount = hookPoints.Count;

        hookSprite.enabled = true;
        HookRendererSetPoints(hookPoints);
    }
    [Client]
    public void HookRendererSetPoints(List<Vector2> points)
    {
        hookSprite.positionCount = points.Count;

        for (int i = 0; i < hookPoints.Count; i++)
        {
            hookSprite.SetPosition(i, points[i]);
        }
        graple.GetComponent<SpriteRenderer>().enabled = false;
        CmdUpdateHookRendererOnServer(points, hookSprite.enabled);
    }
    [Command]
    private void CmdUpdateHookRendererOnServer(List<Vector2> points, bool state)
    {
        hookSprite.enabled = state;
        hookSprite.positionCount = points.Count;

        for (int i = 0; i < hookPoints.Count; i++)
        {
            hookSprite.SetPosition(i, points[i]);
        }
        graple.GetComponent<SpriteRenderer>().enabled = false;
        RpcUpdateHookRendererOnClients(points, state);
    }
    [ClientRpc]
    private void RpcUpdateHookRendererOnClients(List<Vector2> points, bool state)
    {
        if (isLocalPlayer) return;
        hookSprite.enabled = state;
        hookSprite.positionCount = points.Count;

        for (int i = 0; i < hookPoints.Count; i++)
        {
            hookSprite.SetPosition(i, points[i]);
        }
        graple.GetComponent<SpriteRenderer>().enabled = false;
    }
    [Client]
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
        CmdMakeHookLongerOnServer(playerJoint.distance);
    }
    [Command]
    private void CmdMakeHookLongerOnServer(float distance)
    {
        if (playerJoint.distance <= hookMaxDistance)
        {
            playerJoint.distance = distance;
        }
        else
        {
            playerJoint.distance = hookMaxDistance;
        }
        RpcMakeHookLongerOnClinents(distance);
    }
    [ClientRpc]
    private void RpcMakeHookLongerOnClinents(float distance)
    {
        if (isLocalPlayer) return;
        if (playerJoint.distance <= hookMaxDistance)
        {
            playerJoint.distance = distance;
        }
        else
        {
            playerJoint.distance = hookMaxDistance;
        }
    }
    [Client]
    private void ShootGraple()
    {

        shootRay.origin = transform.position;
        shootRay.direction = crossHair.transform.position - transform.position;

        Vector3 hitPoint = Physics2D.Raycast(crossHair.transform.position, shootRay.direction).point;
        //Rigidbody2D hitObj = Physics2D.Raycast(crossHair.transform.position, shootRay.direction).rigidbody;

        shootPos = hitPoint;

        hookFired = true;

    }
    [Client]
    private void MoveGraple()
    {
        if (hookFired)
        {
            time = 0;
            grapleRb.velocity = Vector2.zero;
            if ((graple.transform.position - transform.position).magnitude <= hookMaxDistance)
                grapleRb.velocity = (shootRay.direction * 30);
            else
                hookFired = false;
        }
        else if (!movement.isHooked)
        {
            time += Time.deltaTime;
            grapleRb.velocity = Vector2.Lerp(
                grapleRb.transform.position,
                transform.position - graple.transform.position,
                1
            ) * 15;
            if (time > 1f)
            {
                HideHook();
            }
        }
        CmdMoveGrapleOnServer(grapleRb.velocity);
    }
    [Command]
    private void CmdMoveGrapleOnServer(Vector2 velocity)
    {
       grapleRb.velocity = velocity;

        RpcMoveGrapleOnClients(velocity);
    }
    private void RpcMoveGrapleOnClients(Vector2 velocity)
    {
        if (isLocalPlayer) return;

         grapleRb.velocity = velocity;
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
    [Client]
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

    [Client]
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
    [Client]
    private void MovePlayerToIndicatorPostion()
    {
        Vector3 directionPos = new Vector3(hookRay.GetPoint(12).x, hookRay.GetPoint(12).y) + (graple.transform.position - transform.position)/10;

        if (gasStamina >= 0 & movement.isHooked)
        {
            playerRb.velocity = Vector2.Lerp(
                playerRb.transform.position,
                directionPos - playerRb.transform.position,
                1
                ) * 1.2f;
            if (deleteParticles.Count <= 400)
                PlayParticles(1);

            gasStamina -= Time.deltaTime;
            staminaRegen = false;

            movement.isGasFlying = true;
            if (gasStamina <= 0)
                movement.isGasFlying = false;
        }
        else if (gasStamina >= 0 & !movement.isHooked)
        {
            playerRb.velocity = Vector2.Lerp(
                playerRb.transform.position,
                directionPos - playerRb.transform.position,
                1
                ) / 1.5f;
            if (deleteParticles.Count <= 400)
                PlayParticles(1);

            gasStamina -= Time.deltaTime * 3;
            staminaRegen = false;
        }

        CmdMovePlayerToIndicatorPositonOnServer(playerRb.velocity, gasStamina);
    }
    [Command]
    private void CmdMovePlayerToIndicatorPositonOnServer(Vector2 velocity, float clinetGasStamina)
    {
            playerRb.velocity = velocity;
            gasStamina = clinetGasStamina;

        RpcMovePlayerToIndicatiorPositionOnClients(velocity, clinetGasStamina);
    }
    [ClientRpc]
    private void RpcMovePlayerToIndicatiorPositionOnClients(Vector2 velocity, float clinetGasStamina)
    {
        if (isLocalPlayer) return;

            playerRb.velocity = velocity;
            gasStamina = clinetGasStamina;
    }
    [Client]
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
        CmdPlayParticlesOnServer(amount);
    }
    [Command]
    private void CmdPlayParticlesOnServer(int amount)
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
        RpcPlayParticlesOnClients(amount);
    }
    [ClientRpc]
    private void RpcPlayParticlesOnClients(int amount)
    {
        if (isLocalPlayer) return;
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
    [Client]
    private void CheckParticles()
    {
        if (deleteParticles.Count > 0)
        {
            try
            {
                foreach (var particle in deleteParticles)
                {
                    if (!particle.IsAlive())
                    {
                        deleteParticles.Remove(particle);
                        Destroy(particle.gameObject);
                    }
                }
            }
            catch (System.InvalidOperationException)
            {

            }
            CmdCheckParticlesOnServer();
        }
    }
    [Command]
    private void CmdCheckParticlesOnServer()
    {
        if (deleteParticles.Count > 0)
        {
            try
            {
                foreach (var particle in deleteParticles)
                {
                    if (!particle.IsAlive())
                    {
                        deleteParticles.Remove(particle);
                        Destroy(particle.gameObject);
                    }
                }
            }
            catch (System.InvalidOperationException)
            {

            }
            RpcCheckParticlesOnClients();
        }
    }
    [ClientRpc]
    private void RpcCheckParticlesOnClients()
    {
        if (isLocalPlayer) return;
        if (deleteParticles.Count > 0)
        {
            try
            {
                foreach (var particle in deleteParticles)
                {
                    if (!particle.IsAlive())
                    {
                        deleteParticles.Remove(particle);
                        Destroy(particle.gameObject);
                    }
                }
            }
            catch(System.InvalidOperationException)
            {

            }
        }
    }
    [Client]
    private void CheckHookState()
    {
        hookRay.origin = transform.position;
        hookRay.direction = crossHair.transform.position - transform.position;
        movement.isGrapleShoot = hookIsShooted;
    }
    [Client]
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
    [Client]
    public void DisattachHook()
    {
        hookFired = false;
        try
        {
            Destroy(targetPoint);
            //grapleJoint.connectedBody.gameObject.GetComponent<NetworkTransform>().enabled = true;
        }
        catch (System.NullReferenceException)
        {

        }
        grapleJoint.connectedBody = grapleRb;
        grapleJoint.enabled = false;
        hookIsConnected = false;

        playerRb.mass = 25f;
        playerRb.rotation = 0;
        playerRb.freezeRotation = true;
        movement.isHooked = false;

        graple.GetComponent<BoxCollider2D>().enabled = true;

        playerJoint.connectedBody = playerRb;
        playerJoint.enabled = false;

        CmdUpdateHookDisattachmentOnServer(hookFired, grapleJoint.enabled, playerJoint.distance, playerRb.mass);//, true);
    }
    [Client]
    public void AttachGraple(Vector2 collision)
    {
        hookFired = false;
        grapleRb.velocity = Vector2.zero;
        grapleJoint.enabled = true;
        //collision.gameObject.GetComponent<NetworkTransform>().enabled = false;
        grapleJoint.target = collision;//.gameObject.GetComponent<Rigidbody2D>();

        playerRb.mass = 5f;
        playerRb.freezeRotation = true;
        movement.isHooked = true;
        movement.isFlying = true;

        graple.GetComponent<BoxCollider2D>().enabled = false;

        playerJoint.connectedBody = grapleRb;
        playerJoint.distance = Vector2.Lerp(transform.position, graple.transform.position - transform.position, 1).magnitude;
        playerJoint.enabled = true;

        CmdUpdateHookAttachmentOnServer(collision, hookFired, grapleJoint.enabled, playerJoint.distance, playerRb.mass);//, collision)//.gameObject.GetComponent<NetworkTransform>().enabled);
    }
    [Client]
    public void UpdateTargetPoint()
    {
        if (targetPoint != null)
        {
            grapleJoint.target = targetPoint.transform.position;
            CmdUpdateTargetPointOnServer(targetPoint.transform.position);
        }
    }
    [Command]
    private void CmdUpdateTargetPointOnServer(Vector3 position) 
    {
        if (targetPoint != null)
        {
            grapleJoint.target = position;
        }
        RpcUpdateTargetPositionOnClients(position);
    }
    [Command]
    private void  CmdUpdateHookAttachmentOnServer(Vector2 connectedBody, bool hookFiredState, bool hookState, float playerJointDistance, float playerMass)//, bool transformState)
    {
        hookFired = hookFiredState;
        //connectedBody.gameObject.GetComponent<NetworkTransform>().enabled = transformState;
        grapleJoint.target = connectedBody;//.GetComponent<Rigidbody2D>();
        grapleJoint.enabled = hookState;

        playerRb.mass = playerMass;
        movement.isHooked = hookState;
        movement.isFlying = hookState;

        //playerJoint.connectedBody = grapleRb;
        playerJoint.distance = playerJointDistance;
        playerJoint.enabled = hookState;

        RpcUpdateHookAtachmentOnClinets(connectedBody, hookFiredState, hookState, playerJointDistance, playerMass);//, transformState);
    }
    [Command]
    private void CmdUpdateHookDisattachmentOnServer(bool hookFiredState, bool hookState, float playerJointDistance, float playerMass)//, bool transformState)
    {
        hookFired = hookFiredState;
        try
        {
            //grapleJoint.connectedBody.gameObject.GetComponent<NetworkTransform>().enabled = transformState;
        }
        catch (System.NullReferenceException)
        {

        }
        grapleJoint.enabled = hookState;

        playerRb.mass = playerMass;
        movement.isHooked = hookState;

        playerJoint.distance = playerJointDistance;
        playerJoint.enabled = hookState;

        RpcUpdateHookDisatachmentOnClinets(hookFiredState, hookState, playerJointDistance, playerMass);//, transformState);
    }
    [ClientRpc]
    private void RpcUpdateTargetPositionOnClients(Vector2 position)
    {
        if (isLocalPlayer) return;
        if (targetPoint != null)
        {
            grapleJoint.target = position;
        }
    }
    [ClientRpc]
    private void RpcUpdateHookAtachmentOnClinets(Vector2 connectedBody, bool hookFiredState, bool hookState, float playerJointDistance, float playerMass)//, bool transformState)
    {
        if (isLocalPlayer) return;
        hookFired = hookFiredState;
        //connectedBody.gameObject.GetComponent<NetworkTransform>().enabled = transformState;
        grapleJoint.target = connectedBody;//.GetComponent<Rigidbody2D>();
        grapleJoint.enabled = hookState;

        playerRb.mass = playerMass;
        movement.isHooked = hookState;
        movement.isFlying = hookState;

        playerJoint.connectedBody = grapleRb;
        playerJoint.distance = playerJointDistance;
        playerJoint.enabled = hookState;
    }
    [ClientRpc]
    private void RpcUpdateHookDisatachmentOnClinets(bool hookFiredState, bool hookState, float playerJointDistance, float playerMass)//, bool transformState)
    {
        if (isLocalPlayer) return;
        hookFired = hookFiredState;
        try
        {
            //grapleJoint.connectedBody.gameObject.GetComponent<NetworkTransform>().enabled = transformState;
        }
        catch (System.NullReferenceException)
        {

        }
        grapleJoint.enabled = hookState; 

        playerRb.mass = playerMass;
        movement.isHooked = hookState;
        movement.isFlying = hookState;

        playerJoint.distance = playerJointDistance;
        playerJoint.enabled = hookState;
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



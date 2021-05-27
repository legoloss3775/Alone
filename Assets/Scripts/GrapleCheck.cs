using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapleCheck : MonoBehaviour
{
    public HookSystemV2 hookSystem;
    public GameObject pointPrefab;

    private GameObject player;
    private void Awake()
    {
        player = transform.parent.gameObject;
        hookSystem = player.GetComponent<HookSystemV2>();

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("HookObject"))
        {
            if (Input.GetKey(KeyCode.Space))
            {
                hookSystem.targetPoint = Instantiate(pointPrefab, hookSystem.graple.transform.position, hookSystem.graple.transform.rotation, collision.gameObject.transform);
                hookSystem.AttachGraple(hookSystem.graple.transform.position);//.GetComponent<Rigidbody2D>());
                hookSystem.hookIsConnected = true;
            }
        }
    }
}

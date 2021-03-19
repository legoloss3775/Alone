using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapleCheck : MonoBehaviour
{
    public HookSystem hookSystem;

    private GameObject player;
    private void Awake()
    {
        player = GameObject.Find("Player");
        hookSystem = player.GetComponent<HookSystem>();

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("HookObject"))
        {
            hookSystem.AttachGraple(collision.GetComponent<Rigidbody2D>());
            hookSystem.hookIsConnected = true;
        }
    }
}

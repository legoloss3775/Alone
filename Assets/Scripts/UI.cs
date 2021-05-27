using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public HookSystemV2 hookSystem;
    public PlayerMovement playerMovement;

    public Slider staminaBar;

    private GameObject player;

    private void Awake()
    {
        player = transform.parent.gameObject;

        hookSystem = player.GetComponent<HookSystemV2>();
        playerMovement = player.GetComponent<PlayerMovement>();

        SetUI();
    }

    private void Update()
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        staminaBar.value = hookSystem.gasStamina;

        staminaBar.transform.position = player.transform.position + new Vector3(0, 1);

        if (playerMovement.isHooked)
            staminaBar.gameObject.SetActive(true);
        else if (hookSystem.gasStamina < 3f)
            staminaBar.gameObject.SetActive(true);
        else
            staminaBar.gameObject.SetActive(false);
    }
    private void SetUI()
    {
        staminaBar.minValue = 0;
        staminaBar.maxValue = 3f;
    }
}

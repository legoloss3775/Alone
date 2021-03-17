using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public HookSystem hookSystem;
    public PlayerMovement playerMovement;

    public Slider staminaBar;

    private GameObject player;

    private void Awake()
    {
        player = GameObject.Find("Player");

        hookSystem = player.GetComponent<HookSystem>();
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
    }
    private void SetUI()
    {
        staminaBar.minValue = 0;
        staminaBar.maxValue = 3f;
    }
}

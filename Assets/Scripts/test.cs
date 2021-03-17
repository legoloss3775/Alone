using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public float speed;
    void Update()
    {
        GetComponent<Rigidbody2D>().rotation +=  0.1f;
    }
}

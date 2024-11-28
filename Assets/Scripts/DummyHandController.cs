using System;
using UnityEngine;

public class DummyHandController : MonoBehaviour
{
    public GameObject hand;
    public float speed = 1f;
    float speedDamper;
    public string upKey = "UpArrow";
    public string downKey = "DownArrow";
    public string leftKey = "LeftArrow";
    public string rightKey = "RightArrow";

    KeyCode up;
    KeyCode down;
    KeyCode left;
    KeyCode right;

    void Start()
    {
        speedDamper = speed / 100f;
        SetKeys();
    }

    void FixedUpdate()
    {
        if (Input.GetKey(up))
        {
            hand.transform.position += speedDamper * transform.up;
        }
        if (Input.GetKey(down))
        {
            hand.transform.position += -1f * speedDamper * transform.up;
        }
        if (Input.GetKey(left))
        {
            hand.transform.position += -1f * speedDamper * transform.right;
        }
        if (Input.GetKey(right))
        {
            hand.transform.position += speedDamper * transform.right;
        }
    }

    void SetKeys()
    {
        up = (KeyCode)Enum.Parse(typeof(KeyCode), upKey);
        down = (KeyCode)Enum.Parse(typeof(KeyCode), downKey);
        left = (KeyCode)Enum.Parse(typeof(KeyCode), leftKey);
        right = (KeyCode)Enum.Parse(typeof(KeyCode), rightKey);
    }

    void OnValidate()
    {
        speedDamper = speed / 100f;
    }
}

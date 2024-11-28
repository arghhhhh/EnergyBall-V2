using System;
using UnityEngine;

public class DummySceneControl : MonoBehaviour
{
    private PlayerConstructor player;
    public string bothOpenKey = "U";
    public string bothClosedKey = "I";
    public string leftOpenKey = "O";
    public string rightOpenKey = "P";

    KeyCode bothOpen;
    KeyCode bothClosed;
    KeyCode leftOpen;
    KeyCode rightOpen;

    void Start()
    {
        player = GetComponent<PlayerConstructor>();
        SetKeys();
    }

    void FixedUpdate()
    {
        if (Input.GetKey(bothOpen))
        {
            player.leftHandState = Windows.Kinect.HandState.Open;
            player.rightHandState = Windows.Kinect.HandState.Open;
        }
        if (Input.GetKey(bothClosed))
        {
            player.leftHandState = Windows.Kinect.HandState.Closed;
            player.rightHandState = Windows.Kinect.HandState.Closed;
        }
        if (Input.GetKey(leftOpen))
        {
            player.leftHandState = Windows.Kinect.HandState.Open;
            player.rightHandState = Windows.Kinect.HandState.Closed;
        }
        if (Input.GetKey(rightOpen))
        {
            player.leftHandState = Windows.Kinect.HandState.Closed;
            player.rightHandState = Windows.Kinect.HandState.Open;
        }
    }

    void SetKeys()
    {
        bothOpen = (KeyCode)Enum.Parse(typeof(KeyCode), bothOpenKey);
        bothClosed = (KeyCode)Enum.Parse(typeof(KeyCode), bothClosedKey);
        leftOpen = (KeyCode)Enum.Parse(typeof(KeyCode), leftOpenKey);
        rightOpen = (KeyCode)Enum.Parse(typeof(KeyCode), rightOpenKey);
    }
}

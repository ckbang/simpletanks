using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Tank : MonoBehaviour
{
    public enum TeamType
    {
        Red,
        Blue
    }

    public enum StateType
    {
        Aiming,
        SendShootingQuery,
        Ready,
        Shooting,
    }

    public StateType State { get; private set; }

    public GameObject BulletPrefab;
    public TextMeshPro HUDText;
    public List<MeshRenderer> ColorMeshRenderers = new List<MeshRenderer>();
    public Transform CannonTransform;
    public Transform CannonFireTransform;
    public float RotaionSpeed = 30;
    public float PowerSpeed = 60;

    public static Vector2 LimitPitch = new Vector2(0, 50);
    public static Vector2 LimitYaw = new Vector2(-90, 90);
    public static Vector2 LimitPower = new Vector2(1000, 3000);
    
    public float Pitch;
    public float Yaw;
    public float Power;

    public bool Controllable { get; private set; }
    public int NetId { get; private set; }
    public TeamType Team { get; private set; }
    public string PlayerName { get; private set; }

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!Controllable)
        {
            CannonTransform.localEulerAngles = new Vector3(Pitch, Yaw, 0);
            return;
        }

        SimpleGameManager.Instance.UIStatus.text = $"Pitch: {Pitch}\nYaw: {Yaw}\nPower: {Power}";

        if (State == StateType.Aiming)
        {
            SimpleGameManager.Instance.UIStatus.text += "\n\nAiming";

            float powerSpeed = PowerSpeed * Time.deltaTime;
            float rotationSpeed = RotaionSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.UpArrow))
            {
                Pitch -= rotationSpeed;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                Pitch += rotationSpeed;
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                Yaw -= rotationSpeed;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                Yaw += rotationSpeed;
            }

            if (Input.GetKey(KeyCode.PageUp))
            {
                Power += powerSpeed;
            }
            else if (Input.GetKey(KeyCode.PageDown))
            {
                Power -= powerSpeed;
            }

            Pitch = clampAngle(Pitch, -LimitPitch.y, LimitPitch.x); // swap min / max
            Yaw = clampAngle(Yaw, LimitYaw.x, LimitYaw.y);
            Power = Mathf.Clamp(Power, LimitPower.x, LimitPower.y);
        }

        CannonTransform.localEulerAngles = new Vector3(Pitch, Yaw, 0);

        if (State == StateType.SendShootingQuery)
        {
            SimpleGameManager.Instance.UIStatus.text += "\n\nSend shooting event";
            return;
        }

        if (State == StateType.Ready)
        {
            SimpleGameManager.Instance.UIStatus.text += "\n\nWaiting other player's shooting";
            return;
        }

        if (State == StateType.Shooting)
        {
            SimpleGameManager.Instance.UIStatus.text += "\n\nShooting";
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetState(StateType.SendShootingQuery);

            Hashtable h = new Hashtable();
            h.Add("netId", NetId);
            h.Add("controls", new Vector3(Pitch, Yaw, Power));
            PhotonNetwork.RaiseEvent(NetworkEventCodes.ShootingReady, h, new RaiseEventOptions() { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

    }

    public void SetState(StateType state)
    {
        State = state;
    }

    public void Init(int netId, string playerName, TeamType team, bool controllable)
    {
        NetId = netId;
        Team = team;
        PlayerName = playerName;
        HUDText.text = $"{PlayerName}:[{NetId}]";

        var color = Team == TeamType.Blue ? Color.blue : Color.red;
        foreach (var r in ColorMeshRenderers)
        {
            r.material.color = color;
        }

        Controllable = controllable;
        Pitch = 0;
        Yaw = 0;
        Power = 1000;
    }

    public void Shooting()
    {
        SetState(StateType.Shooting);

        var instance = Instantiate(BulletPrefab);
        instance.transform.position = CannonFireTransform.position;
        instance.transform.eulerAngles = Vector3.zero;

        var bullet = instance.GetComponent<Bullet>();
        bullet.Init(this);

        var rb = instance.GetComponent<Rigidbody>();
        float weight = rb.mass;
        var speed = Power / weight;

        rb.AddForce(CannonTransform.forward * speed);

        SimpleGameManager.Instance.Bullets.Add(bullet);
    }

    float clampAngle(float angle, float min, float max)
    {
        angle = normalizeAngle(angle);
        if (angle > 180)
        {
            angle -= 360;
        }
        else if (angle < -180)
        {
            angle += 360;
        }

        min = normalizeAngle(min);
        if (min > 180)
        {
            min -= 360;
        }
        else if (min < -180)
        {
            min += 360;
        }

        max = normalizeAngle(max);
        if (max > 180)
        {
            max -= 360;
        }
        else if (max < -180)
        {
            max += 360;
        }

        // Aim is, convert angles to -180 until 180.
        return Mathf.Clamp(angle, min, max);
    }

    float normalizeAngle(float angle)
    {
        while (angle > 360)
            angle -= 360;
        while (angle < 0)
            angle += 360;
        return angle;
    }
}

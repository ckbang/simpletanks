using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityTemplateProjects;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SimpleGameManager : MonoBehaviour
{
    public static SimpleGameManager Instance
    {
        get; private set;
    }

    public enum GameStateType
    {
        Idle,
        Connecting,
        WaitingOtherPlayer,
        GameInit,
        GameStarted,
        Aiming,
        Shooting,
        Result,
        GameOver,
    }

    public GameStateType State { get; private set; }

    List<Rect> blueRegionInfos = new List<Rect>()
    {
        new Rect( 10, 30, 40, 30),
        new Rect( 70, 30, 40, 30),
        new Rect(130, 30, 40, 30),
        new Rect(190, 30, 40, 30),
        new Rect(250, 30, 40, 30),
    };

    List<Rect> redRegionInfos = new List<Rect>()
    {
        new Rect( 10, 240, 40, 30),
        new Rect( 70, 240, 40, 30),
        new Rect(130, 240, 40, 30),
        new Rect(190, 240, 40, 30),
        new Rect(250, 240, 40, 30),
    };

    public GameObject TankPrefab;
    public GameObject UIBG;
    public GameObject UIInputName;
    public GameObject UIConnectingMessage;
    public GameObject UIWaitingOtherPlayerMessage;
    public GameObject UIGameInitMessage;
    public GameObject UIGameOverMessage;
    public TextMeshProUGUI UIStatus;
    public SimpleCameraController CameraController;

    public int GameInitCompleteCount { get; private set; }
    public string GameVersion = "1";
    public int MaxRoomPlayers = 2;

    public List<Tank> Tanks = new List<Tank>();
    public List<Bullet> Bullets = new List<Bullet>();

    float resultSnapshot;
    float resultDurationSec = 3;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        UIStatus.text = "";
        UIBG.SetActive(true);
        UIInputName.SetActive(true);
        UIConnectingMessage.SetActive(false);
        UIWaitingOtherPlayerMessage.SetActive(false);
        UIGameInitMessage.SetActive(false);
        UIGameOverMessage.SetActive(false);
    }

    private void Update()
    {
        if (State == GameStateType.WaitingOtherPlayer)
        {
            if (PhotonNetwork.PlayerList.Length == MaxRoomPlayers)
            {
                SetState(GameStateType.GameInit);
                return;
            }
        }

        if (State >= GameStateType.GameStarted)
        {
            if (PhotonNetwork.PlayerList.Length != MaxRoomPlayers)
            {
                PhotonNetwork.Disconnect();
                SetState(GameStateType.GameOver);
                return;
            }
        }

        if (State == GameStateType.GameStarted)
        {
            SetState(GameStateType.Aiming);
            return;
        }

        if (State == GameStateType.Aiming)
        {
            bool allReady = true;
            foreach (var tank in Tanks)
            {
                if (tank.State != Tank.StateType.Ready)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                SetState(GameStateType.Shooting);
                return;
            }
        }

        if (State == GameStateType.Shooting)
        {
            if (Bullets.Count == 0)
            {
                SetState(GameStateType.Result);
                return;
            }
        }

        if (State == GameStateType.Result)
        {
            var elapsed = Time.realtimeSinceStartup - resultSnapshot;
            if (resultDurationSec < elapsed)
            {
                SetState(GameStateType.Aiming);
                return;
            }
        }
    }

    public void SetState(GameStateType state)
    {
        State = state;

        if (State == GameStateType.Connecting)
        {
            UIInputName.SetActive(false);
            UIConnectingMessage.SetActive(true);
        }
        else if (State == GameStateType.WaitingOtherPlayer)
        {
            UIInputName.SetActive(false);
            UIConnectingMessage.SetActive(false);
            UIWaitingOtherPlayerMessage.SetActive(true);
        }
        else if (State == GameStateType.GameInit)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GameInitCompleteCount = 0;
                Hashtable h = new Hashtable();
                h.Add("blueSpawnPosition", getRandomSpawnPosition(blueRegionInfos));
                h.Add("redSpawnPosition", getRandomSpawnPosition(redRegionInfos));
                PhotonNetwork.RaiseEvent(NetworkEventCodes.GameInit, h, new RaiseEventOptions() { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
            }

            UIInputName.SetActive(false);
            UIConnectingMessage.SetActive(false);
            UIWaitingOtherPlayerMessage.SetActive(false);
            UIGameInitMessage.SetActive(true);
        }
        else if (State == GameStateType.GameStarted)
        {
            UIBG.SetActive(false);
            UIInputName.SetActive(false);
            UIConnectingMessage.SetActive(false);
            UIWaitingOtherPlayerMessage.SetActive(false);
            UIGameInitMessage.SetActive(false);
            UIGameOverMessage.SetActive(false);
        }
        else if (State == GameStateType.Aiming)
        {
            foreach (var tank in Tanks)
            {
                tank.SetState(Tank.StateType.Aiming);
            }
        }
        else if (State == GameStateType.Shooting)
        {
            foreach (var tank in Tanks)
            {
                tank.Shooting();
            }
        }
        else if (State == GameStateType.Result)
        {
            resultSnapshot = Time.realtimeSinceStartup;
        }
        else if (State == GameStateType.GameOver)
        {
            UIGameOverMessage.SetActive(true);
        }
    }

    public void Connect()
    {
        SetState(GameStateType.Connecting);

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.GameVersion = this.GameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void Init(Vector3 blueSpawnPosition, Vector3 redSpawnPosition)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            var controllable = player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
            var team = player.IsMasterClient ? Tank.TeamType.Blue : Tank.TeamType.Red;
            var spawnPosition = team == Tank.TeamType.Blue ? blueSpawnPosition : redSpawnPosition;
            var tank = createTank(player.ActorNumber, player.NickName, spawnPosition, team, controllable);

            if (controllable)
            {
                var cam = Camera.main;
                var distance = tank.transform.forward * -30;
                distance += new Vector3(0, 30, 0);
                cam.transform.position = tank.transform.position + distance;
                cam.transform.LookAt(tank.transform);
                var euler = cam.transform.eulerAngles;

                CameraController.ResetTransform(cam.transform.position, euler);
            }
        }

        PhotonNetwork.RaiseEvent(NetworkEventCodes.GameInitComplete, null, new RaiseEventOptions() { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
    }

    public void InitCompleteCheck()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        GameInitCompleteCount++;
        if (GameInitCompleteCount == MaxRoomPlayers)
        {
            PhotonNetwork.RaiseEvent(NetworkEventCodes.GameStart, null, new RaiseEventOptions() { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
        }
    }

    Tank createTank(int netId, string playerName, Vector3 spawnPosition, Tank.TeamType team, bool controllable)
    {
        var instance = Instantiate(TankPrefab, spawnPosition, Quaternion.identity);
        var tank = instance.GetComponent<Tank>();
        tank.Init(netId, playerName, team, controllable);
        float yawByTeam = team == Tank.TeamType.Blue ? 0 : 180;
        tank.transform.rotation = Quaternion.Euler(new Vector3(0, yawByTeam, 0));

        Tanks.Add(tank);

        return tank;
    }

    Vector3 getRandomSpawnPosition(List<Rect> regionInfos)
    {
        var regionInfo = regionInfos[Random.Range(0, regionInfos.Count)];
        Vector3 spawnPosition = new Vector3(
            Random.Range(regionInfo.x, regionInfo.x + regionInfo.width),
            0,
            Random.Range(regionInfo.y, regionInfo.y + regionInfo.height));

        return spawnPosition;
    }
}

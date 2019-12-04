using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkEventCodes
{
    public const byte GameInit = 0;
    public const byte GameInitComplete = 1;
    public const byte GameStart = 2;
    public const byte ShootingReady = 3;
    public const byte MaxCodes = 200; // can't use this (0 ~ 199)
}

public class SimpleNetworkCallbacks : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = (byte)SimpleGameManager.Instance.MaxRoomPlayers });
    }

    public override void OnJoinedRoom()
    {
        SimpleGameManager.Instance.SetState(SimpleGameManager.GameStateType.WaitingOtherPlayer);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetworkEventCodes.GameInit:
                {
                    Hashtable h = photonEvent.CustomData as Hashtable;
                    Vector3 blueSpawnPosition = (Vector3)h["blueSpawnPosition"];
                    Vector3 redSpawnPosition = (Vector3)h["redSpawnPosition"];
                    SimpleGameManager.Instance.Init(blueSpawnPosition, redSpawnPosition);
                }
                break;

            case NetworkEventCodes.GameInitComplete:
                {
                    SimpleGameManager.Instance.InitCompleteCheck();
                }
                break;

            case NetworkEventCodes.GameStart:
                {
                    SimpleGameManager.Instance.SetState(SimpleGameManager.GameStateType.GameStarted);
                }
                break;

            case NetworkEventCodes.ShootingReady:
                {
                    Hashtable h = photonEvent.CustomData as Hashtable;
                    int netId = (int)h["netId"];
                    Vector3 controls = (Vector3)h["controls"];
                    foreach (var tank in SimpleGameManager.Instance.Tanks)
                    {
                        if (tank.NetId == netId)
                        {
                            tank.Pitch = controls.x;
                            tank.Yaw = controls.y;
                            tank.Power = controls.z;
                            tank.SetState(Tank.StateType.Ready);
                            break;
                        }
                    }
                }
                break;
        }
    }
}

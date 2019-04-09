using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetManager : NetworkManager {

	bool m_isReady = false;
	bool b_isSpawned = false;


	public class DataMsgType
	{
		public static short READY_MSG = MsgType.Highest + 1;
	};

	public class ReadyMessage : MessageBase
	{
		public bool isReady = false;
	};

	public override void OnStartServer ()
	{
		base.OnStartServer ();
		NetworkServer.RegisterHandler (DataMsgType.READY_MSG, ServerIsReady);
	}


	public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId)
	{
		GameObject player;
		Transform startPos = GetStartPosition ();

		player = (GameObject)Object.Instantiate (this.playerPrefab, startPos.position, startPos.rotation);
		NetworkServer.AddPlayerForConnection (conn, player, playerControllerId);
	}

	public override void ServerChangeScene (string newSceneName)
	{
		m_isReady = false;

		base.ServerChangeScene (newSceneName);

		ReadyMessage readyMsg = new ReadyMessage ();
		readyMsg.isReady = false;
		NetworkServer.SendToAll (DataMsgType.READY_MSG, readyMsg);
	}


	public override void OnServerSceneChanged (string sceneName)
	{
		base.OnServerSceneChanged (sceneName);

		Debug.Log ("ServerSceneChanged");

		ReadyMessage readyMsg = new ReadyMessage ();
		readyMsg.isReady = true;
		m_isReady = true;
		NetworkServer.SendToAll (DataMsgType.READY_MSG, readyMsg);
	}

	public void ClientAddPlayer(NetworkMessage netMsg)
	{
		Debug.Log ("Client Add player");

		bool isReady = netMsg.ReadMessage<ReadyMessage> ().isReady;

		if (isReady && !b_isSpawned) 
		{

			Debug.Log ("Client spawned");

			ClientScene.AddPlayer (client.connection, 0);
			b_isSpawned = true;
		}

		if (!isReady) 
		{
			Debug.Log ("Client is not spawned");
			b_isSpawned = false;
		} 

		if(isReady && b_isSpawned)
		{
			Debug.Log (b_isSpawned);
		}
	}

	public void ServerIsReady(NetworkMessage netMsg)
	{
		Debug.Log ("Server is ready");
		ReadyMessage readyMsg = new ReadyMessage ();
		readyMsg.isReady = m_isReady;
		NetworkServer.SendToAll (DataMsgType.READY_MSG, readyMsg);
	}

	public override void OnClientConnect (NetworkConnection conn)
	{
		base.OnClientConnect (conn);

		if (client != null)
			if (client.isConnected) 
			{
			Debug.Log ("Client connected");
				b_isSpawned = false;
				client.RegisterHandler (DataMsgType.READY_MSG, ClientAddPlayer);


				client.Send (DataMsgType.READY_MSG, new ReadyMessage ());
			}
	}

	public override void OnClientDisconnect (NetworkConnection conn)
	{
		base.OnClientDisconnect (conn);

		Debug.Log ("Client Disconnected!");
	}

	public override void OnClientError (NetworkConnection conn, int errorCode)
	{
		base.OnClientError (conn, errorCode);

		Debug.Log ("Client error:" + errorCode);
	}

	/*public override void OnClientSceneChanged (NetworkConnection conn)
	{
		ClientScene.AddPlayer (conn, 0);
	}*/
}

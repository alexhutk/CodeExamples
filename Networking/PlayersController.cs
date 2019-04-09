using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AI;

public class PlayersController : NetworkBehaviour {

	public int playersToPlay = 3;
	public GameObject[] spawnPoints;
	public GameObject playerModel;
	public bool isSinglePlayer = false;

	[HideInInspector]
	public bool isMatchPrepared = false;

	System.Random rnd;
	List<GameObject> players = new List<GameObject>();
	int maxPlayers;
	int currentPlayerNumber;
	List<int> playerNumbers = new List<int>();


	void Awake()
	{
		rnd = new System.Random ();

		if (NetworkManager.singleton != null)
			maxPlayers = (int) NetworkManager.singleton.matchSize;

		FormPlayerNumberArray ();


		Invoke ("CheckForActivePlayer", 1f);
	}


	/// <summary> 
	/// When next player is connected add it to player list and set it invulnearable before match starts.
	/// </summary>
	[ServerCallback]
	public void AddNewPlayer(GameObject player)
	{
		players.Add (player);
		player.GetComponent<PlayerHealth> ().SetIsDead (true);

		if (isMatchPrepared)
			player.GetComponent<PlayerSetup> ().RpcHideMatchStartUI ();
		else			
			player.GetComponent<PlayerSetup> ().RpcShowMatchStartUI ();

		StartCoroutine(SyncPlayerData());

		CheckForReady ();
	}

	/// <summary> 
	/// Sync player's skin and accessories between previous players and new one.
	/// </summary>
	[ServerCallback]
	IEnumerator SyncPlayerData()
	{
		yield return new WaitForSeconds (0.5f);

		for (int i = 0; i < (players.Count - 1); i++) 
		{
			if(players[i].GetComponent<PlayerSetup>())
				players[i].GetComponent<PlayerSetup> ().RpcSetPlayerGameOject (playerNumbers[i]);

			if (players [i].GetComponentInChildren<Accesoire> ()) 
			{
				players [i].GetComponent<PlayerSetup> ().RpcSyncPlayerData (players [i].name, players [i].GetComponentInChildren<Accesoire> ().GetEquipped ());
			}
		}

		players[players.Count-1].GetComponent<PlayerSetup> ().RpcSetPlayerGameOject (playerNumbers[players.Count-1]);
	}

	/// <summary> 
	/// Add player in sinle player campaign.
	/// </summary>
	[ServerCallback]
	public void AddNewPlayerSingle(GameObject player)
	{
		players.Add (player);

		player.GetComponent<PlayerSetup> ().RpcSetPlayerGameOject (0);
	}


	/// <summary> 
	/// Remove player from player list. If there is only one playe rin playerList then match is over and we restart match.
	/// </summary>
	[ServerCallback]
	public void RemovePlayer(GameObject player)
	{
		if (players.Contains (player)) 
		{
			players.Remove (player);

			if ((players.Count == 1) && isMatchPrepared) 
			{
				if (players [0] != null) 
				{
					players [0].GetComponent<PlayerHealth> ().RpcGameOver (1);
					Invoke ("RestartMatch", 5.5f);
				}
			}
		}
	}

	/// <summary> 
	/// Remove player from player list. If there is only one playe rin playerList then match is over and we restart match.
	/// </summary>
	[ServerCallback]
	public void RemovePlayer(PlayerSetup player)
	{
		if (players.Contains (player.gameObject)) 
		{
			players.Remove (player.gameObject);

			if ((players.Count == 1) && isMatchPrepared) 
			{
				if (players [0] != null) 
				{
					players [0].GetComponent<PlayerHealth> ().RpcGameOver (1);
					Invoke ("RestartMatch", 5.5f);
				}
			}
		}
	}

	/// <summary> 
	/// Forms rank of players at the end of the round. After some pause restart match
	/// </summary>
	[ServerCallback]
	public void EndGame()
	{
		GameObject buff;

		for (int i = 0; i < players.Count; i++)
			for (int j = 0; j < players.Count; j++) 
			{
				if (players [i].GetComponent<PlayerStats> ().score > players [j].GetComponent<PlayerStats> ().score) 
				{
					buff = players [i];
					players [i] = players [j];
					players [j] = buff;
				}
			}

		for (int i = 0; i < players.Count; i++) 
		{
			players [i].GetComponent<PlayerHealth> ().RpcGameOver (i+1);
		}

		Invoke ("RestartMatch", 5.5f);
	}

	/// <summary> 
	/// Forms rank of players at the end of the round. After some pause restart match
	/// </summary>
	void RestartMatch()
	{
		List<GameObject> activePlayers = new List<GameObject> (GameObject.FindGameObjectsWithTag("Player"));

		if (activePlayers.Count < playersToPlay)  
		{
			if (NetworkManager.singleton.matchMaker != null)
				NetworkManager.singleton.matchMaker.SetMatchAttributes (NetworkManager.singleton.matchInfo.networkId, true, 0, NetworkManager.singleton.OnSetMatchAttributes);
		}

		NetworkManager.singleton.ServerChangeScene (NetworkManager.singleton.onlineScene);
	}

	[ServerCallback]
	public int GiveMyPlace()
	{
		return players.Count;
	}

	[ServerCallback]
	public void SetIsReady(bool _isReady)
	{
		isMatchPrepared = _isReady;
	}

	[ServerCallback]
	public void CheckForReady()
	{
		if (players.Count == NetworkManager.singleton.matchSize) 
		{
			isMatchPrepared = true;
			CancelInvoke ();
			MatchStart ();
		} 
		else if ((players.Count >= playersToPlay) && (isMatchPrepared)) 
		{
			CancelInvoke ();
			MatchStart ();
		}
	}

	[ServerCallback]
	public void MatchStart()
	{
		if (isSinglePlayer)
			return;

		foreach (GameObject player in players) 
		{
			player.GetComponent<PlayerHealth> ().SetIsDead (false);
			player.GetComponent<PlayerSetup> ().RpcStartMatchTime ();
			player.GetComponent<PlayerSetup> ().RpcHideMatchStartUI ();
			SpawnPlayer (player.transform);
		}

		foreach (SpawnLUT spawn in GetComponents<SpawnLUT>()) 
		{
			spawn.enabled = true;
		}


		if(NetworkManager.singleton.matchMaker)
			NetworkManager.singleton.matchMaker.SetMatchAttributes (NetworkManager.singleton.matchInfo.networkId, false, 0, NetworkManager.singleton.OnSetMatchAttributes);
	}


	[ServerCallback]
	void CheckForActivePlayer()
	{
		List<GameObject> activePlayers = new List<GameObject> (GameObject.FindGameObjectsWithTag("Player"));

		if (activePlayers.Count >= playersToPlay) 
		{
			isMatchPrepared = true;
			MatchStart ();
		} 

	}

	[ServerCallback]
	public void UnlistMatch ()
	{
		if(!isSinglePlayer)
			NetworkManager.singleton.matchMaker.SetMatchAttributes (NetworkManager.singleton.matchInfo.networkId, false, 0, NetworkManager.singleton.OnSetMatchAttributes);
	}


	[ServerCallback]
	void SpawnPlayer(Transform player)
	{
		while (true) 
		{
			if ((players.Count > spawnPoints.Length) || (spawnPoints.Length == 0))
				return;

			int spawnNumber = rnd.Next (0, spawnPoints.Length);

			if (spawnPoints [spawnNumber].activeSelf) 
			{
				player.GetComponent<PlayerMovement>().RpcSetOnPosition(spawnPoints [spawnNumber].transform.position);
				spawnPoints [spawnNumber].SetActive (false);
				return;
			}
		}
	}


	[ServerCallback]
	void FormPlayerNumberArray()
	{
		int playerNumber = rnd.Next (0, maxPlayers);

		for (int i = 0; i < maxPlayers; i++) 
		{
			while (true) 
			{
				playerNumber = 0;

				bool isExist = false;

				if (players.Count > maxPlayers)
					break;

				playerNumber = rnd.Next (0, maxPlayers);

				foreach (int number in playerNumbers) {
					if (playerNumber == number)
						isExist = true;
				}

				if (!isExist) 
				{
					playerNumbers.Add (playerNumber);
					break;
				}
			}
		}
	}

	[ServerCallback]
	int SetPlayerGameObject()
	{
		int playerNumber = 0;

		playerNumber = rnd.Next (0, maxPlayers);

		while (true) 
		{
			bool isExist = false;

			if (players.Count > maxPlayers)
				break;

			playerNumber = rnd.Next (0, maxPlayers);

			foreach (int number in playerNumbers) 
			{
				if (playerNumber == number)
					isExist = true;
			}

			if(!isExist)
			{
				playerNumbers.Add(playerNumber);
				break;
			}
		}

		return playerNumber;
	}

}

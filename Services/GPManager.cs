using UnityEngine;
using UnityEngine.UI;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using System.Collections;
using System.Globalization;

public class GPManager : MonoBehaviour {

    public static GPManager singleton;

    bool isSaving = false;
    Canvases canvases;
    TimeSpan timeSpan;
    DateTime currentDateTime;

    Text debugText;

    // Use this for initialization
    void Awake ()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else if(singleton != this)
        {
            Destroy(gameObject);
			return;
        }

        canvases = FindObjectOfType<Canvases>();

        BeginAuthorize();
	}

	/// <summary>
	/// Initialize Google Play Services and authentificate user. After authentification load saved data from cloud.
	/// </summary>
    void BeginAuthorize()
    {

        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder().Build();
        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = false;
        PlayGamesPlatform.Activate();

        Social.localUser.Authenticate((bool succed) => {

            if (succed)
            {
                CloudSaves(false, false, GameManager.singleton.playerStats);
                CloudSaves(false, true, GameManager.singleton.playerData);
				canvases.preroll.ActivateLoadingScene(1);
            }
            else
            {
                LogoutGP();
            }
        });
    }

    public void ShowAchievmentsUI()
    {
        if (Social.localUser.authenticated)
        {
            Social.ShowAchievementsUI();
        }
    }

    public void ShowLeaderboardsUI()
    {
        if (Social.localUser.authenticated)
        {
            Social.ShowLeaderboardUI();
        }
    }

    public void UnlockAchievment(string id)
    {
        if(Social.localUser.authenticated)
            Social.ReportProgress(id, 100.0f, (bool success) => {  });
    }

    public void LogoutGP()
    {
        if (Social.localUser.authenticated)
        {
            PlayGamesPlatform.Instance.SignOut();
            Application.Quit();
        }
        else
            Application.Quit();
    }

    private void OnApplicationQuit()
    {
        if (Social.localUser.authenticated)
        {
            PlayGamesPlatform.Instance.SignOut();
        }
    }

    #region CloudSaves

	/// <summary>
	/// Initialize Google Play Services and authentificate user. After authentification load saved data from cloud.
	/// </summary>
	/// <param name="saving">Is saving data or loading data</param>
	/// <param name="isPlayerData">Type of object to read/save</param>
	/// <param name="obj">Object to read/save</param>
    public void CloudSaves(bool saving, bool isPlayerData, object obj)
    {
        isSaving = saving;

        if (isPlayerData)
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            savedGameClient.OpenWithAutomaticConflictResolution("playerdata", GooglePlayGames.BasicApi.DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime, SavePlayerDataCloud);
        }
        else
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            savedGameClient.OpenWithAutomaticConflictResolution("playerstats", GooglePlayGames.BasicApi.DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime, SavePlayerStatsCloud);
        }
    }

    private void SavePlayerStatsCloud(SavedGameRequestStatus status, ISavedGameMetadata meta)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            if (isSaving)
            {
                GameManager.singleton.playerStats.cards = GameManager.singleton.cardsAmount;

                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
                builder = builder
                    .WithUpdatedPlayedTime(meta.TotalTimePlayed.Add(new TimeSpan(0, 0, (int)Time.realtimeSinceStartup)));
                SavedGameMetadataUpdate update = builder.Build();

                savedGameClient.CommitUpdate(meta, update, ObjectToByteArray(GameManager.singleton.playerStats), SaveUpdate);
            }
            else
            {
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                savedGameClient.ReadBinaryData(meta, SaveReadPlayerStats);
            }
        }
        else
        {
            Debug.Log("Not succeed! Open Save Player Stats Cloud!");
        }
    }

    private void SaveReadPlayerStats(SavedGameRequestStatus status, byte[] data)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            GameManager.singleton.playerStats = (PlayerStats)ByteArrayToObject(data);
            GameManager.singleton.cardsAmount = GameManager.singleton.playerStats.cards;
        }
        else
        {
            Debug.Log("Not succeed! Save Read Player Stats!");
        }
    }

    public void SavePlayerDataCloud(SavedGameRequestStatus status, ISavedGameMetadata meta)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            if (isSaving)
            {
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
                builder = builder
                    .WithUpdatedPlayedTime(meta.TotalTimePlayed.Add(new TimeSpan(0, 0, (int)Time.realtimeSinceStartup)));
                SavedGameMetadataUpdate update = builder.Build();

                savedGameClient.CommitUpdate(meta, update, ObjectToByteArray(GameManager.singleton.playerData), SaveUpdate);
            }
            else
            {
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                savedGameClient.ReadBinaryData(meta, SaveReadPlayerData);
            }
        }
        else
        {
            Debug.Log("Not succeed! Save Player Data Cloud!");
        }
    }

    private void SaveReadPlayerData(SavedGameRequestStatus status, byte[] data)
    {
        if (status == SavedGameRequestStatus.Success)
        {
            GameManager.singleton.playerData = (PlayerData)ByteArrayToObject(data);

            if (GameManager.singleton.playerData.cars[0] == 0)
            {
                GameManager.singleton.playerData.cars[0] = 1;
                GameManager.singleton.playerData.currentCar = 0;
                GameManager.singleton.playerData.currentGarage = 0;

                if (canvases == null)
                    canvases = GameObject.FindObjectOfType<Canvases>();

                if (canvases.mainUI != null)
                    canvases.mainUI.ShowPlayerData();

                CloudSaves(true, true, GameManager.singleton.playerData);
            }

            if (canvases == null)
                canvases = GameObject.FindObjectOfType<Canvases>();
            if(canvases.mainUI != null)
                canvases.mainUI.ShowPlayerData();

            canvases.preroll.ActivateLoadingScene(1);
        }
        else
        {
            Debug.Log("Not succeed! Save Read Player Data!");
        }
    }

    byte[] ObjectToByteArray(object obj)
    {
        if (obj == null)
            return null;

        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    object ByteArrayToObject(byte[] arrBytes)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter bf = new BinaryFormatter();
            ms.Write(arrBytes, 0, arrBytes.Length);
            ms.Seek(0, SeekOrigin.Begin);

            return bf.Deserialize(ms);
        }
    }

    #endregion /CloudSaves

    #region Prize
	/// <summary>
	/// Check time span from previous login
	/// </summary>
    public IEnumerator CurrentDateTime()
    {
        canvases = GameObject.FindObjectOfType<Canvases>();

        if ((Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) || (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork))
        {
            using (WWW www = new WWW("http://www.microsoft.com"))
            {
                yield return www;

                try
                {
                    string s = GameManager.singleton.playerData.lastSession.ToString();
                }
                catch
                {
                    GameManager.singleton.playerData.lastSession = "";
                    GameManager.singleton.SavePlayerData();
                }

                if (GameManager.singleton.playerData.lastSession.Length == 0)
                {
                    GameManager.singleton.playerData.lastSession = DateTime.ParseExact(www.responseHeaders["date"], "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal).ToString();
                    GameManager.singleton.SavePlayerData();

                    if (canvases.mainUI != null)
                        canvases.mainUI.ShowPrizeBtn();
                }
                else
                {
                    currentDateTime = DateTime.ParseExact(www.responseHeaders["date"], "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);
                    timeSpan = currentDateTime - DateTime.Parse(GameManager.singleton.playerData.lastSession);

                    if (timeSpan.TotalHours >= 5)
                    {
                        canvases.mainUI.ShowPrizeBtn();
                    }
                }
            }
        }

        yield return null;
    }

	
	/// <summary>
	/// Set new session DateTime
	/// </summary>
    public IEnumerator SetNewSessionDate()
    {
        if ((Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) || (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork))
        {
            using (WWW www = new WWW("http://www.microsoft.com"))
            {
                yield return www;
                GameManager.singleton.playerData.lastSession = DateTime.ParseExact(www.responseHeaders["date"], "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal).ToString();
                GameManager.singleton.SavePlayerData();
            }
        }
        yield return null;
    }
    #endregion

}

using UnityEngine;
using System.Collections;

/// <summary>
/// Room Engine. used in Cloud Mode. RPC Method should not in this class.
/// 
/// </summary>/
public class RoomEngine : Photon.MonoBehaviour
{
	public static int playerWhoIsIt = 0;
    public static PhotonView ScenePhotonView;
	
	public int _roomStatus = 0;
	private AsyncOperation engineAsync_;
	private string loadingScenePath_ = "";//cur scenename
	// Use this for initialization
	
	public GameObject _locPlayer;
	public string roomMode ="default"; //1v1
	void Start ()
	{
		//maybe we should use event to control this start.
		//i should be created by application.
		//PhotonNetwork.ConnectUsingSettings("0.1");
		EventManager.instance.addEventListener("LoadScene",this.gameObject,"StartLoad");
	}
	
#region connect&disconnect	
	public void Connect(){
		//setup room connection
		PhotonNetwork.ConnectUsingSettings("0.1");
		Debug.LogWarning("RoomConnect connecting..");
	}
	
	public void DisConnect(){		
		//Destroy GameObject. 2013-12-31 done!
		if(photonView!=null)
			PhotonNetwork.Destroy( photonView);
		if(PhotonNetwork.isMasterClient)//only masterclient can do it
			PhotonNetwork.DestroyAll();
		//Quit
		PhotonNetwork.LeaveRoom();		
		PhotonNetwork.Disconnect();
		Debug.LogWarning("Room DisConnecting..");
		
	}
#endregion	
	
	void OnJoinedLobby()
    {
        Debug.LogWarning ("JoinRandom!");
		//it seems we should use customProperties. private_10_scene000_2  私有房间最多10人.场景scene000 难度2.
		//only join 1v1 mode. maybe we should moved to other place. by cnsoft 		
				
		Hashtable expectedCustomRoomProperties = new Hashtable() { { "map", 1 } };
		PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties,2);
		
        //default mode PhotonNetwork.JoinRandomRoom();
		_roomStatus = 0;
    }

    void OnPhotonRandomJoinFailed()
    {
		string[] roomPropsInLobby = { "map", "ai","level" };
		Hashtable customRoomProperties = new Hashtable() { { "map", 1 },{"level",5} };
		string roomName = null;
		PhotonNetwork.CreateRoom(roomName, true, true, 2, customRoomProperties, roomPropsInLobby);		
        //PhotonNetwork.CreateRoom(null);
		//
		Debug.Log("Let me create Room");
		this._roomStatus = 1; //
		
    }

#region  LoadScene and Initlize. 
	
	void onSceneLoaded(CustomEvent evt)
	{
		//
		PhotonNetwork.isMessageQueueRunning = true;
		//open it
	    ScenePhotonView.RPC("ResetToStone",PhotonTargets.All);
		//Todo: load player data 
		ScenePhotonView.RPC("LoadPlayerData",PhotonTargets.Others,new Hashtable());
		//
		this.doManualPvInit();
		//dont do this.since this function is called by EventManager. 
		//EventManager.instance.removeEventListener("onSceneLoaded",this.gameObject);//clear it.
	}
	
	
	void doManualPvInit(){
	  if (PhotonNetwork.isMasterClient)
		{
			//Master Client will req server buffer these message to enable other joiner bind proxy correctly. 
			GameObject[] coms = GameObject.FindGameObjectsWithTag("Monster");
			foreach(GameObject com in coms)
			{
				GuidProperty guid = (GuidProperty) com.GetComponent<GuidProperty>();
				if (guid ==null)
					continue;
				//if null do nothing
				string objid = guid.objectid;
				//manual alloc pvid. 
				PhotonView pv = (PhotonView) com.GetComponent<PhotonView>();
				if (pv==null)
				{
					//attached one by hand
					com.gameObject.AddComponent(typeof(PhotonView));
					pv = (PhotonView) com.GetComponent<PhotonView>();
				}
				pv.viewID = PhotonNetwork.AllocateViewID();
				Hashtable _params = new Hashtable();
				_params.Add(0,objid);
				_params.Add(1,pv.viewID);
				//Notify All Remote Client to bind proxy .
				ScenePhotonView.RPC("BindRemoteProxy", PhotonTargets.OthersBuffered,_params);
			}	
		}
	}
	
#endregion	

    void OnJoinedRoom()
    {
		Debug.LogWarning("RoomPhotonView onJoinedRoom");		
		EventManager.instance.dispatchEvent(new CustomEvent("onTeleportTo"));
		//should changed to ui or other handler not here.
		int sceneId = 8886; //not same with buildings. 
		//?
		HelperUtils.hlpEngineGetPlayer().chmGetPhysicsHandler().pcsTeleportTo( -1, sceneId );
		//Debug.LogWarning("will moved to scene 3");
		//
		EventManager.instance.dispatchEvent(new CustomEvent("JoinedRoom"));		
		EventManager.instance.addEventListener("onSceneLoaded",this.gameObject,"onSceneLoaded");
		
		//
		//Manual close
		//PhotonNetwork.LoadLevel();
		PhotonNetwork.isMessageQueueRunning = false;
     	//after loaded open it again.
		
		//RoomEntity need PhotonNetwork		
		//Notice: we should call PhotonNetwork.Instantiate to make we can see each other in room.
		//Or PhotonInstantiate scensor object, observe local realobject. 
		//create the proxy object?
		_locPlayer = PhotonNetwork.Instantiate("network/NetRobotPaperDoll", Vector3.zero, Quaternion.identity,0);//(int) PhotonTargets.Others);
		GameObject.DontDestroyOnLoad(_locPlayer);		
        ScenePhotonView = _locPlayer.GetComponent<PhotonView>();
		_locPlayer.name = GuidProperty.GetUniqueID();
		//_locPlayer.SetActive(true);
		//only local interface can be used.
		Debug.Log(" ScenePV is used locally. " + ScenePhotonView);
		
		
		//Attached Chat here.
		//ChatDemo chatPv = (ChatDemo) _locPlayer.gameObject.AddComponent("ChatDemo");
		//TestNetBox.. 
		//set Player Id to used in chat system.
		//PhotonNetwork.SetPlayerCustomProperties(new Hashtable(){{"idname",string.Format("id%1",photonView.viewID) }});
		//		
		//PhotonNetwork.LoadLevel(4);
		
		//Todo: Spawn Level Game Object by level file. instead of .unity. 
		//e.g: create a monster.
		//PhotonNetwork.InstantiateSceneObject("network/NetRobotPaperDoll",Vector3.zero,Quaternion.identity,0);//
		PhotonNetwork.playerName = "Player" + PhotonNetwork.room.playerCount;
		//Component[] coms = GameObject.FindGameObjectsWithTag("monster");
		//int id = coms[0].GetInstanceID;
		
		return;
		
		//ScenePhotonView = this.gameObject.GetComponent<PhotonView>();
		//To add method call and convert local exist object to network object.
		//To check transfer position to remote. 2013-12-17 

		
		//Notify Application to initlize Player?
		//PhotonNetwork error: Could not Instantiate the prefab [network/RobotPaperDoll] as it has no PhotonView attached to the root.
		//To fix it,we attached a photonView to prefabs.		
        _locPlayer = PhotonNetwork.Instantiate("network/NetRobotPaperDoll", Vector3.zero, Quaternion.identity, 0);
		GameObject.DontDestroyOnLoad(_locPlayer);		
        ScenePhotonView = _locPlayer.GetComponent<PhotonView>();
		EventManager.instance.dispatchEvent(new CustomEvent("JoinedRoom"));
		//EntityManager should initlize it.
		
		if (_roomStatus ==1)
		{
			//you enter room first.
			Debug.LogWarning("i am loading donot destroy me");
			//Scene_000_01
			engineAsync_ = Application.LoadLevelAsync(3);
		}else {
			//get roominfo. and property. use scenename to load local scene.
			engineAsync_ = Application.LoadLevelAsync(3);
			
		}
		
		
    }
	
	
	bool _loadSceneUpdate(){
		if( engineAsync_ == null || loadingScenePath_ == null )
				return true;
		int progress = System.Convert.ToInt32( engineAsync_.progress * 100 );		
		EventManager.instance.dispatchEvent(new CustomEventObj("onSceneLoading"));
		if( engineAsync_.isDone )
			{
				// clear done things here.
				engineAsync_ = null;
				EventManager.instance.dispatchEvent(new CustomEventObj("onSceneLoaded"));
			}
			// done.
		return engineAsync_ == null;
		
	}
	public void Update()
	{
		//_loadSceneUpdate();	
		if (_locPlayer && !_locPlayer.activeInHierarchy)
		{
			Debug.Log(" _local net link is: " + _locPlayer.activeInHierarchy);
			//_locPlayer.SetActive(true);
		}
		//state description:		
		//if playercount ==2 Room.SetCustomProperties("gamestate",1) //broadcast to all clients. (change from waiting to running - remove ui.)
		//if not enough player. is waiting only.. 
		//from running. count 10 second to stage1:open door. 10m stage2:show bottle. 30m: failure.
		//anytime 1player die. changed to BattleDone. >show ui click to quit pvp
		if(PhotonNetwork.room!=null)
		{
			int playerCount = PhotonNetwork.room.playerCount;
			Debug.Log(" playercount="+playerCount);
			//string lastState = PhotonNetwork.room.customProperties["gamestate"];
			//if changed state. do change.
			//if(PhotonNetwork.isMasterClient)
			//	PhotonNetwork.room.SetCustomProperties(new Hashtable(){{"map",1}} ); //current sceneid.
			//...hide room room name will be encrypted with secretky. so have to use chat and click into room.
		}
		
	}
	
    void OnGUI()		
    {
		//Debug.Log("on RoomPV gui callback");
		
        GUI.Label(new Rect(100,160,100,40),PhotonNetwork.connectionStateDetailed.ToString());

     	//if (PhotonNetwork.room == null)
		return;
		
		if (PhotonNetwork.connectionStateDetailed == PeerState.Joined)
        {
            bool shoutMarco = PhotonNetwork.isMasterClient;//judge with master GameLogic.playerWhoIsIt == PhotonNetwork.player.ID;

            if (shoutMarco && GUI.Button(new Rect(100,180,100,30),"Marco!"))
            {
				Debug.Log("req Marco method be called");
				ScenePhotonView.RPC("Marco", PhotonTargets.All);
            }
            if (!shoutMarco && GUI.Button(new Rect(100,180,100,30),"Polo!"))
            {
                ScenePhotonView.RPC("Polo", PhotonTargets.All);
            }
        }
    }
	
	#region RoomEvent Region	
	public void OnPhotonPlayerConnected(PhotonPlayer player)
	{
		Debug.Log("OnPhotonPlayerConnected: " + player);
	}
	public void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{
		if (player.isMasterClient)
			Debug.LogWarning(" master quit room!");
		Debug.Log("OnPhotonPlayerDisConnected: " + player);
		
		
	}
	
	public void onPhotonInstantiate(PhotonMessageInfo info)
	{
		Debug.Log(" on Instantiate be called " + info.sender);		
		//object[] objs = photonView.instantiationData; //The instantiate data..
        //bool[] mybools = (bool[])objs[0];   //Our bools!		
		GameObject stoneObject = GameObject.FindGameObjectWithTag( "PlayerStone" );
		if (stoneObject!=null){
			GameObject _netbox = GameObject.FindGameObjectWithTag("NetBox");
			_netbox.transform.position = stoneObject.transform.position;
			_netbox.transform.rotation = stoneObject.transform.rotation;
			_netbox.transform.position[0]+= Random.Range(-50,50);
		}	
	}
	
	public void onFailedToConnecToPhoton(){
		Debug.Log ("fail connect to photon");
	}
	
	#endregion
	

}


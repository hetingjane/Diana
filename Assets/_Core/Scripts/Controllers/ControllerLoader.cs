using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerLoader : MonoBehaviour {

    public Controller Instance
    {
        get;
        private set;
    }

    public enum ControllerType
    {
        LOCAL,
        SOCKET,
        WEB
    }

    public ControllerType controllerType = ControllerType.LOCAL;

	void Awake () {
        if (controllerType == ControllerType.LOCAL)
            Instance = gameObject.AddComponent<LocalController>();
        else if (controllerType == ControllerType.SOCKET)
            Instance = gameObject.AddComponent<SocketController>();
        else if (controllerType == ControllerType.WEB)
            Instance = gameObject.AddComponent<WebController>();
        if (Instance == null)
            Debug.LogError("No controller set");
	}

    public static Controller LocateInScene()
    {
        var loader = FindObjectOfType<ControllerLoader>();
        if (loader != null)
            return loader.Instance;
        return null;
    }
}

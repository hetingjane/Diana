/* This module provides a socket interface to the DataStore.  Usage:

1. Attach this component to some GameObject in the scene.
2. Set the public 'port' property to the port you want to use.
3. Connect to that port from any other process; send messages terminated with \r\n.

Messages should have one of the following formats:

	SETI <key> <integer value>
	SETS <key> <string value>
	SETV <key> <vector x> <vector y> <vector z>
	SETB <key> <boolean value: T or F>
	SUB <key>			(subscribes to updates on the given key)
	NAME <new name>		(changes client name, for use in logs)

That's about it.  Note that if you SUBscribe to a key, you will receive
any changes to that key via SETI, SETS, SETV, or SETB messages.

You can connect with telnet and just type the above commands manually, 
and it should work.  Or use Python or whatever.
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class SocketInterfaceModule : ModuleBase
{
	[Tooltip("Network port on which to listen for incoming connections")]
	public int port = 38276;
	
	// Socket that accepts incoming connections
	TcpListener serverSocket;
	
	// Whether our socket is currently listening
	public bool isListening { get; private set; }
	
	// We keep a queue of messages as the interface between the threaded
	// part of the system, and actual processing of the messages on the
	// main thread.
	struct QueuedMessage {
		public Client client;
		public string message;

		public QueuedMessage(Client client, string message) {
			this.client = client;
			this.message = message;
		}
	}
	Queue<QueuedMessage> messageQueue;
	
	// List of clients subscribed to each DataStore key.
	Dictionary<string, List<Client>> subscribersByKey;
	
	/// <summary>
	/// Awake: Unity callback that fires when the component is created.
	/// We use that to initialize our fields, and start the server socket.
	/// </summary>
	protected void Awake() {
		messageQueue = new Queue<QueuedMessage>();
		subscribersByKey = new Dictionary<string, List<Client>>();
		
		serverSocket = new TcpListener(IPAddress.Any, port);
		try {
			serverSocket.Start(1);
			Debug.Log("SocketInterfaceModule: Server Listening on port " + port); 
			isListening = true;
			var acceptThread = new Thread(AcceptConnections);
			acceptThread.Start();
		} catch (System.Exception e) {
			Debug.LogError("Unable to bind port " + port + ": " + e.Message);
		}		
	}
	
	/// <summary>
	/// Start: Unity callback that fires before the first frame after 
	/// this component is created.  Used here to subscribe to DataStore changes.
	/// </summary>
	protected override void Start() {
		base.Start();
		// Subscribe to all datastore changes, since we don't know what
		// socket clients might need.
		DataStore.instance.onValueChanged.AddListener(ValueChanged);
	}
	
	/// <summary>
	/// OnDestroy: Unity callback when the object is destroyed, as by changing
	/// scenes or stopping the run.  Used to clean up the server socket.
	/// </summary>
	protected void OnDestroy() {
		isListening = false;
		serverSocket.Stop();
	}
	
	/// <summary>
	/// Update: Unity callback invoked on every frame.  Used here to process
	/// messages on our message queue (on the main thread).
	/// </summary>
	protected void Update() {
		while (messageQueue.Count > 0) {
			QueuedMessage qm = messageQueue.Dequeue();
			HandleMessage(qm.client, qm.message);
		}
	}
	
	/// <summary>
	/// Put a message onto the message queue.  This method may be
	/// called from a thread.
	/// </summary>
	/// <param name="client">client that received the message</param>
	/// <param name="message">message (command) text</param>
	protected void EnqueueMessage(Client client, string message) {
		messageQueue.Enqueue(new QueuedMessage(client, message));
	}
	
	/// <summary>
	/// Handle a message.  This method should be called on the main thread.
	/// This method defines (and interprets) our command syntax.
	/// </summary>
	/// <param name="client">client that received the message</param>
	/// <param name="message">message (command) text</param>
	protected void HandleMessage(Client client, string message) {
		//Debug.Log("Handling command: " + message);
		string[] parts = message.Split(new char[]{' '}, 3);
		if (parts.Length < 2) {
			// All our commands require at least 2 parts (command and at least 1 argument).
			client.WriteLine("Invalid command: " + message);
			return;
		}
		switch (parts[0]) {

		case "SETI":		// set integer
			{
				int value;
				if (parts.Length > 2 && int.TryParse(parts[2], out value)) {
					SetValue(parts[1], value, "Set by client " + client.clientID);
					client.WriteLine("OK");
				} else {
					client.WriteLine("Invalid argument to SETI command");
				}
			} break;
		
		case "SETB":		// set boolean
			{
				int value;
				if (parts.Length > 2) {
					string val = parts[2].ToUpper();
					bool truth = (val=="T" || val=="TRUE" || val=="1");
					SetValue(parts[1], truth, "Set by client " + client.clientID);
					client.WriteLine("OK");
				} else {
					client.WriteLine("Invalid argument to SETB command");
				}
			} break;
		
		case "SETS":		// set string
			{
				if (parts.Length > 2) {
					SetValue(parts[1], parts[2], "Set by client " + client.clientID);
					client.WriteLine("OK");
				} else {
					client.WriteLine("Invalid argument to SETS command");
				}
			} break;

            case "SETV":
                {
                    if (parts.Length > 2)
                    {
                        //float[] floats = parts[2].Split(',').Select(x => Single.Parse(x).ToArray();  // this could fail if the message is malformed
                        float[] floats = Array.ConvertAll<string, float>(parts[2].Split(','), float.Parse);
                        SetValue(parts[1], floats, "Set by client " + client.clientID);
                        client.WriteLine("OK");
                    }
                    else
                    {
                        client.WriteLine("Invalid argument to SETV command");
                    }
                } break;

            case "SETV3":       // set 3d vector
                {
                    string[] valparts = null;
                    if (parts.Length > 2) valparts = parts[2].Split(new char[] { ' ' });
                    Vector3 v;
                    if (valparts != null && valparts.Length > 2 && float.TryParse(valparts[0], out v.x)
                        && float.TryParse(valparts[1], out v.y) && float.TryParse(valparts[2], out v.z))
                    {
                        SetValue(parts[1], v, "Set by client " + client.clientID);
                        client.WriteLine("OK");
                    }
                    else
                    {
                        client.WriteLine("Invalid argument to SETV command");
                    }
                }
                break;

            case "SUB":			// subscribe
			{
				string key = parts[1];
				List<Client> clients;
				if (!subscribersByKey.TryGetValue(key, out clients)) {
					clients = new List<Client>();
					subscribersByKey[key] = clients;
				}
				if (clients.IndexOf(client) < 0) clients.Add(client);
				client.WriteLine("OK");
			} break;
		
		case "NAME":		// set client name
			client.clientID = parts[1];
			client.WriteLine("OK");
			break;
		
		default:
			client.WriteLine("Invalid command: " + message);
			break;
		}
	}
	
	/// <summary>
	/// Handle a change in a DataStore value.  (This method is hooked up
	/// as a DataStore.onValueChanged receiver in the Start method above).
	/// </summary>
	/// <param name="key">key whose value has changed</param>
	private void ValueChanged(string key) {
		// Pass the change on to any subscribers.
		List<Client> clients;
		if (subscribersByKey.TryGetValue(key, out clients)) {
			DataStore.IValue value = DataStore.GetValue(key);
			string msg = null;
			if (value is DataStore.IntValue) {
				msg = string.Format("SETI {0} {1}", key, ((DataStore.IntValue)value).val);
			} else if (value is DataStore.IntValue) {
				msg = string.Format("SETB {0} {1}", key, ((DataStore.BoolValue)value).val ? "T" : "F");
			} else if (value is DataStore.StringValue) {
				msg = string.Format("SETS {0} {1}", key, ((DataStore.StringValue)value).val);
			} else if (value is DataStore.Vector3Value) {
				Vector3 v = ((DataStore.Vector3Value)value).val;
				msg = string.Format("SETV {0} {1} {2} {3}", key, v.x, v.y, v.z);
			}
			if (msg == null) return;
			foreach (Client client in clients) {
				client.WriteLine(msg);
			}
		}
	}
	
	/// <summary>
	/// Thread body that sits in a loop, accepting incoming connections.
	/// </summary>
	private void AcceptConnections() {
		int counter = 0;
		while (isListening) {
			counter++;
			try {
				TcpClient clientSocket = serverSocket.AcceptTcpClient();
				Debug.Log("SocketInterfaceModule: Client #" + counter + " connected from " + clientSocket.Client.RemoteEndPoint);
				var client = new Client();
				client.StartClient(this, clientSocket, counter.ToString());
			} catch (SocketException e) {
				// Commonly happens when aborting a run.  Nothing to worry about.
			}
			Thread.Sleep(16);	// about 1/60th of a second
		}		
	}
	
	/// <summary>
	/// Method called when a client connection is lost.
	/// </summary>
	/// <param name="client">client whose connection has dropped</param>
	protected void ClientDropped(Client client) {
		// remove the given client from our subscribers lists
		foreach (List<Client> clients in subscribersByKey.Values) {
			for (int i=clients.Count-1; i>=0; i--) {
				if (clients[i] == client) clients.RemoveAt(i);
			}
		}
	}

	// class Client:
	// Internal class to handle each client.
	protected class Client {
		// Human-readable client identifier (used mainly for logging)
		public string clientID;

		// Actual socket connecting to the remote endpoint
		TcpClient clientSocket;
		
		// Stream wrapper for our socket I/O
		NetworkStream stream;
		
		// SocketInterfaceModule that created this client
		SocketInterfaceModule owner;
		
		/// <summary>
		/// Start a thread to handle I/O with the given client.
		/// </summary>
		/// <param name="owner">SocketInterfaceModule that created this client</param>
		/// <param name="inClientSocket">actual socket connecting to the remote endpoint</param>
		/// <param name="clientID">human-readable client identifier (used mainly for logging)</param>
		public void StartClient(SocketInterfaceModule owner, TcpClient inClientSocket, string clientID) {
			this.owner = owner;
			this.clientSocket = inClientSocket;
			this.clientID = clientID;
			Thread ctThread = new Thread(Process);
			ctThread.Start();
		}
		
		/// <summary>
		/// Thread body that sits in a loop, handling messages from the client
		/// until either the client disconnects, or our owner shuts down.  We
		/// deal here with partial and multiple messages, using \r\n to delimit
		/// complete messages.
		/// </summary>
	    void Process() {
		    byte[] buffer = new byte[10240];  // read up to 10k at once
		    stream = clientSocket.GetStream();
		    string message = null;		// message or partial message waiting for completion
		    
		    while (owner != null && owner.isListening) {
			
			    // Check for client disconnect
			    if (clientSocket.Client.Poll(0, SelectMode.SelectRead)) {
				    int bytesAvail = 0;
				    try {
					    bytesAvail = clientSocket.Client.Receive(buffer, SocketFlags.Peek);
				    } catch (System.Exception e) {
					    Debug.Log(clientID + " EXCEPTION: " + e.Message);
				    }
				    if (bytesAvail == 0) {
					    Debug.Log("Client " + clientID + " disconnected");
					    owner.ClientDropped(this);
					    return;
				    }
			    }
				
			    // If there is no new data available, snooze for a while then check again.
			    if (!stream.DataAvailable) {	
				    Thread.Sleep(16);	// about 1/60th of a second
				    continue;
			    }
				
			    // Grab the data from the stream, find complete messages,
			    // and pass them off to our owner to enqueue for later processing.
			    int bytesRead = stream.Read(buffer, 0, buffer.Length);
			    string rawData = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
			    message += rawData;
			    int terminatorPos = message.IndexOf("\r\n");
			    while (terminatorPos >= 0) {
				    string msg = message.Substring(0, terminatorPos);
				    message = message.Substring(terminatorPos + 2);
				    owner.EnqueueMessage(this, msg);
				    terminatorPos = message.IndexOf("\r\n");
			    }
		    }
		    
		    // Clean up.
		    stream.Dispose();
		    clientSocket.Dispose();
	    }
	    
		/// <summary>
		/// Send the given message to the client, followed by our standard \r\n terminator.
		/// </summary>
		/// <param name="msg">message to send</param>
		public void WriteLine(string msg) {
			msg = msg + "\r\n";
			Byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
			try {
				stream.Write(sendBytes, 0, sendBytes.Length);
				stream.Flush();
				//Debug.Log(clientID + " << " + msg);
			} catch (System.IO.IOException e) {
				Debug.Log(clientID + " EXCEPTION: " + e);
			}
			
		}
	} 
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using SimpleJSON;

public class AccelServer : MonoBehaviour
{
    Thread rxThread;
    UdpClient clientSock;
    public int port = 9999;
    ConcurrentQueue<Vector3> forceVectors;
    ConcurrentQueue<Vector3> rotations;

    // Use this for initialization
    void Start()
    {
        Application.runInBackground = true;
        forceVectors = new ConcurrentQueue<Vector3>();
        rotations = new ConcurrentQueue<Vector3>();

        rxThread = new Thread(new ThreadStart(ReceiveDataThread));
        rxThread.IsBackground = true;
        rxThread.Start();
    }

    private void ReceiveDataThread()
    {
        print("Starting RX thread...");
        clientSock = new UdpClient(port);   

        try
        {
            while (true)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = clientSock.Receive(ref sender);

                // parse the input string
                String dataStr = Encoding.ASCII.GetString(data);

                JSONNode transformJson = JSON.Parse(dataStr);
        
                if (transformJson != null)
                {
                    Vector3 newVec = new Vector3(transformJson["rotation"]["x"].AsFloat, 
                                                 transformJson["rotation"]["y"].AsFloat,
                                                 transformJson["rotation"]["z"].AsFloat);
                    
                    print(String.Format("Enqueued new vector: {0}", newVec.ToString()));
                    rotations.Enqueue(newVec);
                }
                else
                {
                    print("Ignoring bad data...");
                    
                    byte[] errorMsg = Encoding.ASCII.GetBytes("Error. I need a vector like x,y,z\n");
                    clientSock.Send(errorMsg, errorMsg.Length, sender);
                }
            }
        }
        catch (ThreadAbortException e)
        {
            print("Exiting...");
        }
        catch (Exception e)
        {
            print(e.ToString());
        }
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3[] rots = rotations.CopyToArray();
        rotations.Clear();

        foreach (Vector3 angle in rots)
        {
            //rigidbody.AddForce(force);
            transform.eulerAngles = angle;
        }

        if (rots.Length > 0)
          print(String.Format("Added {0} force(s)", rots.Length));
    }

    void OnDestroy()
    {
        if (rxThread.IsAlive)
        {
            rxThread.Abort();
            clientSock.Close();
        }

        // try serializing a transform
    }
}

class ConcurrentQueue<T>
{
    private readonly object syncLock = new object();
    private Queue<T> queue;

    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return queue.Count;
            }
        }
    }

    public ConcurrentQueue()
    {
        this.queue = new Queue<T>();
    }

    public T Peek()
    {
        lock (syncLock)
        {
            return queue.Peek();
        }
    }

    public void Enqueue(T obj)
    {
        lock (syncLock)
        {
            queue.Enqueue(obj);
        }
    }

    public T Dequeue()
    {
        lock (syncLock)
        {
            return queue.Dequeue();
        }
    }

    public void Clear()
    {
        lock (syncLock)
        {
            queue.Clear();
        }
    }

    public T[] CopyToArray()
    {
        lock (syncLock)
        {
            if (queue.Count == 0)
            {
                return new T[0];
            }
        
            T[] values = new T[queue.Count];
            queue.CopyTo(values, 0);    
            return values;
        }
    }

    public static ConcurrentQueue<T> InitFromArray(IEnumerable<T> initValues)
    {
        var queue = new ConcurrentQueue<T>();
    
        if (initValues == null)
        {
            return queue;
        }
    
        foreach (T val in initValues)
        {
            queue.Enqueue(val);
        }
    
        return queue;
    }
}

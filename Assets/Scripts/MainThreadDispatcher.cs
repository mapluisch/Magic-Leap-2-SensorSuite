using System;
using System.Collections.Generic;
using UnityEngine;
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private static readonly Queue<Action> executionQueue = new Queue<Action>();
    public static MainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MainThreadDispatcher>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MainThreadDispatcher");
                    instance = go.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue.Dequeue().Invoke();
            }
        }
    }
    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }
    public static bool IsMainThread()
    {
        return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
    }
}
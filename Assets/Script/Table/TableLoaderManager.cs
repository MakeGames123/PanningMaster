using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public interface ITableLoader
{
    event Action OnLoaded;
}
public class TableLoaderManager : MonoBehaviour
{
    public static TableLoaderManager Instance { get; private set; }

    public UnityEvent OnAllTablesLoaded = new();
    public List<GameObject> tables = new();
    public List<ITableLoader> loaders = new();
    private int loadedCount = 0;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach(GameObject obj in tables)
        {
            loaders.Add(obj.GetComponent<ITableLoader>());
        }
        SubscribeLoaders();
    }
    void SubscribeLoaders()
    {
        foreach (var loader in loaders)
        {
            loader.OnLoaded += HandleTableLoaded;
        }
    }

    void HandleTableLoaded()
    {
        loadedCount++;

        if (loadedCount >= loaders.Count)
        {
            Debug.Log("🔥 All Tables Loaded");
            OnAllTablesLoaded?.Invoke();
        }
    }

}
using System;
using UnityEngine;

public abstract class MonoSingletone<T> : MonoBehaviour where T : class
{
    protected static T _instance;
    public static T Instance => _instance;

    public static event Action<T> OnCreate;
    public static event Action<T> OnReset;

    protected virtual void OnEnable()
    {
        if (_instance != null)
        {
            Debug.LogWarning($"Having multiple instances of {nameof(T)} is not allowed");
            HandleCollision();
        }
        else
        {
            _instance = GetInstance();
            OnCreate?.Invoke(_instance);
        }
    }

    protected virtual void HandleCollision() { Destroy(gameObject); }
    protected abstract T GetInstance();

    protected void ResetInstance() { OnReset?.Invoke(_instance); _instance = null; }

    public static bool Exists => _instance != null;
}
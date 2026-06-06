using JetBrains.Annotations;
using System;
using Unity.Netcode;

public class FumoNV<T>
{/*
    public NetworkVariable<T> Value { get; }
    private readonly Action<T, T> _onChanged;
    public FumoNV(T defaultValue, Action<T, T> onChanged,
        NetworkVariableReadPermission read = NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission write = NetworkVariableWritePermission.Server)
    {
        _onChanged = onChanged;
        Value = new NetworkVariable<T>(defaultValue, read, write);
        Value.OnValueChanged += Handle;
    }
    private void Handle(T oldValue, T newValue)
    {
        _onChanged?.Invoke(oldValue, newValue);
    }*/
}
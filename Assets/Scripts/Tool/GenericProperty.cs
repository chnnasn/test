using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GenericProperty<T>
{
    private T mValue = default(T);

    public Action<T> OnValueChanged;

    public T Value
    {
        get { return mValue; }
        set
        {
            if (!EqualityComparer<T>.Default.Equals(value, mValue))
            {
                mValue = value;
                OnValueChanged?.Invoke(mValue);
            }
            
        }

    }
}

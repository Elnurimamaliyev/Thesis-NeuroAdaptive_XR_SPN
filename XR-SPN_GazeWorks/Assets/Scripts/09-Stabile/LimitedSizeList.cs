using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitedSizeList<T> : List<T>
{
    private readonly int _maxSize;

    public LimitedSizeList(int maxSize)
    {
        _maxSize = maxSize;
    }

    
    public new void Add(T item)
    {
        base.Add(item);

        if (this.Count > _maxSize)
            this.RemoveAt(0);
    }
}

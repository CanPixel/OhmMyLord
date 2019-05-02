using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Heap<T> where T : IHeapItem<T> {
    private T[] items;
    private int currentCount;

    public int Count {
        get {
            return currentCount;
        }
    }

    public Heap(int maxSize) {
        items = new T[maxSize];
    }

    public void Add(T item) {
        item.HeapIndex = currentCount;
        items[currentCount] = item;
        SortUp(item);
        currentCount++;
    }

    public T RemoveFirst() {
        T first = items[0];
        currentCount--;
        items[0] = items[currentCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return first;
    }

    public bool Contains(T item) {
        return Equals(items[item.HeapIndex], item);
    }

    public void Update(T item) {
        SortUp(item);
    }

    protected void SortDown(T item) {
        while(true) {
            int childLeft = item.HeapIndex * 2 + 1;
            int childRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if(childLeft < currentCount) {
                swapIndex = childLeft;
                if(childRight < currentCount) {
                    if(items[childLeft].CompareTo(items[childRight]) < 0) swapIndex = childRight;
                }

                if(item.CompareTo(items[swapIndex]) < 0) Swap(item, items[swapIndex]);
                else return;
            } else return;
        }
    }

    protected void SortUp(T item) {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while(true) {
            T parent = items[parentIndex];
            if(item.CompareTo(parent) > 0) Swap(item, parent);
            else break;
        }
    }

    protected void Swap(T itemA, T itemB) {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int aIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = aIndex;
    }
}

public interface IHeapItem<T> : IComparable<T> {
    int HeapIndex {
        get; set;
    }
}

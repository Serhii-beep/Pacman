using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<T> _heap;
    private readonly Comparison<T> _compare;

    public PriorityQueue(Comparison<T> compare)
    {
        _heap = new List<T>();
        _compare = compare;
    }

    public int Count => _heap.Count;

    public void Enqueue(T item)
    {
        _heap.Add(item);
        BubbleUp(_heap.Count - 1);
    }

    public T Dequeue()
    {
        if(_heap.Count == 0)
        {
            throw new InvalidOperationException("Priority queue is empty.");
        }

        T item = _heap[0];

        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);

        if(_heap.Count > 0)
        {
            BubbleDown(0);
        }

        return item;
    }

    private void BubbleUp(int index)
    {
        while(index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if(_compare(_heap[index], _heap[parentIndex]) >= 0)
            {
                break;
            }

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void BubbleDown(int index)
    {
        int childIndex = index * 2 + 1;

        while(childIndex < _heap.Count)
        {
            int otherChildIndex = childIndex + 1;

            if(otherChildIndex < _heap.Count && _compare(_heap[otherChildIndex], _heap[childIndex]) < 0)
            {
                childIndex = otherChildIndex;
            }

            if(_compare(_heap[index], _heap[childIndex]) <= 0)
            {
                break;
            }

            Swap(index, childIndex);
            index = childIndex;
            childIndex = index * 2 + 1;
        }
    }

    private void Swap(int index1, int index2)
    {
        T temp = _heap[index1];
        _heap[index1] = _heap[index2];
        _heap[index2] = temp;
    }
}


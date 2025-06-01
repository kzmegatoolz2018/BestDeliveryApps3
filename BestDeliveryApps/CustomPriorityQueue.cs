using System;
using System.Collections.Generic;

namespace BestDeliveryApps
{
    public class CustomPriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private readonly List<(TElement element, TPriority priority)> _items = new();

        public int Count => _items.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            _items.Add((element, priority));
            int i = _items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_items[parent].priority.CompareTo(_items[i].priority) <= 0) break;
                (_items[i], _items[parent]) = (_items[parent], _items[i]);
                i = parent;
            }
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (_items.Count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            element = _items[0].element;
            priority = _items[0].priority;

            _items[0] = _items[^1];
            _items.RemoveAt(_items.Count - 1);

            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < _items.Count && _items[left].priority.CompareTo(_items[smallest].priority) < 0)
                    smallest = left;
                if (right < _items.Count && _items[right].priority.CompareTo(_items[smallest].priority) < 0)
                    smallest = right;

                if (smallest == i) break;
                (_items[i], _items[smallest]) = (_items[smallest], _items[i]);
                i = smallest;
            }

            return true;
        }
    }
}
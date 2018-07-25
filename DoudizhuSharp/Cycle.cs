using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DoudizhuSharp
{
    public class Cycle<T> : ICollection<T>, IEnumerator<T>
    {
        public List<T> List { get; } = new List<T>();
        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T item) => List.Add(item);
        public void Clear() => List.Clear();
        public bool Contains(T item) => List.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);
        public bool Remove(T item) => List.Remove(item);
        public int Count => List.Count;
        public bool IsReadOnly => false;
        public bool MoveNext()
        {
            if (Count == 0) throw new NotSupportedException("Fork 您");
            CurrentIndex = (CurrentIndex + 1) % Count;
            return true;
        }
        
        public void Reset()
        {
            CurrentIndex = 0;
        }

        public T Current => List[CurrentIndex];
        public int CurrentIndex { get; internal set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Sniper.Lighting.DMX
{
    //http://codereview.stackexchange.com/questions/7276/is-this-collection-actually-thread-safe-is-concurrent-iterating-querying-and-m

    public class SafeEnumerator<T> : IEnumerator<T>
    {
        private readonly ReaderWriterLockSlim _readerWriterLock;
        private readonly IEnumerator<T> _innerCollection;

        public SafeEnumerator(IEnumerator<T> innerCollection, ReaderWriterLockSlim readerWriterLock)
        {
            _innerCollection = innerCollection;
            _readerWriterLock = readerWriterLock;
            _readerWriterLock.EnterReadLock();
        }

        public void Dispose()
        {
            _readerWriterLock.ExitReadLock();
        }

        public bool MoveNext()
        {
            return _innerCollection.MoveNext();
        }

        public void Reset()
        {
            _innerCollection.Reset();
        }

        public T Current
        {
            get
            {
                return _innerCollection.Current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }
    }


    public class ThreadSafeList<T> : IList<T>
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly List<T> _innerList = new List<T>();


        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                return new SafeEnumerator<T>(_innerList.GetEnumerator(), _readerWriterLock);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return (this as IEnumerable<T>).GetEnumerator();
        }

        public void Add(T item)
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                _innerList.Add(item);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                _innerList.Clear();
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                _readerWriterLock.EnterReadLock();
                return _innerList.Contains(item);
            }
            finally
            {
                _readerWriterLock.EnterReadLock();
            }

        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                _innerList.CopyTo(array, arrayIndex);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                return _innerList.Remove(item);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    _readerWriterLock.EnterReadLock();
                    return _innerList.Count;
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                try
                {
                    _readerWriterLock.EnterReadLock();
                    return ((ICollection<T>)_innerList).IsReadOnly;
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }
        }

        public int IndexOf(T item)
        {
            try
            {
                _readerWriterLock.EnterReadLock();
                return _innerList.IndexOf(item);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }

        public void Insert(int index, T item)
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                _innerList.Insert(index, item);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public void RemoveAt(int index)
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                _innerList.RemoveAt(index);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    _readerWriterLock.EnterReadLock();
                    return _innerList[index];
                }
                finally
                {
                    _readerWriterLock.ExitReadLock();
                }
            }

            set
            {
                try
                {
                    _readerWriterLock.EnterWriteLock();
                    _innerList[index] = value;
                }
                finally
                {
                    _readerWriterLock.ExitWriteLock();
                }
            }
        }
    }
}
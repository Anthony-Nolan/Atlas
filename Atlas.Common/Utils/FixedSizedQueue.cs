using System.Collections.Generic;

namespace Atlas.Common.Utils
{
    /// <summary>
    ///  Queue which automatically drops the oldest (first) entry upon new entry insertion when it gets full.
    /// </summary>
    public class FixedSizedQueue<T> : Queue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            lock (syncObject)
            {
                while (Count >= Size)
                {
                    TryDequeue(out _);
                }
            }

            base.Enqueue(obj);
        }
    }
}

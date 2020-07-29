using System.Collections.Generic;

namespace Atlas.Common.Helpers
{
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
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (Count > Size)
                {
                    TryDequeue(out _);
                }
            }
        }
    }
}

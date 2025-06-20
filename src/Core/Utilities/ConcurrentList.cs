namespace BackBuddy.Core.Library.Utilities
{
    public class ConcurrentList<T>
    {
        private readonly Lock _lockObject = new();
        private readonly List<T> _list = [];

        public void Add(T item)
        {
            lock (_lockObject)
            {
                _list.Add(item);
            }
        }

        public void Remove(T item)
        {
            lock (_lockObject)
            {
                _list.Remove(item);
            }
        }

        public T[] ToArray()
        {
            lock (_lockObject)
            {
                return [.. _list];
            }
        }
    }
}

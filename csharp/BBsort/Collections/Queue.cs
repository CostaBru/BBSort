namespace Flexols.Data.Collections.Interfaces
{
    public interface IDequeue<T>
    {
        T Dequeue();
        int Count { get; }
    }

    public class Queue<T> : IDequeue<T>
    {
        private readonly System.Collections.Generic.Queue<T> m_queue;

        public Queue()
            : this(0)
        {
        }

        public Queue(int capacity)
        {
            m_queue = new System.Collections.Generic.Queue<T>(capacity);
        }

        public int Count
        {
            get { return m_queue.Count; }
        }

        public T Dequeue()
        {
            return m_queue.Dequeue();
        }

        public void Enqueue(T item)
        {
            m_queue.Enqueue(item);
        }
    }
}

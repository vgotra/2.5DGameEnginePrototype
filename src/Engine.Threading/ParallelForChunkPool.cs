namespace Engine.Threading;

internal sealed class ParallelForChunkPool
{
    private ParallelForChunk? _head;

    internal ParallelForChunk Rent()
    {
        while (true)
        {
            ParallelForChunk? head = Volatile.Read(ref _head);
            if (head == null) return new ParallelForChunk();
            if (Interlocked.CompareExchange(ref _head, head.Next, head) == head)
            {
                head.Next = null;
                return head;
            }
        }
    }

    internal void Return(ParallelForChunk chunk)
    {
        chunk.Body = null;
        chunk.Pool = this;
        while (true)
        {
            ParallelForChunk? head = Volatile.Read(ref _head);
            chunk.Next = head;
            if (Interlocked.CompareExchange(ref _head, chunk, head) == head) return;
        }
    }
}

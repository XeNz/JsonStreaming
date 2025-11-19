namespace JsonStreaming.Example;

public static class AsyncEnumerableExtensions
{
    extension<T>(IAsyncEnumerable<T> source)
    {
        /// <summary>
        ///     Allows peeking at each item in the stream without modifying it.
        /// </summary>
        public async IAsyncEnumerable<T> Peek(Action<T> peekAction)
        {
            await foreach (var item in source)
            {
                peekAction(item);
                yield return item;
            }
        }
    }
}

namespace BackBuddy.Core.Library.Utilities
{
    /// <summary>
    /// DotNet Random is not ThreadSafe so we need ThreadSafeRandom.
    /// See also: https://stackoverflow.com/questions/3049467/is-c-sharp-random-number-generator-thread-safe.
    /// Design notes:
    /// 1. Uses own Random for each thread (thread local).
    /// 2. Seed can be set in ThreadSafeRandom ctor. Note: Be careful - one seed for all threads can lead same values for several threads.
    /// 3. ThreadSafeRandom implements Random class for simple usage instead ordinary Random.
    /// 4. ThreadSafeRandom can be used by global static instance. Example: `int randomInt = ThreadSafeRandom.Global.Next()`.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class.
    /// </remarks>
    /// <param name="seed">Optional seed for <see cref="Random"/>. If not provided then random seed will be used.</param>
    public class ThreadSafeRandom(int? seed = null) : Random
    {
        /// <summary>
        /// Gets global static instance.
        /// </summary>
        public static ThreadSafeRandom Global { get; } = new ThreadSafeRandom();

        // Thread local Random is safe to use on that thread.
        private readonly ThreadLocal<Random> _threadLocalRandom = new(() => seed != null ? new Random(seed.Value) : new Random());

        /// <inheritdoc />
        public override int Next() => _threadLocalRandom.Value!.Next();

        /// <inheritdoc />
        public override int Next(int maxValue) => _threadLocalRandom.Value!.Next(maxValue);

        /// <inheritdoc />
        public override int Next(int minValue, int maxValue) => _threadLocalRandom.Value!.Next(minValue, maxValue);

        /// <inheritdoc />
        public override void NextBytes(byte[] buffer) => _threadLocalRandom.Value!.NextBytes(buffer);

        /// <inheritdoc />
        public override void NextBytes(Span<byte> buffer) => _threadLocalRandom.Value!.NextBytes(buffer);

        /// <inheritdoc />
        public override double NextDouble() => _threadLocalRandom.Value!.NextDouble();
    }
}

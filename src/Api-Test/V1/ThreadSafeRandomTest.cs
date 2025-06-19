//using BackBuddy.Core.Library.Utilities;
//using System.Collections.Concurrent;

//namespace BackBuddy.Api.Test.V1
//{
//    [TestClass]
//    public class ThreadSafeRandomTest
//    {
//        [TestMethod]
//        public void Next_ShouldReturnInt()
//        {
//            // Arrange
//            ThreadSafeRandom random = ThreadSafeRandom.Global;

//            // Act
//            int value = random.Next();

//            // Assert
//            Assert.IsInstanceOfType<int>(value);
//        }

//        [TestMethod]
//        public void Next_WithMaxValue_ShouldReturnWithinRange()
//        {
//            // Arrange
//            ThreadSafeRandom random = ThreadSafeRandom.Global;
//            int max = 10;

//            // Act
//            int value = random.Next(max);

//            // Assert
//            Assert.IsTrue(value >= 0 && value < max);
//        }

//        [TestMethod]
//        public void Next_WithMinAndMaxValue_ShouldReturnWithinRange()
//        {
//            // Arrange
//            ThreadSafeRandom random = ThreadSafeRandom.Global;
//            int min = 5;
//            int max = 15;

//            // Act
//            int value = random.Next(min, max);

//            // Assert
//            Assert.IsTrue(value >= min && value < max);
//        }

//        [TestMethod]
//        public void NextDouble_ShouldReturnBetweenZeroAndOne()
//        {
//            // Arrange
//            ThreadSafeRandom random = ThreadSafeRandom.Global;

//            // Act
//            double value = random.NextDouble();

//            // Assert
//            Assert.IsTrue(value >= 0.0 && value < 1.0);
//        }

//        [TestMethod]
//        public void NextBytes_ShouldFillArray()
//        {
//            // Arrange
//            ThreadSafeRandom random = ThreadSafeRandom.Global;
//            byte[] buffer = new byte[8];

//            // Act
//            random.NextBytes(buffer);

//            // Assert
//            bool allZero = true;
//            for (int i = 0; i < buffer.Length; i++)
//            {
//                if (buffer[i] != 0)
//                {
//                    allZero = false;
//                    break;
//                }
//            }
//            Assert.IsFalse(allZero, "Buffer should not be all zeros after NextBytes.");
//        }

//        [TestMethod]
//        public void Next_ShouldBeThreadSafe_AndProduceDifferentValues()
//        {
//            // Arrange
//            ThreadSafeRandom random = ThreadSafeRandom.Global;
//            int threadCount = 10;
//            int iterations = 1000;
//            ConcurrentBag<int> results = [];
//            ManualResetEventSlim startEvent = new(false);
//            Task[] tasks = new Task[threadCount];

//            // Act
//            for (int i = 0; i < threadCount; i++)
//            {
//                tasks[i] = Task.Run(() =>
//                {
//                    startEvent.Wait();
//                    for (int j = 0; j < iterations; j++)
//                    {
//                        int value = random.Next();
//                        results.Add(value);
//                    }
//                });
//            }
//            startEvent.Set();
//            Task.WaitAll(tasks);

//            // Assert
//            // Es sollten viele verschiedene Werte vorhanden sein
//            int uniqueCount = new HashSet<int>(results).Count;
//            Assert.IsTrue(uniqueCount > threadCount, "ThreadSafeRandom should produce many unique values across threads.");
//        }
//    }
//}

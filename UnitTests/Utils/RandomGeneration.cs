using System;
using NUnit.Framework;

namespace UnitTests.Utils
{
    public static class RandomGeneration
    {
        private static readonly Random random = new Random();

        public static string RandomString(int sizeFrom, int sizeTo)
        {
            return TestContext.CurrentContext.Random.GetString(random.Next(sizeFrom, sizeTo));
        }

        public static byte[] RandomArray(int size)
        {
            var list = new byte[size];
            for (var i = 0; i < size; i++)
                list[i] = (byte)random.Next(Byte.MinValue, Byte.MaxValue);
            return list;
        }
    }
}
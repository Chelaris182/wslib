using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UnitTests.Utils
{
    public static class RandomGeneration
    {
        private static readonly Random random = new Random();
        private static readonly ReadOnlyCollection<char> asciiSymbols = Array.AsReadOnly(Enumerable.Range(32, 126).Select(Convert.ToChar).ToArray());

        public static string RandomString(int sizeFrom, int sizeTo)
        {
            return new string(RandomCollection(asciiSymbols, sizeFrom, sizeTo));
        }

        public static string RandomString(IReadOnlyList<char> alphabet, int sizeFrom, int sizeTo)
        {
            return new string(RandomCollection(alphabet, sizeFrom, sizeTo));
        }

        public static T[] RandomCollection<T>(IReadOnlyList<T> alphabet, int sizeFrom, int sizeTo)
        {
            return RandomCollection(alphabet, random.Next(sizeFrom, sizeTo));
        }

        public static T[] RandomCollection<T>(IReadOnlyList<T> alphabet, int size)
        {
            var list = new T[size];
            for (var i = 0; i < size; i++)
                list[i] = alphabet[random.Next(alphabet.Count)];
            return list;
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
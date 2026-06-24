using System;
using System.Collections.Generic;

namespace DustBot
{
    /// <summary>
    /// Small local PRNG. It never touches UnityEngine.Random, so published level
    /// layouts remain stable for a given seed and generation version.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(string seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException("seed");
            }

            state = StableHash(seed);
            if (state == 0)
            {
                state = 0x6D2B79F5u;
            }
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            uint range = (uint)(maximumExclusive - minimumInclusive);
            return minimumInclusive + (int)(NextUInt() % range);
        }

        public bool Chance(int numerator, int denominator)
        {
            return Range(0, denominator) < numerator;
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T value = list[i];
                list[i] = list[j];
                list[j] = value;
            }
        }

        public static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }
    }
}

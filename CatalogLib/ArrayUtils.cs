using System;
using System.Collections.Generic;

namespace CatalogLib
{
    public static class ArrayUtils
    {
        public static IEnumerable<int[]> IterateIndices(int[] lengths)
        {
            if (lengths == null) throw new ArgumentNullException(nameof(lengths));

            var rank = lengths.Length;
            if (rank == 0)
                yield break;

            var indices = new int[rank];

            int depth = rank - 1;
            while (true)
            {
                yield return indices;

                indices[depth]++;
                while (indices[depth] >= lengths[depth])
                {
                    indices[depth] = 0;
                    if (depth == 0)
                        yield break;

                    depth--;
                    indices[depth]++;
                }

                depth = rank - 1;
            }
        }
    }
}

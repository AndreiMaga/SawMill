using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FIPF
{
    public class BoyerMooreBinarySearch
    {
        private readonly long[] mBadCharacterShift;
        private readonly long[] mGoodSuffixShift;
        private readonly long[] mSuffixes;
        private readonly byte[] mSearchPattern;


        public BoyerMooreBinarySearch(byte[] searchPattern)
        {
            if ((searchPattern == null) ||
                !searchPattern.Any())
            {
                throw new ArgumentNullException(nameof(searchPattern));
            }

            /* Preprocessing */
            mSearchPattern = searchPattern;
            mBadCharacterShift = BuildBadCharacterShift(searchPattern);
            mSuffixes = FindSuffixes(searchPattern);
            mGoodSuffixShift = BuildGoodSuffixShift(searchPattern, mSuffixes);
        }



        public ReadOnlyCollection<long> GetMatchIndexes(byte[] dataToSearch)
        {
            return new ReadOnlyCollection<long>(GetMatchIndexes_Internal(dataToSearch));
        }

        public ReadOnlyCollection<long> GetMatchIndexes(FileInfo fileToSearch, int bufferSize = 1024 * 1024)
        {
            var matchIndexes = new List<long>();

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, @"Size of the file buffer must be greater than zero.");
            }
            int maxBufferSizeAllowed = (int.MaxValue - (mSearchPattern.Length - 1));
            if (bufferSize > maxBufferSizeAllowed)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, string.Format("Size of the file buffer ({0}) plus the size of the search pattern minus one ({1}) may not exceed Int32.MaxValue ({2}).", bufferSize, (mSearchPattern.Length - 1), int.MaxValue));
            }

            if ((fileToSearch != null) &&
                fileToSearch.Exists)
            {
                using FileStream stream = fileToSearch.OpenRead();
                if (!stream.CanSeek)
                {
                    throw new Exception(string.Format("The file '{0}' is not seekable!  Search cannot be performed.", fileToSearch));
                }

                int chunkIndex = 0;
                while (true)
                {
                    byte[] fileData = GetNextChunkForSearch(stream, chunkIndex, bufferSize);
                    if ((fileData != null) &&
                        fileData.Any())
                    {
                        List<long> occuranceIndexes = GetMatchIndexes_Internal(fileData);
                        if (occuranceIndexes != null)
                        {
                            int bufferOffset = (bufferSize * chunkIndex);
                            matchIndexes.AddRange(occuranceIndexes.Select(bufferMatchIndex => (bufferMatchIndex + bufferOffset)));
                        }
                    }
                    else
                    {
                        break;
                    }
                    chunkIndex++;
                }
            }

            return new ReadOnlyCollection<long>(matchIndexes);
        }



        private static long[] BuildBadCharacterShift(byte[] pattern)
        {
            var badCharacterShift = new long[256];
            long patternLength = Convert.ToInt64(pattern.Length);

            for (long c = 0; c < Convert.ToInt64(badCharacterShift.Length); ++c)
            {
                badCharacterShift[c] = patternLength;
            }

            for (long i = 0; i < patternLength - 1; ++i)
            {
                badCharacterShift[pattern[i]] = patternLength - i - 1;
            }

            return badCharacterShift;
        }

        private static long[] FindSuffixes(byte[] pattern)
        {
            long f = 0;

            var patternLength = Convert.ToInt64(pattern.Length);
            var suffixes = new long[pattern.Length + 1];

            suffixes[patternLength - 1] = patternLength;
            long g = patternLength - 1;
            for (long i = patternLength - 2; i >= 0; --i)
            {
                if (i > g && suffixes[i + patternLength - 1 - f] < i - g)
                {
                    suffixes[i] = suffixes[i + patternLength - 1 - f];
                }
                else
                {
                    if (i < g)
                    {
                        g = i;
                    }
                    f = i;
                    while (g >= 0 && (pattern[g] == pattern[g + patternLength - 1 - f]))
                    {
                        --g;
                    }
                    suffixes[i] = f - g;
                }
            }

            return suffixes;
        }

        private static long[] BuildGoodSuffixShift(byte[] pattern, long[] suff)
        {
            var patternLength = Convert.ToInt64(pattern.Length);
            var goodSuffixShift = new long[pattern.Length + 1];

            for (long i = 0; i < patternLength; ++i)
            {
                goodSuffixShift[i] = patternLength;
            }

            long j = 0;
            for (long i = patternLength - 1; i >= -1; --i)
            {
                if (i == -1 || suff[i] == i + 1)
                {
                    for (; j < patternLength - 1 - i; ++j)
                    {
                        if (goodSuffixShift[j] == patternLength)
                        {
                            goodSuffixShift[j] = patternLength - 1 - i;
                        }
                    }
                }
            }

            for (long i = 0; i <= patternLength - 2; ++i)
            {
                goodSuffixShift[patternLength - 1 - suff[i]] = patternLength - 1 - i;
            }

            return goodSuffixShift;
        }

        private byte[] GetNextChunkForSearch(Stream stream, int chunkIndex, int fileSearchBufferSize)
        {
            byte[] chunk = null;

            long fileStartIndex = Convert.ToInt64(chunkIndex) * Convert.ToInt64(fileSearchBufferSize);
            if (fileStartIndex < stream.Length)
            {
                stream.Seek(fileStartIndex, SeekOrigin.Begin);

                int searchBytesLength = mSearchPattern.Length;

                int bufferSize = fileSearchBufferSize + (searchBytesLength - 1);
                var buffer = new byte[bufferSize];
                long numBytesRead = Convert.ToInt64(stream.Read(buffer, 0, bufferSize));

                if (numBytesRead >= searchBytesLength)
                {
                    if (numBytesRead < bufferSize)
                    {
                        chunk = new byte[numBytesRead];
                        Array.Copy(buffer, chunk, numBytesRead);
                    }
                    else
                    {
                        chunk = buffer;
                    }
                }

            }

            return chunk;
        }

        private List<long> GetMatchIndexes_Internal(byte[] dataToSearch)
        {
            var matchIndexes = new List<long>();

            if (dataToSearch == null)
            {
                throw new ArgumentNullException(nameof(dataToSearch));
            }

            long patternLength = Convert.ToInt64(mSearchPattern.Length);
            long textLength = Convert.ToInt64(dataToSearch.Length);

            /* Searching */
            long index = 0;
            while (index <= (textLength - patternLength))
            {
                long unmatched;
                for (unmatched = patternLength - 1; unmatched >= 0 && (mSearchPattern[unmatched] == dataToSearch[unmatched + index]); --unmatched)
                {
                }

                if (unmatched < 0)
                {
                    matchIndexes.Add(index);
                    index += mGoodSuffixShift[0];
                }
                else
                {
                    index += Math.Max(mGoodSuffixShift[unmatched], mBadCharacterShift[dataToSearch[unmatched + index]] - patternLength + 1 + unmatched);
                }
            }

            return matchIndexes;
        }
    }
}

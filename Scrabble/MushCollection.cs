namespace Scrabble
{
    /// <summary>
    /// Collection of mushes. Eg all 3 letter mushes
    /// </summary>
    class MushCollection
    {
        private List<Mush> mushes;

        public MushCollection()
        {
            mushes = new List<Mush>();
        }

        public void LoadMushes(string path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                byte[] bytes;

                while ((bytes = reader.ReadBytes(Mush.byteCount)).Length == Mush.byteCount)
                {
                    mushes.Add(new Mush(bytes));
                }
            }
        }

        public void FindMushMatches(Mush mushToMatch, List<Mush> mushesFound, Int16[]? lettersMask)
        {
            // If we have any letters, just check every word
            if (mushToMatch.HasAnyLetters())
            {
                for (int i = 0; i < mushes.Count; i++)
                {
                    AddIfValidAmounts(mushToMatch, mushes[i], mushesFound, lettersMask);
                }
            }
            // No any letters, use a smarter approach
            else
            {
                int itemIndex = mushes.BinarySearch(mushToMatch);

                //  This mush is in the list of known mushes
                if (itemIndex >= 0)
                {
                    AddIfValidAmounts(mushToMatch, mushes[itemIndex], mushesFound, lettersMask);

                    //  Loop backwards and forwards to catch any other matching mushes
                    //  Loop backwards 
                    for (int i = itemIndex - 1; i >= 0 && mushes[i].CompareTo(mushToMatch) == 0; i--)
                    {
                        AddIfValidAmounts(mushToMatch, mushes[i], mushesFound, lettersMask);
                    }

                    //  Loop forwards
                    for (int i = itemIndex + 1; i < mushes.Count && mushes[i].CompareTo(mushToMatch) == 0; i++)
                    {
                        AddIfValidAmounts(mushToMatch, mushes[i], mushesFound, lettersMask);
                    }
                }
            }
        }

        private void AddIfValidAmounts(Mush mushToMatch, Mush matchedMush, List<Mush> mushesFound, Int16[]? lettersMask)
        {
            if (mushToMatch.HasCorrectLetters(matchedMush) && (lettersMask == null || matchedMush.HasCorrectMasks(lettersMask)))
            {
                mushesFound.Add(matchedMush);
            }
        }

        public void FindPatternMatches(Mush patternMush, List<Mush> mushesFound)
        {
            for (int i = 0; i < mushes.Count; i++)
            {
                if (mushes[i].HasMatchingPatterns(patternMush))
                {
                    mushesFound.Add(mushes[i]);
                }
            }
        }
    }
}
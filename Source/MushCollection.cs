namespace Source
{
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

        public void FindMushMatches(Mush mushToMatch, List<Mush> mushesFound)
        {
            int itemIndex = mushes.BinarySearch(mushToMatch);

            //  This mush is in the list of known mushes
            if (itemIndex >= 0)
            {
                AddIfValidAmounts(mushToMatch, mushes[itemIndex], mushesFound);

                //  Loop backwards and forwards to catch any other matching mushes
                //  Loop backwards 
                for (int i = itemIndex - 1; i >= 0 && mushes[i].CompareTo(mushToMatch) == 0; i--)
                {
                    AddIfValidAmounts(mushToMatch, mushes[i], mushesFound);
                }

                //  Loop forwards
                for (int i = itemIndex + 1; i < mushes.Count && mushes[i].CompareTo(mushToMatch) == 0; i++)
                {
                    AddIfValidAmounts(mushToMatch, mushes[i], mushesFound);
                }
            }
        }

        private void AddIfValidAmounts(Mush mustToMatch, Mush matchedMush, List<Mush> mushesFound)
        {
            if (mustToMatch.HasCorrectLetters(matchedMush))
            {
                mushesFound.Add(matchedMush);
            }
        }
    }
}
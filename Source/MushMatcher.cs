namespace Source
{
    class MushMatcher
    {
        Dictionary<int, MushCollection> mushLists;
        Config config;

        public MushMatcher(Config config)
        {
            this.config = config;

            mushLists = new Dictionary<int, MushCollection>();
        }

        public void FindMatches(Mush mushToMatch, List<Mush> mushes)
        {
            int length = mushToMatch.length;

            if (!mushLists.ContainsKey(length))
            {
                LoadMushCollection(length);
            }

            mushLists[length].FindMushMatches(mushToMatch, mushes);
        }

        public List<string> FindMatchStrings(string word, bool findSubMatches)
        {
            List<Mush> mushes = new List<Mush>();

            if (findSubMatches)
            {
                foreach (string subWord in GetWordCombinations(word))
                {
                    FindMatches(new Mush(subWord), mushes);
                }
            }
            else
            {
                FindMatches(new Mush(word), mushes);
            }

            List<string> mushNames = new List<string>(mushes.Count);

            foreach (Mush mush in mushes)
            {
                mushNames.Add(mush.ToString());
            }

            return mushNames;
        }

        private void LoadMushCollection(int mushGroup)
        {
            //  Create a new mush collection, and load it in
            MushCollection collection = new MushCollection();
            collection.LoadMushes(config.path + config.mushGroupPrefix + mushGroup);

            mushLists.Add(mushGroup, collection);
        }

        //  Probably smarter ways exist of getting the max mask and building the strings, but im happy
        private ICollection<string> GetWordCombinations(string word)
        {
            //  Set to prevent the same sub string being considered twice
            HashSet<string> combinations = new HashSet<string>();

            //  Sort the word. Otherwise if it contains duplicate letters we can get unique strings which have the same letters
            char[] wordChars = word.ToCharArray();
            Array.Sort(wordChars);

            //  Generate combinations of letters from a bit mask
            //  Max mask represents all characters being selected
            int maxMask = 0;
            for (int i = 0; i < wordChars.Length; i++)
            {
                maxMask |= 1 << i;
            }

            //  Loop until that max mask to generate all the possible string combinations
            for (int i = 1; i <= maxMask; i++)
            {
                string temp = "";
                for (int j = 0; j < wordChars.Length; j++)
                {
                    if (((i >> j) & 1) == 1)
                    {
                        temp += wordChars[j];
                    }
                }

                combinations.Add(temp);
            }

            return combinations;
        }
    }
}
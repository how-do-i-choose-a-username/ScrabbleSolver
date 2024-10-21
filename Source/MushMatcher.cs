namespace Source
{
    class MushMatcher
    {
        // Dictionary<int, MushCollection> mushLists;
        List<MushCollection> mushLists;
        Config config;

        public MushMatcher(Config config)
        {
            this.config = config;

            mushLists = new List<MushCollection>();
        }

        private void FindMatches(Mush mushToMatch, List<Mush> mushes, Int16[]? lettersMask)
        {
            int length = mushToMatch.length;

            mushLists[length - 1].FindMushMatches(mushToMatch, mushes, lettersMask);
        }

        public List<string> FindMatchStrings(string word, bool findSubMatches, Int16[]? lettersMask = null)
        {
            CheckLoadedMushCollections(word.Length);

            List<Mush> mushes = new List<Mush>();

            if (findSubMatches)
            {
                foreach (string subWord in GetWordCombinations(word))
                {
                    FindMatches(new Mush(subWord), mushes, lettersMask);
                }
            }
            else
            {
                FindMatches(new Mush(word), mushes, lettersMask);
            }

            List<string> mushNames = new List<string>(mushes.Count);

            foreach (Mush mush in mushes)
            {
                mushNames.Add(mush.ToString());
            }

            return mushNames;
        }

        public bool HasExactWord(string word)
        {
            CheckLoadedMushCollections(word.Length);

            List<Mush> mushes = new List<Mush>();
            FindMatches(new Mush(word), mushes, null);

            bool foundWord = false;
            for (int i = 0; i < mushes.Count && !foundWord; i++)
            {
                foundWord = mushes[i].ToString() == word;
            }

            return foundWord;
        }

        /// <summary>
        /// Check we have the correct mush lists loaded, if not then lazy load the list
        /// </summary>
        /// <param name="length">Maximum mush length that needs to be loaded</param>
        private void CheckLoadedMushCollections(int length) {
            if (length <= Mush.maxWordLength && mushLists.Count < length) {
                for (int i = mushLists.Count; i < length; i++) {
                    LoadMushCollection(i + 1);
                }
            }
        }

        private void LoadMushCollection(int mushGroup)
        {
            //  Create a new mush collection, and load it in
            MushCollection collection = new MushCollection();
            collection.LoadMushes(config.path + config.mushGroupPrefix + mushGroup);

            mushLists.Add(collection);
        }

        public static Dictionary<int, ICollection<string>> WordCombinationsByCount(string letters)
        {
            ICollection<string> combos = GetWordCombinations(letters);

            Dictionary<int, ICollection<string>> result = new Dictionary<int, ICollection<string>>();

            foreach (string str in combos)
            {
                int strLength = str.Length;
                if (!result.ContainsKey(strLength))
                {
                    result.Add(strLength, new List<string>());
                }

                result[strLength].Add(str);
            }

            return result;
        }

        //  Probably smarter ways exist of getting the max mask and building the strings, but im happy
        private static ICollection<string> GetWordCombinations(string word)
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
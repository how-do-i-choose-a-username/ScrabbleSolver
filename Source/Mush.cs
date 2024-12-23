namespace Source
{
    /// <summary>
    /// Class to convert strings to mush, and back out again
    /// The idea is that a word with a maximum length of 16 characters can be encoded,
    /// such that it can easily be checked if it contains certain letters or not
    /// </summary>
    class Mush : IComparable
    {
        public static readonly int maxWordLength = 15;
        //  Byte count is 4 for characters, plus 26 times 2 for letters
        public static readonly int byteCount = 83;
        public static readonly int letterCount = 26;

        //  Indicates which letters are in this mush
        //  Uses least significant bit to indicate mush has character 'a'
        //  Then works its way up to 'z'. Some bits unused
        public Int32 key { get; internal set; }
        //  Has a length of 26, one for each letter. 
        //  Each int stores the bit positions of that letter in the word, but reversed
        //  Eg. 'aardvark' would become '00100011' (truncated) and so on
        private Int16[] letters;

        // Which of the mush letters are the mystical any letter tile
        // Used when the user provides a blank scrabble tile
        // Not presently stored or read from binary data
        private Int16 anyLetter;

        // How many any letters there are
        private byte anyLetterCount;

        //  Indicates that this mush is invalid for some reason
        private bool badMush = false;

        //  Overall length of word represented by the mush
        public byte length { get; internal set; }

        //  How many of each letter there is
        private byte[] letterAmounts;

        /// <summary>
        /// Load an existing mush from its binary data
        /// </summary>
        /// <param name="bytes">Bytes containing mush data</param>
        public Mush(byte[] bytes)
        {
            //  Must have bytes passed in, in the following specific order
            //  4 bytes key, 1 byte length, 52 bytes letters, 26 bytes letterAmounts
            letters = new Int16[letterCount];
            letterAmounts = new byte[letterCount];

            if (bytes.Length != byteCount)
            {
                badMush = true;
            }
            else
            {
                key = BitConverter.ToInt32(bytes, 0);
                length = bytes[4];

                for (int i = 0; i < letters.Length; i++)
                {
                    letters[i] = BitConverter.ToInt16(bytes, 5 + i * 2);

                    letterAmounts[i] = bytes[57 + i];
                }
            }

            anyLetter = 0;
            anyLetterCount = 0;
        }

        /// <summary>
        /// Generate a new mush from a given string
        /// </summary>
        /// <param name="word">The string to mush</param>
        public Mush(string word)
        {
            if (word.Length > maxWordLength)
            {
                word = word.Substring(0, maxWordLength);
                badMush = true;
            }

            key = GetCharactersInt(word);

            letters = GetLettersArray(word);

            letterAmounts = GetLetterAmountsArray(word);

            anyLetter = GetAnyLetters(word);

            anyLetterCount = GetAnyLettersCount(word);

            length = (byte)word.Length;
        }

        /// <summary>
        /// Generate a key to describe the letters in this mush
        /// </summary>
        /// <param name="word">String to use</param>
        /// <returns>The calculated key</returns>
        private Int32 GetCharactersInt(string word)
        {
            Int32 result = 0;

            for (char letter = 'a'; letter <= 'z'; letter++)
            {
                if (word.Contains(letter))
                {
                    result |= 1 << (letter - 'a');
                }
            }

            return result;
        }

        /// <summary>
        /// Produce an array of ints. Each int stores the locations of a letter in the mush
        /// </summary>
        /// <param name="word">Source word</param>
        /// <returns>Array of ints storing letter locations</returns>
        public static Int16[] GetLettersArray(string word)
        {
            Int16[] letters = new Int16[letterCount];

            for (int i = 0; i < word.Length; i++)
            {
                char currentLetter = word[i];

                if (currentLetter >= 'a' && currentLetter <= 'z')
                {
                    letters[currentLetter - 'a'] |= (Int16)(1 << i);
                }
            }

            return letters;
        }

        /// <summary>
        /// Generate an array of values to store how often each letter appears in the mush
        /// </summary>
        /// <param name="word">Source word</param>
        /// <returns>Array of letter counts</returns>
        private byte[] GetLetterAmountsArray(string word)
        {
            byte[] result = new byte[letterCount];

            for (int i = 0; i < word.Length; i++)
            {
                char currentLetter = word[i];

                if (currentLetter >= 'a' && currentLetter <= 'z')
                {
                    result[currentLetter - 'a'] += 1;
                }
                else
                {
                    badMush = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Generate the bit field which indicates where the any letters are
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static Int16 GetAnyLetters(string word)
        {
            Int16 letters = 0;

            for (int i = 0; i < word.Length; i++)
            {
                char currentLetter = word[i];

                if (currentLetter == MushMatcher.BLANK_TILE_INDICATOR)
                {
                    letters |= (Int16)(1 << i);
                }
            }

            return letters;
        }

        /// <summary>
        /// How many any letters are in this mush
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static byte GetAnyLettersCount(string word)
        {
            byte count = 0;

            for (int i = 0; i < word.Length; i++)
            {
                char currentLetter = word[i];

                if (currentLetter == MushMatcher.BLANK_TILE_INDICATOR)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Convert this mush back to a string for easier reading
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            char[] result = new char[maxWordLength];
            for (int i = 0; i < result.Length; i++) { result[i] = ' '; }

            //  Loop over each of the letter containers
            for (int i = 0; i < letters.Length; i++)
            {
                //  Then loop over the letters in those containers
                for (int j = 0; j < maxWordLength; j++)
                {
                    if (((letters[i] >>> j) & 1) != 0)
                    {
                        result[j] = (char)(i + 'a');
                    }
                }
            }

            //  Also loop over the any letters and include those
            for (int j = 0; j < maxWordLength; j++)
            {
                if (((anyLetter >>> j) & 1) != 0)
                {
                    result[j] = '_';
                }
            }

            return new string(result).Trim();
        }

        /// <summary>
        /// Pcak the mush into a byte array for writing to disk
        /// </summary>
        /// <returns>Byte array of the mush</returns>
        public byte[] ToBytes()
        {
            if (badMush)
            {
                Console.WriteLine("Converting the bad mush '" + ToString() + "' to bytes, this is a problem.");
            }

            byte[] result = new byte[byteCount];

            byte[] keyBytes = BitConverter.GetBytes(key);

            for (int i = 0; i < keyBytes.Length; i++)
            {
                result[i] = keyBytes[i];
            }

            result[4] = length;

            for (int i = 0; i < letters.Length; i++)
            {
                byte[] lettersBytes = BitConverter.GetBytes(letters[i]);

                result[5 + i * 2] = lettersBytes[0];
                result[5 + i * 2 + 1] = lettersBytes[1];
            }

            for (int i = 0; i < letterAmounts.Length; i++)
            {
                result[57 + i] = letterAmounts[i];
            }

            return result;
        }

        /// <summary>
        /// Use the key to find matching mushes. This ignores letter ordering to catch all words. 
        /// Also ignores letter count which is caught later
        /// </summary>
        /// <param name="obj">Object to compare, should be a mush</param>
        /// <returns>Integer result from comparing the keys</returns>
        public int CompareTo(object? obj)
        {
            int result = 0;

            // If we have any blank letters match all words
            // TODO Find a smarter way of implementing this

            // The words are sorted to allow faster finding of words with relevant letters
            if (anyLetterCount == 0 && obj is Mush)
            {
                result = key.CompareTo(((Mush)obj).key);
            }

            return result;
        }

        //  Return true if both mushes have the same length, and the same letters
        //  Otherwise false
        //  This supports blank letters only if this instance of the object is the only one with blank letters
        public bool HasCorrectLetters(Mush mushToCheck)
        {
            bool enough = true;

            if (mushToCheck.length == length)
            {
                int availableAnyLetters = anyLetter;

                for (int i = 0; i < letterCount && enough; i++)
                {
                    int checkAmount = mushToCheck.letterAmounts[i];
                    int ourAmount = letterAmounts[i];

                    // Perfect letter count match, we are happy
                    if (checkAmount == ourAmount)
                    {
                        enough = true;
                    }
                    // Target has less than we do, fail
                    else if (checkAmount < ourAmount)
                    {
                        enough = false;
                    }
                    // Target has more than us, but use our blank tiles to make the difference
                    else if (checkAmount <= ourAmount + availableAnyLetters)
                    {
                        enough = true;
                        availableAnyLetters -= checkAmount - ourAmount;
                    }
                    // Target has more than us and our any letters will allow
                    else
                    {
                        enough = false;
                    }
                }
            }
            else
            {
                enough = false;
            }

            return enough;
        }

        public bool HasCorrectMasks(Int16[] masksToCheck)
        {
            bool matchesMask = true;

            for (int i = 0; i < masksToCheck.Length && matchesMask; i++)
            {
                // If the incoming mask has a bit set the existing mask must not be 0 when applied
                if (masksToCheck[i] != 0)
                {
                    matchesMask = matchesMask && (masksToCheck[i] & letters[i]) == masksToCheck[i];
                }
            }

            return matchesMask;
        }
    }
}
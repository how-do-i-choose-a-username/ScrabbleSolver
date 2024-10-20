namespace Source
{
    //  Class to convert strings to mush, and back out again
    //  The idea is that a word with a maximum length of 16 characters can be encoded,
    //  such that it can easily be checked if it contains certain letters or not
    class Mush : IComparable
    {
        private static readonly int maxWordLength = 16;
        //  Byte count is 4 for characters, plus 26 times 2 for letters
        public static readonly int byteCount = 83;
        public static readonly int letterCount = 26;

        //  Uses least significant bit to indicate mush has character 'a'
        //  Then works its way up to 'z'. Some bits unused
        private Int32 key;
        //  Has a length of 26, one for each letter. 
        //  Each int stores the bit positions of that letter in the word, but reversed
        //  Eg. 'aardvark' would become '00100011' (truncated) and so on
        private Int16[] letters;

        //  Indicates that this mush is invlaid for some reason
        private bool badMush = false;

        //  Overall length of mush
        public byte length { get; internal set; }

        //  How many of each letter there is
        private byte[] letterAmounts;

        //  Must have bytes passed in, in the following specific order
        //  4 bytes key, 1 byte length, 52 bytes letters, 26 bytes letterAmounts
        public Mush(byte[] bytes)
        {
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
        }

        public Mush(string word)
        {
            if (word.Length > maxWordLength)
            {
                word = word.Substring(0,maxWordLength);
                badMush = true;
            }

            key = GetCharactersInt(word);

            letters = GetLettersArray(word);

            letterAmounts = GetLetterAmountsArray(word);

            length = (byte)word.Length;
        }

        private Int32 GetCharactersInt(string word)
        {
            Int32 result = 0;

            for (char letter = 'a'; letter <= 'z'; letter ++)
            {
                if (word.Contains(letter))
                {
                    result |= (1 << letter);
                }
            }

            return result;
        }

        private Int16[] GetLettersArray(string word)
        {
            Int16[] letters = new Int16[letterCount];

            for (int i = 0; i < word.Length; i++)
            {
                char currentLetter = word[i];

                if (currentLetter < 'a' || currentLetter > 'z')
                {
                    badMush = true;
                }
                else
                {
                    letters[currentLetter - 'a'] |= (Int16)(1 << i);
                }
            }

            return letters;
        }

        private byte[] GetLetterAmountsArray(string word)
        {
            byte[] result = new byte[letterCount];

            for (int i = 0; i < word.Length; i++)
            {
                char currentLetter = word[i];

                if (currentLetter > 'a' && currentLetter < 'z')
                {
                    result[currentLetter - 'a'] += 1;
                }
            }

            return result;
        }

        public Int32 GetCharactersCode()
        {
            return key;
        }

        public override string ToString()
        {
            char[] result = new char[maxWordLength];
            for (int i = 0; i < result.Length; i ++) { result[i] = ' '; }

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

            return new string(result).Trim();
        }

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

        public int CompareTo(object? obj)
        {
            int result = 0;

            if (obj is Mush)
            {
                result = key.CompareTo(((Mush)obj).key);
            }

            return result;
        }

        //  Return true if both mushes have the same length, and the same letters
        //  Otherwise false
        public bool HasCorrectLetters(Mush mushToCheck)
        {
            bool enough = true;

            if (mushToCheck.length == length)
            {
                for (int i = 0; i < letterCount; i++)
                {
                    if (mushToCheck.letterAmounts[i] != letterAmounts[i])
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
    }
}
using Source;

namespace Scrabble
{
    enum PowerUp { None, DoubleLetter, TripleLetter, DoubleWord, TripleWord }

    // Logic for the solver is as follows
    // Generate all possible valid word positions
    // Find words that could fit in that position
    // Validate those words against all other letters
    // Score the remaining words

    public class ScrabbleGame
    {
        private static readonly int boardDimensions = 15;
        private static readonly char defaultBoardChar = ' ';

        //  Both of these are x,y with 0,0 at the top left
        private PowerUp[,] powerUps;
        private char[,] lettersOnBoard;

        private Config config;

        private char CharAtTile(Coord coord) => CharAtTile(coord.x, coord.y);
        private char CharAtTile(int x, int y) => lettersOnBoard[x, y];

        private bool TileIsBlank(Coord coord) => TileIsBlank(coord.x, coord.y);
        private bool TileIsBlank(int x, int y) => CharAtTile(x, y) == defaultBoardChar;

        private bool TileOnBoard(Coord coord) => TileOnBoard(coord.x, coord.y);
        private bool TileOnBoard(int x, int y) => x >= 0 && y >= 0 && x < boardDimensions && y < boardDimensions;

        public ScrabbleGame(Config config)
        {
            this.config = config;

            powerUps = new PowerUp[boardDimensions, boardDimensions];
            lettersOnBoard = new char[boardDimensions, boardDimensions];

            for (int i = 0; i < boardDimensions; i++)
            {
                for (int j = 0; j < boardDimensions; j++)
                {
                    lettersOnBoard[i, j] = defaultBoardChar;
                }
            }
        }

        public void LoadScrabblePowerUps(string powerUpsPath)
        {
            using (var streamReader = new StreamReader(File.OpenRead(powerUpsPath)))
            {
                int i = 0;
                int character;
                while ((character = streamReader.Read()) > 0)
                {
                    //  If its a newline character, just skip it
                    if (character != '\n')
                    {
                        PowerUp powerUp = PowerUp.None;

                        switch (character)
                        {
                            case 'd':
                                powerUp = PowerUp.DoubleLetter;
                                break;
                            case 't':
                                powerUp = PowerUp.TripleLetter;
                                break;
                            case 'D':
                                powerUp = PowerUp.DoubleWord;
                                break;
                            case 'T':
                                powerUp = PowerUp.TripleWord;
                                break;
                            default:
                                break;
                        }

                        powerUps[i % boardDimensions, i / boardDimensions] = powerUp;

                        i += 1;
                    }
                }
            }
        }

        public void LoadScrabbleLetters(string path)
        {
            using (var streamReader = new StreamReader(File.OpenRead(path)))
            {
                int lineNumber = 0;
                String? line;
                while ((line = streamReader.ReadLine()) != null && lineNumber < boardDimensions)
                {
                    line = line.ToLower();

                    for (int i = 0; i < line.Length && i < boardDimensions; i++)
                    {
                        if (line[i] >= 'a' && line[i] <= 'z')
                        {
                            lettersOnBoard[i, lineNumber] = line[i];
                        }
                    }

                    lineNumber += 1;
                }
            }
        }

        /// <summary>
        /// Calculate the best possible placement of the provided letters
        /// </summary>
        /// <param name="letters">Input letters to use</param>
        public void SolveGame(string letters)
        {
            MushMatcher matcher = new MushMatcher(config);

            Dictionary<int, ICollection<string>> letterCombos = MushMatcher.WordCombinationsByCount(letters);

            List<ScrabbleSolution> solutions = new List<ScrabbleSolution>();

            for (int i = letters.Length; i > 0; i--)
            {
                List<WordPosition> positions = GenerateWordPositions(i);

                foreach (WordPosition wordPosition in positions)
                {
                    List<string> matches = new List<string>();

                    // Get the letters from that board position and process them
                    string lettersAtPosition = ExtractWordPositionFromBoard(wordPosition);
                    string rawBoardLetters = lettersAtPosition.Replace(defaultBoardChar.ToString(), "");

                    // No characters collected from the board, just find a word normally
                    if (string.IsNullOrEmpty(rawBoardLetters))
                    {
                        foreach (string letterCombo in letterCombos[i])
                        {
                            matches.AddRange(matcher.FindMatchStrings(letterCombo, false));
                        }
                    }
                    // Characters were found on the board and will need to be used as a filter
                    else
                    {
                        Int16[] lettersMask = Mush.GetLettersArray(lettersAtPosition);

                        foreach (string letterCombo in letterCombos[i])
                        {
                            matches.AddRange(matcher.FindMatchStrings(rawBoardLetters + letterCombo, false, lettersMask));
                        }
                    }

                    matches.Sort(new SortSizeLetters());

                    foreach (string match in matches)
                    {
                        if ((wordPosition.lettersUsed > 1 && ValidWordPlacement(matcher, wordPosition, match)) || 
                            (wordPosition.lettersUsed == 1 && ValidWordPlacement(matcher, wordPosition, match) && ValidWordPlacement(matcher, wordPosition.ThisWithOtherDirection(), match)))
                        {
                            solutions.Add(new ScrabbleSolution(match, wordPosition, rawBoardLetters));
                        }
                    }
                }
            }

            foreach (ScrabbleSolution solution in solutions)
            {
                string lettersUsed = solution.word;
                foreach (char character in solution.boardLetters)
                {
                    lettersUsed = lettersUsed.Remove(lettersUsed.IndexOf(character), 1);
                }
                Console.WriteLine("\nFound the word '" + solution.word + "' using the letters '" + lettersUsed + "'");
                OutputBoardToConsole(solution.position, solution.word);
            }

        }

        private List<WordPosition> GenerateWordPositions(int letterCount)
        {
            List<WordPosition> positions = new List<WordPosition>();

            for (int dir = 0; dir < Math.Min(2, letterCount); dir++)
            {
                for (int i = 0; i < boardDimensions; i++)
                {
                    int letterIndex = 0;
                    Coord[] placedLetters = new Coord[letterCount];

                    // Phase 1, place all letter positions on free tiles
                    for (int j = 0; j < boardDimensions && letterIndex < letterCount; j++)
                    {
                        int x, y;
                        if (dir == 0)
                        {
                            x = j;
                            y = i;
                        }
                        else
                        {
                            x = i;
                            y = j;
                        }
                        // If there is no letter on this tile its available
                        if (TileIsBlank(x, y))
                        {
                            placedLetters[letterIndex] = new Coord(x, y);
                            letterIndex++;
                        }
                    }

                    // Phase 2, shuffle the letter positions along to get word positions
                    // Ensure we actually placed all of the letters on this row
                    if (letterIndex == letterCount)
                    {
                        // Move the letter index back to the end of the array
                        // Letter index points to the last element of the word sequence
                        letterIndex--;

                        bool runLoop = true;
                        // Add word positions from the placed letters
                        do
                        {
                            // Get the next array index (looping around to the start)
                            int firstPosition = (letterIndex >= letterCount - 1) ? 0 : letterIndex + 1;
                            int length = placedLetters[letterIndex][dir] - placedLetters[firstPosition][dir];

                            // Check that the proposed word is a valid move to play i.e. touches other letters
                            bool wordIntersectsLetters = length > letterCount;
                            bool letterAfterWord = placedLetters[letterIndex][dir] + 1 < boardDimensions && !TileIsBlank(new Coord(placedLetters[letterIndex], dir, 1));
                            bool letterBeforeWord = placedLetters[firstPosition][dir] - 1 > 0 && !TileIsBlank(new Coord(placedLetters[firstPosition], dir, -1));
                            bool letterNextToWord = false;

                            // Check the letters next to this word until we find one with contents
                            if (letterCount == 1)
                            {
                                Coord side1Coord = new Coord(placedLetters[firstPosition], dir - 1, -1);
                                Coord side2Coord = new Coord(placedLetters[firstPosition], dir - 1, 1);

                                bool side1 = TileOnBoard(side1Coord) && !TileIsBlank(side1Coord);
                                bool side2 = TileOnBoard(side2Coord) && !TileIsBlank(side2Coord);

                                letterNextToWord = side1 || side2;
                            }
                            else
                            {
                                for (int j = firstPosition; j != letterIndex && !letterNextToWord; j = (j + 1) % letterCount)
                                {
                                    Coord side1Coord = new Coord(placedLetters[j], dir - 1, -1);
                                    Coord side2Coord = new Coord(placedLetters[j], dir - 1, 1);

                                    bool side1 = TileOnBoard(side1Coord) && !TileIsBlank(side1Coord);
                                    bool side2 = TileOnBoard(side2Coord) && !TileIsBlank(side2Coord);

                                    letterNextToWord = letterNextToWord || side1 || side2;
                                }
                            }

                            bool wordTouchesLetter = wordIntersectsLetters || letterAfterWord || letterBeforeWord || letterNextToWord;

                            if (wordTouchesLetter)
                            {
                                WordPosition initialPosition = new WordPosition(placedLetters[firstPosition], length, letterCount, (WordDirection)dir);

                                initialPosition = StretchWordPosToFill(initialPosition);

                                // TODO This is a bit of a bandaid fix, and will not consider single letter moves. This is only an issue on the first turn of play, and could be considered negligable
                                if (initialPosition.length > 0)
                                {
                                    positions.Add(initialPosition);
                                }
                            }

                            // Move a copy of the last letter until we find a suitable place for it
                            Coord lastLetterPos = placedLetters[letterIndex];
                            for (int j = lastLetterPos[dir] + 1; ; j++)
                            {
                                // Stopping condition for this loop. When it reaches the end also stop the do while loop
                                if (j >= boardDimensions)
                                {
                                    runLoop = false;
                                    break;
                                }

                                int x, y;
                                if (dir == 0)
                                {
                                    x = j;
                                    y = i;
                                }
                                else
                                {
                                    x = i;
                                    y = j;
                                }

                                if (TileIsBlank(x, y))
                                {
                                    placedLetters[firstPosition] = new Coord(x, y);
                                    letterIndex = firstPosition;
                                    // Once we have placed a letter break this loop and add a new word position
                                    break;
                                }
                            }
                        } while (runLoop);
                    }
                }
            }

            return positions;

        }

        private string ExtractWordPositionFromBoard(WordPosition wordPosition)
        {
            char[] characters = new char[wordPosition.length + 1];

            for (int i = 0; i <= wordPosition.length; i++)
            {
                characters[i] = CharAtTile(wordPosition.GetCoordAtIndex(i));
            }

            return new string(characters);
        }

        // Check all the by-words are valid if this word is placed at the given position 
        // (by-words, aka words that run perpendicular to this one)
        private bool ValidWordPlacement(MushMatcher matcher, WordPosition wordPosition, string word)
        {
            bool validWord = true;

            // Console.WriteLine("\nSource word " + word + "\n" + wordPosition.ToString());
            // OutputBoardToConsole(wordPosition, word);
            for (int i = 0; i <= wordPosition.length && validWord; i++)
            {
                Coord startCoord = wordPosition.GetCoordAtIndex(i);
                WordPosition newPosition = new WordPosition(startCoord, 1, 0, (int)wordPosition.wordDirection - 1);
                // Console.WriteLine("Initial new Word position "+ newPosition);
                newPosition = StretchWordPosToFill(newPosition);
                if (newPosition.length > 1)
                {
                    // Console.WriteLine("Modified Word position "+ newPosition);
                    string byWord = ExtractWordPositionFromBoard(newPosition);
                    // Console.WriteLine("Byword is " + byWord.Replace(defaultBoardChar, '.'));
                    byWord = byWord.Replace(defaultBoardChar, word[i]);
                    // Console.WriteLine("Modified byword is " + byWord);
                    validWord = validWord && matcher.HasExactWord(byWord);

                    // Console.WriteLine("This is a valid word " + validWord);
                }
            }

            return validWord;
        }

        // TODO This is causing issues where it stretches too far. It somehow manages to check 2 letters over instead of just 1
        private WordPosition StretchWordPosToFill(WordPosition initialPosition)
        {
            // Extend the word length to include all the letters after the word
            int j = initialPosition.length;
            bool running = true;
            while (running)
            {
                Coord coord = initialPosition.GetCoordAtIndex(j + 1);
                running = TileOnBoard(coord) && !TileIsBlank(coord);
                if (running)
                {
                    j++;
                }
            }
            initialPosition.length = j;

            // Move and extend the word position to include all the letters before the word too
            int k = 0;
            Coord newStart = initialPosition.start;
            running = true;
            while (running)
            {
                Coord coord = initialPosition.GetCoordAtIndex(-k - 1);
                running = TileOnBoard(coord) && !TileIsBlank(coord);
                if (running)
                {
                    k++;
                    newStart = coord;
                }
            }
            initialPosition.length = initialPosition.length + k;
            initialPosition.start = newStart;
            return initialPosition;
        }

        public void OutputBoardToConsole(WordPosition? wordPosition = null, string? word = null)
        {
            ConsoleColor defaultBackColour = Console.BackgroundColor;
            ConsoleColor defaultForeColour = Console.ForegroundColor;

            int charIndex = 0;

            for (int i = -1; i < boardDimensions; i++)
            {
                if (i >= 0)
                {
                    Console.Write(i.ToString().PadRight(2));
                }
                else
                {
                    Console.Write("  ");
                }

                for (int j = 0; j < boardDimensions; j++)
                {
                    if (i == -1)
                    {
                        Console.Write(j.ToString().PadRight(2));
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        switch (powerUps[j, i])
                        {
                            case PowerUp.None:
                                Console.BackgroundColor = ConsoleColor.White;
                                Console.ForegroundColor = ConsoleColor.Black;
                                break;
                            case PowerUp.DoubleLetter:
                                Console.BackgroundColor = ConsoleColor.Blue;
                                break;
                            case PowerUp.TripleLetter:
                                Console.BackgroundColor = ConsoleColor.DarkBlue;
                                break;
                            case PowerUp.DoubleWord:
                                Console.BackgroundColor = ConsoleColor.Red;
                                break;
                            case PowerUp.TripleWord:
                                Console.BackgroundColor = ConsoleColor.DarkRed;
                                break;
                        }

                        char toWrite = lettersOnBoard[j, i];

                        if ((toWrite == defaultBoardChar || word != null) && wordPosition != null)
                        {
                            bool writeWordChar = false;
                            WordPosition position = (WordPosition)wordPosition;
                            if (position.wordDirection == WordDirection.RIGHT && position.start.y == i)
                            {
                                if (position.start.x <= j && position.start.x + position.length >= j)
                                {
                                    writeWordChar = true;
                                }
                            }
                            else if (position.wordDirection == WordDirection.DOWN && position.start.x == j)
                            {
                                if (position.start.y <= i && position.start.y + position.length >= i)
                                {
                                    writeWordChar = true;
                                }
                            }

                            if (writeWordChar)
                            {
                                if (word != null)
                                {
                                    if (toWrite == defaultBoardChar)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                    }
                                    toWrite = word[charIndex];
                                    charIndex++;
                                }
                                else
                                {
                                    toWrite = '.';
                                }
                            }
                        }

                        Console.Write(toWrite);
                        Console.Write(' ');
                    }
                }

                Console.BackgroundColor = defaultBackColour;
                Console.ForegroundColor = defaultForeColour;
                Console.WriteLine();
            }

        }
    }
}
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

            for (int i = letters.Length; i > 0; i--)
            {
                List<WordPosition> positions = GenerateWordPositions(i);

                foreach (WordPosition wordPosition in positions)
                {
                    // OutputBoardToConsole(wordPosition);

                    List<string> matches = new List<string>();

                    // Get the letters from that board position and process them
                    string lettersAtPosition = ExtractWordPositionFromBoard(wordPosition);
                    string rawBoardLetters = lettersAtPosition.Replace(defaultBoardChar.ToString(),"");

                    // No characters collected from the board, just find a word normally
                    if (string.IsNullOrEmpty(rawBoardLetters))
                    {
                        matches = matcher.FindMatchStrings(letters, false);
                    }
                    // Characters were found on the board and will need to be used as a filter
                    else
                    {
                        Console.WriteLine("Using the following characters form the board: " + lettersAtPosition.Replace(" ", "."));
                        Int16[] lettersMask = Mush.GetLettersArray(lettersAtPosition);
                        matches = matcher.FindMatchStrings(letters + rawBoardLetters, false, lettersMask);
                    }

                    if (matches.Count > 0)
                    {
                        OutputBoardToConsole(wordPosition);

                        matches.Sort(new SortSizeLetters());

                        Console.WriteLine("Found the following matches:");
                        foreach (string match in matches)
                        {
                            Console.WriteLine(match);
                        }
                    }
                }
            }
        }

        // TODO Word positions need to include the letters before and after them
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
                                positions.Add(new WordPosition(placedLetters[firstPosition], length, letterCount, (WordDirection)dir));
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

        public void OutputBoardToConsole(WordPosition? wordPosition = null)
        {
            ConsoleColor defaultBackColour = Console.BackgroundColor;
            ConsoleColor defaultForeColour = Console.ForegroundColor;

            Console.WriteLine();

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

                        if (toWrite == defaultBoardChar && wordPosition != null)
                        {
                            WordPosition position = (WordPosition)wordPosition;
                            if (position.wordDirection == WordDirection.RIGHT && position.start.y == i)
                            {
                                if (position.start.x <= j && position.start.x + position.length >= j)
                                {
                                    toWrite = '.';
                                }
                            }
                            else if (position.wordDirection == WordDirection.DOWN && position.start.x == j)
                            {
                                if (position.start.y <= i && position.start.y + position.length >= i)
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
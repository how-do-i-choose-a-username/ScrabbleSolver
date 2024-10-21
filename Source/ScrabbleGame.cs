using System.Security.Cryptography.X509Certificates;
using Source;

namespace Scrabble
{
    enum PowerUp { None, DoubleLetter, TripleLetter, DoubleWord, TripleWord }

    public class ScrabbleGame
    {
        private static readonly int boardDimensions = 15;

        //  Both of these are x,y with 0,0 at the top left
        private PowerUp[,] powerUps;
        private char[,] lettersOnBoard;

        private Config config;

        private bool TileIsBlank(int x, int y) => lettersOnBoard[x, y] == ' ';

        public ScrabbleGame(Config config)
        {
            this.config = config;

            powerUps = new PowerUp[boardDimensions, boardDimensions];
            lettersOnBoard = new char[boardDimensions, boardDimensions];

            for (int i = 0; i < boardDimensions; i++)
            {
                for (int j = 0; j < boardDimensions; j++)
                {
                    lettersOnBoard[i, j] = ' ';
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

            List<string> matches = matcher.FindMatchStrings(letters, true);

            matches.Sort(new SortSizeLetters());

            Console.WriteLine("Found the following matches:");
            foreach (string match in matches)
            {
                Console.WriteLine(match);
            }

            List<WordPosition> positions = GenerateWordPositions(letters.Length);
            Console.WriteLine("Positions found: " + positions.Count);
            foreach (WordPosition pos in positions)
            {
                // Console.WriteLine(pos.ToString());
            }
        }

        private List<WordPosition> GenerateWordPositions(int letterCount)
        {
            List<WordPosition> positions = new List<WordPosition>();

            for (int i = 0; i < boardDimensions; i++)
            {
                int letterIndex = 0;
                Coord[] placedLetters = new Coord[letterCount];

                // Phase 1, place all letter positions on free tiles
                for (int j = 0; j < boardDimensions && letterIndex < letterCount; j++)
                {
                    // If there is no letter on this tile its available
                    if (TileIsBlank(j, i))
                    {
                        placedLetters[letterIndex] = new Coord(j, i);
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
                        // int firstPosition = (letterIndex == 0) ? letterCount - 1 : letterIndex - 1;
                        Console.Write("Array size " + placedLetters.Length + " letterindex " + letterIndex + " last position " + firstPosition + "   ");
                        int length = placedLetters[letterIndex].x - placedLetters[firstPosition].x + 1;
                        positions.Add(new WordPosition(placedLetters[firstPosition], length, letterCount, WordDirection.RIGHT));
                        Console.WriteLine(positions.Last().ToString());

                        // Move a copy of the last letter until we find a suitable place for it
                        Coord lastLetterPos = placedLetters[letterIndex];
                        for (int j = lastLetterPos.x + 1; ; j++)
                        {
                            // Stopping condition for this loop. When it reaches the end also stop the do while loop
                            if (j >= boardDimensions)
                            {
                                runLoop = false;
                                break;
                            }
                            else if (TileIsBlank(j, i))
                            {
                                placedLetters[firstPosition] = new Coord(j, i);
                                letterIndex = firstPosition;
                                // Once we have placed a letter break this loop and add a new word position
                                break;
                            }
                        }
                    } while (runLoop);
                }
            }

            return positions;
        }

        // Generate all possible valid word positions
        // Find words that could fit in that position
        // Validate those words against all other letters
        // Score the remaining words

        public void OutputBoardToConsole()
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

                        Console.Write(lettersOnBoard[j, i]);
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
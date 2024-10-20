using Source;

namespace Scrabble
{
    enum PowerUp { None, DoubleLetter, TrippleLetter, DoubleWord, TrippleWord }

    public class ScrabbleGame
    {
        private static readonly int boardDimensions = 15;

        //  Both of these are x,y with 0,0 at the top left
        private PowerUp[,] powerUps;
        private char[,] lettersOnBoard;

        private Config config;

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
                                powerUp = PowerUp.TrippleLetter;
                                break;
                            case 'D':
                                powerUp = PowerUp.DoubleWord;
                                break;
                            case 'T':
                                powerUp = PowerUp.TrippleWord;
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
                            lettersOnBoard[lineNumber, i] = line[i];
                        }
                    }

                    lineNumber += 1;
                }
            }
        }

        /// <summary>
        /// Calculate the best possible placement of the provided letters
        /// </summary>
        /// <param name="letters"></param>
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
        }

        public void OutputBoardToConsole()
        {
            ConsoleColor defaultColour = Console.BackgroundColor;

            Console.WriteLine();

            for (int i = 0; i < boardDimensions; i++)
            {
                for (int j = 0; j < boardDimensions; j++)
                {
                    Console.ForegroundColor = ConsoleColor.White;

                    switch (powerUps[i, j])
                    {
                        case PowerUp.None:
                            Console.BackgroundColor = ConsoleColor.White;
                            Console.ForegroundColor = ConsoleColor.Black;
                            break;
                        case PowerUp.DoubleLetter:
                            Console.BackgroundColor = ConsoleColor.Blue;
                            break;
                        case PowerUp.TrippleLetter:
                            Console.BackgroundColor = ConsoleColor.DarkBlue;
                            break;
                        case PowerUp.DoubleWord:
                            Console.BackgroundColor = ConsoleColor.Red;
                            break;
                        case PowerUp.TrippleWord:
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                            break;
                    }

                    Console.Write(lettersOnBoard[i, j]);
                    Console.Write(' ');
                }

                Console.BackgroundColor = defaultColour;
                Console.WriteLine();
            }

        }
    }
}
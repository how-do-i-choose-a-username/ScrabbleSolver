using Scrabble;

namespace Source
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = new Config();
            config.LoadConfig();

            //  Make sure that the length is appropriate to what we actually have as data
            if (args.Length == 1 && args[0].Length <= 15)
            {
                MushMatcher matcher = new MushMatcher(config);
                
                List<string> matches = matcher.FindMatchStrings(args[0], true);

                matches.Sort(new SortSizeLetters());

                Console.WriteLine("Found the following matches:");
                foreach (string match in matches)
                {
                    Console.WriteLine(match);
                }
            }
            else if (args.Length == 2)
            {
                Mushifier mushifier = new Mushifier();

                mushifier.MushifyDirectory(args[0], args[1]);
            }
            else if (args.Length == 3)
            {
                ScrabbleGame game = new ScrabbleGame(config);
                game.LoadScrabblePowerUps(config.powerUpsFile);

                game.LoadScrabbleLetters(args[0]);

                game.SolveGame(args[1]);
            }
            else
            {
                Console.WriteLine("Operating modes:");
                Console.WriteLine("Provide a character string (max length 15), and the program will search the mushes for a match.");
                Console.WriteLine("Provide two strings and it will mushify from the first directory to the second.");
                Console.WriteLine("Provide three strings to solve a game. The first string is the game board directory, the second is the letters to use, the third does nothing.");
            }
        }
    }
}
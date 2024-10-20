using Scrabble;

namespace Source
{
    class Program
    {
        static void Main(string[] args)
        {
            //  Make sure that the length is appropriate to what we actually have as data
            if (args.Length == 1 && args[0].Length <= 15)
            {
                Config config = new Config();
                config.LoadConfig();

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
                ScrabbleGame game = new ScrabbleGame();
                game.LoadScrabblePowerUps();

                game.LoadScrabbleLetters(args[0]);

                game.OutputBoardToConsole();
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Operating modes:");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine("Provide a character string (max length 15), and the program will search the mushes for a match.");
                Console.WriteLine("Provide two strings and it will mushify from the first directory to the second.");
            }
        }

        private class SortSizeLetters : IComparer<string>
        {
            int IComparer<string>.Compare(string? a, string? b)
            {
                int result;

                //  Make sure a and b are not null
                a = a == null ? a = "" : a;
                b = b == null ? b = "" : b;

                result = a.Length.CompareTo(b.Length);

                if (result == 0)
                {
                    result = a.CompareTo(b);
                }

                return result;
            }
        }
    }
}
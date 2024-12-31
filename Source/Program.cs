using Scrabble;

namespace Source
{
    public class Program
    {
        static void Main(string[] args)
        {
            Arguments arguments = new Arguments(args);

            if (arguments.showHelp)
            {
                Console.WriteLine(arguments.getHelpMessage());
            }
            else 
            {
                RunProgram(arguments);
            }
        }

        private static void RunProgram(Arguments args)
        {
            Config config = args.ReadConfig();

            // Mush the dictionary files for faster use
            if (config.MushifyDirectory())
            {
                Mushifier mushifier = new Mushifier();

                mushifier.MushifyDirectory(config.mushSourceDirectory, config.mushesDirectory);
            }

            if (config.SolveGame())
            {
                ScrabbleGame game = new ScrabbleGame(config);
                game.Load();

                game.SolveGame(config.letters.ToLower());
            }
            else if (config.FindWords())
            {
                string inputLetters = config.letters;
                inputLetters = inputLetters.ToLower();

                MushMatcher matcher = new MushMatcher(config);

                List<string> matches = matcher.FindMatchStrings(inputLetters, true);

                matches.Sort(new SortSizeLetters());

                Console.WriteLine("Found the following matches:");
                foreach (string match in matches)
                {
                    Console.WriteLine(match);
                }

                Console.WriteLine();
                if (matcher.HasExactWord(inputLetters))
                {
                    Console.WriteLine(inputLetters + " is a word");
                }
                else
                {
                    Console.WriteLine(inputLetters + " is not an exact word");
                }
                Console.WriteLine("Letter count: " + inputLetters.Length);
                Console.WriteLine(matches.Count + " possible words found");
            }
        }
    }
}
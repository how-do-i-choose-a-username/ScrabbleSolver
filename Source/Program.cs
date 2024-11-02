using Scrabble;
using CommandLine;

namespace Source
{
    class Program
    {
        public class Arguments
        {
            [Option('l', HelpText = "Letters to process to find words with")]
            public string Letters
            {
                get
                {
                    return lettersInternal;
                }
                set
                {
                    if (value.Length > 15)
                    {
                        lettersInternal = value.Substring(0, 15);
                    }
                    else
                    {
                        lettersInternal = value;
                    }
                }
            }

            private string lettersInternal = "";

            [Option('b', HelpText = "Path to Scrabble board to load")]
            public string Board { get; set; } = "";

            [Option('i', HelpText = "Input directory with files for mushifying")]
            public string InputDirectory { get; set; } = "";

            [Option('o', HelpText = "Output directory to write files after mushifying")]
            public string OutputDirectory { get; set; } = "";

            public bool FindWords() => Letters.Length > 0 && Board == "";
            public bool SolveGame() => Letters.Length > 0 && Board.Length > 0;
            public bool MushifyDirectory() => InputDirectory.Length > 0 && OutputDirectory.Length > 0;
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Arguments>(args).WithParsed<Arguments>(RunProgram);
        }

        private static void RunProgram(Arguments args)
        {
            Config config = new Config();
            config.LoadConfig();

            if (args.MushifyDirectory())
            {
                Mushifier mushifier = new Mushifier();

                mushifier.MushifyDirectory(args.InputDirectory, args.OutputDirectory);
            }

            if (args.FindWords())
            {
                string inputLetters = args.Letters;
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
            else if (args.SolveGame())
            {
                ScrabbleGame game = new ScrabbleGame(config);
                game.Load();

                game.LoadGameState(args.Board);

                game.SolveGame(args.Letters.ToLower());
            }
        }
    }
}
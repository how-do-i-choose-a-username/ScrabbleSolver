using System.Diagnostics;
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
        // Word must touch the centre tile if the board is blank
        private static readonly int boardCentreAxis = 7;
        private static readonly char defaultBoardChar = ' ';

        //  All of these are y,x with 0,0 at the top left
        //  It was supposed to be x,y but something got muddled and this is what works
        private PowerUp[,] powerUps;
        private char[,] lettersOnBoard;
        private bool[,] blankLettersOnBoard;

        // True if there is a letter anywhere on the board
        private bool boardHasContents;

        // Records how many points each letter is worth
        private Dictionary<char, int> letterToScore = new Dictionary<char, int>();

        // Records how many of each letter there is
        private Dictionary<char, int> letterCounts = new Dictionary<char, int>();

        private Config config;

        private bool logging;

        private char CharAtTile(Coord coord) => CharAtTile(coord.x, coord.y);
        private char CharAtTile(int x, int y) => lettersOnBoard[x, y];

        private bool TileOnBoardAndNotBlank(Coord coord) => TileOnBoard(coord) && !TileIsBlank(coord);

        private bool BlankLetterTile(Coord coord) => blankLettersOnBoard[coord.x, coord.y];

        private bool TileIsBlank(Coord coord) => TileIsBlank(coord.x, coord.y);
        private bool TileIsBlank(int x, int y) => CharAtTile(x, y) == defaultBoardChar;

        private bool TileOnBoard(Coord coord) => TileOnBoard(coord.x, coord.y);
        private bool TileOnBoard(int x, int y) => x >= 0 && y >= 0 && x < boardDimensions && y < boardDimensions;

        public ScrabbleGame(Config config)
        {
            this.config = config;

            powerUps = new PowerUp[boardDimensions, boardDimensions];
            lettersOnBoard = new char[boardDimensions, boardDimensions];
            blankLettersOnBoard = new bool[boardDimensions, boardDimensions];

            for (int i = 0; i < boardDimensions; i++)
            {
                for (int j = 0; j < boardDimensions; j++)
                {
                    lettersOnBoard[i, j] = defaultBoardChar;
                }
            }
        }

        public void Load()
        {
            LoadScrabblePowerUps(config.powerUpsFile);
            LoadLetterScores(config.letterScoresFile);
            LoadGameState(config.gameBoardFile);
        }

        public void LoadScrabblePowerUps(string powerUpsPath)
        {
            try
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
            catch
            {
                Console.WriteLine("Failed to load the letter and word multipliers. Please ensure the file path is set correctly and that the file is formatted correctly.");
            }
        }

        private void LoadLetterScores(string letterScorePath)
        {
            try
            {
                using (var streamReader = new StreamReader(File.OpenRead(letterScorePath)))
                {
                    string? line = "";
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(" ");

                        letterToScore.Add(parts[0][0], Convert.ToInt32(parts[1]));
                        letterCounts.Add(parts[0][0], Convert.ToInt32(parts[2]));
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to load the letter scores. Please ensure the file path is set correctly and that the file is formatted correctly.");
            }
        }

        private void LoadGameState(string path)
        {
            try
            {
                using (var streamReader = new StreamReader(File.OpenRead(path)))
                {
                    int lineNumber = 0;
                    String? line;
                    while ((line = streamReader.ReadLine()) != null && lineNumber < boardDimensions)
                    {
                        for (int i = 0; i < line.Length && i < boardDimensions; i++)
                        {
                            if (line[i] >= 'a' && line[i] <= 'z')
                            {
                                lettersOnBoard[i, lineNumber] = line[i];
                                boardHasContents = true;
                            }
                            else if (line[i] >= 'A' && line[i] <= 'Z')
                            {
                                lettersOnBoard[i, lineNumber] = (char)(line[i] - ('A' - 'a'));
                                blankLettersOnBoard[i, lineNumber] = true;
                                boardHasContents = true;
                            }
                        }

                        lineNumber += 1;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to load the initial game board. Please ensure the file path is set correctly and that the file is formatted correctly.");
            }
        }

        /// <summary>
        /// Calculate the best possible placement of the provided letters
        /// </summary>
        /// <param name="letters">Input letters to use</param>
        public void SolveGame(string letters)
        {
            long startTime = Stopwatch.GetTimestamp();

            MushMatcher matcher = new MushMatcher(config);

            Dictionary<int, ICollection<string>> letterCombos = MushMatcher.WordCombinationsByCount(letters);

            List<ScrabbleSolution> solutions = new List<ScrabbleSolution>();

            int emptyBagBonus = 0;
            if (config.EmptyTileBag())
            {
                emptyBagBonus = CalculateEmptyBagBonus(letters);
            }

            for (int i = letters.Length; i > 0; i--)
            {
                List<WordPosition> positions = GenerateWordPositions(i);

                foreach (WordPosition wordPosition in positions)
                {
                    logging = wordPosition.start == new Coord(0, 0) && wordPosition.length == 15;

                    List<string> matches = new List<string>();

                    // Get the letters from that board position and process them
                    string lettersAtPosition = ExtractWordPositionFromBoard(wordPosition);
                    string rawBoardLetters = lettersAtPosition.Replace(defaultBoardChar.ToString(), "");

                    if (logging)
                    {
                        Console.WriteLine("Selected letters from the board: " + lettersAtPosition.Replace(defaultBoardChar, '.'));
                    }

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
                            List<string> subMatches = matcher.FindMatchStrings(rawBoardLetters + letterCombo, false, lettersMask);
                            matches.AddRange(subMatches);

                            if (logging)
                            {
                                foreach (string match in subMatches)
                                {
                                    Console.WriteLine("Found the word: " + match);
                                }
                            }
                        }
                    }

                    foreach (string match in matches)
                    {
                        int score = CalculateScore(matcher, wordPosition, match);

                        if (logging)
                        {
                            Console.WriteLine("Score for word " + match + " is " + score);
                        }

                        if (score >= 0)
                        {
                            if (i >= 7)
                            {
                                score += 50;
                            }
                            
                            // If the tile bag is empty, add a bonus for using all letters
                            if (i == letters.Length && config.EmptyTileBag())
                            {
                                score += emptyBagBonus;
                            }

                            solutions.Add(new ScrabbleSolution(match, wordPosition, rawBoardLetters, score));
                        }
                    }
                }
            }

            TimeSpan calculationTime = Stopwatch.GetElapsedTime(startTime);
            long printStartTime = Stopwatch.GetTimestamp();

            solutions.Sort();

            for (int i = Math.Max(0, solutions.Count - 10); i < solutions.Count; i++)
            {
                ScrabbleSolution solution = solutions[i];
                // Recalculate which of the input letters are used for this word

                string lettersUsed = solution.word;
                foreach (char character in solution.boardLetters)
                {
                    lettersUsed = lettersUsed.Remove(lettersUsed.IndexOf(character), 1);
                }

                Console.WriteLine("\nFound the word '" + solution.word + "' using the letters '" + lettersUsed + "' with a score of " + solution.score);
                OutputBoardToConsole(solution.position, solution.word);
            }

            TimeSpan printTime = Stopwatch.GetElapsedTime(printStartTime);

            Console.WriteLine();
            Console.WriteLine("Found " + solutions.Count + " solutions for the letters " + letters + " on the given board");
            Console.WriteLine("Calculation time: " + calculationTime.TotalSeconds + "s");
            Console.WriteLine("Printing time: " + printTime.TotalSeconds + "s");
        }

        private List<WordPosition> GenerateWordPositions(int letterCount)
        {
            List<WordPosition> wordPositions;

            if (boardHasContents)
            {
                wordPositions = GenerateWordPositionsContents(letterCount);
            }
            else
            {
                wordPositions = GenerateWordPositionsNoContents(letterCount);
            }

            return wordPositions;
        }

        /// <summary>
        /// When the board has letters, find where on the board a word could be played
        /// </summary>
        /// <param name="letterCount"></param>
        /// <returns></returns>
        private List<WordPosition> GenerateWordPositionsContents(int letterCount)
        {
            List<WordPosition> positions = new List<WordPosition>();

            // Check both horizontal and vertical
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
                            int length = placedLetters[letterIndex][dir] - placedLetters[firstPosition][dir] + 1;

                            // Check that the proposed word is a valid move to play i.e. touches other letters
                            bool wordIntersectsLetters = length > letterCount;
                            bool letterAfterWord = placedLetters[letterIndex][dir] + 1 < boardDimensions && !TileIsBlank(new Coord(placedLetters[letterIndex], dir, 1));
                            bool letterBeforeWord = placedLetters[firstPosition][dir] - 1 > 0 && !TileIsBlank(new Coord(placedLetters[firstPosition], dir, -1));
                            bool letterNextToWord = false;

                            // Check the letters next to this word until we find one with contents
                            for (int j = firstPosition; !letterNextToWord; j = (j + 1) % letterCount)
                            {
                                Coord side1Coord = new Coord(placedLetters[j], dir - 1, -1);
                                Coord side2Coord = new Coord(placedLetters[j], dir - 1, 1);

                                bool side1 = TileOnBoardAndNotBlank(side1Coord);
                                bool side2 = TileOnBoardAndNotBlank(side2Coord);

                                letterNextToWord = letterNextToWord || side1 || side2;

                                if (j == letterIndex)
                                {
                                    break;
                                }
                            }

                            bool wordTouchesLetter = wordIntersectsLetters || letterAfterWord || letterBeforeWord || letterNextToWord;

                            if (wordTouchesLetter)
                            {
                                WordPosition initialPosition = new WordPosition(placedLetters[firstPosition], length, letterCount, (WordDirection)dir);

                                initialPosition = StretchWordPosToFill(initialPosition);

                                // This is a bit of a bandaid fix, and will not consider single letter moves. 
                                // This is only an issue on the first turn of play, and is considered negligable
                                // It will not occur in later moves since letters must be placed next to other letters
                                if (initialPosition.length > 1)
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

        /// <summary>
        /// When the board has nothing on it (i.e. the start of a game) generate word positions
        /// </summary>
        /// <param name="letterCount"></param>
        /// <returns></returns>
        private List<WordPosition> GenerateWordPositionsNoContents(int letterCount)
        {
            List<WordPosition> positions = new();

            // Check both horizontal and vertical
            for (int dir = 0; dir < Math.Min(2, letterCount); dir++)
            {
                // Board is blank, only check the centre axis
                int i = boardCentreAxis;

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

                    // No letters on the tile, always available
                    placedLetters[letterIndex] = new Coord(x, y);
                    letterIndex++;
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
                        int length = placedLetters[letterIndex][dir] - placedLetters[firstPosition][dir] + 1;

                        // Check that the proposed word is a valid move to play i.e. touches the centre of the board
                        bool validWordPosition = placedLetters.Contains(new Coord(boardCentreAxis, boardCentreAxis));

                        if (validWordPosition)
                        {
                            WordPosition initialPosition = new WordPosition(placedLetters[firstPosition], length, letterCount, (WordDirection)dir);

                            positions.Add(initialPosition);
                        }

                        // Move the last letter along
                        int j = placedLetters[letterIndex][dir] + 1;

                        // When we reach the edge of the board, stop the loop
                        if (j >= boardDimensions)
                        {
                            runLoop = false;
                        }
                        else
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

                            placedLetters[firstPosition] = new Coord(x, y);
                            letterIndex = firstPosition;
                        }
                    } while (runLoop);
                }
            }

            return positions;
        }

        private string ExtractWordPositionFromBoard(WordPosition wordPosition)
        {
            char[] characters = new char[wordPosition.length];

            for (int i = 0; i < wordPosition.length; i++)
            {
                characters[i] = CharAtTile(wordPosition.GetCoordAtIndex(i));
            }

            return new string(characters);
        }

        // Check all the by-words are valid if this word is placed at the given position 
        // (by-words, aka words that run perpendicular to this one)
        // In the process of doing this, calculate the score from this word
        private int CalculateScore(MushMatcher matcher, WordPosition wordPosition, string word)
        {
            bool validWord = true;
            int score = 0;

            for (int i = 0; i < wordPosition.length && validWord; i++)
            {
                Coord startCoord = wordPosition.GetCoordAtIndex(i);
                WordPosition newPosition = new WordPosition(startCoord, 1, 0, (int)wordPosition.wordDirection - 1);
                newPosition = StretchWordPosToFill(newPosition);
                if (newPosition.length > 1)
                {
                    string byWord = ExtractWordPositionFromBoard(newPosition);
                    string filledByWord = byWord.Replace(defaultBoardChar, word[i]);

                    // If the current byword was created thanks to the letters being placed, then work with it
                    if (byWord.Contains(defaultBoardChar))
                    {
                        validWord = matcher.HasExactWord(filledByWord);
                        score += ScoreWord(newPosition, filledByWord);
                    }

                    if (logging)
                    {
                        Console.WriteLine("Found the byword " + byWord.Replace(defaultBoardChar, '.') + " filled to be " + filledByWord);
                    }

                }
            }

            if (validWord)
            {
                score += ScoreWord(wordPosition, word);
            }
            else
            {
                score = -1;
            }

            return score;
        }

        // Calculate the score from placing the given word at the given position
        // Looks at the current board to determine which letters are being placed by you
        private int ScoreWord(WordPosition wordPosition, string word)
        {
            int score = 0;
            int wordMultiplier = 1;

            for (int i = 0; i < wordPosition.length; i++)
            {
                Coord letterCoord = wordPosition.GetCoordAtIndex(i);
                int letterScore = BlankLetterTile(letterCoord) ? 0 : letterToScore[word[i]];
                if (TileIsBlank(letterCoord))
                {
                    PowerUp powerUp = powerUps[letterCoord.x, letterCoord.y];

                    switch (powerUp)
                    {
                        case PowerUp.None:
                            break;
                        case PowerUp.DoubleLetter:
                            letterScore *= 2;
                            break;
                        case PowerUp.TripleLetter:
                            letterScore *= 3;
                            break;
                        case PowerUp.DoubleWord:
                            wordMultiplier *= 2;
                            break;
                        case PowerUp.TripleWord:
                            wordMultiplier *= 3;
                            break;
                        default:
                            break;
                    }
                }

                score += letterScore;
            }

            score *= wordMultiplier;

            return score;
        }

        /// <summary>
        /// First player to go out when the bag is empty gets a bonus from the total score of everyone elses letters
        /// Calculate what that bonus would be
        /// </summary>
        /// <param name="lettersInHand"></param>
        /// <returns></returns>
        private int CalculateEmptyBagBonus(string lettersInHand)
        {
            Dictionary<char, int> lettersRemaining = new();

            // Initalise the starting letter counts
            foreach (char key in letterCounts.Keys)
            {
                lettersRemaining[key] = letterCounts[key];
            }

            // Remove the letters we already have in our hand
            for (int i = 0; i < lettersInHand.Length; i++)
            {
                lettersRemaining[lettersInHand[i]] -= 1;
            }

            // Remove the letters on the board
            for (int i = 0; i < boardDimensions; i++)
            {
                for (int j = 0; j < boardDimensions; j++)
                {
                    if (blankLettersOnBoard[i, j])
                    {
                        lettersRemaining[MushMatcher.BLANK_TILE_INDICATOR] -= 1;
                    }
                    else if (lettersOnBoard[i, j] != defaultBoardChar)
                    {
                        lettersRemaining[lettersOnBoard[i, j]] -= 1;
                    }
                }
            }

            // Sum the total score
            int sum = 0;
            foreach (char key in lettersRemaining.Keys)
            {
                if (lettersRemaining[key] < 0)
                {
                    Console.WriteLine("Warning, when calculating the remaining letter sum, '" + key + "' was detected as being overused.");
                }
                else
                {
                    sum += lettersRemaining[key] * letterToScore[key];
                }
            }

            return sum;
        }

        private WordPosition StretchWordPosToFill(WordPosition initialPosition)
        {
            // Extend the word length to include all the letters after the word
            int j = initialPosition.length;
            bool running = true;
            while (running)
            {
                Coord coord = initialPosition.GetCoordAtIndex(j);
                running = TileOnBoard(coord) && !TileIsBlank(coord);
                if (running)
                {
                    j++;
                }
            }
            initialPosition.length = j;

            // Move and extend the word position to include all the letters before the word too
            running = true;
            while (running)
            {
                Coord coord = initialPosition.GetCoordAtIndex(-1);
                running = TileOnBoard(coord) && !TileIsBlank(coord);
                if (running)
                {
                    initialPosition.length++;
                    initialPosition.start = coord;
                }
            }
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

                        char toWrite = (char)(lettersOnBoard[j, i] + (BlankLetterTile(new Coord(j, i)) ? 'A' - 'a' : 0));

                        if ((toWrite == defaultBoardChar || word != null) && wordPosition != null)
                        {
                            bool writeWordChar = false;
                            WordPosition position = (WordPosition)wordPosition;
                            if (position.wordDirection == WordDirection.RIGHT && position.start.y == i)
                            {
                                if (position.start.x <= j && position.start.x + position.length > j)
                                {
                                    writeWordChar = true;
                                }
                            }
                            else if (position.wordDirection == WordDirection.DOWN && position.start.x == j)
                            {
                                if (position.start.y <= i && position.start.y + position.length > i)
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
                                    toWrite = (char)(word[charIndex] + (BlankLetterTile(new Coord(j, i)) ? 'A' - 'a' : 0));
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

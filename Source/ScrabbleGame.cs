using System.Runtime.ConstrainedExecution;
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

        private Dictionary<char, int> letterToScore = new Dictionary<char, int>();

        private Config config;

        private bool logging;

        private char CharAtTile(Coord coord) => CharAtTile(coord.x, coord.y);
        private char CharAtTile(int x, int y) => lettersOnBoard[x, y];

        private bool TileOnBoardAndNotBlank(Coord coord) => TileOnBoard(coord) && !TileIsBlank(coord);

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

        public void Load()
        {
            LoadScrabblePowerUps(config.powerUpsFile);
            LoadLetterScores(config.letterScoresFile);
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

        private void LoadLetterScores(string letterScorePath)
        {
            using (var streamReader = new StreamReader(File.OpenRead(letterScorePath)))
            {
                string? line = "";
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] parts = line.Split(" ");

                    letterToScore.Add(parts[0][0], Convert.ToInt32(parts[1]));
                }
            }
        }

        public void LoadGameState(string path)
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
                                foreach(string match in subMatches)
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

                            solutions.Add(new ScrabbleSolution(match, wordPosition, rawBoardLetters, score));
                        }
                    }
                }
            }

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
                        // Only score valid words
                        if (validWord)
                        {
                            score += ScoreWord(newPosition, filledByWord);
                        }
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
                int letterScore = letterToScore[word[i]];
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

                        char toWrite = lettersOnBoard[j, i];

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
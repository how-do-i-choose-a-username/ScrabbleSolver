namespace Source
{
    public class SortSizeLetters : IComparer<string>
    {
        int IComparer<string>.Compare(string? a, string? b)
        {
            int result;

            //  Make sure a and b are not null
            a = a == null ? "" : a;
            b = b == null ? "" : b;

            result = a.Length.CompareTo(b.Length);

            if (result == 0)
            {
                result = a.CompareTo(b);
            }

            return result;
        }
    }

    public class SortSolutionsByLength : IComparer<ScrabbleSolution>
    {
        int IComparer<ScrabbleSolution>.Compare(ScrabbleSolution? a, ScrabbleSolution? b)
        {
            int aValue = a == null ? 0 : a.word.Length;
            int bValue = b == null ? 0 : b.word.Length;

            int result = aValue.CompareTo(bValue);

            return result;
        }
    }

    public enum WordDirection { RIGHT = 0, DOWN = 1 }

    public struct Coord
    {
        // Element 0 is x, any other value is y
        public int x { get; internal set; } = -1;
        public int y { get; internal set; } = -1;

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Coord(Coord coord, int dimension, int change)
        {
            x = coord.x;
            y = coord.y;
            if (dimension == 0)
            {
                x += change;
            }
            else
            {
                y += change;
            }
        }

        // TODO This is a bandaid fix
        public override string ToString()
        {
            return "(" + y + ", " + x + ")";
        }

        public int this[int i]
        {
            get { return (i == 0) ? x : y; }
            set
            {
                if (i == 0)
                {
                    x = value;
                }
                else
                {
                    y = value;
                }
            }
        }

        public override bool Equals(object? obj)
        {
            bool equals = false;

            if (obj is Coord coord)
            {
                equals = coord.x == x && coord.y == y;
            }

            return equals;
        }

        public static bool operator ==(Coord left, Coord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Coord left, Coord right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return x + y;
        }
    }

    public struct WordPosition
    {
        public Coord start { get; internal set; }

        public int length { get; internal set; }

        public int lettersUsed { get; internal set; }

        public WordDirection wordDirection { get; internal set; }

        public WordPosition(Coord start, int length, int lettersUsed, WordDirection wordDirection)
        {
            this.start = start;
            this.length = length;
            this.lettersUsed = lettersUsed;
            this.wordDirection = wordDirection;
        }

        public WordPosition(Coord start, int length, int lettersUsed, int wordDirection)
        {
            this.start = start;
            this.length = length;
            this.lettersUsed = lettersUsed;
            if (wordDirection == 0)
            {
                this.wordDirection = WordDirection.RIGHT;
            }
            else
            {
                this.wordDirection = WordDirection.DOWN;
            }
        }

        public Coord GetCoordAtIndex(int i)
        {
            return new Coord(start, (int)wordDirection, i);
        }

        public WordPosition ThisWithOtherDirection()
        {
            return new WordPosition(start, length, lettersUsed, (int)wordDirection - 1);
        }

        public override string ToString()
        {
            return "Start: " + start.ToString() + " Length: " + length + " Direction: " + ((wordDirection == 0) ? "Right" : "Down");
        }
    }

    public class ScrabbleSolution : IComparable
    {
        public string word;
        public WordPosition position;
        public int score;

        public string boardLetters;

        public ScrabbleSolution(string word, WordPosition position, string boardLetters, int score)
        {
            this.word = word;
            this.position = position;
            this.boardLetters = boardLetters;
            this.score = score;
        }

        public int CompareTo(object? obj)
        {
            int result = 0;

            if (obj is ScrabbleSolution solution)
            {
                result = score.CompareTo(solution.score);
            }

            return result;
        }
    }
}
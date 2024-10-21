namespace Source
{
    public class SortSizeLetters : IComparer<string>
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

    public enum WordDirection { RIGHT = 0, DOWN = 1 }

    public struct Coord
    {
        public int x { get; internal set; } = -1;
        public int y { get; internal set; } = -1;

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
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

        public override string ToString()
        {
            return start.ToString() + " - " + length + " " + wordDirection.ToString();
        }
    }
}
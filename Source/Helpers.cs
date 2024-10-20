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
}
public class ValueKeys
{
    public const string LETTERS = "letters";
    public const string MUSHES = "mushes";
    public const string MUSH_GROUP_PREFIX = "mushGroupPrefix";
    public const string MUSH_GROUP_SUFFIX = "mushGroupSuffix";
    public const string POWERUPS_FILE = "powerups";
    public const string LETTER_VALUES_FILE = "lettervalues";
    public const string GAMEBOARD_FILE = "gameboard";

    public static readonly Dictionary<char, string> keyLookup = new Dictionary<char, string>()
    { 
        { 'l', LETTERS },
        { 'm', MUSHES },
        { 'P', MUSH_GROUP_PREFIX },
        { 'S', MUSH_GROUP_SUFFIX },
        { 'p', POWERUPS_FILE },
        { 'L', LETTER_VALUES_FILE },
        { 'g', GAMEBOARD_FILE },
    };
}
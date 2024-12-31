public class ValueKeys
{
    public const string LETTERS = "letters";
    public const string MUSHES = "mushes";
    public const string MUSH_SOURCE_DIR = "mushsource";
    public const string MUSH_GROUP_PREFIX = "mushGroupPrefix";
    public const string MUSH_GROUP_SUFFIX = "mushGroupSuffix";
    public const string POWERUPS_FILE = "powerups";
    public const string LETTER_VALUES_FILE = "lettervalues";
    public const string GAMEBOARD_FILE = "gameboard";
    public const string TILE_BAG_IS_EMPTY = "tilebag";
    public const string HELP = "help";
    public const string CONFIG = "config";

    public static readonly Dictionary<char, (string, string)> keyLookup = new Dictionary<char, (string, string)>()
    { 
        { 'l', (LETTERS, "The letters to solve with. These are the letters you have on your rack and are available to play.") },
        { 'm', (MUSHES, "The directory containing the mushed wordlists to load. These dictate what is counted as a valid word.") },
        { 'M', (MUSH_SOURCE_DIR, "The directory containing the wordlists to load. These can then be mushed for use.") },
        { 'P', (MUSH_GROUP_PREFIX, "Prefix for each file in the mush directory. Eg. 'list-' to find the file 'list-2'.") },
        { 'S', (MUSH_GROUP_SUFFIX, "Suffix for each file in the mush directory. Eg. '.mush' to find the file '2.mush'.") },
        { 'p', (POWERUPS_FILE, "The file containing the positions of each word and letter modifier on the Scrabble board.") },
        { 'L', (LETTER_VALUES_FILE, "The file containing the scores of each letter.") },
        { 'g', (GAMEBOARD_FILE, "The file containing the gameboard to load. If included the program will find the best word to play on this board. If omitted the program will list all words that can be made with your letters.") },
        { 't', (TILE_BAG_IS_EMPTY, "If the tile bag is empty account for the score bonus from going out first. Set this field to any non empty value to enable this behaviour.") },
        { 'c', (CONFIG, "The path to a config file to load. File will be loaded after the default config file, and in order of their appearance in the command. Not a valid option in a config file.") },
    };
}
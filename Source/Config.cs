namespace Source
{
    /// <summary>
    /// Config file which specifies where to load other information from, and includes details about how the program should be run.
    /// </summary>
    public class Config
    {
        public bool FindWords() => !string.IsNullOrEmpty(letters) && !string.IsNullOrEmpty(mushesDirectory);
        public bool SolveGame() => FindWords() && !string.IsNullOrEmpty(gameBoardFile);
        public bool MushifyDirectory() => !string.IsNullOrEmpty(mushSourceDirectory) && !string.IsNullOrEmpty(mushesDirectory);

        public string letters { get; internal set; } = "";
        public string mushSourceDirectory { get; internal set; } = "";
        public string mushesDirectory { get; internal set; } = "";
        public string mushGroupPrefix { get; internal set; } = "";
        public string mushGroupSuffix { get; internal set; } = "";
        public string powerUpsFile { get; internal set; } = "powerups.config";
        public string letterScoresFile { get; internal set; } = "lettervalues.config";
        public string gameBoardFile { get; internal set; } = "";

        public bool LoadConfigFile(string filePath)
        {
            bool success = true;
            try
            {
                using (var streamReader = new StreamReader(File.OpenRead(filePath)))
                {
                    string? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (!string.IsNullOrEmpty(line) && !line.StartsWith("#") && line.Contains("="))
                        {
                            string[] splitLine = line.Split("=", 2);
                            string key = splitLine[0];
                            string value = splitLine[1];

                            SetConfigValue(key, value);
                        }
                    }
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        public void SetConfigValue(string key, string value)
        {
            switch (key)
            {
                case ValueKeys.LETTERS:
                    letters = value;
                    break;
                case ValueKeys.MUSHES:
                    mushesDirectory = value;
                    break;
                case ValueKeys.MUSH_SOURCE_DIR:
                    mushSourceDirectory = value;
                    break;
                case ValueKeys.MUSH_GROUP_PREFIX:
                    mushGroupPrefix = value;
                    break;
                case ValueKeys.MUSH_GROUP_SUFFIX:
                    mushGroupSuffix = value;
                    break;
                case ValueKeys.POWERUPS_FILE:
                    powerUpsFile = value;
                    break;
                case ValueKeys.LETTER_VALUES_FILE:
                    letterScoresFile = value;
                    break;
                case ValueKeys.GAMEBOARD_FILE:
                    gameBoardFile = value;
                    break;
                default:
                    Console.WriteLine("Unknown key '" + key + "', its value '" + value + "' will be unused");
                    break;
            }
        }
    }
}
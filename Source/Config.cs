namespace Source
{
    /// <summary>
    /// Load the config file which specifies where to load other files from
    /// </summary>
    public class Config
    {
        private string configFileName = "program.config";

        public string path { get; internal set; } = "";
        public string mushGroupPrefix { get; internal set; } = "";
        public string powerUpsFile { get; internal set; } = "powerups.config";
        public string letterScoresFile { get; internal set; } = "lettervalues.config";

        public void LoadConfig()
        {
            using (var streamReader = new StreamReader(File.OpenRead(configFileName)))
            {
                String? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] splitLine = line.Split("=");

                    if (splitLine.Length == 2)
                    {
                        string key = splitLine[0];
                        string value = splitLine[1];

                        switch (key)
                        {
                            case "mushes":
                                path = value;
                                break;
                            case "mushGroupPrefix":
                                mushGroupPrefix = value;
                                break;
                            case "powerups":
                                powerUpsFile = value;
                                break;
                            case "lettervalues":
                                letterScoresFile = value;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
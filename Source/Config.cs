namespace Source
{
    class Config
    {
        private string configFileName = "program.config";

        public string path { get; internal set; } = "";
        public string mushGroupPrefix { get; internal set; } = "";

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
                        switch (splitLine[0])
                        {
                            case "mushes":
                                path = splitLine[1];
                                break;
                            case "mushGroupPrefix":
                                mushGroupPrefix = splitLine[1];
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
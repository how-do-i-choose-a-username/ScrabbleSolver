namespace Source
{
    /// <summary>
    /// Parse command line arguments to build a config file from various properties.
    /// </summary>
    public class Arguments
    {
        private readonly string defaultConfigFile = "program.config";

        private string[] defaultParameterKeys = new string[] { ValueKeys.LETTERS, ValueKeys.GAMEBOARD_FILE };

        private string[] commandArgs = new string[0];

        private Config? config;

        public Arguments(string[] args)
        {
            if (args != null)
            {
                commandArgs = args;
            }
        }

        public Config ReadConfig()
        {
            if (config == null)
            {
                LoadConfig();
            }

            // If config would otherwise be null it gets initalised
#pragma warning disable CS8603
            return config;
#pragma warning restore CS8603
        }

        private void LoadConfig()
        {
            config = new Config();

            config.LoadConfigFile(defaultConfigFile);

            int defaultKeyID = 0;
            string key = "";

            for (int i = 0; i < commandArgs.Length; i++)
            {
                string arg = commandArgs[i];
                // TODO Write this as documentation
                // If no identifier use the next defaultParameterKey
                // If - then single letter identifier (using a lookup table) check the length of the value
                //  If its got extra characters, they are the value
                //  Otherwise the next arg is the value
                // If -- then a word, thats the key with the next arg being the next parameter
                // TODO Allow specification of other config files to be loaded

                if (arg.StartsWith("--"))
                {
                    if (arg.Length > 2)
                    {
                        key = arg.Substring(2);
                    }
                    else
                    {
                        Console.WriteLine("Found the arg '--' with no key included");
                    }
                }
                else if (arg.StartsWith("-"))
                {
                    if (arg.Length >= 2)
                    {
                        key = ValueKeys.keyLookup[arg[1]];
                    }
                    else
                    {
                        Console.WriteLine("Found the arg '-' with no key included");
                    }

                    if (arg.Length >= 3)
                    {
                        config.SetConfigValue(key, arg.Substring(2));
                        key = "";
                    }
                }
                else if (string.IsNullOrEmpty(key))
                {
                    if (defaultKeyID < defaultParameterKeys.Length)
                    {
                        config.SetConfigValue(defaultParameterKeys[defaultKeyID], arg);
                        defaultKeyID++;
                    }
                    else
                    {
                        Console.WriteLine("Unable to use the value '" + arg + "' no known key");
                    }
                }
                else
                {
                    config.SetConfigValue(key, arg);
                    key = "";
                }
            }

            if (string.IsNullOrEmpty(key))
            {
                Console.WriteLine("Found key '" + key + "' with no associated value");
            }

        }
    }
}
namespace Source
{
    /// <summary>
    /// Accepts a directory of word lists. Mushifies them and saves the resulting mushes
    /// </summary>
    class Mushifier
    {
        public void MushifyDirectory(string pathToOpen, string pathToWrite)
        {
            Directory.CreateDirectory(pathToWrite);

            foreach (string fileName in Directory.EnumerateFiles(pathToOpen))
            {   
                string outputPath = fileName.Replace(pathToOpen, pathToWrite);
                List<Mush> mushes = new List<Mush>();

                //  Read the file line by line and generate mushes
                using (var streamReader = new StreamReader(File.OpenRead(fileName)))
                {
                    String? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        Mush mush = new Mush(line);

                        mushes.Add(mush);
                    }
                }

                //  Sort the mushes so I can binary search them later
                mushes.Sort();

                //  Write all the mushes to disk
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(outputPath)))
                {
                    foreach (Mush mush in mushes)
                    {
                        writer.Write(mush.ToBytes());
                    }
                }
            }
        }
    }
}
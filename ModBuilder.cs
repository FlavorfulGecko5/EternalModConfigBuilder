using System.IO.Compression;
using static System.IO.SearchOption;
class ModBuilder
{
    private ParsedConfig cfg;
    private ArgContainer io;
    private string startDir, activeDir;

    public ModBuilder(ParsedConfig cfgParameter, ArgContainer ioParameter)
    {
        cfg = cfgParameter;
        io = ioParameter;
        startDir = Directory.GetCurrentDirectory();
        activeDir = "";
    }

    public void buildMod()
    {
        createActiveOutputDir();
        Directory.SetCurrentDirectory(activeDir);

        FileParser parser = new FileParser(cfg.options);
        if (cfg.mustCheckAllFiles)
        {
            string[] allFiles = Directory.GetFiles(".", "*.*", AllDirectories);
            foreach (string file in allFiles)
                if (hasValidModFileExtension(file))
                    parser.parseFile(file);
        }
        // All locations have already had extensions validated
        else foreach (string file in cfg.locations)
            if (File.Exists(file))
                parser.parseFile(file);
            else
                ProcessErrorCode(LOCATIONS_FILE_NOT_FOUND, file);

        propagateAll();
        Directory.SetCurrentDirectory(startDir);
        buildZip();
    }

    private void createActiveOutputDir()
    {
        // If zip, use temp. directory then zip to output after processing
        activeDir = io.outToZip ? TEMP_DIRECTORY : io.outPath;

        // Clone the contents of src to the active output directory
        Directory.CreateDirectory(activeDir);
        if (io.srcIsZip)
            ZipFile.ExtractToDirectory(io.srcPath, activeDir);
        else
        {
            DirectoryInfo copyFrom = new DirectoryInfo(io.srcPath);
            DirectoryInfo copyTo = new DirectoryInfo(activeDir);
            CopyDir(copyFrom, copyTo);
        }
    }

    private void propagateAll()
    {
        if (Directory.Exists(PROPAGATE_DIRECTORY))
        {
            if (cfg.propagations.Count == 0)
                ProcessErrorCode(PROPAGATE_DIR_NO_LISTS);

            foreach (PropagateList resource in cfg.propagations)
                resource.propagate();

            Directory.Delete(PROPAGATE_DIRECTORY, true);
        }
        else if (cfg.propagations.Count > 0)
            ProcessErrorCode(PROPAGATE_LISTS_NO_DIR);
    }

    private void buildZip()
    {
        if (io.outToZip)
        {
            ZipFile.CreateFromDirectory(TEMP_DIRECTORY, io.outPath);
            Directory.Delete(TEMP_DIRECTORY, true);
        }
    }
}
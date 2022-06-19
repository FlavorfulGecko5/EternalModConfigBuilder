using System.IO.Compression;
using static System.IO.SearchOption;
using static ModBuilder.Error;
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

        parseFiles();
        propagateAll();

        Directory.SetCurrentDirectory(startDir);
        if(io.outToZip)
            buildZip();
    }

    private void createActiveOutputDir()
    {
        // If zip, use temp. directory then zip to output after processing
        activeDir = io.outToZip ? DIRECTORY_TEMP : io.outPath;

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

    private void parseFiles()
    {
        FileParser parser = new FileParser(cfg.options);

        string[] allFiles = Directory.GetFiles(".", "*.*", AllDirectories);
        foreach (string file in allFiles)
            if (hasValidModFileExtension(file))
                parser.parseFile(file);
    }

    private void propagateAll()
    {
        if (Directory.Exists(DIRECTORY_PROPAGATE))
        {
            if (cfg.propagations.Count == 0)
                ThrowError(PROPAGATE_DIR_NO_LISTS);

            foreach (PropagateList resource in cfg.propagations)
                resource.propagate();

            Directory.Delete(DIRECTORY_PROPAGATE, true);
        }
        else if (cfg.propagations.Count > 0)
            ThrowError(PROPAGATE_LISTS_NO_DIR);
    }

    private void buildZip()
    {
        // Allows zips to be output to another folder
        // (The zip can only be created inside a pre-existing directory)
        string? zipDir = Path.GetDirectoryName(io.outPath);
        if(zipDir != null && !zipDir.Equals("")) // Null should be impossible
            Directory.CreateDirectory(zipDir);
        ZipFile.CreateFromDirectory(DIRECTORY_TEMP, io.outPath);
        Directory.Delete(DIRECTORY_TEMP, true);
    }

    public enum Error
    {
        PROPAGATE_DIR_NO_LISTS,
        PROPAGATE_LISTS_NO_DIR,
    }

    private void ThrowError(Error error)
    {
        switch(error)
        {
            case PROPAGATE_DIR_NO_LISTS:
            reportWarning(String.Format(
               "The '{0}' directory exists in your mod, but no propagation "
               + "lists are defined. Propagation will not occur.", 
               DIRECTORY_PROPAGATE
            ));
            break;

            case PROPAGATE_LISTS_NO_DIR:
            reportWarning(String.Format(
                "You have propagation lists, but no '{0}' directory in "
                + "your mod. Propagation will not occur.",
                DIRECTORY_PROPAGATE
            ));
            break;
        }
    }
}
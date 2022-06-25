using static ModBuilder.Error;
class ModBuilder
{
    private ParsedConfig cfg;
    private ArgContainer argData;
    private string startDir, activeDir;

    public ModBuilder(string[] args)
    {
        argData = new ArgContainer(args);
        cfg = new ParsedConfig(argData.configPath);
        startDir = Directory.GetCurrentDirectory();
        activeDir = "";
        //System.Console.WriteLine(cfg.ToString());
    }

    public void buildMod()
    {
        createActiveOutputDir();
        Directory.SetCurrentDirectory(activeDir);

        parseFiles();
        propagateAll();

        Directory.SetCurrentDirectory(startDir);
        if(argData.outToZip)
            buildZip();
    }

    private void createActiveOutputDir()
    {
        // If zip, use temp. directory then zip to output after processing
        activeDir = argData.outToZip ? DIRECTORY_TEMP : argData.outPath;

        // Clone the contents of src to the active output directory
        Directory.CreateDirectory(activeDir);
        if (argData.srcIsZip)
            ZipUtil.unzip(argData.srcPath, activeDir);
        else
            DirUtil.copyDirectory(argData.srcPath, activeDir);
    }

    private void parseFiles()
    {
        FileParser parser = new FileParser(cfg.options);

        string[] allFiles = DirUtil.getAllDirectoryFiles(".");
        foreach (string file in allFiles)
            if (ExtUtil.hasValidModFileExtension(file))
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
        ZipUtil.makeZip(DIRECTORY_TEMP, argData.outPath);
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
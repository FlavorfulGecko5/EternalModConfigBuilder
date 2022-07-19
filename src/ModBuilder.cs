using static ModBuilder.Error;
class ModBuilder
{
    private ParsedConfig cfg;
    private ArgContainer argData;
    private string startDir, activeDir;

    public ModBuilder(string[] args)
    {
        argData = new ArgContainer(args);
        cfg = new ParsedConfig(argData.configPaths);
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
        activeDir = argData.outToZip ? DIR_TEMP : argData.outPath;

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
        if (Directory.Exists(DIR_PROPAGATE))
        {
            if (cfg.propagations.Count == 0)
                EMBWarning(PROPAGATE_DIR_NO_LISTS);

            foreach (PropagateList resource in cfg.propagations)
                resource.propagate();

            Directory.Delete(DIR_PROPAGATE, true);
        }
        else if (cfg.propagations.Count > 0)
            EMBWarning(PROPAGATE_LISTS_NO_DIR);
    }

    private void buildZip()
    {
        ZipUtil.makeZip(DIR_TEMP, argData.outPath);
        Directory.Delete(DIR_TEMP, true);
    }

    public enum Error
    {
        PROPAGATE_DIR_NO_LISTS,
        PROPAGATE_LISTS_NO_DIR,
    }

    private void EMBWarning(Error e)
    {
        string msg = "";
        string[] args = {""};
        switch(e)
        {
            case PROPAGATE_DIR_NO_LISTS:
            msg = "The '{0}' directory exists in your mod, but no propagation "
                    + "lists are defined. Propagation will not occur.";
            args[0] = DIR_PROPAGATE;
            break;

            case PROPAGATE_LISTS_NO_DIR:
            msg = "You have propagation lists, but no '{0}' directory in "
                    + "your mod. Propagation will not occur.";
            args[0] = DIR_PROPAGATE;
            break;
        }
        RuntimeManager.reportWarning(msg, args);
    }
}
using static ModBuilder.Error;
using static RuntimeConfig.ExecutionMode;
class ModBuilder
{
    private ParsedConfig cfg;
    private string startDir, activeDir;

    public ModBuilder()
    {
        cfg = new ParsedConfig(RuntimeConfig.configPaths);
        startDir = Directory.GetCurrentDirectory();
        activeDir = "";
    }

    public void buildMod()
    {
        switch(RuntimeConfig.exeMode)
        {
            case COMPLETE:
            createAndSetActiveOutputDir();
            parseFiles();
            propagateAll();
            finishBuilding();
            break;

            case READONLY:
            break;

            case PARSE:
            createAndSetActiveOutputDir();
            parseFiles();
            finishBuilding();
            break;

            case PROPAGATE:
            createAndSetActiveOutputDir();
            propagateAll();
            finishBuilding();
            break;
        }
    }

    private void createAndSetActiveOutputDir()
    {
        // If zip, use temp. directory then zip to output after processing
        activeDir = RuntimeConfig.outToZip ? DIR_TEMP : RuntimeConfig.outPath;

        // Clone the contents of src to the active output directory
        Directory.CreateDirectory(activeDir);
        if (RuntimeConfig.srcIsZip)
            ZipUtil.unzip(RuntimeConfig.srcPath, activeDir);
        else
            DirUtil.copyDirectory(RuntimeConfig.srcPath, activeDir);
        Directory.SetCurrentDirectory(activeDir);
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

    private void finishBuilding()
    {
        Directory.SetCurrentDirectory(startDir);
        if(RuntimeConfig.outToZip)
        {
            ZipUtil.makeZip(DIR_TEMP, RuntimeConfig.outPath);
            Directory.Delete(DIR_TEMP, true);
        }
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
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
            case ExecutionMode.COMPLETE:
            createAndSetActiveOutputDir();
            parseFiles();
            propagateAll();
            finishBuilding();
            break;

            case ExecutionMode.READONLY:
            break;

            case ExecutionMode.PARSE:
            createAndSetActiveOutputDir();
            parseFiles();
            finishBuilding();
            break;

            case ExecutionMode.PROPAGATE:
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
        ModBuilderLogMaker warningLogger = new ModBuilderLogMaker();
        if (Directory.Exists(DIR_PROPAGATE))
        {
            if (cfg.propagations.Count == 0)
                warningLogger.logWarningNoPropLists();

            foreach (PropagateList resource in cfg.propagations)
                resource.propagate();

            Directory.Delete(DIR_PROPAGATE, true);
        }
        else if (cfg.propagations.Count > 0)
            warningLogger.logWarningNoPropFolder();
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
}
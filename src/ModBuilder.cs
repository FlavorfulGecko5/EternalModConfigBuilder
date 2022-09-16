class ModBuilder
{
    private ParsedConfig cfg;
    private string startDir, activeDir;

    public ModBuilder()
    {
        cfg = new ParsedConfig();
        startDir = Directory.GetCurrentDirectory();
        activeDir = "";
    }

    public void buildMod()
    {
        if(EternalModBuilder.exeMode == ExecutionMode.READONLY)
            return;

        createActiveOutputDir();
        Directory.SetCurrentDirectory(activeDir);
        switch(EternalModBuilder.exeMode)
        {
            case ExecutionMode.COMPLETE:
            parseFiles();
            propagateAll();
            if(EternalModBuilder.compressEntities)
                compressEntities();
            break;

            case ExecutionMode.PARSE:
            parseFiles();
            if(EternalModBuilder.compressEntities)
                compressEntities();
            break;

            case ExecutionMode.PROPAGATE:
            propagateAll();
            break;
        }
        Directory.SetCurrentDirectory(startDir);
        finishBuilding();
    }

    private void createActiveOutputDir()
    {
        // If zip, use temp. directory then zip to output after processing
        activeDir = EternalModBuilder.outToZip ? DIR_TEMP : EternalModBuilder.outPath;

        // Clone the contents of src to the active output directory
        Directory.CreateDirectory(activeDir);
        if (EternalModBuilder.srcIsZip)
            ZipUtil.unzip(EternalModBuilder.srcPath, activeDir);
        else
            DirUtil.copyDirectory(EternalModBuilder.srcPath, activeDir);
    }

    private void parseFiles()
    {
        FileParser parser = new FileParser(cfg.options);
        List<string> filesToParse = new List<string>();

        string[] declFiles = DirUtil.getFilePathsFromCurrentDir("*.decl");
        foreach (string decl in declFiles)
            filesToParse.Add(decl);

        string[] jsonFiles = DirUtil.getFilePathsFromCurrentDir("*.json");
        foreach (string json in jsonFiles)
            filesToParse.Add(json);

        string[] entityFiles = DirUtil.getFilePathsFromCurrentDir("*.entities");
        foreach(string entity in entityFiles)
            if(!EntityCompressor.isEntityFileCompressed(entity))
                filesToParse.Add(entity);
        
        foreach(string file in filesToParse)
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

    private void compressEntities()
    {

    }

    private void finishBuilding()
    {
        if(EternalModBuilder.outToZip)
        {
            ZipUtil.makeZip(DIR_TEMP, EternalModBuilder.outPath);
            Directory.Delete(DIR_TEMP, true);
        }
    }
}
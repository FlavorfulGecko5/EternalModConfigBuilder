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
            FSUtil.copyDirectory(EternalModBuilder.srcPath, activeDir);
    }

    private void parseFiles()
    {
        FileParser parser = new FileParser(cfg.options);
        List<string> labelFiles = new List<string>();

        string[] allFiles = FSUtil.getAllFilesInCurrentDir();
        foreach(string file in allFiles)
            if(file.EndsWithCCIC(".decl") || file.EndsWithCCIC(".json"))
                labelFiles.Add(file);
            else if(file.EndsWithCCIC(".entities"))
                if(!EntityCompressor.isEntityFileCompressed(file))
                    labelFiles.Add(file);

        foreach(string file in labelFiles)
            parser.parseFile(file);
    }

    private void propagateAll()
    {
        const string WARNING_NO_LISTS = "The '" + DIR_PROPAGATE + "' folder"
        + " exists in your mod, but no propagation lists are defined."
        + " Propagation will not occur."; 
        const string WARNING_NO_DIR = "You have propagation lists, but no '"
        + DIR_PROPAGATE + "' folder in your mod. Propagation will not occur.";

        if (Directory.Exists(DIR_PROPAGATE))
        {
            if (cfg.propagations.Count == 0)
                LogMaker.reportWarning(WARNING_NO_LISTS);

            foreach (PropagateList resource in cfg.propagations)
                resource.propagate();

            Directory.Delete(DIR_PROPAGATE, true);
        }
        else if (cfg.propagations.Count > 0)
            LogMaker.reportWarning(WARNING_NO_DIR);
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
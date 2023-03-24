class PropagateList
{
    const string RULES_PROPAGATE_PATHS = "Propagation string lists must:\n"
    + "- Have a relative, non-backtracking directory as their name.\n"
    + "- Each list string must be a relative path to files or directories inside your mod's '" + EternalModBuilder.DIR_PROPAGATE + "' folder.\n"
    + "When your mod is built, listed files/directories will be copied to the directory specified by the list's name.";


    public string name {get;}
    public string[] filePaths {get;}

    public PropagateList(string nameParameter, string[] filePathsParameter)
    {
        const string
        ERR_BAD_NAME = "The propagation list '{0}' has a non-relative or backtracking name.\n\n{1}",
        ERR_BAD_ELEMENT = "The propagation list '{0}' contains a non-relative or backtracking path '{1}'\n\n{2}";

        name = nameParameter;
        filePaths = filePathsParameter;
        string workingDir = Directory.GetCurrentDirectory();

        if (!isPropPathValid(name))
            throw ListError(ERR_BAD_NAME, name, RULES_PROPAGATE_PATHS);

        foreach (string path in filePaths)
            if (!isPropPathValid(path))
                throw ListError(ERR_BAD_ELEMENT, name, path, RULES_PROPAGATE_PATHS);

        /// <summary>
        /// Ensures a propagation pathway is valid
        /// </summary>
        /// <param name="propPath"> The propagation pathway to check </param>
        /// <returns>
        /// True if the path is not empty, is relative, and does not backtrack.
        /// Otherwise, returns false
        /// </returns>
        bool isPropPathValid(string propPath)
        {
            if (propPath.Length == 0)
                return false;
            if (Path.IsPathRooted(propPath))
                return false;
            string pathAbs = Path.GetFullPath(propPath);
            return pathAbs.StartsWith(workingDir);
        }
    }

    public override string ToString()
    {
        string formattedString = "Propagation List '" + name + "'";
        foreach(string file in filePaths)
            formattedString += "\n- '" + file + "'";
        return formattedString;
    }

    public void propagate()
    {
        foreach(string path in filePaths)
        {
            string  copyFrom = Path.Combine(EternalModBuilder.DIR_PROPAGATE, path),
                    copyTo = Path.Combine(name, path);
            
            if(File.Exists(copyFrom))
            {
                FSUtil.createDirectoryInFilePath(copyTo);
                File.Copy(copyFrom, copyTo, true);
            }
            else if (Directory.Exists(copyFrom))
            {
                Directory.CreateDirectory(copyTo);
                FSUtil.copyDirectory(copyFrom, copyTo);
            }    
            else
            {
                string warning = String.Format(
                    "The path '{0}' in propagation list '{1}' does not exist in"
                    + " '{2}'. This path will be ignored.",
                    path,
                    name,
                    EternalModBuilder.DIR_PROPAGATE
                );
                EternalModBuilder.reportWarning(warning);
            }
        }
    }

    private EMBPropagaterListException ListError(string msg, params string[] args)
    {
        string formattedMessage = String.Format(msg, args);
        return new EMBPropagaterListException(formattedMessage);
    }

    public class EMBPropagaterListException : EMBException
    {
        public EMBPropagaterListException(string msg) : base (msg) {}
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GraviaSoftware.SmartNS.SmartNS.Editor
{
    /// <summary>
    /// An Asset Creation Post-processor that acts on newly created c# scripts to insert a smart namespace based
    /// on the path of the script.
    /// </summary>
    public class SmartNS : UnityEditor.AssetModificationProcessor
    {
        public const string SmartNSVersionNumber = "2.0.2";



        // TODO: Do we need something else for Mac?
        private static string PathSeparator = "/";

        private static string ByteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        private static bool _shouldWriteDebugLogInfo = false;

        #region Asset Creation

        /// <summary>
        /// Intercepts the creation of assets, looking for new .cs scripts, and inserts a namespace.
        /// </summary>
        /// <param name="path"></param>
        public static void OnWillCreateAsset(string path)
        {
            try
            {
                // We only intercept C# scripts.
                if (!path.EndsWith(".cs.meta"))
                {
                    return;
                }


                path = path.Replace(".meta", "");
                path = path.Trim();
                int index = path.LastIndexOf(".");
                if (index < 0)
                {
                    return;
                }

                var smartNSSettings = SmartNSSettings.GetSerializedSettings();
                var enableDebugLogging = smartNSSettings.FindProperty("m_EnableDebugLogging").boolValue;
                _shouldWriteDebugLogInfo = enableDebugLogging;

                ProcessNamespaceForScriptAtPath(path);

            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Something went really wrong trying to execute SmartNS: {0}", ex.Message));
                Debug.LogError(string.Format("SmartNS Failure Stack Trace: {0}", ex.StackTrace));
            }
        }


        #endregion


        #region Asset Moved
        private static HashSet<string> _currentlyMovingAssets = new HashSet<string>();
        public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            var assetMoveResult = AssetMoveResult.DidNotMove;

            try
            {
                if (!_currentlyMovingAssets.Contains(sourcePath))
                {
                    if (!SmartNSSettings.SettingsFileExists())
                    {
                        SmartNSSettings.GetOrCreateSettings();
                    }

                    var smartNSSettings = SmartNSSettings.GetSerializedSettings();
                    var updateNamespacesWhenMovingScripts = smartNSSettings.FindProperty("m_UpdateNamespacesWhenMovingScripts").boolValue;
                    var enableDebugLogging = smartNSSettings.FindProperty("m_EnableDebugLogging").boolValue;
                    _shouldWriteDebugLogInfo = enableDebugLogging;

                    if (updateNamespacesWhenMovingScripts)
                    {
                        // We only intercept C# scripts.
                        if (sourcePath.EndsWith(".cs"))
                        {
                            WriteDebug("Moving asset. Source path: " + sourcePath + ". Destination path: " + destinationPath + ".");

                            // Perform operations on the asset and set the value of 'assetMoveResult' accordingly.

                            var validationMessage = AssetDatabase.ValidateMoveAsset(sourcePath, destinationPath);

                            if (string.IsNullOrWhiteSpace(validationMessage))
                            {
                                // Keep track of the fact that we're moving this asset, because OnWillMoveAsset will be called again
                                // when we call MoveAsset below. We keep track of it to avoid recursing into this code again.
                                _currentlyMovingAssets.Add(sourcePath);

                                // We can move this. So go ahead.
                                AssetDatabase.MoveAsset(sourcePath, destinationPath);


                                // Now post-process the moved script to clean up its namespace.
                                ProcessNamespaceForScriptAtPath(destinationPath);

                                assetMoveResult = AssetMoveResult.DidMove;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Something went really wrong trying to execute SmartNS: {0}", ex.Message));
                Debug.LogError(string.Format("SmartNS Failure Stack Trace: {0}", ex.StackTrace));
            }
            finally
            {
                _currentlyMovingAssets.Remove(sourcePath);
            }

            return assetMoveResult;
        }

        #endregion



        private static void ProcessNamespaceForScriptAtPath(string path, HashSet<string> directoryIgnoreList = null)
        {
            // We depend on a properly created Project Settings file. Create it now, if it doesn't exist.
            if (!SmartNSSettings.SettingsFileExists())
            {
                SmartNSSettings.GetOrCreateSettings();
            }

            var smartNSSettings = SmartNSSettings.GetSerializedSettings();
            var scriptRootSettingsValue = smartNSSettings.FindProperty("m_ScriptRoot").stringValue;
            var prefixSettingsValue = smartNSSettings.FindProperty("m_NamespacePrefix").stringValue;
            var universalNamespaceSettingsValue = smartNSSettings.FindProperty("m_UniversalNamespace").stringValue;
            var useSpacesSettingsValue = smartNSSettings.FindProperty("m_IndentUsingSpaces").boolValue;
            var numberOfSpacesSettingsValue = smartNSSettings.FindProperty("m_NumberOfSpaces").intValue;
            //var defaultScriptCreationDirectorySettingsValue = smartNSSettings.FindProperty("m_DefaultScriptCreationDirectory").stringValue;
            var directoryDenyListSettingsValue = smartNSSettings.FindProperty("m_DirectoryIgnoreList").stringValue;
            var enableDebugLogging = smartNSSettings.FindProperty("m_EnableDebugLogging").boolValue;



            /*
             * This has been commented out. It was causing errors when moving files. I'm not sure why yet, but
             * moving a file in this was would always produce a "File couldn't be read!" error.
             * 
            // If this script was created directly under the Assets folder, and the settings tell us a default script location
            // (other than "Assets") move it there.
            if (!string.IsNullOrWhiteSpace(defaultScriptCreationDirectorySettingsValue))
            {
                var trimmedDefaultDir = defaultScriptCreationDirectorySettingsValue.Trim();

                if (path.Split('/').Length == 2)
                {
                    var destinationDirectory = trimmedDefaultDir;
                    if (!destinationDirectory.StartsWith("Assets"))
                    {
                        destinationDirectory = string.Join(PathSeparator, "Assets", destinationDirectory);
                    }
                    // Replace anu double-slashes.
                    destinationDirectory = Regex.Replace(destinationDirectory, "//", "/");

                    // Make sure the directory exists
                    if (AssetDatabase.IsValidFolder(destinationDirectory))
                    {
                        var preferredPath = path.Replace("Assets", destinationDirectory);

                        var moveAssetTestResult = AssetDatabase.ValidateMoveAsset(path, preferredPath);
                        if (string.IsNullOrWhiteSpace(moveAssetTestResult))
                        {
                            WriteDebug(string.Format("Moving file from {0} to Default Script Creation Directory: {1}", path, preferredPath));
                            AssetDatabase.MoveAsset(path, preferredPath);

                            AssetDatabase.Refresh();

                            return;
                        }
                        else
                        {
                            Debug.LogError(string.Format("SmartNS unable to move script to default script creation directory, '{0}': {1}", destinationDirectory, moveAssetTestResult));
                        }
                    }
                    else
                    {
                        Debug.LogError(string.Format("SmartNS unable to move script to default script creation directory, '{0}', because the folder does not exist. Make sure the 'Default Script Creation Directory' specified in the Project Settings is valid.", destinationDirectory));
                    }
                }
            }
            */


            UpdateAssetNamespace(path,
                scriptRootSettingsValue,
                prefixSettingsValue,
                universalNamespaceSettingsValue,
                useSpacesSettingsValue,
                numberOfSpacesSettingsValue,
                directoryDenyListSettingsValue,
                enableDebugLogging,
                directoryIgnoreList);


            // Without this, the file won't update in Unity, and won't look right.
            AssetDatabase.ImportAsset(path);
        }

        public static void UpdateAssetNamespace(string assetPath,
            string scriptRootSettingsValue,
            string prefixSettingsValue,
            string universalNamespaceSettingsValue,
            bool useSpacesSettingsValue,
            int numberOfSpacesSettingsValue,
            string directoryDenyListSettingsValue,
            bool enableDebugLogging,
            HashSet<string> directoryIgnoreList = null)
        {

            _shouldWriteDebugLogInfo = enableDebugLogging;

            WriteDebug(string.Format("Acting on new C# file: {0}", assetPath));
            var indexOfAsset = Application.dataPath.LastIndexOf("Assets");
            var fullFilePath = Application.dataPath.Substring(0, indexOfAsset) + assetPath;
            var fileInfo = new FileInfo(fullFilePath);
            WriteDebug(string.Format("Full Path: {0}", fullFilePath));
            if (!System.IO.File.Exists(fullFilePath))
            {
                WriteDebug(string.Format("Path doesn't exist: {0}. Exiting.", fullFilePath));
                return;
            }

            if (directoryIgnoreList == null)
            {
                directoryIgnoreList = GetIgnoredDirectories();
                if (directoryIgnoreList.Contains(fileInfo.Directory.FullName))
                {
                    WriteDebug(string.Format("File {0} is a child of an ignored directory. Exiting.", fullFilePath));
                    return;
                }
            }


            // Generate the namespace.
            string namespaceValue = GetNamespaceValue(assetPath, scriptRootSettingsValue, prefixSettingsValue, universalNamespaceSettingsValue);
            if (namespaceValue == null)
            {
                WriteDebug(string.Format("Generated namespace for {0} is null. Exiting.", fullFilePath));
                return;
            }

            // We try to keep line endings consistent. Detect the file's current line endings, and 
            // from that, determine which kind of line ending we should use.
            var lineEnding = DetectLineEndings(fullFilePath);


            // Read the file contents, so we can insert the namespace line, and indent other lines under it.
            // Note that we read the full file contents so we can avoid problems with leading non-breaking zero-width
            // spaces, and so we can detect whether the file ends with a linebreak.
            string rawFileContents = System.IO.File.ReadAllText(fullFilePath);
            string[] rawLines = rawFileContents.Split(new[] { lineEnding }, StringSplitOptions.None);


            // I don't know why there might be zero lines in the file, but we don't do anything if that's the case.
            if (rawLines.Length == 0)
            {
                WriteDebug(string.Format("File {0} contains no lines. Exiting.", fullFilePath));
                return;
            }


            var modifiedLines = new List<string>();

            // Determining exactly where to insert the namespace is a little tricky. A user could have modified the
            // default template in any number of ways. And now that we're doing bulk correction, we could end up with 
            // any weird formatting. The file may or may not contain a namespace declaration already.
            // Namespaces can even span multiple lines. So our approach is to search the file until 
            // we find a line starting with "\w*namespace ", and assume that's the start of the namespace declaration.
            // If we don't find an existing namespace, we use a (hopefully) safest approach of assuming that all 
            // 'using' statements come first, and that the namespace declaration will appear after the last using statement.
            // If we DO find a namespace declaration, we will remove is so that we can add a new one at that position. 
            // Removing the existing namespace has a bit of complexity, though, to make sure we remove all of it, and not too much.


            bool hasExistingNamespace = false;
            for (int rawLineIndex = 0; rawLineIndex < rawLines.Length; rawLineIndex++)
            {
                var line = rawLines[rawLineIndex];
                if (!hasExistingNamespace && line.TrimStart().StartsWith("namespace "))
                {
                    hasExistingNamespace = true;

                    WriteDebug(string.Format("Existing namespace found on line index {0}", rawLineIndex));


                    // We found the line on which the namespace begins. We need to remove everything from the start of "namespace" right up 
                    // until we find the opening curly brace. This might go over many lines, with any amount of other content in between.
                    // This won't handle really ridiculous cases like:
                    //     namespace A.B
                    //     /* Here's a comment embedded in the namespace, which contains a {. Yup, this is valid C# within a namespace declaration. */
                    //     .C

                    while (rawLineIndex < rawLines.Length)
                    {
                        // Keep looking until we find the curly brace, or we EOF.
                        if (line.Contains("{"))
                        {
                            // We've found the curly brace. Remove everything leading up to it, 
                            // then add the namespace. As an optimization against unnecessary edits, if the curly 
                            // brace starts a line, then we leave it on its own line. Otherwise, we concatenate it with the namespace.
                            var indexOfCurlyBrace = line.IndexOf('{');
                            var curlyBraceOnward = line.Substring(indexOfCurlyBrace);
                            if (indexOfCurlyBrace == 0)
                            {
                                modifiedLines.Add(string.Format("namespace {0}", namespaceValue));
                                modifiedLines.Add(curlyBraceOnward);
                            }
                            else
                            {
                                modifiedLines.Add(string.Format("namespace {0}{1}", namespaceValue, curlyBraceOnward));
                            }
                            break;
                        }
                        else
                        {
                            // This line doesn't contains 
                            rawLineIndex++;
                            line = rawLines[rawLineIndex];
                        }
                    }
                }
                else
                {
                    modifiedLines.Add(line);
                }
            }

            if (!hasExistingNamespace)
            {
                // We didn't find an existing namespace, so instead we seek out the the last 'using' statement in the file.

                var lastUsingLineIndex = 0;

                // Find the last "using" statement in the file.
                for (var i = modifiedLines.Count - 1; i >= 0; i--)
                {
                    if (modifiedLines[i].StartsWith("using "))
                    {
                        lastUsingLineIndex = i;
                        break;
                    }
                }

                // A blank line, followed by the namespace declaration.
                modifiedLines.Insert(lastUsingLineIndex + 1, "");
                modifiedLines.Insert(lastUsingLineIndex + 2, string.Format("namespace {0} {{", namespaceValue));


                // Indent all lines in between.
                for (var i = lastUsingLineIndex + 3; i < modifiedLines.Count; i++)
                {
                    var prefix = useSpacesSettingsValue
                        ? new string(' ', numberOfSpacesSettingsValue)
                        : "\t";
                    modifiedLines[i] = string.Format("{0}{1}", prefix, modifiedLines[i]);
                }


                // Add the closing brace
                modifiedLines.Add("}");
            }

            // Combine the modified lines into a single string.
            var newFileContents = string.Join(lineEnding, modifiedLines.ToArray());


            // There were cases where final line endings were being stripped by out splitting into an array of strings.
            // So, if the final ended with a line ending, ensure it still does.
            if (rawFileContents.EndsWith(Environment.NewLine) || rawFileContents.EndsWith(lineEnding))
            {
                if (!(newFileContents.EndsWith(Environment.NewLine) || newFileContents.EndsWith(lineEnding)))
                {
                    newFileContents += lineEnding;
                }
            }

            // Similarly, if the file began with "\ufeff", ensure it still does.
            if (rawFileContents.StartsWith(ByteOrderMarkUtf8))
            {
                // This is weird. If I test whether newFileContents already starts with a BOM, 
                // it tells me it does. But if I write the string as-is, the resulting file won't start
                // with a BOM. So I always need to add the BOM, even if it already has one.
                // However, if I add two BOMs, then it clear ends up with two BOMs. So, I don't see why
                // adding one while it has one results in still only having one.
                newFileContents = ByteOrderMarkUtf8 + newFileContents;
            }



            System.IO.File.WriteAllText(fullFilePath, newFileContents);


        }

        private static string DetectLineEndings(string filePath)
        {
            // There are three options for line endings: Mac, Unix, and Windows. More generally, they are:
            //  - \r = CR
            //  - \n = LF
            //  - \r\n = CR LF
            // 
            // Ideally we'll remain consistent in the line endings we use when composing the file. So, we
            // inspect the raw file data here to determine which line endings are being used.

            var allText = System.IO.File.ReadAllText(filePath);

            if (allText.Contains("\r\n"))
            {
                //Debug.Log($"{filePath}'s line endings are \\r\\n");
                return "\r\n";
            }
            else if (allText.Contains("\r"))
            {
                //Debug.Log($"{filePath}'s line endings are \\r");
                return "\r";
            }
            else if (allText.Contains("\n"))
            {
                //Debug.Log($"{filePath}'s line endings are \\n");
                return "\n";
            }
            else
            {
                //Debug.Log($"{filePath}'s line endings are indeterminate, so using system default.");
                // Default to returning the OS default
                return Environment.NewLine;
            }
        }


        public static string GetNamespaceValue(string path, string scriptRootValue, string prefixValue, string universalNamespaceValue)
        {
            string namespaceValue = null;


            if (string.IsNullOrEmpty(universalNamespaceValue) || string.IsNullOrEmpty(universalNamespaceValue.Trim()))
            {
                // We're not using a Universal namespace. So, generate the smart namespace.
                namespaceValue = path;

                if (scriptRootValue.Trim().Length > 0)
                {
                    // Old Version: Used to require an exact match between the scriptRootValue and the path. 
                    // That had a defect where using a ScriptRoot of "Assets/ABC", then created a script in "Assets"
                    // would not strip off the "Assets" from the start of the namespace.
                    /*
                    var toTrim = scriptRootValue.Trim();
                    if (namespaceValue.StartsWith(toTrim))
                    {
                        WriteDebug(string.Format("Trimming script root '{0}' from start of namespace", toTrim));
                        namespaceValue = namespaceValue.Substring(toTrim.Length);
                    }
                    */

                    // New Version: Remove as much of the ScriptRoot as exists in the path, as long as the elements line up
                    // exactly. 
                    foreach (var scriptRootPathPart in Regex.Split(scriptRootValue.Trim(), PathSeparator))
                    {
                        var toTrim = scriptRootPathPart.Trim();

                        // We need to match exactly on each element in the path. We used to just check for StartsWith, but if we 
                        // had a prefix of "A" when the path was "ABC", we used to strip the A off the ABC, which was wrong.
                        if (namespaceValue == toTrim || namespaceValue.StartsWith(toTrim + PathSeparator))
                        {
                            WriteDebug(string.Format("Trimming script root part '{0}' from start of namespace", toTrim));
                            namespaceValue = namespaceValue.Substring(toTrim.Length);

                            // If this leaves the namespace with a "/" at the start, remove that.
                            if (namespaceValue.StartsWith(PathSeparator))
                            {
                                namespaceValue = namespaceValue.Substring(1);
                            }
                        }
                    }

                }


                var rawPathParts = namespaceValue.Split(PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                // Ignore the last "part", as that's the file name.
                var namespaceParts = rawPathParts.Take(rawPathParts.Count() - 1).ToArray();


                // Namespace identifiers can't start with a number. So we prefix those with underscores
                // if they exist.
                for (int namespacePartIndex = 0; namespacePartIndex < namespaceParts.Length; namespacePartIndex++)
                {
                    var match = Regex.Match(namespaceParts[namespacePartIndex], "^\\d");
                    if (match.Success)
                    {
                        namespaceParts[namespacePartIndex] = "_" + namespaceParts[namespacePartIndex];
                    }
                }


                namespaceValue = string.Join("/", namespaceParts);

                // Trim any spaces.
                namespaceValue = namespaceValue.Replace(" ", "");
                // Invalid characters replaced with _
                namespaceValue = namespaceValue.Replace(".", "_");
                // Separators replaced with .
                namespaceValue = namespaceValue.Replace("/", ".");

                WriteDebug(string.Format("Script Root = {0}", scriptRootValue));




                if (namespaceValue.StartsWith("."))
                {
                    namespaceValue = namespaceValue.Substring(1);
                }

                if (prefixValue.Trim().Length > 0)
                {
                    if (string.IsNullOrEmpty(namespaceValue.Trim()))
                    {
                        // This script was likely added at Assets root.
                        namespaceValue = prefixValue;
                    }
                    else
                    {
                        namespaceValue = string.Format("{0}{1}{2}",
                            prefixValue,
                            prefixValue.EndsWith(".") ? "" : ".",
                            namespaceValue);
                    }

                }

                WriteDebug(string.Format("Using smart namespace: '{0}'", namespaceValue));

            }
            else
            {
                // Use the universal namespace.
                namespaceValue = universalNamespaceValue.Trim();
                WriteDebug(string.Format("Using 'Universal' namespace: {0}", namespaceValue));
            }

            if (namespaceValue.Trim().Length == 0)
            {
                //WriteDebug(string.Format("Namespace is empty, probably because it was placed directly within Script Root. Not adding namespace to script."));
                return null;
            }

            return namespaceValue;
        }

        /// <summary>
        /// Returns the list of directories that should be ignored.
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetIgnoredDirectories()
        {
            var retval = new HashSet<string>();
            var smartNSSettings = SmartNSSettings.GetSerializedSettings();
            var directoryDenyListSettingsValue = smartNSSettings.FindProperty("m_DirectoryIgnoreList").stringValue;

            foreach (var directoryPathPart in directoryDenyListSettingsValue.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var dpp = directoryPathPart.Trim();
                if (dpp.StartsWith("/"))
                {
                    dpp = dpp.Remove(0, 1);
                }
                if (!dpp.StartsWith("Assets"))
                {
                    dpp = "Assets/" + dpp;
                }

                var fullDenyDirectoryPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + dpp;
                var di = new DirectoryInfo(fullDenyDirectoryPath);
                if (di.Exists)
                {
                    retval.Add(di.FullName);
                    foreach (var subDir in di.GetDirectories("*.*", SearchOption.AllDirectories))
                    {
                        retval.Add(subDir.FullName);
                    }
                }
                else
                {
                    Debug.LogWarning(string.Format("Directory {0} in SmartNS Project Setting's Directory Ignore List was not a valid directory.", dpp));
                }
            }

            return retval;
        }

        #region Debug

        public static void WriteDebug(string message)
        {
            // Note that we depend on calls to have set _shouldWriteDebugLogInfo, for performance reasons.
            if (_shouldWriteDebugLogInfo)
            {
                Debug.Log(string.Format("SmartNS Debug: {0}", message));
            }
        }

        #endregion
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

class FolderSync
{
    // Variables to store folder paths, synchronization interval, and log file path
    static string sourceFolder = string.Empty; // Source folder path
    static string replicaFolder = string.Empty; // Replica folder path
    static int syncInterval = 60; // Default synchronization interval in seconds
    static string logFilePath = string.Empty; // Log file path

    static void Main(string[] args)
    {
        // Get the directory where the program is running
        string programDirectory = Directory.GetCurrentDirectory();

        // Define default dynamic paths
        string defaultSourceFolder = Path.Combine(programDirectory, "Folders", "SourceFolder");
        string defaultReplicaFolder = Path.Combine(programDirectory, "Folders", "ReplicaFolder");
        string defaultLogFilePath = Path.Combine(programDirectory, "LogFile", "log.txt");

        // Parse command-line arguments or use defaults
        sourceFolder = args.Length > 0 ? args[0] : defaultSourceFolder;
        replicaFolder = args.Length > 1 ? args[1] : defaultReplicaFolder;
        syncInterval = args.Length > 2 ? int.Parse(args[2]) : syncInterval;
        logFilePath = args.Length > 3 ? args[3] : defaultLogFilePath;

        // Display the configuration
        Console.WriteLine($"Source Folder: {sourceFolder}");
        Console.WriteLine($"Replica Folder: {replicaFolder}");
        Console.WriteLine($"Log File: {logFilePath}");
        Console.WriteLine($"Sync Interval: {syncInterval} seconds");

        // Ensure the source folder exists
        if (!Directory.Exists(sourceFolder))
        {
            Console.WriteLine($"Error: Source folder '{sourceFolder}' does not exist.");
            return;
        }

        // Ensure the replica folder exists or create it
        if (!Directory.Exists(replicaFolder))
        {
            Console.WriteLine($"Replica folder '{replicaFolder}' does not exist. Creating it...");
            Directory.CreateDirectory(replicaFolder);
        }

        // Ensure the directory for the log file exists
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath) ?? throw new InvalidOperationException());

        // Log the start of the synchronization process
        Log("Starting folder synchronization.");

        try
        {
            // Run synchronization continuously
            while (true)
            {
                // Synchronize folders and log changes
                bool anyChangesMade = SynchronizeFolders(sourceFolder, replicaFolder);

                if (anyChangesMade)
                {
                    // Log if changes were detected and synchronized
                    Log("Changes detected and synchronized.");
                }
                else
                {
                    // Log if no changes were detected
                    Log("No changes detected.");
                }

                // Wait for the specified interval before the next synchronization
                Thread.Sleep(syncInterval * 1000);
            }
        }
        catch (Exception ex)
        {
            // Catch and log any unexpected errors
            Log($"An error occurred: {ex.Message}");
        }
    }

    static bool SynchronizeFolders(string sourceDir, string replicaDir)
    {
        bool changesMade = false; // Track whether any changes were made during synchronization
        Console.WriteLine($"Synchronizing from {sourceDir} to {replicaDir}");

        // Ensure the replica directory exists
        if (!Directory.Exists(replicaDir))
        {
            Directory.CreateDirectory(replicaDir); // Create the replica directory if missing
            Log($"Created directory: {replicaDir}");
            changesMade = true; // Mark the change
        }

        // Copy or update files from the source folder to the replica folder
        foreach (var sourceFilePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            // Determine the relative path of the file within the source directory
            var relativePath = Path.GetRelativePath(sourceDir, sourceFilePath);
            var replicaFilePath = Path.Combine(replicaDir, relativePath);

            try
            {
                if (File.Exists(replicaFilePath))
                {
                    // If the file exists in the replica, check if it matches the source file
                    if (!FilesAreEqual(sourceFilePath, replicaFilePath))
                    {
                        // If the files are different, overwrite the replica file with the source file
                        File.Copy(sourceFilePath, replicaFilePath, true);
                        Log($"Updated file: {replicaFilePath}");
                        changesMade = true;
                    }
                }
                else
                {
                    // If the file does not exist in the replica, copy it from the source
                    Directory.CreateDirectory(Path.GetDirectoryName(replicaFilePath)); // Make sure directory exists
                    File.Copy(sourceFilePath, replicaFilePath);
                    Log($"Copied new file: {replicaFilePath}");
                    changesMade = true;
                }
            }
            catch (Exception ex)
            {
                // Log any errors during file operations
                Log($"Error processing file {sourceFilePath}: {ex.Message}");
            }
        }

        // Delete files in the replica that no longer exist in the source
        foreach (var replicaFilePath in Directory.GetFiles(replicaDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(replicaDir, replicaFilePath);
            var sourceFilePath = Path.Combine(sourceDir, relativePath);

            if (!File.Exists(sourceFilePath))
            {
                // If the file exists in the replica but not in the source, delete it
                File.Delete(replicaFilePath);
                Log($"Deleted file: {replicaFilePath}");
                changesMade = true;
            }
        }

        // Delete directories in the replica that no longer exist in the source
        foreach (var replicaSubDir in Directory.GetDirectories(replicaDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(replicaDir, replicaSubDir);
            var sourceSubDir = Path.Combine(sourceDir, relativePath);

            if (!Directory.Exists(sourceSubDir))
            {
                // If the directory exists in the replica but not in the source, delete it
                Directory.Delete(replicaSubDir, true); // Recursively delete the directory
                Log($"Deleted directory: {replicaSubDir}");
                changesMade = true;
            }
        }

        return changesMade; // Return whether any changes were made
    }

    static bool FilesAreEqual(string filePath1, string filePath2)
    {
        try
        {
            using var md5 = MD5.Create(); // Create an MD5 hashing object
            byte[] hash1, hash2;

            // Compute the hash for the first file
            using (var stream1 = File.OpenRead(filePath1))
            {
                hash1 = md5.ComputeHash(stream1);
            }
            // Compute the hash for the second file
            using (var stream2 = File.OpenRead(filePath2))
            {
                hash2 = md5.ComputeHash(stream2);
            }

            // Compare the two hashes and return true if they are equal
            return BitConverter.ToString(hash1) == BitConverter.ToString(hash2);
        }
        catch (Exception ex)
        {
            // Log any errors during file comparison
            Log($"Error comparing files {filePath1} and {filePath2}: {ex.Message}");
            return false;
        }
    }

    static void Log(string message)
    {
        string logMessage = $"{DateTime.Now}: {message}"; // Timestamp the log message
        Console.WriteLine(logMessage); // Print the log message to the console

        try
        {
            // Append the log message to the specified log file
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Handle errors during log writing
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }
}

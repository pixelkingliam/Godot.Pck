using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Godot.Pck;

public class Package
{
    private bool _pendingCreation = false;
    private string[] _cache;
    private bool _cacheNeedsUpdate;
    private string _path;
    /// <summary>
    /// The path of godotpcktool used by the wrapper
    /// </summary>
    public static string Godotpcktool
    {
        get
        {
            //var file = new FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/godotpcktool")
            if (OperatingSystem.IsWindows())
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/godotpcktool.exe";
            }
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/godotpcktool";
        }
    }
    /// <summary>
    /// Temporary paths for *Nix and NT
    /// </summary>
    public static string TemporaryPath
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return Environment.GetEnvironmentVariable("%TMP%") ?? "C:/Windows/Temp";
            }
            return "/tmp";
        }
    }
    /// <summary>
    /// Creates or opens a pck for editing.
    /// </summary>
    /// <param name="pckPath">
    /// The .pck file on disk.
    /// </param>
    /// <param name="mode"></param>
    public Package(string pckPath, PckMode mode)
    {
        if (mode == PckMode.Create)
        {
            _pendingCreation = true;
            _cacheNeedsUpdate = true;
        }

        _cacheNeedsUpdate = false;
        _cache = GetFiles();
        _path = pckPath;

    }
    /// <summary>
    /// Returns all the files in the package, use GetFiles(string) for getting files in a directory.
    /// </summary>
    /// <returns>
    /// All the files in the package
    /// </returns>
    public string[] GetFiles()
    {
        if (_pendingCreation)
        {
            return [];
        }
        var cmd = new ProcessStartInfo
        {
            FileName = Godotpcktool,
            Arguments = $"-p {_path} -a l",
            RedirectStandardOutput = true
        };

        using var proc = Process.Start(cmd);
        List<string> output = new();
        proc.OutputDataReceived += new DataReceivedEventHandler
        (
            delegate(object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null && e.Data.StartsWith("res:/"))
                    output.Add(e.Data.Split("res:/")[1].Split(" size")[0]);
            }
        );
        proc.BeginOutputReadLine();
        proc.WaitForExit();
        return output.ToArray();

    }
    /// <summary>
    /// Deletes a file in the .pck, this will also delete the containing directory if the file was a unique child.
    /// </summary>
    /// <param name="filePath">The internal path</param>
    public void DeleteFile(string filePath)
    {
        if (!GetFiles().Contains(filePath))
        {
            return;
        }
        var pwdPath = $"{TemporaryPath}/{Random.Shared.Next()}";
        Extract(pwdPath);

        File.Delete($"{pwdPath}/{filePath}");
        if (Directory.GetFileSystemEntries(Path.GetDirectoryName($"{pwdPath}/{filePath}")!).Length == 0)
        {
            Directory.Delete(Path.GetDirectoryName($"{pwdPath}/{filePath}")!);
        }
        File.Delete(_path);
        Pack(pwdPath, true);
        Directory.Delete(pwdPath, true);
    }
    /// <summary>
    /// Extracts the .pck into the given 
    /// </summary>
    /// <param name="dest"></param>
    public void Extract(string dest)
    {
        if (_pendingCreation)
        {
            return;
        }
        var cmd = new ProcessStartInfo
        {
            FileName = Godotpcktool,
            Arguments = $"-p {_path} -a e -o {dest}",
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        var proc =Process.Start(cmd);
        proc?.WaitForExit();
    }

    public bool Pack(string dir, bool overwrite = false)
    {
        if (!_pendingCreation && !overwrite)
            throw new ConstraintException("Package file exists!");
        if (!_pendingCreation)
        {
            File.Delete(_path);
        }

        if (!Directory.Exists(dir))
        {
            throw new DirectoryNotFoundException();
        }

        var items = string.Empty;
        foreach (var entry in Directory.GetFileSystemEntries(dir))
        {
            // items += $"{entry.Split(dir)[1]} ";
            items += $"{Path.GetFileName(entry)} ";
            var cmd = new ProcessStartInfo
            {
                FileName = Godotpcktool,
                Arguments = $"-p {_path} -a a {Path.GetFileName(entry)}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = dir
            };
        
            var proc = Process.Start(cmd);
            Console.WriteLine($"{Godotpcktool} -p {_path} -a a {Path.GetFileName(entry)}");
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            proc.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
            };
            proc.ErrorDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
            };
            proc.Start();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                return false;
            }
        }

        _pendingCreation = false;
        return true;



    }
    public string[] GetFiles(string dir)
    {
        if (_cacheNeedsUpdate == false)
        {
            return _cache;
        }
        
        var files = GetFiles();
        if (!dir.EndsWith('/'))
            dir += '/';
        if (files.Count(f => f.StartsWith(dir) && f.Split(dir)[1].Split("/").Length == 1) == 0)
        {
            throw new Exception();
        }
        _cache = files.Where(f => f.StartsWith(dir) && f.Split(dir)[1].Split("/").Length == 1).ToArray();
        return _cache;
    }

    public AddError AddFile(Stream data, string internalPath)
    {
        if (internalPath.StartsWith('/'))
        {
            internalPath = internalPath.Substring(1);
        }

        var pwdPath = $"{TemporaryPath}/{Random.Shared.Next()}";
        Directory.CreateDirectory($"{pwdPath}/{Path.GetDirectoryName(internalPath)}");
        var file = File.Open($"{pwdPath}/{internalPath}", FileMode.Create);
        data.CopyTo(file);
        file.Close();
        
        var cmd = new ProcessStartInfo
        {
            FileName = Godotpcktool,
            Arguments = $"-p {_path} -a a \"{internalPath}\"",
            WorkingDirectory = pwdPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(cmd);
        StringBuilder outputBuilder = new();

        
        if (proc == null)
            return AddError.Null;
        proc.ErrorDataReceived += new DataReceivedEventHandler
        (
            delegate(object sender, DataReceivedEventArgs e)
            {
                outputBuilder.Append(e.Data);
            }
        );
        proc.OutputDataReceived += new DataReceivedEventHandler
        (
            delegate(object sender, DataReceivedEventArgs e)
            {
                outputBuilder.Append(e.Data);
            }
        );
        proc.BeginErrorReadLine();
        proc.BeginOutputReadLine();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            Console.WriteLine(outputBuilder.ToString());
            return AddError.UnknownError;

        }
        Directory.Delete(pwdPath, true);
        _cacheNeedsUpdate = true;
        _pendingCreation = false;
        return AddError.Ok;
        
    }
    public AddError AddFile(string filePath, string internalPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException();
        }
        if (internalPath.StartsWith('/'))
        {
            internalPath = internalPath.Substring(1);
        }


        var pwdPath = $"{TemporaryPath}/{Random.Shared.Next()}";
        Directory.CreateDirectory($"{pwdPath}/{Path.GetDirectoryName(internalPath)}");
        File.Copy(filePath, $"{pwdPath}/{internalPath}");
        
        var cmd = new ProcessStartInfo
        {
            FileName = Godotpcktool,
            Arguments = $"-p {_path} -a a {internalPath}",
            WorkingDirectory = pwdPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(cmd);
        StringBuilder outputBuilder = new();

        
        if (proc == null)
            return AddError.Null;
        proc.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
        {
            outputBuilder.Append(e.Data);
        };
        proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
        {
            outputBuilder.Append(e.Data);
        };
        proc.BeginErrorReadLine();
        proc.BeginOutputReadLine();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            Console.WriteLine(outputBuilder.ToString());
            return AddError.UnknownError;

        }
        Directory.Delete(pwdPath, true);
        _cacheNeedsUpdate = true;
        _pendingCreation = false;
        return AddError.Ok;
    }
    public AddError AddFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException();
        }
        var cmd = new ProcessStartInfo
        {
            FileName = Godotpcktool,
            Arguments = $"-p {_path} -a a {Path.GetFileName(filePath)}",
            WorkingDirectory = Path.GetDirectoryName(filePath)
        };
        using var proc = Process.Start(cmd);
        if (proc == null)
            return AddError.UnknownError;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            return AddError.UnknownError;

        }
        _cacheNeedsUpdate = true;
        _pendingCreation = false;
        return AddError.Ok;

        

    }
}

public enum AddError
{
    UnknownError,
    Ok,
    Null
}

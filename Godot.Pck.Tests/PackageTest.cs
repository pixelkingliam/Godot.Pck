using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Godot.Pck.Tests;

[TestFixture]
[TestOf(typeof(Package))]
public class PackageTest
{

    [Test]
    public void ExecutablePaths()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.That(Path.GetFullPath(Package.Godotpcktool), Is.EqualTo(Path.GetFullPath("godotpcktool.exe")));
        }
        Assert.That(Path.GetFullPath(Package.Godotpcktool), Is.EqualTo(Path.GetFullPath("godotpcktool")));

    }

    [Test]
    public void AddRootFilePath()
    {
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        File.WriteAllText($"{Package.TemporaryPath}/ExampleFile.txt", "Hello World");
        Assert.That(pck.AddFile($"{Package.TemporaryPath}/ExampleFile.txt"), Is.EqualTo(AddError.Ok));
        var cmd = new ProcessStartInfo();
        cmd.FileName = Package.Godotpcktool;
        cmd.Arguments = $"{Package.TemporaryPath}/test.pck -a l";
        cmd.WorkingDirectory = Package.TemporaryPath;
        cmd.RedirectStandardOutput = true;
        StringBuilder outputBuilder = new();

        using var proc = Process.Start(cmd);
        proc.OutputDataReceived += new DataReceivedEventHandler
        (
            delegate(object sender, DataReceivedEventArgs e)
            {
                outputBuilder.Append(e.Data);
            }
        );
        proc.Start();

        proc.BeginOutputReadLine();
        proc.WaitForExit();
        
        Assert.That(outputBuilder.ToString(), Is.EqualTo("Contents of '/tmp/test.pck':res://ExampleFile.txt size: 11end of contents."));
    }
    [Test]
    public void AddFileStream()
    {
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        Assert.That(pck.AddFile(new MemoryStream("Hello World"u8.ToArray()), "/Dir1/Dir2/File"), Is.EqualTo(AddError.Ok));
        var cmd = new ProcessStartInfo();
        cmd.FileName = Package.Godotpcktool;
        cmd.Arguments = $"{Package.TemporaryPath}/test.pck -a l";
        cmd.WorkingDirectory = Package.TemporaryPath;
        cmd.RedirectStandardOutput = true;
        StringBuilder outputBuilder = new();

        using var proc = Process.Start(cmd);
        proc.OutputDataReceived += new DataReceivedEventHandler
        (
            delegate(object sender, DataReceivedEventArgs e)
            {
                outputBuilder.Append(e.Data);
            }
        );
        proc.Start();

        proc.BeginOutputReadLine();
        proc.WaitForExit();
        
        Assert.That(outputBuilder.ToString(), Is.EqualTo("Contents of '/tmp/test.pck':res://Dir1/Dir2/File size: 11end of contents."));
    }

    [Test]
    public void AddFilePath()
    {
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        File.WriteAllText($"{Package.TemporaryPath}/ExampleFile.txt", "Hello World");
        Assert.That(pck.AddFile($"{Package.TemporaryPath}/ExampleFile.txt", "/Dir1/Dir2/File.txt"), Is.EqualTo(AddError.Ok));
    }

    [Test]
    public void ListAll()
    {
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        Assert.That(pck.AddFile(new MemoryStream("helloworld"u8.ToArray()), "/ImADir/HelloWorld.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.AddFile(new MemoryStream("Im a bigger file"u8.ToArray()), "/Im on the root!.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.GetFiles(), Is.EqualTo(new string[] {"/Im on the root!.txt", "/ImADir/HelloWorld.txt"}));
    }

    [Test]
    public void ListDir()
    {
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        Assert.That(pck.AddFile(new MemoryStream("helloworld"u8.ToArray()), "/ImADir/HelloWorld.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.AddFile(new MemoryStream("helloworld"u8.ToArray()), "/ImADir/File2.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.AddFile(new MemoryStream("Im a bigger file"u8.ToArray()), "/Im on the root!.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.GetFiles("/ImADir"), Is.EqualTo(new[] {"/ImADir/File2.txt", "/ImADir/HelloWorld.txt"}));
    }

    [Test]
    public void Pack()
    {
        Directory.Delete($"{Package.TemporaryPath}/godotpack", true);
        File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        Directory.CreateDirectory($"{Package.TemporaryPath}/godotpack/");
        File.WriteAllText($"{Package.TemporaryPath}/godotpack/gpcktest1.txt", "Hii");
        File.WriteAllText($"{Package.TemporaryPath}/godotpack/gpcktest2.txt", "Hii");
        Assert.That(pck.Pack($"{Package.TemporaryPath}/godotpack"));
        Assert.That(File.Exists($"{Package.TemporaryPath}/test.pck"));
        
        Assert.That(pck.GetFiles(),  Is.EqualTo(new[] {"/gpcktest1.txt", "/gpcktest2.txt"}));
    }

    [Test]
    public void Extract()
    {
        if (Directory.Exists($"{Package.TemporaryPath}/godotpack"))
            Directory.Delete($"{Package.TemporaryPath}/godotpack/", true);
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        Assert.That(pck.AddFile(new MemoryStream("helloworld"u8.ToArray()), "/ImADir/HelloWorld.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.AddFile(new MemoryStream("helloworld"u8.ToArray()), "/ImADir/File2.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.AddFile(new MemoryStream("Im a bigger file"u8.ToArray()), "/ImOnTheRoot!.txt"), Is.EqualTo(AddError.Ok));
        Directory.CreateDirectory($"{Package.TemporaryPath}/godotpack/");
        pck.Extract($"{Package.TemporaryPath}/godotpack/");
        Assert.That(Directory.GetFileSystemEntries($"{Package.TemporaryPath}/godotpack"), Is.EqualTo(new []{"/tmp/godotpack/ImADir", "/tmp/godotpack/ImOnTheRoot!.txt"}));
    }
    [Test]
    public void DeleteFile()
    {
        if (File.Exists($"{Package.TemporaryPath}/test.pck"))
            File.Delete($"{Package.TemporaryPath}/test.pck");
        var pck = new Package($"{Package.TemporaryPath}/test.pck", PckMode.Create);
        Assert.That(pck.AddFile(new MemoryStream("helloworld"u8.ToArray()), "/ImADir/HelloWorld.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.AddFile(new MemoryStream("Im a bigger file"u8.ToArray()), "/ImOnTheRoot!.txt"), Is.EqualTo(AddError.Ok));
        Assert.That(pck.GetFiles(), Is.EqualTo(new string[] { "/ImADir/HelloWorld.txt", "/ImOnTheRoot!.txt",}));
        pck.DeleteFile("/ImOnTheRoot!.txt");
        Assert.That(pck.GetFiles(), Is.EqualTo(new string[] {"/ImADir/HelloWorld.txt"}));
    }

}
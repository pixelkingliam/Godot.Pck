# Godot.Pck
Godot.Pck is a C# wrapper around Henri Hyyryläinen
's GodotPckTool. Providing a simple-to-use library for handling Godot 4 .pck files with C#.

## Installation
Godot.Pck can be found on nuget.org as `Pixel.Godot.Pck`

# Contributions

We are open to any contribution deemed helpful to the project.

We lack macOS support and Godot 3 support!

# Usage

Include the `Godot.Pck` namespace

## Setting up a Package

### Create a Package 

Create a new pck file using the Package class's constructor with the PckMode.Create mode

```csharp
Package(path, PckMode.Create)
```
For example:
```csharp
var myPck = Package("package.pck", PckMode.Create)
```

If a file exists at `path` it will be overwritten, if it's a directory an error will be thrown.

### Opening a Package

If you already have a pck file you can use the Package class's constructor with the PckMode.Open mode

```csharp
Package(path, PckMode.Open)
```
For example:
```csharp
var myPck = Package("package.pck", PckMode.Open)
```
If the given file path does not exist an error will be thrown.

## Adding files to the Package

### Adding a file directly

For adding files to the pck file you can use the `AddFile` functions
All of these functions return an AddError indicating the result of the function.


```csharp
Package.AddFile(string filePath)
```
Adds `filePath` to the root directory of the pck file

```csharp
Package.AddFile(string filePath, string internalPath)
```
Adds a `filePath` to `internalPath` in the pck file. Creates parent directories if needed.

```csharp
Package.AddFile(Stream data, string internalPath)
```

Adds a file from memory to `internalPath` in the pck file, as above it will create parent directories.

### Packing a directory

If you already have your pck laid out as a directory you use

```csharp
Package.Pack(string dir, bool overwrite)
```

This will create a pck file, writing all the contents of `dir` to the pck file's root
If overwrite is false, this will throw an error if the file was opened via PckMode.Open OR if you modified the pck file after creating it with PckMode.Create

Will throw an exception if `dir` does not exist.

## Listing the files in the Package

This can be done with the `GetFiles` functions.

### Getting all the files

```csharp
Package.GetFiles()
```

This will return a `string[]` with the path of every file in the package

### Getting all the files and directory in a directory

```csharp
Package.GetFiles(string dir)
```

This will return a `string[]` with the path of every file and directory in `dir` inside in the package.
Returned directories will always end with `/` 

## Deleting files in the Package

You can delete files using the `DeleteFile` function.

```csharp
Package.DeleteFile(string filePath)
```

This will delete the file in the pck, if the remaining directory is empty it will be deleted.

## Extracting the package

To extract the pck file into an empty directory you can use the `Extract` function.

```csharp
Package.Extract(string dest)
```

This will extract every file in the pck file into `dest`.

If `dest` does not exist; it will be created.

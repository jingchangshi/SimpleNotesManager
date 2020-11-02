# Overview

Simple Notes Manager(SNM) converts markdown files to html file and organizes them in another directory. It also generates index.html as a summary of them. Then it monitors any change in the source directory to process them in the real-time.

Publish cmd: `dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true`

# Screenshots

![Left: source files; Right: output files](doc/md2html.png)

![Left: overview of index.html; Right: results by clicking the plus symbol to expand the item](doc/index.html.png)

# Requirements

Install `pandoc` and put it in the PATH.

# Usage

```
SimpleNotesManager.exe config.json
```

You can create a shortcut of `SimpleNotesManager.exe`. Then edit the properties of the shortcut. Modify its `target` to something like `C:\Users\Me\SimpleNotesManager.exe C:\Users\Me\NotesProject\config.json`. Now you can just double click this shortcut to start the application.

## Configurations

Check the example `monitor.json` in the `doc` directory.

# TODO

1. GUI

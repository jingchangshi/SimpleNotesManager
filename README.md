# Overview

Simple Notes Manager(SNM) converts markdown files to html file and organizes them in another directory. It also generates index.html as a summary of them. Then it monitors any change in the source directory to process them in the real-time.

Publish cmd: `dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishSingleFile=true`.

# Screenshots

![Left: source files; Right: output files](doc/md2html.png)

![Left: overview of index.html; Right: results by clicking the plus symbol to expand the item](doc/index.html.png)

# Requirements

None.

# Usage

Directly download the file in this [Github repo](https://github.com/jingchangshi/NoteProject).
It contains the executable to start the note manager.

## Configurations

Check the example `monitor.json` in the `doc` directory.


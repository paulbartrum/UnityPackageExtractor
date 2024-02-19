# Unity Package Extractor

Extracts the contents of a Unity package file (.unitypackage).

I found that [existing python tool](https://github.com/Cobertos/unitypackage_extractor) for this task couldn't
handle large package files in a reasonable time. This tool should be able to quickly handle packages of essentially unlimited size
(caveat: I've only tested up to 8GB).

## Requirements

The release binary is Windows 10+, 64-bit only. (Other platforms should work if you compile from source.)

## Usage

* Download [UnityPackageExtractor.zip](https://github.com/paulbartrum/UnityPackageExtractor/releases/latest) from the Releases tab.
* Extract into a new directory
* Drag and drop your `.unitypackage` onto `UnityPackageExtractor.exe` (this will output into the same directory as your package file) OR
* Run from the command line with `UnityPackageExtractor.exe [path/to/your/package.unitypackage] (optional/output/path)`

Note: Unity packages should first be downloaded using the package manager inside Unity.
The .unitypackage file should then be available under the package cache directory (which as of this writing is: `%APPDATA%\Unity\Asset Store-5.x`).
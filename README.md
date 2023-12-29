# Text Asset Dumper
Dumps all text assets from addressables and all localizations.

## Requirements
- [SpaceWarp 1.7.0](https://github.com/SpaceWarpDev/SpaceWarp/releases) or later.

## Installation
1. Download the latest version from the [releases page](https://github.com/KSP2Community/TextAssetDumper/releases).
2. Unzip the archive into your KSP2 installation folder.

## Usage

This mod adds a **DUMP** button to the main menu. After you click on it and wait for a few seconds, all text assets and
localizations will be dumped into `KSP2/BepInEx/plugins/TextAssetDumper/dump`. Inside it, you will find the folders
`text_assets`, which contains subfolders for individual addressable labels, and `localizations`, which contains files
for each localization source.

The dump folder is deleted and recreated every time you dump, so make sure to move any files you want to keep out of it
before dumping again.

The dumped text assets will contain any changes made to them by Patch Manager, so if you want to get the original
text assets, you will need to either uninstall Patch Manager temporarily, or get rid of all the patches that modify the
original text assets.

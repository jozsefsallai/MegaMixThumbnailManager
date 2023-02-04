# Mega Mix Thumbnail Manager

Automatically manage the thumbnails of the songs in Hatsune Miku: Project DIVA
Mega Mix+. This mod will generate a unified thumbnail database for all songs
from the base game and your installed mods, so none of your songs will have
broken thumbnails (hopefully).

**[Download from GameBanana][gb-url]** | **[Download from GitHub][gh-download-url]**

## Usage

Put the mod in your `mods` folder and depending on your setup, either:

- Make sure it's the first mod in the file list, if you don't use the priority
  line.
- Make sure it's the first mod in your priority line, if you use it.

Then just run the game as usual. The mod will do its magic when the game starts.

## Bug Reporting

If you encounter any bugs, please report them on the [GitHub issue tracker][issues-url]
or in a comment on the [GameBanana page][gb-url].

Getting my attention on Discord is possible but it makes it harder to keep track
of the issues, so please use the methods described above, if possible.

## Building

To build this mod from source, you will need Visual Studio (preferably 2019) and
the C# development toolchains.

**1. Clone the repository:**

```sh
git clone git@github.com:jozsefsallai/MegaMixThumbnailManager.git
cd MegaMixThumbnailManager
```

**2. Download [DllExport][dllexport-bat-url] and place it into the root of the
repo.**

_The DllExport batch file is not included in this repository to avoid cluttering
it._

**3. Clone MikuMikuLibrary into the `packages` directory:**

```sh
cd packages
git clone git@github.com:blueskythlikesclouds/MikuMikuLibrary.git
```

**4. Open the MikuMikuLibrary solution in Visual Studio and build it.**

**5. Open the MegaMixThumbnailManager solution in Visual Studio and build it.**

Make sure you're selecting the **x64** target and the **Release** configuration.

## Credits

- [DivaModLoader][divamodloader-url]
- [MikuMikuLibrary][mikumikulibrary-url]
- [Newtonsoft.Json][newtonsoft-url]
- [DllExport][dllexport-url]
- BunBun for testing.

## License

MIT. See the LICENSE file for more details.

[gb-url]: https://gamebanana.com/mods/414252
[gh-download-url]: https://github.com/jozsefsallai/MegaMixThumbnailManager/releases/latest
[issues-url]: https://github.com/jozsefsallai/MegaMixThumbnailManager/issues
[dllexport-bat-url]: https://3f.github.io/DllExport/releases/latest/manager/
[divamodloader-url]: https://github.com/blueskythlikesclouds/DivaModLoader
[mikumikulibrary-url]: https://github.com/blueskythlikesclouds/MikuMikuLibrary
[newtonsoft-url]: https://github.com/JamesNK/Newtonsoft.Json
[dllexport-url]: https://github.com/3F/DllExport

<!-- Common Project Tags: 
desktop-app 
desktop-application 
dotnet 
dotnet-core 
netcore 
tool 
tools 
vbnet 
visualstudio 
windows 
windows-app 
windows-application 
windows-applications 
windows-forms 
winforms 
playstation
playstation-3
ps3
playstation3
consolegames
games
videogame
emulators
 -->

# PS3 Quick Disc Decryptor (PS3QDD) 💿🔑

### User-friendly GUI to decrypt Redump's PS3 disc images using PS3Dec.

![screenshot](/Images/REDUMP.png)

------------------

## 👋 Introduction

**PS3 Quick Disc Decryptor** or just **PS3QDD** is an application that allows you to decrypt PS3 disc images (\*.iso files) in a friendly way.

The decrypted PS3 disc images will work with [RPCS3](https://rpcs3.net/) emulator (*if marked as playable in their compatibility list*).

## 👌 Features

 - Simple, user-friendly graphical user-interface.
 - Designed for batch processing.
 - **Smart fuzzy matching** - Automatically suggests likely ISO-to-key matches when exact filename matching fails, with visual confidence indicators.
 - Supports zip archives (for both PS3 disc images and decryption key files).
 - **Comprehensive failure tracking** - Shows detailed success/failure summary with specific files listed and error reasons.
 - **Automatic ISO extraction** - Option to extract decrypted ISOs using 7-Zip (fast) or DiscUtils library.
 - Meticulous status report and error handling.
 - Logging features.
 - Allows to abort the decryption procedure on demand.
 - Real-time PS3Dec.exe output display with proper DPI scaling.
 - Automatically deletes successfully decrypted disc images to save disk space (optional).
 - Prevents cleanup of failed decryptions to allow investigation and retry.

## 🖼️ Screenshots

![screenshot](/Images/Screenshot_06.png)

![screenshot](/Images/Screenshot_07.png)

## 📝 Requirements

- Microsoft Windows OS with [net 6.0 desktop runtime](https://dotnet.microsoft.com/download/dotnet/6.0).
- [Microsoft Visual C++ 2010 runtime (x86)](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2010-vc-100-sp1-no-longer-supported) (for running **ps3dec.exe**)
- **[7-Zip](https://www.7-zip.org/)** (optional, for fast ISO extraction - recommended)

## 🤖 Getting Started

Open the program, configure the *self-explanatory* program settings, and finally press the button with name 'Start Decryption'.

### 🔨 Building from Source

For developers who want to build the application from source code, see [BUILD.md](BUILD.md) for comprehensive build instructions, prerequisites, and troubleshooting.

### ✨ New Features Guide

#### Smart Fuzzy Matching
When ISO filenames don't exactly match decryption key filenames, the program will:
- Analyze similarity using multiple algorithms (Levenshtein distance, token matching, Game ID extraction)
- Show a dialog with ranked suggestions and confidence percentages
- Allow you to accept, skip, or manually select the correct key
- Display visual confidence indicators (green for high confidence, yellow/orange for medium)

#### ISO Extraction
After successful decryption, the program can automatically extract ISOs:
1. Enable "Extract ISOs after decryption" in settings
2. Choose extraction method:
   - **7-Zip** (recommended): Fastest, requires 7z.exe installation
   - **DiscUtils**: Slower, pure .NET library (no external dependencies)
3. Configure 7-Zip path if using 7-Zip method (default: `C:\Program Files\7-Zip\7z.exe`)

The extraction will be skipped if any decryptions fail, preventing incomplete results.

#### Comprehensive Results
At the end of each batch operation, you'll see:
- Summary: "3 succeeded, 1 failed"
- Complete list of successful files (✓)
- Complete list of failed files (✗) with specific error reasons
- Failed ISOs are preserved (not deleted) for investigation

## 🌐 External resources

### Encrypted PS3 ISOs

To use this program you will need encrypted PS3 disc images (\*.iso files) from the **Redump** group. It will not work with PS3 disc images from **NO-INTRO** or other groups.

1. You can download **Redump**'s encrypted PS3 disc images from one of these links:

    - [Myrient](https://myrient.erista.me/files/Redump/Sony%20-%20PlayStation%203/)
    - [Archive.org](https://archive.org/details/@cvlt_of_mirrors?query=%22Sony+Playstation+3%22+%22Redump.org%22&sort=title)

2. Once you have your encrypted PS3 disc images, put all the \*.iso files together in the same folder, like this:

    ![screenshot](/Images/Screenshot_02.png)

3. Finally, in the program's user interface you just need to select the directory containing the encrypted PS3 ISO files by doing click in the following button:
    
    ![screenshot](/Images/Screenshot_03.png)

    💡 Tip: You can put all the \*.iso files in a folder with name "Encrypted" inside the program directory to skip this step.

    ❗ Note that the program will **not** perform a recursive \*.iso file search.

### Decryption keys

To use this program you will need decryption keys for the **Redump**'s encrypted PS3 ISO files, which are distributed as plain text files that each contain a string of 32 characters long.

1. Download the desired PS3 decryption keys from one of these links:

    - [Archive.org](https://archive.org/download/video_game_keys_and_sbi) (*you only require to download and extract the "Disc Keys TXT" zipped file*)
    - [Myrient](https://myrient.erista.me/files/Redump/Sony%20-%20PlayStation%203%20-%20Disc%20Keys%20TXT/)
    - [Aldo's Tools](https://ps3.aldostools.org/dkey.html)
    
2. Once you have your desired decryption keys, put all the \*.dkey files together in the same folder, like this:

    ![screenshot](/Images/Screenshot_01.png)

3. Finally, in the program's user interface you just need to select the directory containing the decryption keys by doing click in the following button: 

    ![screenshot](/Images/Screenshot_04.png)

    💡 Tip: You can put all the \*.dkey files in a folder with name "Keys" inside the program directory to skip this step.

    ❗ Note that the program will **not** perform a recursive \*.dkey file search.

### PS3Dec.exe

To use this program you will need a copy of **PS3Dec.exe** file, which is actually included in this package, however if you want to use your own:

1. Download **PS3Dec.exe** from one of these links: 

    - [al3xtjames's PS3Dec.exe from RomHacking.net](https://www.romhacking.net/utilities/1456/)
    - [al3xtjames's PS3Dec.exe from ConsoleMods.org](https://consolemods.org/wiki/File:PS3DecR5.7z)
           
      *(✅ This is the one tested and already included in the program package)*

    - [al3xtjames's Github repository](https://github.com/al3xtjames/PS3Dec) or one of its [forks](https://github.com/al3xtjames/PS3Dec/forks).

      (*❗ I have not tested any of those forks nor checked if they are virus free. Use them at your own risk.*)

       ❗  Do **not** try to use **PS3Dec.exe** from [Redrrx's Github repository](https://github.com/Redrrx/ps3dec), since it was rewrote using a different (incompatible) command-line syntax with my program.

2. Once you have your copy of **PS3Dec.exe**, in the program's user interface you just need to select the **PS3Dec.exe** file by doing click in the following button: 

    ![screenshot](/Images/Screenshot_05.png)

    💡 Tip: You can put the **PS3Dec.exe** inside the program directory - *overwriting the included one or making a backup* -  to skip this step.

## 🔄 Change Log

Explore the complete list of changes, bug fixes and improvements across different releases by clicking [here](/Docs/CHANGELOG.md).

## 🏆 Credits

This work relies on the following resources:

 - [ElektroStudios's PS3 Quick Disc Decryptor](https://github.com/ElektroStudios/PS3-Quick-Disc-Decryptor) - Original project
 - [al3xtjames's PS3Dec](https://github.com/al3xtjames/PS3Dec)
 - [DiscUtils](https://github.com/discutils/discutils) - ISO9660 filesystem reading
 - [7-Zip](https://www.7-zip.org/) - Fast ISO extraction (optional)
 - [WinCopies Windows API Code Pack](https://wincopies.com/windows_api_codepack/)
 - [Redump Disc Preservation Project's PS3 resources](http://redump.org/discs/system/ps3/)

🤔 Some PS3Dec GUI alternatives to my program that you would try:

 - [PS3Dec Simple GUI](https://consolemods.org/wiki/File:PS3Dec_Simple_GUI_1.16.zip)
 - [PS3 ISO Decryptor GUI](https://github.com/akinozgen/ps3dec-gui) (*❗ Ensure to use the PowerShell script instead of the standalone executable that has 36 virus alerts out of 72...*)

## ⚠️ Disclaimer:

This Work (the repository and the content provided in) is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the Work or the use or other dealings in the Work.

This Work has no affiliation, approval or endorsement by Sony neither by the author(s) of any third-party resources used by this Work.

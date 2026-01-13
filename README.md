# PyMacInstall - Python Environment Installer for macOS

A .NET MAUI application for macOS that automates the installation of Python, cloning of Git repositories, and setting up Python virtual environments with package installation.

## Features

- **Python Installation**: Installs Python via Homebrew or directs to python.org downloads
- **Git Repository Cloning**: Clones repositories using LibGit2Sharp
- **Virtual Environment Setup**: Creates Python virtual environments automatically
- **Package Installation**: Installs packages from requirements.txt or common packages
- **Mac App Bundles**: Creates native macOS .app bundles for Python applications
- **Launch Scripts**: Generates bash scripts for easy application launching
- **User-Friendly Interface**: Native macOS interface with real-time progress tracking and output logging
- **Automated Workflow**: "Complete Setup" button for fully automated installation

## Requirements

- macOS 14.0 (Sonoma) or later
- .NET 8.0 SDK or later ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
- Git (usually pre-installed on macOS, or install via Xcode Command Line Tools)
- Homebrew (optional but recommended for Python installation)

## Installation

### Prerequisites

1. **Install .NET 8.0 SDK**:
   ```bash
   # Download from Microsoft or use Homebrew
   brew install dotnet-sdk
   
   # Verify installation
   dotnet --version
   ```

2. **Install Git** (if not already installed):
   ```bash
   xcode-select --install
   ```

3. **Install Homebrew** (recommended for Python installation):
   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

## Building the Application

1. Clone or download this repository
2. Open terminal in project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the application:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

Or use the provided build script:
```bash
chmod +x build.sh
./build.sh
```

## Building for Distribution

To create a distributable .app bundle:

```bash
chmod +x build-release.sh
./build-release.sh
```

Or manually:
```bash
dotnet publish -f net8.0-maccatalyst -c Release -p:CreatePackage=true
```

The app bundle will be in:
```
bin/Release/net8.0-maccatalyst/maccatalyst-arm64/PyMacInstall.app
```

Copy to Applications folder:
```bash
cp -r bin/Release/net8.0-maccatalyst/maccatalyst-arm64/PyMacInstall.app ~/Applications/
```

## Usage

### Individual Operations

1. **Install Python**:
   - Check "Python already installed" if you have Python 3.9+ already installed
   - Or select desired Python version (3.9.19 to 3.12.6)
   - Choose installation method (Homebrew recommended)
   - Click "Install Python"

2. **Clone Repository**:
   - Enter Git repository URL (https or ssh)
   - Select destination directory (e.g., ~/Projects/)
   - Click "Clone Repository"

3. **Setup Python Environment**:
   - Check "Create Virtual Environment" to create .venv in project
   - Check "Install Python Packages" to install dependencies
   - Specify target Python program (e.g., main.py, astrofiler.py)
   - Enable "Create Application Launcher" to create Mac .app bundle
   - Click "Setup Environment"

4. **Create Run Scripts**:
   - Creates bash launch scripts with git pull integration
   - Optionally creates native macOS app bundle in ~/Applications
   - Click "Create Run Scripts"

### Automated Installation

1. Configure all options as desired
2. Click "Complete Setup (All Steps)" to run the entire process automatically
3. The application will:
   - Install Python (if not skipped)
   - Clone the specified repository
   - Create virtual environment
   - Install all packages
   - Create launch scripts and app bundle

## Configuration

The application uses `setup.json` for default settings:

```json
{
  "DefaultSettings": {
    "Python": {
      "Version": "3.12.6",
      "InstallPath": "/usr/local/python3",
      "AlreadyInstalled": true
    },
    "Git": {
      "RepositoryUrl": "https://github.com/user/repo.git",
      "ClonePath": "~/Projects/"
    },
    "Application": {
      "AutoDetectPython": true,
      "CreateDesktopShortcut": false,
      "AddPythonToPath": true,
      "TargetProgram": "main.py"
    }
  }
}
```

Settings are automatically saved as you make changes in the UI.

## Python Installation Methods

### Homebrew (Recommended)
- Installs official Python builds
- Automatic PATH configuration
- Easy version management
- Command: `brew install python@3.12`

### Manual Download
- Download from python.org
- Install .pkg file
- Manual PATH configuration may be needed

### Using System Python
- macOS comes with Python 3
- Check with: `python3 --version`
- May need to install via Xcode Command Line Tools

## Package Installation

The application can install packages in two ways:

1. **From requirements.txt**: If found in the cloned repository, packages are installed automatically
2. **Default packages**: If no requirements.txt is found, installs common packages:
   - astropy
   - peewee
   - numpy
   - matplotlib
   - pytz
   - PySide6

## Created Files

After setup, the following files are created in your project directory:

- `.venv/` - Python virtual environment
- `run_<program>.sh` - Bash launch script with git pull
- `~/Applications/<Program>.app` - Native Mac application bundle (optional)

## Mac App Bundle Structure

When creating an app bundle, the structure is:

```
Program.app/
├── Contents/
│   ├── Info.plist          # App metadata
│   ├── MacOS/
│   │   └── program         # Launch script
│   └── Resources/          # Icons and resources
```

The app bundle:
- Appears in Launchpad and Applications
- Can be launched like any Mac application
- Automatically activates virtual environment
- Runs with proper working directory

## Troubleshooting

### Python not found
- Ensure Python is installed: `which python3`
- Check Homebrew installation: `brew list python@3.12`
- Try system Python: `/usr/bin/python3 --version`

### Permission errors during installation
- Some operations require admin password (e.g., Homebrew installation)
- Grant permissions when prompted

### Git clone fails
- Check repository URL is correct
- For private repos, ensure SSH keys are configured
- Try with https URL instead of SSH

### Virtual environment creation fails
- Ensure Python is properly installed
- Check disk space is available
- Verify write permissions in project directory

### Package installation fails
- Check internet connection
- Some packages may need system dependencies
- Try running the command manually in terminal

## Example Workflow

Setting up astrofiler-gui:

1. Launch PyMacInstall
2. Check "Python already installed" if you have Python 3.9+
3. Enter repository URL: `https://github.com/gordtulloch/astrofiler-gui.git`
4. Set clone path: `~/Projects/`
5. Set target program: `astrofiler.py`
6. Enable "Create Application Launcher"
7. Click "Complete Setup (All Steps)"
8. Launch from `~/Applications/Astrofiler.app`

## Technical Details

- **Framework**: .NET 8.0 MAUI
- **UI**: MAUI XAML with native macOS controls
- **Git**: LibGit2Sharp for repository operations
- **Config**: JSON-based configuration with Newtonsoft.Json
- **Platform**: macOS 14.0+ (Mac Catalyst)

## Differences from Windows Version

- Uses Homebrew instead of Windows installers
- Creates bash scripts instead of PowerShell/batch files
- Generates .app bundles instead of .lnk shortcuts
- Uses `python3` command instead of `python.exe`
- Virtual environment activation uses `source` instead of `.bat`
- Path separators are forward slashes (/)
- Uses `~` for home directory expansion

## Project Structure

```
PyMacInstall/
├── App.xaml / App.xaml.cs           # Application entry point
├── MainPage.xaml / MainPage.xaml.cs # Main UI and logic
├── MauiProgram.cs                   # MAUI configuration
├── InstallationConfig.cs            # Configuration model
├── PyMacInstall.csproj              # Project file
├── setup.json                       # Default settings
├── build.sh                         # Debug build script
├── build-release.sh                 # Release build script
└── Resources/                       # App resources
    ├── AppIcon/                     # Application icons
    ├── Splash/                      # Splash screen
    └── Styles/                      # UI styles
```

## License

See LICENSE file for details.

## Contributing

This is a port of PyWinInstall adapted for macOS. Contributions and improvements are welcome.

## Support

For issues, questions, or feature requests, please open an issue on the GitHub repository.

## Acknowledgments

- Ported from PyWinInstall (Windows version)
- Uses LibGit2Sharp for Git operations
- Built with .NET MAUI for cross-platform support

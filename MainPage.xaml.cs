using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace PyMacInstall;

public partial class MainPage : ContentPage
{
    private readonly HttpClient httpClient;
    private InstallationConfig? config;
    private bool isInitializing = true;

    public MainPage()
    {
        InitializeComponent();
        httpClient = new HttpClient();
        
        LoadConfiguration();
        LogOutput("PyMacInstall ready. Select options and click 'Complete Setup' or use individual buttons.");
        
        if (config?.DefaultSettings?.Application?.AutoDetectPython == true)
        {
            CheckForExistingPython();
        }
        
        isInitializing = false;
    }

    private void LoadConfiguration()
    {
        try
        {
            config = InstallationConfig.Load();
            ApplyDefaultSettings();
            LogOutput("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            LogOutput($"Error loading configuration: {ex.Message}");
            config = new InstallationConfig();
        }
    }

    private void ApplyDefaultSettings()
    {
        if (config?.DefaultSettings == null) return;
        
        PythonPathEntry.Text = config.DefaultSettings.Python.InstallPath;
        
        var versions = new[] { "3.12.6", "3.12.5", "3.11.9", "3.10.14", "3.9.19" };
        int versionIndex = Array.IndexOf(versions, config.DefaultSettings.Python.Version);
        if (versionIndex >= 0)
        {
            PythonVersionPicker.SelectedIndex = versionIndex;
        }

        RepoUrlEntry.Text = config.DefaultSettings.Git.RepositoryUrl;
        ClonePathEntry.Text = config.DefaultSettings.Git.ClonePath;
        PythonAlreadyInstalledCheckBox.IsChecked = config.DefaultSettings.Python.AlreadyInstalled;
        CreateDesktopShortcutCheckBox.IsChecked = config.DefaultSettings.Application.CreateDesktopShortcut;
        TargetProgramEntry.Text = config.DefaultSettings.Application.TargetProgram;
        
        UpdatePythonSectionState(!config.DefaultSettings.Python.AlreadyInstalled);
    }

    private void CheckForExistingPython()
    {
        string existingPython = CheckExistingPython(PythonPathEntry.Text);
        if (!string.IsNullOrEmpty(existingPython))
        {
            LogOutput($"Detected existing Python installation: {existingPython}");
            if (config?.DefaultSettings?.Python?.AlreadyInstalled != true)
            {
                PythonAlreadyInstalledCheckBox.IsChecked = true;
                UpdatePythonSectionState(false);
            }
        }
    }

    private void PythonAlreadyInstalledCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        UpdatePythonSectionState(!e.Value);
        LogOutput(e.Value ? "Python installation section disabled - using existing Python installation" 
                          : "Python installation section enabled");
        if (!isInitializing)
            SaveCurrentSettings();
    }

    private void UpdatePythonSectionState(bool isEnabled)
    {
        PythonInstallSection.IsEnabled = isEnabled;
        PythonInstallSection.Opacity = isEnabled ? 1.0 : 0.5;
        InstallPythonButton.IsEnabled = isEnabled;
    }

    private void SaveCurrentSettings()
    {
        try
        {
            if (config?.DefaultSettings == null) return;
            
            config.DefaultSettings.Python.InstallPath = PythonPathEntry.Text;
            config.DefaultSettings.Python.AlreadyInstalled = PythonAlreadyInstalledCheckBox.IsChecked;
            
            if (PythonVersionPicker.SelectedIndex >= 0)
            {
                config.DefaultSettings.Python.Version = PythonVersionPicker.SelectedItem?.ToString() ?? "3.12.6";
            }

            config.DefaultSettings.Git.RepositoryUrl = RepoUrlEntry.Text;
            config.DefaultSettings.Git.ClonePath = ClonePathEntry.Text;
            config.DefaultSettings.Application.CreateDesktopShortcut = CreateDesktopShortcutCheckBox.IsChecked;
            config.DefaultSettings.Application.TargetProgram = TargetProgramEntry.Text;

            config.Save();
            LogOutput("Settings saved to setup.json");
        }
        catch (Exception ex)
        {
            LogOutput($"Warning: Could not save settings: {ex.Message}");
        }
    }

    // Event handlers for auto-saving settings
    private void PythonPathEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (config != null && !isInitializing)
            SaveCurrentSettings();
    }

    private void RepoUrlEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (config != null && !isInitializing)
            SaveCurrentSettings();
    }

    private void ClonePathEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (config != null && !isInitializing)
            SaveCurrentSettings();
    }

    private void TargetProgramEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (config != null && !isInitializing)
            SaveCurrentSettings();
    }

    private void PythonVersionPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (config != null && !isInitializing)
            SaveCurrentSettings();
    }

    private void LogOutput(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OutputEditor.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        });
    }

    private void SetProgress(bool isVisible, double value = 0)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ProgressBar.IsVisible = isVisible;
            ProgressBar.Progress = value;
        });
    }

    private async void InstallPythonButton_Clicked(object sender, EventArgs e)
    {
        await InstallPython();
    }

    private async Task<bool> InstallPython()
    {
        try
        {
            if (PythonAlreadyInstalledCheckBox.IsChecked)
            {
                LogOutput("Skipping Python installation - using existing installation");
                string verifyPython = CheckExistingPython(PythonPathEntry.Text);
                if (!string.IsNullOrEmpty(verifyPython))
                {
                    LogOutput($"Verified existing Python: {verifyPython}");
                    return true;
                }
                else
                {
                    LogOutput("WARNING: Could not verify existing Python installation");
                    bool proceed = await DisplayAlert("Python Verification Failed",
                        "Could not verify existing Python installation.\n\nDo you want to proceed anyway?",
                        "Yes", "No");
                    return proceed;
                }
            }

            SetProgress(true, 0);
            LogOutput("Starting Python installation...");

            string version = PythonVersionPicker.SelectedItem?.ToString() ?? "3.12.6";
            string installPath = PythonPathEntry.Text.Trim();

            if (string.IsNullOrEmpty(installPath))
            {
                LogOutput("ERROR: Please specify a Python installation path.");
                return false;
            }

            // On Mac, Python is best installed via Homebrew
            LogOutput("Mac Python Installation:");
            LogOutput("Option 1 (Recommended): Use Homebrew");
            LogOutput("  Run in Terminal: brew install python@3.12");
            LogOutput("");
            LogOutput("Option 2: Download from python.org");
            LogOutput($"  Visit: https://www.python.org/downloads/release/python-{version.Replace(".", "")}/");
            LogOutput("  Download the macOS installer (.pkg file)");
            LogOutput("");
            
            bool installViaHomebrew = await DisplayAlert("Python Installation Method",
                "Choose installation method:\n\n" +
                "YES: Install via Homebrew (recommended)\n" +
                "NO: Open python.org download page",
                "Homebrew", "Download Page");

            if (installViaHomebrew)
            {
                LogOutput("Checking for Homebrew...");
                
                var checkBrew = await RunCommand("which", "brew");
                if (string.IsNullOrWhiteSpace(checkBrew.output))
                {
                    LogOutput("Homebrew not found. Installing Homebrew first...");
                    LogOutput("This requires admin password and may take a few minutes.");
                    
                    bool installHomebrew = await DisplayAlert("Install Homebrew",
                        "Homebrew is not installed. Install it now?\n\n" +
                        "This will run:\n/bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"",
                        "Yes", "No");
                    
                    if (!installHomebrew)
                    {
                        LogOutput("Python installation cancelled.");
                        return false;
                    }

                    var homebrewInstall = await RunCommand("/bin/bash", 
                        "-c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"");
                    
                    if (homebrewInstall.exitCode != 0)
                    {
                        LogOutput($"ERROR: Failed to install Homebrew: {homebrewInstall.error}");
                        return false;
                    }
                    
                    LogOutput("Homebrew installed successfully!");
                }

                LogOutput($"Installing Python {version} via Homebrew...");
                SetProgress(true, 50);
                
                // Extract major.minor version for Homebrew
                var versionParts = version.Split('.');
                string brewVersion = $"{versionParts[0]}.{versionParts[1]}";
                
                var result = await RunCommand("brew", $"install python@{brewVersion}");
                
                if (result.exitCode == 0)
                {
                    LogOutput($"Python {version} installed successfully!");
                    LogOutput("Verifying installation...");
                    
                    var pythonCheck = await RunCommand("python3", "--version");
                    LogOutput($"Python version: {pythonCheck.output}");
                    
                    SetProgress(true, 100);
                    return true;
                }
                else
                {
                    LogOutput($"ERROR: {result.error}");
                    LogOutput("Note: If Python is already installed, this is expected.");
                    
                    // Verify Python is available anyway
                    var pythonCheck = await RunCommand("python3", "--version");
                    if (!string.IsNullOrWhiteSpace(pythonCheck.output))
                    {
                        LogOutput($"Python is available: {pythonCheck.output}");
                        return true;
                    }
                }
            }
            else
            {
                // Open python.org download page
                string url = $"https://www.python.org/downloads/release/python-{version.Replace(".", "")}/";
                await Launcher.OpenAsync(url);
                LogOutput($"Opening {url} in browser...");
                LogOutput("Please download and install Python manually, then click 'Python already installed'");
            }

            return false;
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR during Python installation: {ex.Message}");
            return false;
        }
        finally
        {
            SetProgress(false);
        }
    }

    private string CheckExistingPython(string targetPath)
    {
        try
        {
            // Check common Mac Python locations
            string[] commonPaths = {
                "/usr/local/bin/python3",
                "/opt/homebrew/bin/python3",
                "/usr/bin/python3",
                "/Library/Frameworks/Python.framework/Versions/3.12/bin/python3",
                "/Library/Frameworks/Python.framework/Versions/3.11/bin/python3",
                "/Library/Frameworks/Python.framework/Versions/3.10/bin/python3",
                "/Library/Frameworks/Python.framework/Versions/3.9/bin/python3",
                Path.Combine(targetPath, "bin", "python3"),
                Path.Combine(targetPath, "python3")
            };

            foreach (string pythonPath in commonPaths)
            {
                if (File.Exists(pythonPath))
                {
                    try
                    {
                        var result = RunCommand(pythonPath, "--version").Result;
                        if (result.exitCode == 0 && !string.IsNullOrEmpty(result.output))
                        {
                            return $"{pythonPath} ({result.output.Trim()})";
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            // Try python3 command in PATH
            try
            {
                var result = RunCommand("python3", "--version").Result;
                if (result.exitCode == 0 && !string.IsNullOrEmpty(result.output))
                {
                    return $"python3 (in PATH) ({result.output.Trim()})";
                }
            }
            catch
            {
                // Python not in PATH
            }
        }
        catch (Exception ex)
        {
            LogOutput($"Error checking existing Python: {ex.Message}");
        }

        return string.Empty;
    }

    private async void CloneRepoButton_Clicked(object sender, EventArgs e)
    {
        await CloneRepository();
    }

    private async Task<bool> CloneRepository()
    {
        try
        {
            SetProgress(true, 0);
            LogOutput("Starting repository clone...");

            string repoUrl = RepoUrlEntry.Text.Trim();
            string clonePath = ClonePathEntry.Text.Trim();

            if (string.IsNullOrEmpty(repoUrl) || string.IsNullOrEmpty(clonePath))
            {
                LogOutput("ERROR: Please specify both repository URL and clone path.");
                return false;
            }

            // Expand ~ to home directory
            clonePath = clonePath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            string repoName = Path.GetFileNameWithoutExtension(repoUrl.Split('/').Last());
            string fullClonePath = Path.Combine(clonePath, repoName);

            Directory.CreateDirectory(clonePath);
            SetProgress(true, 25);

            await Task.Run(() =>
            {
                try
                {
                    LogOutput($"Cloning {repoUrl} to {fullClonePath}...");
                    
                    var cloneOptions = new CloneOptions();
                    Repository.Clone(repoUrl, fullClonePath, cloneOptions);
                    
                    LogOutput($"Repository cloned successfully to {fullClonePath}");
                    SetProgress(true, 100);
                }
                catch (Exception ex)
                {
                    LogOutput($"ERROR during repository clone: {ex.Message}");
                    throw;
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR during repository clone: {ex.Message}");
            return false;
        }
        finally
        {
            SetProgress(false);
        }
    }

    private async void SetupEnvironmentButton_Clicked(object sender, EventArgs e)
    {
        await SetupPythonEnvironment();
    }

    private async Task<bool> SetupPythonEnvironment()
    {
        try
        {
            SetProgress(true, 0);
            LogOutput("Starting Python environment setup...");

            string projectPath = ClonePathEntry.Text.Trim().Replace("~", 
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            
            if (string.IsNullOrEmpty(projectPath))
            {
                LogOutput("ERROR: Please specify a project directory.");
                return false;
            }

            string repoUrl = RepoUrlEntry.Text.Trim();
            string repoName = Path.GetFileNameWithoutExtension(repoUrl.Split('/').Last());
            string fullProjectPath = Path.Combine(projectPath, repoName);

            if (!Directory.Exists(fullProjectPath))
            {
                LogOutput($"ERROR: Project directory does not exist: {fullProjectPath}");
                LogOutput("Please clone the repository first.");
                return false;
            }

            SetProgress(true, 25);

            if (CreateVenvCheckBox.IsChecked)
            {
                await CreateVirtualEnvironment(fullProjectPath);
            }

            SetProgress(true, 60);

            if (InstallPackagesCheckBox.IsChecked)
            {
                await InstallPythonPackages(fullProjectPath);
            }

            SetProgress(true, 100);
            LogOutput("Python environment setup completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR during Python environment setup: {ex.Message}");
            return false;
        }
        finally
        {
            SetProgress(false);
        }
    }

    private async Task<bool> CreateVirtualEnvironment(string projectPath)
    {
        try
        {
            LogOutput("Creating virtual environment...");

            string venvPath = Path.Combine(projectPath, ".venv");

            if (Directory.Exists(venvPath))
            {
                LogOutput("Removing existing virtual environment...");
                Directory.Delete(venvPath, true);
            }

            string pythonPath = GetPythonPath();
            if (string.IsNullOrEmpty(pythonPath))
            {
                LogOutput("ERROR: Could not find Python executable. Please ensure Python is installed.");
                return false;
            }

            LogOutput($"Using Python at: {pythonPath}");

            var result = await RunCommand(pythonPath, $"-m venv {venvPath}");

            if (result.exitCode == 0)
            {
                LogOutput("Virtual environment created successfully.");
                return true;
            }
            else
            {
                LogOutput($"ERROR creating virtual environment: {result.error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR creating virtual environment: {ex.Message}");
            return false;
        }
    }

    private string GetPythonPath()
    {
        string installPath = PythonPathEntry.Text.Trim();
        if (!string.IsNullOrEmpty(installPath))
        {
            string python3 = Path.Combine(installPath, "bin", "python3");
            if (File.Exists(python3))
                return python3;
        }

        // Check common locations
        string[] pythonPaths = {
            "/opt/homebrew/bin/python3",
            "/usr/local/bin/python3",
            "/usr/bin/python3"
        };

        foreach (string path in pythonPaths)
        {
            if (File.Exists(path))
                return path;
        }

        try
        {
            var result = RunCommand("which", "python3").Result;
            if (result.exitCode == 0 && !string.IsNullOrWhiteSpace(result.output))
            {
                return result.output.Trim();
            }
        }
        catch { }

        return "python3"; // Fallback to PATH
    }

    private async Task<bool> InstallPythonPackages(string projectPath)
    {
        try
        {
            LogOutput("Installing Python packages...");
            SetProgress(true, 0);

            string venvPython = Path.Combine(projectPath, ".venv", "bin", "python");
            
            if (!File.Exists(venvPython))
            {
                LogOutput("ERROR: Virtual environment not found. Please create it first.");
                SetProgress(false);
                return false;
            }

            LogOutput("Upgrading pip...");
            SetProgress(true, 10);
            var pipUpgrade = await RunCommand(venvPython, "-m pip install --upgrade pip");
            
            if (pipUpgrade.exitCode != 0)
            {
                LogOutput("Warning: Failed to upgrade pip, but continuing...");
            }

            string requirementsPath = Path.Combine(projectPath, "requirements.txt");
            
            if (File.Exists(requirementsPath))
            {
                LogOutput("Installing packages from requirements.txt...");
                SetProgress(true, 30);
                var result = await RunCommandWithLiveOutput(venvPython, $"-m pip install -r {requirementsPath}");
                
                if (result.exitCode != 0)
                {
                    LogOutput("ERROR: Failed to install packages from requirements.txt");
                    SetProgress(false);
                    return false;
                }
                SetProgress(true, 90);
            }
            else
            {
                LogOutput("No requirements.txt found. Installing common packages...");
                string[] packages = { "astropy", "peewee", "numpy", "matplotlib", "pytz", "PySide6" };
                
                for (int i = 0; i < packages.Length; i++)
                {
                    string package = packages[i];
                    LogOutput($"Installing {package}...");
                    SetProgress(true, 30 + (50 * (i + 1) / packages.Length));
                    
                    var result = await RunCommandWithLiveOutput(venvPython, $"-m pip install {package}");
                    
                    if (result.exitCode != 0)
                    {
                        LogOutput($"Warning: Failed to install {package}, but continuing...");
                    }
                }
            }

            LogOutput("Package installation completed.");
            SetProgress(true, 100);
            await Task.Delay(1000);
            SetProgress(false);
            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR installing packages: {ex.Message}");
            SetProgress(false);
            return false;
        }
    }

    private async void CreateRunScriptsButton_Clicked(object sender, EventArgs e)
    {
        await CreateRunScripts();
    }

    private async Task<bool> CreateRunScripts()
    {
        try
        {
            LogOutput("Creating run scripts...");

            string projectPath = ClonePathEntry.Text.Trim().Replace("~", 
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            
            if (string.IsNullOrEmpty(projectPath))
            {
                LogOutput("ERROR: Please specify a project directory.");
                return false;
            }

            string repoUrl = RepoUrlEntry.Text.Trim();
            string repoName = Path.GetFileNameWithoutExtension(repoUrl.Split('/').Last());
            string fullProjectPath = Path.Combine(projectPath, repoName);

            if (!Directory.Exists(fullProjectPath))
            {
                LogOutput($"ERROR: Project directory does not exist: {fullProjectPath}");
                return false;
            }

            string targetProgram = TargetProgramEntry.Text.Trim();
            if (string.IsNullOrEmpty(targetProgram))
            {
                targetProgram = "main.py";
            }

            string programName = Path.GetFileNameWithoutExtension(targetProgram);

            await Task.Run(() =>
            {
                // Create bash run script
                string bashScript = $@"#!/bin/bash
cd ""$(dirname ""$0"")""

echo ""Checking for updates...""
git pull
if [ $? -ne 0 ]; then
    echo ""Warning: Git pull failed or no git repository found""
fi

echo ""Starting {programName}...""
source .venv/bin/activate
python {targetProgram}
read -p ""Press Enter to exit""";

                string bashPath = Path.Combine(fullProjectPath, $"run_{programName}.sh");
                File.WriteAllText(bashPath, bashScript);
                
                // Make script executable
                RunCommand("chmod", $"+x \"{bashPath}\"").Wait();

                LogOutput($"Run script created: run_{programName}.sh");
                LogOutput("Script will check for updates via 'git pull' on startup");
            });

            // Create app bundle if requested
            if (CreateDesktopShortcutCheckBox.IsChecked)
            {
                await CreateMacAppBundle(fullProjectPath, programName, targetProgram);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR creating run scripts: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CreateMacAppBundle(string projectPath, string programName, string targetProgram)
    {
        try
        {
            LogOutput("Creating Mac application bundle...");

            string appName = char.ToUpper(programName[0]) + programName.Substring(1);
            string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "Applications", $"{appName}.app");
            string contentsPath = Path.Combine(appPath, "Contents");
            string macOSPath = Path.Combine(contentsPath, "MacOS");
            string resourcesPath = Path.Combine(contentsPath, "Resources");

            // Create app bundle structure
            Directory.CreateDirectory(macOSPath);
            Directory.CreateDirectory(resourcesPath);

            // Create Info.plist
            string plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>CFBundleName</key>
    <string>{appName}</string>
    <key>CFBundleDisplayName</key>
    <string>{appName}</string>
    <key>CFBundleIdentifier</key>
    <string>com.user.{programName}</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>{programName}</string>
</dict>
</plist>";

            File.WriteAllText(Path.Combine(contentsPath, "Info.plist"), plistContent);

            // Create launcher script
            string launcherScript = $@"#!/bin/bash
cd ""{projectPath}""
source .venv/bin/activate
python {targetProgram}";

            string launcherPath = Path.Combine(macOSPath, programName);
            File.WriteAllText(launcherPath, launcherScript);

            // Make launcher executable
            await RunCommand("chmod", $"+x \"{launcherPath}\"");

            LogOutput($"Mac app bundle created: {appPath}");
            LogOutput($"Application is available in ~/Applications/{appName}.app");

            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR creating app bundle: {ex.Message}");
            return false;
        }
    }

    private async void InstallAllButton_Clicked(object sender, EventArgs e)
    {
        LogOutput("Starting complete setup process...");
        
        bool pythonSuccess = await InstallPython();
        if (!pythonSuccess)
        {
            LogOutput("Python installation failed. Stopping process.");
            return;
        }

        bool cloneSuccess = await CloneRepository();
        if (!cloneSuccess)
        {
            LogOutput("Repository clone failed. Stopping process.");
            return;
        }

        bool environmentSuccess = await SetupPythonEnvironment();
        if (!environmentSuccess)
        {
            LogOutput("Python environment setup failed. Stopping process.");
            return;
        }

        bool scriptsSuccess = await CreateRunScripts();
        if (!scriptsSuccess)
        {
            LogOutput("Run script creation failed.");
            return;
        }

        LogOutput("Complete setup process finished successfully!");
        await DisplayAlert("Success", "Setup completed successfully! Your Python environment is ready to use.", "OK");
    }

    private async Task<(int exitCode, string output, string error)> RunCommand(string command, string arguments)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return (-1, "", "Failed to start process");

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }

    private async Task<(int exitCode, string output, string error)> RunCommandWithLiveOutput(string command, string arguments)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return (-1, "", "Failed to start process");

            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    string? line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (line.Contains("Collecting") || 
                            line.Contains("Downloading") || 
                            line.Contains("Installing") ||
                            line.Contains("Successfully installed") ||
                            line.Contains("Requirement already satisfied"))
                        {
                            LogOutput($"  {line.Trim()}");
                        }
                    }
                }
            });

            var errorBuilder = new System.Text.StringBuilder();
            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    string? line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        LogOutput($"  WARNING: {line.Trim()}");
                        errorBuilder.AppendLine(line);
                    }
                }
            });

            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);

            return (process.ExitCode, "", errorBuilder.ToString());
        }
        catch (Exception ex)
        {
            LogOutput($"ERROR running command: {ex.Message}");
            return (-1, "", ex.Message);
        }
    }
}

import Foundation
import Combine
import AppKit

@MainActor
class InstallationManager: ObservableObject {
    // Python Installation
    @Published var pythonAlreadyInstalled = true
    @Published var selectedPythonVersion = "3.12.6"
    @Published var pythonInstallPath = "/usr/local/python3"
    @Published var isInstallingPython = false
    
    let pythonVersions = ["3.12.6", "3.12.5", "3.11.9", "3.10.14", "3.9.19"]
    
    // Git Repository
    @Published var skipCloning = true
    @Published var gitRepoUrl = ""
    @Published var gitClonePath = NSHomeDirectory() + "/Projects"
    @Published var gitBranch = "main"
    @Published var isCloningRepo = false
    
    // Virtual Environment
    @Published var projectPath = NSHomeDirectory() + "/Projects"
    @Published var venvName = ".venv"
    @Published var packagesToInstall = ""
    @Published var isCreatingVenv = false
    
    // Scripts & App Bundle
    @Published var mainScriptName = "main.py"
    @Published var appBundleName = "MyApp"
    @Published var createAppBundle = false
    @Published var isGeneratingScripts = false
    
    // Complete Setup
    @Published var isRunningCompleteSetup = false
    
    // Console Output
    @Published var consoleOutput = "PyMacInstall ready. Configure options and click buttons to proceed.\n"
    
    private let shell = ShellExecutor()
    
    func log(_ message: String) {
        let timestamp = DateFormatter.localizedString(from: Date(), dateStyle: .none, timeStyle: .medium)
        consoleOutput += "[\(timestamp)] \(message)\n"
    }
    
    func browsePythonPath() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.prompt = "Select Install Location"
        
        if panel.runModal() == .OK, let url = panel.url {
            pythonInstallPath = url.path
        }
    }
    
    func browseClonePath() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.prompt = "Select Clone Location"
        
        if panel.runModal() == .OK, let url = panel.url {
            gitClonePath = url.path
        }
    }
    
    func browseProjectPath() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.prompt = "Select Project Path"
        
        if panel.runModal() == .OK, let url = panel.url {
            projectPath = url.path
        }
    }
    
    func installPython() {
        Task {
            isInstallingPython = true
            log("Starting Python \(selectedPythonVersion) installation...")
            
            // Check if Homebrew is installed
            let brewCheck = await shell.execute("which brew")
            if brewCheck.exitCode != 0 {
                log("Homebrew not found. Installing Homebrew...")
                let brewInstall = await shell.execute("/bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"")
                if brewInstall.exitCode == 0 {
                    log("Homebrew installed successfully")
                } else {
                    log("ERROR: Failed to install Homebrew: \(brewInstall.error)")
                    isInstallingPython = false
                    return
                }
            }
            
            // Install Python via Homebrew
            log("Installing Python via Homebrew...")
            let result = await shell.execute("brew install python@\(selectedPythonVersion.prefix(4))")
            
            if result.exitCode == 0 {
                log("✓ Python installed successfully")
                log(result.output)
            } else {
                log("ERROR: Python installation failed")
                log(result.error)
            }
            
            isInstallingPython = false
        }
    }
    
    func cloneRepository() {
        guard !gitRepoUrl.isEmpty else {
            log("ERROR: Repository URL is required")
            return
        }
        
        Task {
            isCloningRepo = true
            log("Cloning repository from \(gitRepoUrl)...")
            
            let repoName = URL(fileURLWithPath: gitRepoUrl).deletingPathExtension().lastPathComponent
            let targetPath = "\(gitClonePath)/\(repoName)"
            
            let command = "git clone -b \(gitBranch) \(gitRepoUrl) \(targetPath)"
            let result = await shell.execute(command)
            
            if result.exitCode == 0 {
                log("✓ Repository cloned to: \(targetPath)")
                projectPath = targetPath
            } else {
                log("ERROR: Failed to clone repository")
                log(result.error)
            }
            
            isCloningRepo = false
        }
    }
    
    func createVirtualEnvironment() {
        Task {
            isCreatingVenv = true
            log("Creating virtual environment at \(projectPath)/\(venvName)...")
            
            // Find Python3
            let pythonPath = await shell.execute("which python3")
            guard pythonPath.exitCode == 0 else {
                log("ERROR: Python3 not found. Please install Python first.")
                isCreatingVenv = false
                return
            }
            
            let python = pythonPath.output.trimmingCharacters(in: .whitespacesAndNewlines)
            log("Using Python: \(python)")
            
            // Create venv
            let venvResult = await shell.execute("\(python) -m venv \(projectPath)/\(venvName)")
            if venvResult.exitCode != 0 {
                log("ERROR: Failed to create virtual environment")
                log(venvResult.error)
                isCreatingVenv = false
                return
            }
            
            log("✓ Virtual environment created")
            
            // Install packages if specified
            if !packagesToInstall.isEmpty {
                log("Installing packages: \(packagesToInstall)")
                let packages = packagesToInstall.components(separatedBy: " ").filter { !$0.isEmpty }
                
                for package in packages {
                    log("Installing \(package)...")
                    let pipResult = await shell.execute("\(projectPath)/\(venvName)/bin/pip install \(package)")
                    if pipResult.exitCode == 0 {
                        log("✓ \(package) installed")
                    } else {
                        log("ERROR: Failed to install \(package)")
                        log(pipResult.error)
                    }
                }
            }
            
            log("✓ Virtual environment setup complete")
            isCreatingVenv = false
        }
    }
    
    func generateScriptsAndBundle() {
        Task {
            isGeneratingScripts = true
            log("Generating scripts...")
            
            // Create run script
            let runScript = """
            #!/bin/bash
            cd "\(projectPath)"
            source \(venvName)/bin/activate
            python \(mainScriptName) "$@"
            """
            
            let runScriptPath = "\(projectPath)/run.sh"
            do {
                try runScript.write(toFile: runScriptPath, atomically: true, encoding: .utf8)
                await shell.execute("chmod +x \(runScriptPath)")
                log("✓ Created run.sh")
            } catch {
                log("ERROR: Failed to create run.sh: \(error)")
            }
            
            // Create app bundle if requested
            if createAppBundle {
                log("Creating .app bundle...")
                let appPath = "\(projectPath)/\(appBundleName).app/Contents/MacOS"
                let resourcesPath = "\(projectPath)/\(appBundleName).app/Contents/Resources"
                
                do {
                    try FileManager.default.createDirectory(atPath: appPath, withIntermediateDirectories: true)
                    try FileManager.default.createDirectory(atPath: resourcesPath, withIntermediateDirectories: true)
                    
                    // Create launcher script
                    let launcher = """
                    #!/bin/bash
                    DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
                    cd "\(projectPath)"
                    source \(venvName)/bin/activate
                    python \(mainScriptName)
                    """
                    
                    let launcherPath = "\(appPath)/\(appBundleName)"
                    try launcher.write(toFile: launcherPath, atomically: true, encoding: .utf8)
                    await shell.execute("chmod +x \(launcherPath)")
                    
                    // Create Info.plist
                    let infoPlist = """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                    <plist version="1.0">
                    <dict>
                        <key>CFBundleExecutable</key>
                        <string>\(appBundleName)</string>
                        <key>CFBundleName</key>
                        <string>\(appBundleName)</string>
                        <key>CFBundleIdentifier</key>
                        <string>com.pymacinstall.\(appBundleName.lowercased())</string>
                        <key>CFBundleVersion</key>
                        <string>1.0</string>
                        <key>CFBundlePackageType</key>
                        <string>APPL</string>
                    </dict>
                    </plist>
                    """
                    
                    let plistPath = "\(projectPath)/\(appBundleName).app/Contents/Info.plist"
                    try infoPlist.write(toFile: plistPath, atomically: true, encoding: .utf8)
                    
                    log("✓ Created \(appBundleName).app")
                } catch {
                    log("ERROR: Failed to create app bundle: \(error)")
                }
            }
            
            log("✓ Script generation complete")
            isGeneratingScripts = false
        }
    }
    
    func completeSetup() {
        Task {
            isRunningCompleteSetup = true
            log("=== Starting Complete Setup ===")
            
            if !pythonAlreadyInstalled {
                await installPython()
                try? await Task.sleep(nanoseconds: 1_000_000_000)
            }
            
            if !skipCloning {
                await cloneRepository()
                try? await Task.sleep(nanoseconds: 1_000_000_000)
            }
            
            await createVirtualEnvironment()
            try? await Task.sleep(nanoseconds: 1_000_000_000)
            
            await generateScriptsAndBundle()
            
            log("=== Complete Setup Finished ===")
            isRunningCompleteSetup = false
        }
    }
}

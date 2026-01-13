import SwiftUI

struct ContentView: View {
    @StateObject private var installManager = InstallationManager()
    
    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Image(nsImage: NSImage(named: "AppIcon") ?? NSImage())
                    .resizable()
                    .frame(width: 48, height: 48)
                Text("PyMacInstall - Python Environment Setup")
                    .font(.title2)
                    .fontWeight(.bold)
                Spacer()
            }
            .padding()
            .background(Color(NSColor.controlBackgroundColor))
            
            ScrollView {
                VStack(alignment: .leading, spacing: 20) {
                    // Python Installation Section
                    GroupBox(label: Label("1. Python Installation", systemImage: "gear")) {
                        VStack(alignment: .leading, spacing: 12) {
                            Toggle("Python already installed (skip installation)", 
                                   isOn: $installManager.pythonAlreadyInstalled)
                            
                            if !installManager.pythonAlreadyInstalled {
                                HStack {
                                    Text("Python Version:")
                                        .frame(width: 120, alignment: .trailing)
                                    Picker("", selection: $installManager.selectedPythonVersion) {
                                        ForEach(installManager.pythonVersions, id: \.self) { version in
                                            Text(version).tag(version)
                                        }
                                    }
                                    .frame(width: 200)
                                }
                                
                                HStack {
                                    Text("Install Path:")
                                        .frame(width: 120, alignment: .trailing)
                                    TextField("", text: $installManager.pythonInstallPath)
                                    Button("Browse...") {
                                        installManager.browsePythonPath()
                                    }
                                }
                            }
                            
                            Button(action: { installManager.installPython() }) {
                                HStack {
                                    if installManager.isInstallingPython {
                                        ProgressView()
                                            .scaleEffect(0.7)
                                    }
                                    Text(installManager.isInstallingPython ? "Installing..." : "Install Python")
                                }
                            }
                            .disabled(installManager.pythonAlreadyInstalled || installManager.isInstallingPython)
                        }
                        .padding(8)
                    }
                    
                    // Git Repository Section
                    GroupBox(label: Label("2. Git Repository", systemImage: "folder")) {
                        VStack(alignment: .leading, spacing: 12) {
                            Toggle("Skip repository cloning", isOn: $installManager.skipCloning)
                            
                            if !installManager.skipCloning {
                                HStack {
                                    Text("Repository URL:")
                                        .frame(width: 120, alignment: .trailing)
                                    TextField("https://github.com/user/repo.git", 
                                            text: $installManager.gitRepoUrl)
                                }
                                
                                HStack {
                                    Text("Clone to:")
                                        .frame(width: 120, alignment: .trailing)
                                    TextField("", text: $installManager.gitClonePath)
                                    Button("Browse...") {
                                        installManager.browseClonePath()
                                    }
                                }
                                
                                HStack {
                                    Text("Branch:")
                                        .frame(width: 120, alignment: .trailing)
                                    TextField("main", text: $installManager.gitBranch)
                                }
                            }
                            
                            Button(action: { installManager.cloneRepository() }) {
                                HStack {
                                    if installManager.isCloningRepo {
                                        ProgressView()
                                            .scaleEffect(0.7)
                                    }
                                    Text(installManager.isCloningRepo ? "Cloning..." : "Clone Repository")
                                }
                            }
                            .disabled(installManager.skipCloning || installManager.isCloningRepo)
                        }
                        .padding(8)
                    }
                    
                    // Virtual Environment Section
                    GroupBox(label: Label("3. Virtual Environment", systemImage: "cube")) {
                        VStack(alignment: .leading, spacing: 12) {
                            HStack {
                                Text("Project Path:")
                                    .frame(width: 120, alignment: .trailing)
                                TextField("", text: $installManager.projectPath)
                                Button("Browse...") {
                                    installManager.browseProjectPath()
                                }
                            }
                            
                            HStack {
                                Text("Venv Name:")
                                    .frame(width: 120, alignment: .trailing)
                                TextField(".venv", text: $installManager.venvName)
                            }
                            
                            HStack {
                                Text("Packages:")
                                    .frame(width: 120, alignment: .trailing)
                                TextField("flask pandas numpy", text: $installManager.packagesToInstall)
                            }
                            .help("Space-separated list of packages")
                            
                            Button(action: { installManager.createVirtualEnvironment() }) {
                                HStack {
                                    if installManager.isCreatingVenv {
                                        ProgressView()
                                            .scaleEffect(0.7)
                                    }
                                    Text(installManager.isCreatingVenv ? "Creating..." : "Create Virtual Environment")
                                }
                            }
                            .disabled(installManager.isCreatingVenv)
                        }
                        .padding(8)
                    }
                    
                    // Script Generation Section
                    GroupBox(label: Label("4. Scripts & App Bundle", systemImage: "doc.text")) {
                        VStack(alignment: .leading, spacing: 12) {
                            HStack {
                                Text("Main Script:")
                                    .frame(width: 120, alignment: .trailing)
                                TextField("main.py", text: $installManager.mainScriptName)
                            }
                            
                            HStack {
                                Text("App Name:")
                                    .frame(width: 120, alignment: .trailing)
                                TextField("MyApp", text: $installManager.appBundleName)
                            }
                            
                            Toggle("Create .app bundle", isOn: $installManager.createAppBundle)
                            
                            Button(action: { installManager.generateScriptsAndBundle() }) {
                                HStack {
                                    if installManager.isGeneratingScripts {
                                        ProgressView()
                                            .scaleEffect(0.7)
                                    }
                                    Text(installManager.isGeneratingScripts ? "Generating..." : "Generate Scripts & Bundle")
                                }
                            }
                            .disabled(installManager.isGeneratingScripts)
                        }
                        .padding(8)
                    }
                    
                    // Complete Setup Button
                    Button(action: { installManager.completeSetup() }) {
                        HStack {
                            if installManager.isRunningCompleteSetup {
                                ProgressView()
                                    .scaleEffect(0.7)
                            }
                            Text(installManager.isRunningCompleteSetup ? "Running Complete Setup..." : "Complete Setup")
                                .fontWeight(.bold)
                        }
                        .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.borderedProminent)
                    .controlSize(.large)
                    .disabled(installManager.isRunningCompleteSetup)
                }
                .padding()
            }
            
            // Console Output
            GroupBox(label: Label("Console Output", systemImage: "terminal")) {
                ScrollViewReader { proxy in
                    ScrollView {
                        Text(installManager.consoleOutput)
                            .font(.system(.body, design: .monospaced))
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .textSelection(.enabled)
                            .id("consoleBottom")
                    }
                    .frame(height: 200)
                    .onChange(of: installManager.consoleOutput) { _ in
                        proxy.scrollTo("consoleBottom", anchor: .bottom)
                    }
                }
            }
            .padding()
        }
        .frame(width: 800, height: 900)
    }
}

#Preview {
    ContentView()
}

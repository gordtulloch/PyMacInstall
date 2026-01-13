import Foundation

struct ShellResult {
    let output: String
    let error: String
    let exitCode: Int32
}

class ShellExecutor {
    func execute(_ command: String) async -> ShellResult {
        return await withCheckedContinuation { continuation in
            let task = Process()
            let outputPipe = Pipe()
            let errorPipe = Pipe()
            
            task.standardOutput = outputPipe
            task.standardError = errorPipe
            task.arguments = ["-c", command]
            task.executableURL = URL(fileURLWithPath: "/bin/bash")
            task.standardInput = nil
            
            do {
                try task.run()
                task.waitUntilExit()
                
                let outputData = outputPipe.fileHandleForReading.readDataToEndOfFile()
                let errorData = errorPipe.fileHandleForReading.readDataToEndOfFile()
                
                let output = String(data: outputData, encoding: .utf8) ?? ""
                let error = String(data: errorData, encoding: .utf8) ?? ""
                
                continuation.resume(returning: ShellResult(
                    output: output,
                    error: error,
                    exitCode: task.terminationStatus
                ))
            } catch {
                continuation.resume(returning: ShellResult(
                    output: "",
                    error: error.localizedDescription,
                    exitCode: -1
                ))
            }
        }
    }
}

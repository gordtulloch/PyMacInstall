// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "PyMacInstall",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(name: "PyMacInstall", targets: ["PyMacInstall"])
    ],
    targets: [
        .executableTarget(
            name: "PyMacInstall",
            path: "Sources/PyMacInstall",
            swiftSettings: [
                .unsafeFlags(["-parse-as-library"])
            ]
        )
    ]
)

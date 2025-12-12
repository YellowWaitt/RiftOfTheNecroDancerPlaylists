param (
    [switch]$release,
    [switch]$debug
)

# Game name (adjust according to your needs)
$gameName = "RiftOfTheNecroDancerOSTVolume1"
$makeSubDirectory = $true

# List of build files to ignore
$ignoredDebugFiles = @(
    "*.pdb",
    "*.deps.json",
    "*.runtimeconfig.json",
    "*.xml"
)

# Determine build configuration (Debug or Release)
$configuration = "Debug"
if ($release) { $configuration = "Release" }
elseif ($debug) { $configuration = "Debug" }

# Find the .csproj file
$CsprojFilePath = Get-ChildItem -Path . -Filter "*.csproj" -File | Select-Object -First 1
if (-not $CsprojFilePath) {
    Write-Error "No .csproj file found in the current directory."
    exit 1
}

# Read the .csproj file
$content = Get-Content -Path $CsprojFilePath.FullName -Raw
$assemblyName = [regex]::Match($content, '<AssemblyName>(.*?)</AssemblyName>').Groups[1].Value
$targetFramework = [regex]::Match($content, '<TargetFramework>(.*?)</TargetFramework>').Groups[1].Value
if (-not $assemblyName -or -not $targetFramework) {
    Write-Error "Could not find AssemblyName or TargetFramework in the .csproj file."
    exit 1
}

# Build source and destination paths
$SourceDir = Join-Path -Path . -ChildPath "bin\$configuration\$targetFramework"
$DestinationDir = "C:\Program Files (x86)\Steam\steamapps\common\$gameName\BepInEx\plugins"
if ($makeSubDirectory) { $DestinationDir = Join-Path -Path $DestinationDir -ChildPath $assemblyName }

# Create destination directory
if (-not (Test-Path -Path $DestinationDir)) {
    New-Item -ItemType Directory -Path $DestinationDir -Force -ErrorAction Stop | Out-Null
    Write-Host "Destination directory '$DestinationDir' created."
}

# Extract referenced DLLs from .csproj
$referencedDlls = @()
$referencePattern = '<Reference\s+Include="([^"]+)"'
$referenceMatches = [regex]::Matches($content, $referencePattern)
foreach ($match in $referenceMatches) {
    $referencedDlls += "$($match.Groups[1].Value).dll"
}

# Extract files marked with <CopyToOutputDirectory>
$filesToCopy = @()
$pattern = '<(?:None|Content)\s+Update="([^"]+)"[^>]*>(?s:.*)<CopyToOutputDirectory>[^<]+</CopyToOutputDirectory>'
$regexMatches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
foreach ($match in $regexMatches) {
    $filesToCopy += $match.Groups[1].Value
}

# Copy all files except ignored ones
try {
    # Copy files marked with <CopyToOutputDirectory> (with directory structure)
    foreach ($filePath in $filesToCopy) {
        $sourceFile = Join-Path -Path . -ChildPath $filePath
        if (Test-Path $sourceFile) {
            $destPath = Join-Path -Path $DestinationDir -ChildPath $filePath
            New-Item -ItemType Directory -Path (Split-Path $destPath -Parent) -Force | Out-Null
            Copy-Item -Path $sourceFile -Destination $destPath -Force
            Write-Host "Copied (with structure): $filePath"
        }
        else {
            Write-Warning "File '$filePath' not found."
        }
    }

    # Get all files in the source directory
    $allFiles = Get-ChildItem -Path $SourceDir -File

    foreach ($file in $allFiles) {
        $shouldCopy = $true

        # Skip build files
        foreach ($pattern in $ignoredDebugFiles) {
            if ($file.Name -like $pattern) {
                $shouldCopy = $false
                break
            }
        }

        # Skip referenced DLLs
        if ($referencedDlls -contains $file.Name) {
            $shouldCopy = $false
        }

        # Copy the file if it passes all checks
        if ($shouldCopy) {
            $destPath = Join-Path -Path $DestinationDir -ChildPath $file.Name
            Copy-Item -Path $file.FullName -Destination $destPath -Force
            Write-Host "Copied: $($file.Name)"
        }
    }
}
catch {
    Write-Error "Error: $_"
    exit 1
}

Write-Host "All files copied successfully!"

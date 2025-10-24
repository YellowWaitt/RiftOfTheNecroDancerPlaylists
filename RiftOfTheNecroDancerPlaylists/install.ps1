param (
    [switch]$release,
    [switch]$debug
)

# Game name (adjust according to your needs)
$gameName = "RiftOfTheNecroDancerOSTVolume1"
$makeSubDirectory = $true

# Determine build configuration (Debug or Release)
$configuration = "Debug"  # Default to Debug
if ($release) {
    $configuration = "Release"
} elseif ($debug) {
    $configuration = "Debug"
}

# Find the .csproj file in the current directory
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

# Build source directory path
$SourceDir = Join-Path -Path . -ChildPath "bin\$configuration\$targetFramework"
if (-not (Test-Path -Path $SourceDir)) {
    Write-Error "Source directory '$SourceDir' does not exist."
    exit 1
}

# Build destination directory path
$DestinationDir = "C:\Program Files (x86)\Steam\steamapps\common\$gameName\BepInEx\plugins"
if ($makeSubDirectory) {
    $DestinationDir = Join-Path -Path $DestinationDir -ChildPath $assemblyName
}

# Create destination directory if it does not exist
if (-not (Test-Path -Path $DestinationDir)) {
    try {
        New-Item -ItemType Directory -Path $DestinationDir -Force -ErrorAction Stop | Out-Null
        Write-Host "Destination directory '$DestinationDir' created successfully."
    }
    catch {
        Write-Error "An error occurred while creating the destination directory: $_"
        exit 1
    }
}

# Extract files marked with <Update> and <CopyToOutputDirectory> (handles newlines and indentation)
$filesToCopy = @()
$pattern = '<(?:None|Content)\s+Update="([^"]+)"[^>]*>(?s:.*)<CopyToOutputDirectory>[^<]+</CopyToOutputDirectory>'
$matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
foreach ($match in $matches) {
    $relativePath = $match.Groups[1].Value
    $filesToCopy += $relativePath
}

# Copy the main DLL and additional files, preserving directory structure
try {
    # Copy the main DLL
    $dllFile = Get-ChildItem -Path $SourceDir -Filter "$assemblyName.dll" -File
    if ($dllFile) {
        $destDllPath = Join-Path -Path $DestinationDir -ChildPath $dllFile.Name
        Copy-Item -Path $dllFile.FullName -Destination $destDllPath -Force -ErrorAction Stop
        Write-Host "DLL '$($dllFile.Name)' successfully copied to '$DestinationDir'."
    } else {
        Write-Error "Main DLL '$assemblyName.dll' not found in '$SourceDir'."
        exit 1
    }

    # Copy additional files, preserving directory structure
    foreach ($filePath in $filesToCopy) {
        $sourceFile = Join-Path -Path . -ChildPath $filePath
        if (Test-Path -Path $sourceFile) {
            # Create the subdirectory structure in the destination
            $destSubDir = Join-Path -Path $DestinationDir -ChildPath (Split-Path -Path $filePath -Parent)
            if (-not (Test-Path -Path $destSubDir)) {
                New-Item -ItemType Directory -Path $destSubDir -Force -ErrorAction Stop | Out-Null
            }
            # Copy the file to the correct subdirectory
            $destFilePath = Join-Path -Path $DestinationDir -ChildPath $filePath
            Copy-Item -Path $sourceFile -Destination $destFilePath -Force -ErrorAction Stop
            Write-Host "File '$filePath' successfully copied to '$destFilePath'."
        } else {
            Write-Warning "File '$filePath' not found in the project directory."
        }
    }
}
catch {
    Write-Error "An error occurred while copying files: $_"
    exit 1
}

Write-Host "All required files copied successfully!"

$gamePath = "RiftOfTheNecroDancerOSTVolume1\RiftOfTheNecroDancer_Data"
$dllList = @(
    "Assembly-CSharp.dll",
    "com.rlabrecque.steamworks.net.dll",
    "Newtonsoft.Json.dll",
    "TicToc.Localization.dll",
    "UnityEngine.dll",
    "UnityEngine.UI.dll",
    "Unity.InputSystem.dll",
    "Unity.TextMeshPro.dll"
    # Add all DLL filenames to copy here
)

$sourceFolder = "C:\Program Files (x86)\Steam\steamapps\common\$gamePath\Managed"
$destinationFolder = Join-Path -Path $PSScriptRoot -ChildPath "lib"

# Create destination folder if it doesn't exist
if (-not (Test-Path -Path $destinationFolder)) {
    New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
}

# Copy files
foreach ($dll in $dllList) {
    $sourcePath = Join-Path -Path $sourceFolder -ChildPath $dll
    $destinationPath = Join-Path -Path $destinationFolder -ChildPath $dll

    if (Test-Path -Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destinationPath -Force
        Write-Host "Copied $dll successfully."
    }
    else {
        Write-Warning "File $dll does not exist in the source folder."
    }
}

Write-Host "Copy completed."

param(
    [string]$RimWorldPath = $env:RimWorldPath
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Source\Columbarium\Columbarium.csproj'
$package = Join-Path $root 'dist\Columbarium'
$buildOutput = Join-Path $root 'build\1.7'
$preserved = Join-Path $root 'Compatibility\1.6\Assemblies\Columbarium.dll'
$hashRecord = Join-Path $root 'Compatibility\1.6\Columbarium.sha256'

if (-not $RimWorldPath) {
    foreach ($candidate in @(
        'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
        'C:\Program Files\Steam\steamapps\common\RimWorld',
        'D:\SteamLibrary\steamapps\common\RimWorld'
    )) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'Assembly-CSharp.dll')) {
            $RimWorldPath = $candidate
            break
        }
    }
}

if (-not $RimWorldPath -or -not (Test-Path -LiteralPath (Join-Path $RimWorldPath 'Assembly-CSharp.dll')) -or -not (Test-Path -LiteralPath (Join-Path $RimWorldPath 'UnityStubs.dll')) -or -not (Test-Path -LiteralPath (Join-Path $RimWorldPath 'Waylith.dll'))) {
    throw 'RimWorld was not found. Pass -RimWorldPath "<RimWorld installation folder>" or set the RimWorldPath environment variable.'
}

$RimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).Path
$versionFile = Join-Path $RimWorldPath 'Version.txt'
if (-not (Test-Path -LiteralPath $versionFile)) {
    throw "RimWorld version file not found: $versionFile"
}
$version = (Get-Content -LiteralPath $versionFile -TotalCount 1).Trim()
if ($version -notmatch '^1\.7(?:\.|\s|$)') {
    throw "A RimWorld 1.7 installation is required for the default build; found $version at $RimWorldPath."
}
if (-not (Test-Path -LiteralPath $preserved) -or -not (Test-Path -LiteralPath $hashRecord)) {
    throw 'The preserved 1.6 DLL and its SHA-256 record are required.'
}
$expectedHash = ((Get-Content -LiteralPath $hashRecord -TotalCount 1) -split '\s+')[0]
$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $preserved).Hash
if ($actualHash -ne $expectedHash) {
    throw "The preserved 1.6 DLL hash does not match $hashRecord."
}
New-Item -ItemType Directory -Path $buildOutput -Force | Out-Null

# Restore only compile-time dependencies; the game's Harmony mod supplies Harmony at runtime.
dotnet build $project --configuration Release "-p:RimWorldPath=$RimWorldPath" "-p:OutputPath=$buildOutput" '-p:NuGetAudit=false'
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}
$newDll = Join-Path $buildOutput 'Columbarium.dll'
if (-not (Test-Path -LiteralPath $newDll)) {
    throw "Build produced no DLL at $newDll."
}
$newHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $newDll).Hash

$version16Assemblies = Join-Path $package '1.6\Assemblies'
$version17Assemblies = Join-Path $package '1.7\Assemblies'
New-Item -ItemType Directory -Path $version16Assemblies, $version17Assemblies -Force | Out-Null
Copy-Item -LiteralPath $preserved -Destination (Join-Path $version16Assemblies 'Columbarium.dll') -Force
Copy-Item -LiteralPath $newDll -Destination (Join-Path $version17Assemblies 'Columbarium.dll') -Force
if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $version16Assemblies 'Columbarium.dll')).Hash -ne $expectedHash) {
    throw 'Packaged 1.6 DLL does not match the preserved DLL.'
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $version17Assemblies 'Columbarium.dll')).Hash -ne $newHash) {
    throw 'Packaged 1.7 DLL does not match the Release build.'
}
$oldAssembly = Join-Path $package 'Assemblies\Columbarium.dll'
if (Test-Path -LiteralPath $oldAssembly) { Remove-Item -LiteralPath $oldAssembly -Force }
$oldSymbols = Join-Path $package 'Assemblies\Columbarium.pdb'
if (Test-Path -LiteralPath $oldSymbols) { Remove-Item -LiteralPath $oldSymbols -Force }
Copy-Item -LiteralPath (Join-Path $root 'LoadFolders.xml') -Destination $package -Force

foreach ($directory in @('About', 'Defs', 'Textures', 'Patches', 'Languages')) {
    $source = Join-Path $root $directory
    if (-not (Test-Path -LiteralPath $source)) { continue }
    $destination = Join-Path $package $directory
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $destination -Recurse -Force
}

# RimWorld looks for About/Preview.png. Convert the editable JPEG source rather
# than changing only its extension, so both the game and Workshop can read it.
$previewSource = Join-Path $root 'About\Preview.jpg'
if (Test-Path -LiteralPath $previewSource) {
    Add-Type -AssemblyName System.Drawing
    $previewDestination = Join-Path $package 'About\Preview.png'
    $previewImage = [System.Drawing.Image]::FromFile($previewSource)
    try {
        $previewImage.Save($previewDestination, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $previewImage.Dispose()
    }
    Remove-Item -LiteralPath (Join-Path $package 'About\Preview.jpg') -Force
}

# Keep removed XML patches out of packages built over an earlier dist directory.
foreach ($name in @('Columbarium_RoomRole.xml', 'Columbarium_MemorialUrnMarketValue.xml', 'Columbarium_TwoAxisDesignators.xml')) {
    $obsoletePatch = Join-Path $package "Patches\$name"
    if (Test-Path -LiteralPath $obsoletePatch) {
        Remove-Item -LiteralPath $obsoletePatch -Force
    }
}

$archive = Join-Path $root 'dist\Columbarium-1.6-1.7.zip'
Compress-Archive -LiteralPath $package -DestinationPath $archive -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    foreach ($versionAndHash in @(@('1.6', $expectedHash), @('1.7', $newHash))) {
        $entryName = "Columbarium/$($versionAndHash[0])/Assemblies/Columbarium.dll"
        $entry = $zip.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq $entryName } | Select-Object -First 1
        if ($null -eq $entry) { throw "Archive is missing $entryName." }
        $stream = $entry.Open()
        try {
            $sha = [System.Security.Cryptography.SHA256]::Create()
            try {
                $entryHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '')
            }
            finally { $sha.Dispose() }
        }
        finally { $stream.Dispose() }
        if ($entryHash -ne $versionAndHash[1]) { throw "Archive hash mismatch for $entryName." }
    }
}
finally { $zip.Dispose() }
Write-Host "Build complete: $package ($archive)"

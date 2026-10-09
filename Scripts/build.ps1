param(
    [string]$RimWorldPath = $env:RimWorldPath
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Source\Columbarium\Columbarium.csproj'
$package = Join-Path $root 'dist\Columbarium'
$assemblies = Join-Path $package 'Assemblies'

if (-not $RimWorldPath) {
    foreach ($candidate in @(
        'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
        'C:\Program Files\Steam\steamapps\common\RimWorld',
        'D:\SteamLibrary\steamapps\common\RimWorld'
    )) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll')) {
            $RimWorldPath = $candidate
            break
        }
    }
}

if (-not $RimWorldPath -or -not (Test-Path -LiteralPath (Join-Path $RimWorldPath 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'))) {
    throw 'RimWorld was not found. Pass -RimWorldPath "<RimWorld installation folder>" or set the RimWorldPath environment variable.'
}

$RimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).Path
New-Item -ItemType Directory -Path $assemblies -Force | Out-Null

# The build only restores .NET Framework reference assemblies; NuGet's online audit is unnecessary here.
dotnet build $project --configuration Release "-p:RimWorldPath=$RimWorldPath" "-p:OutputPath=$assemblies" '-p:NuGetAudit=false'
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

foreach ($directory in @('About', 'Defs', 'Textures', 'Patches', 'Languages')) {
    $source = Join-Path $root $directory
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
foreach ($name in @('Columbarium_RoomRole.xml', 'Columbarium_MemorialUrnMarketValue.xml')) {
    $obsoletePatch = Join-Path $package "Patches\$name"
    if (Test-Path -LiteralPath $obsoletePatch) {
        Remove-Item -LiteralPath $obsoletePatch -Force
    }
}

Write-Host "Build complete: $package"

param(
    [Parameter(Mandatory)][string]$Directory,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+\.\d+$')][string]$Version
)

$ErrorActionPreference = 'Stop'
$archives = @(
    @{ Name = "Gurux.DLMS.AMI-$Version.zip"; Markers = @('wwwroot/version.json', 'standalone/version.json') },
    @{ Name = "Gurux.DLMS.AMI.Wasm-$Version.zip"; Markers = @('version.json') }
)
foreach ($archive in $archives) {
    $path = Join-Path $Directory $archive.Name
    $zip = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        foreach ($name in $archive.Markers) {
            $entry = $zip.GetEntry($name)
            if ($null -eq $entry) {
                throw "Release package '$($archive.Name)' is missing '$name'."
            }
            $reader = [IO.StreamReader]::new($entry.Open())
            try {
                $marker = $reader.ReadToEnd() | ConvertFrom-Json
            } finally {
                $reader.Dispose()
            }
            if ($marker.version -cne $Version) {
                throw "Release package '$($archive.Name)' contains version '$($marker.version)' in '$name'; expected '$Version'."
            }
        }
    } finally {
        $zip.Dispose()
    }
}
Write-Host "Server, standalone and WASM version.json markers match release $Version."

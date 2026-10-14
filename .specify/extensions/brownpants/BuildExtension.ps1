[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $RepositoryUrl,

    [Parameter(Mandatory = $true)]
    [string] $License,

    [string] $OutputPath
)

$parsedRepositoryUrl = $null
if (-not [Uri]::TryCreate($RepositoryUrl, [UriKind]::Absolute, [ref] $parsedRepositoryUrl) -or
    $parsedRepositoryUrl.Scheme -notin @('https', 'http')) {
    throw 'RepositoryUrl must be an absolute HTTP or HTTPS URL.'
}

if ($License -notmatch '^[A-Za-z0-9][A-Za-z0-9.+-]*( (AND|OR|WITH) [A-Za-z0-9][A-Za-z0-9.+-]*)*$') {
    throw 'License must be a manifest-safe SPDX license identifier or expression.'
}

$templatePath = Join-Path $PSScriptRoot 'extension.yml.template'
$manifestPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $PSScriptRoot 'extension.yml'
} else {
    [Path]::GetFullPath($OutputPath)
}
$manifest = Get-Content -LiteralPath $templatePath -Raw
$manifest = $manifest.Replace('__BROWNPANTS_REPOSITORY_URL__', $parsedRepositoryUrl.AbsoluteUri)
$manifest = $manifest.Replace('__BROWNPANTS_LICENSE__', $License)
Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding utf8
Write-Output "Created $manifestPath"

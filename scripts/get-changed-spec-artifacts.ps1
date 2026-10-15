[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('pull_request', 'push', 'workflow_dispatch')][string] $EventName,
    [Parameter(Mandatory = $true)][ValidateSet('branch', 'tag', 'unknown')][string] $RefType,
    [string] $Before,
    [Parameter(Mandatory = $true)][string] $Head,
    [string] $PullRequestBase,
    [string] $PullRequestHead
)

$ErrorActionPreference = 'Stop'

if ($EventName -eq 'workflow_dispatch') { return }

if ($EventName -eq 'pull_request') {
    if ([string]::IsNullOrWhiteSpace($PullRequestBase) -or [string]::IsNullOrWhiteSpace($PullRequestHead)) {
        throw 'Pull request base and head commits are required.'
    }
    $changed = & git diff --name-only "$PullRequestBase...$PullRequestHead" -- specs
}
elseif ($Before -notmatch '^0+$') {
    $changed = & git diff --name-only $Before $Head -- specs
}
elseif ($RefType -eq 'tag') {
    $parent = & git rev-parse "$Head^" 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace(($parent | Select-Object -First 1))) {
        $changed = & git diff --name-only $parent $Head -- specs
    }
    else {
        $changed = & git ls-tree -r --name-only $Head -- specs
    }
}
else {
    $changed = & git ls-tree -r --name-only $Head -- specs
}

if ($LASTEXITCODE -ne 0) { throw 'Could not determine changed Spec Kit artifacts from Git history.' }
$changed | Where-Object { $_ -match '^specs/[^/]+/(tasks\.md|acceptance-evidence\.json)$' } | Sort-Object -Unique

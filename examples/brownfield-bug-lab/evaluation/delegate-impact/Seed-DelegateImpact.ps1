param([Parameter(Mandatory=$true)][string] $FixtureRoot)
$ErrorActionPreference = 'Stop'
$path = Join-Path (Resolve-Path -LiteralPath $FixtureRoot).Path 'src/WorkRepository.cs'
$text = [IO.File]::ReadAllText($path)
$correct = 'public void Save(WorkItem item) => _saved.Add(item);'
$seeded = 'public void Save(WorkItem item) { }'
if ($text.Contains($seeded, [StringComparison]::Ordinal)) { throw 'Delegate impact defect is already seeded.' }
if (-not $text.Contains($correct, [StringComparison]::Ordinal)) { throw 'Expected repository save implementation is missing; refusing to seed.' }
[IO.File]::WriteAllText($path, $text.Replace($correct, $seeded), [Text.UTF8Encoding]::new($false))

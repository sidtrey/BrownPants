param([Parameter(Mandatory=$true)][string] $DemoRoot)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $DemoRoot).Path
$path = Join-Path $root 'managed/Workflows/OrderImportWorkflow.cs'
$text = [IO.File]::ReadAllText($path)
$correct = '=> _repository = repository;'
$seeded = '=> _repository = new OrderRepository<T>();'
if ($text.Contains($seeded, [StringComparison]::Ordinal)) { throw 'The constructor defect is already seeded.' }
if (-not $text.Contains($correct, [StringComparison]::Ordinal)) { throw 'Expected repository-injection assignment was not found; refusing to modify the source.' }
$text = $text.Replace($correct, $seeded)
[IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false))

param([Parameter(Mandatory=$true)][string] $DemoRoot)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $DemoRoot).Path
$path = Join-Path $root 'managed/Persistence/OrderRepository.cs'
$text = [IO.File]::ReadAllText($path)
$correct = @"
        foreach (var item in items)
            _saved.Add(item);
"@
$seeded = @"
        foreach (var item in items)
        {
            if (item is IRetryTracked { RequiresRetry: true })
                continue;
            _saved.Add(item);
        }
"@
$correct = $correct.Trim("`r", "`n")
$seeded = $seeded.Trim("`r", "`n")
if ($text.Contains($seeded, [StringComparison]::Ordinal)) { throw 'The source is already seeded.' }
if (-not $text.Contains($correct, [StringComparison]::Ordinal)) { throw 'Expected batch loop not found; refusing to modify source.' }
$text = $text.Replace($correct, $seeded)
[IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false))

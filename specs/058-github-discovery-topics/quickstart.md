# Quickstart: Verify BrownPants GitHub Topics

Open the repository's About section and confirm the 11 required topic labels are visible: `brownfield`, `legacy-modernization`, `code-analysis`, `repository-analysis`, `spec-driven-development`, `spec-kit`, `dotnet`, `csharp`, `visual-basic`, `cpp`, and `mfc`.

The public topic endpoint can be read without authentication:

```text
https://api.github.com/repos/sidtrey/BrownPants/topics
```

The repository metadata edit requires admin-level write permission. If an authenticated update is not available, use GitHub's **About → Edit repository metadata → Topics** control, save the reviewed list, then re-read the public endpoint. Never put access tokens in shell history or committed files.

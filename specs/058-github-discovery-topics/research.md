# Research: GitHub Discovery Topics

## Decisions

- Use repository topics to classify BrownPants by purpose, supported ecosystems, and workflow. GitHub documents that topics appear on repository pages and help users discover related repositories.
- Use this required core set: `brownfield`, `legacy-modernization`, `code-analysis`, `repository-analysis`, `spec-driven-development`, `spec-kit`, `dotnet`, `csharp`, `visual-basic`, `cpp`, and `mfc`.
- Avoid `static-analysis`, `ai`, and engine-specific host labels. The product is a source evidence and repository analysis tool; it supports optional AI/IDE adapters but does not require an LLM. MFC findings are lexical candidates, not compiler-resolved behavior.
- Read current topics before writing. The endpoint replaces the complete set, so preserve any newly added, accurate topics that fit GitHub's cap. Read the public list again after writing.

## Constraints and Permissions

GitHub's official topic guide permits lowercase letters, numbers, and hyphens; limits each topic to 50 characters; and allows up to 20 topics. The repository currently has no topics, according to its public About section.

The official REST API's replace-topics endpoint replaces the complete topic set and saves names in lowercase. Fine-grained tokens need repository `Administration: write`; a metadata read is public for public repositories. Do not expose the credential in logs or save it in the repository. If the available Git credential cannot write metadata, stop without changing anything and provide the permission requirement.

## Sources

- [Classifying your repository with topics](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/classifying-your-repository-with-topics)
- [REST API: replace all repository topics](https://docs.github.com/en/rest/repos/repos#replace-all-repository-topics)
- Repository README and implemented language/workflow support, reviewed on 2026-10-15.

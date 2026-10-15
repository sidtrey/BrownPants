# Quickstart: GitHub Draft Pull Requests

1. Prepare and inspect a local preview. This command needs no Azure DevOps file, credentials, or network access:

   ```powershell
   brownpants pr preview . --feature specs/039-github-draft-pull-requests --repository sidtrey/BrownPants --source feature/039-github-draft-pull-requests --target main --output artifacts/039-pr-preview.md
   ```

2. Review the generated file, including criteria digest, task evidence, QA instructions, gaps, title, body, and branch/repository metadata. Resolve the preview's gaps before claiming that the work is verified.

3. Provide a least-privilege GitHub token through your approved environment process. The token needs repository `Pull requests: write` permission. Do not put it in repository configuration.

4. Create the GitHub draft PR explicitly:

   ```powershell
   brownpants pr create . --provider github --preview artifacts/039-pr-preview.md --repository sidtrey/BrownPants --source feature/039-github-draft-pull-requests --target main --confirm --online
   ```

BrownPants creates only a draft PR and prints its number and URL. A timeout may occur after GitHub creates the PR; inspect GitHub before trying again. Review, approval, merge, and completion stay with the human team. This first provider adapter supports GitHub.com, not GitHub Enterprise Server.

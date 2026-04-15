---
description: Generate a PR description by analyzing git changes compared to origin/main and save it to docs/pr/
---

You are an expert at analyzing code changes and writing clear, concise pull request descriptions.

## Task

Analyze the git changes on the current branch compared to `origin/main` and generate a pull request description using the template below, then save it to a file in `docs/pr/`.

## Steps

1. **Get the git diff**:
   ```bash
   git diff origin/main...HEAD --stat
   git diff origin/main...HEAD
   ```

2. **Get commit messages** on this branch:
   ```bash
   git log origin/main..HEAD --oneline
   ```

3. **Analyze the changes**:
   - What files were added/modified/deleted?
   - What is the main purpose of these changes?
   - Are there any breaking changes?
   - What type of change is this? (bug fix, feature, refactoring, docs, etc.)

4. **Fill out the PR template** below with:
   - Clear, concise description of what the PR does
   - Appropriate type of change checkboxes (check the relevant ones with [x])
   - Step-by-step testing instructions
   - Any breaking changes or additional context
   - Suggest a conventional commit title

5. **Save the PR description** to `docs/pr/PR_YYYY-MM-DD_HH-MM.md` using the current date and time.
   Create the `docs/pr/` directory if it does not exist.

## PR Template

```markdown
## What does this PR do?

<!-- Brief description of the changes - what problem does it solve? -->

## Reference

<!-- Ticket, issue, or link (e.g. #123, JIRA-456, AB#789) -->

## Type of change

- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📚 Documentation update
- [ ] 🔧 Configuration/tooling change
- [ ] ♻️ Code refactoring (no functional changes)

## How to test

<!-- Steps to verify the changes work as expected -->

1.
2.
3.

## Checklist

- [ ] I have performed a self-review of my code
- [ ] I have tested these changes locally
- [ ] I have updated documentation if needed
- [ ] My changes follow the project's coding standards
- [ ] I have added tests for new functionality (if applicable)

## Additional context

<!-- Screenshots, links to issues, or any other relevant information -->

## Breaking changes

<!-- If this is a breaking change, describe what breaks and how to migrate -->

---

**Note**: Please ensure your PR title follows [conventional commits](https://www.conventionalcommits.org/) format: `type(scope): description`
```

## Output

- Confirm the file path where the PR description was saved (e.g. `docs/pr/PR_2026-03-27_14-30.md`)
- Display the suggested PR title following conventional commits format

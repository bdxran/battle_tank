---
description: Prepare a conventional commit message and commit changes
---

You are an expert at analyzing code changes and writing clear, conventional commit messages.

## Task

Analyze the current git changes and create a conventional commit following the project's standards, then commit the changes.

## Project Commit Standards (MANDATORY)

From CLAUDE.md:
- **Scope is MANDATORY**: `feat(scope): message`
- Prefixes: `feat`, `fix`, `refactor`, `test`, `docs`, `style`, `perf`, `chore`
- Language: French or English (depending on context)
- Examples:
  - `feat(api): add pagination to list endpoint`
  - `fix(auth): resolve token expiry handling`
  - `refactor(db): simplify query builder`
  - `chore(deps): update dependencies`
  - `docs(claude): update architecture diagram`

## Steps

1. **Check current git status**:
   ```bash
   git status
   ```

2. **Review the staged and unstaged changes**:
   ```bash
   git diff --stat
   git diff --cached --stat
   ```

3. **Analyze the changes**:
   - What files were modified?
   - What is the main purpose of these changes?
   - What scope best represents these changes? (e.g., api, db, auth, worker, cli, config)
   - What type is this? (feat, fix, refactor, chore, docs, etc.)

4. **Determine the commit type and scope**:
   - `feat(scope)`: New feature
   - `fix(scope)`: Bug fix
   - `refactor(scope)`: Code refactoring
   - `chore(scope)`: Maintenance, config, dependencies
   - `docs(scope)`: Documentation changes
   - `test(scope)`: Tests
   - `style(scope)`: Code style/formatting
   - `perf(scope)`: Performance improvements

5. **Generate commit message**:
   - First line: `type(scope): short description` (max 72 chars)
   - Blank line
   - Body: Detailed explanation (if needed)
   - Blank line
   - Footer with co-authorship:
   ```
   🤖 Generated with [Claude Code](https://claude.com/claude-code)

   Co-Authored-By: Claude <noreply@anthropic.com>
   ```

6. **Execute the pre-commit command**:
   - Run `pre-commit run --all-files` first and then a `git add .` if some files were auto-fixed so we can be sure that the pre-commit will pass when we actually commit

7. **Execute the commit**:
   - Stage all changes with `git add .`
   - Commit with the generated message using heredoc format
   - Run `git status` to verify success

## Important Notes

- **ALWAYS include a scope** - this is mandatory per project standards
- Keep the first line under 72 characters
- Use present tense ("add feature" not "added feature")
- Be clear and concise
- Include the Claude Code footer
- DO NOT ask for confirmation - prepare and execute the commit directly
- If there are no changes to commit, inform the user and stop

## Example Output Format

First, show the user the commit message you're about to use:

```
📝 Commit message:
feat(api): add pagination support to list endpoints

- Add page and limit query parameters
- Return total count in response headers
- Default page size: 20, max: 100

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

Then execute the commit and show the result.

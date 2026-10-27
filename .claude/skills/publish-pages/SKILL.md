---
name: publish-pages
description: Publish the newest version of the routine simulator to the project's GitHub Pages site. Use when asked to publish, deploy, update or upload the GitHub page / web version / online simulator, or to put the latest routines online.
---

# Publish the simulator to GitHub Pages

Goal: the live site shows the current version of the project, verified, with one command
from the user. Run everything from the project root (the folder with `Assets/`).

## Steps

1. **Check what will go out.** `git status --short` and `git log origin/main..HEAD --oneline`.
   The build is made from the working tree, so uncommitted routine changes are included.
   If there are uncommitted or unpushed changes, say so in one line and continue; the user
   asked for the newest version. Don't commit or push main unless they ask.

2. **Publish:** run `tools/publish-pages.sh` in the background (a rebuild takes a few
   minutes; it only rebuilds when scripts changed since the last build). Use `--rebuild` if
   the user says the site looks stale or the last build may be broken. If it fails, it prints
   the compile errors - fix them, then rerun. If it says Pages is not on yet, the gh account
   lacks admin rights on the repo: the owner must turn Pages on once in the repo's
   Settings > Pages (Deploy from a branch, gh-pages, root). Give them that link and wait.

3. **Wait for GitHub to deploy.** Check
   `gh api repos/{owner}/{repo}/pages/builds/latest -q .status` until it is `built`
   (usually 1-2 minutes; space the checks out, don't hammer it). `errored` means look at
   `gh api repos/{owner}/{repo}/pages/builds/latest` for the reason.

4. **Verify the live site** (the URL the script prints, e.g.
   https://ninaxhqiu.github.io/swarmRobots_ER/): `curl -sI` the page and
   `Build/SimPreview.wasm` both return 200, and the gh-pages commit message names the
   commit you expected (`git log origin/gh-pages -1 --oneline` after `git fetch origin gh-pages`).
   If a browser is at hand, open it and confirm the simulator loads with its routine buttons.
   Browsers cache the old build: suggest a hard refresh (Cmd+Shift+R) if it looks old.

5. **Report** in two or three lines: the URL, which commit it is built from, and anything
   not included or not verified.

## How it works

- The site is the `gh-pages` branch, which holds only the built `SimPreview/` files plus
  `.nojekyll`. Each publish replaces that branch with a single fresh commit, so the repo
  doesn't grow with every build. `main` never contains build output.
- `?play=N` on the URL autoplays routine N (alphabetical order, from 0).
- The Real robots screen works on the live site too (Web Bluetooth needs https, which Pages
  provides), in Chrome.

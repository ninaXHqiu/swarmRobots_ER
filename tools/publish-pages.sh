#!/bin/bash
# Publishes the routine simulator to GitHub Pages: builds it if scripts changed since the last
# build, then pushes SimPreview/ as the only content of the gh-pages branch (replacing what was
# there, so the branch never grows). main stays free of build output.
#   tools/publish-pages.sh            build if needed, then publish
#   tools/publish-pages.sh --rebuild  always build first
set -e
cd "$(dirname "$0")/.."

UNITY="$HOME/Unity/Editors/2021.3.26f1/Unity.app/Contents/MacOS/Unity"
LOG="$HOME/Unity/build.log"
BRANCH=gh-pages

REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
OWNER=${REPO%%/*}
NAME=${REPO#*/}
URL="https://$(echo "$OWNER" | tr '[:upper:]' '[:lower:]').github.io/$NAME/"

needs_build=no
if [ "$1" = "--rebuild" ] || [ ! -f SimPreview/index.html ]; then
  needs_build=yes
elif [ -n "$(find Assets -name '*.cs' -newer SimPreview/index.html | head -1)" ]; then
  needs_build=yes
fi

if [ "$needs_build" = yes ]; then
  if [ ! -x "$UNITY" ]; then
    echo "Unity not found. Run tools/first-time-setup.sh first."
    exit 1
  fi
  echo "Building the simulator (a few minutes). Log: $LOG"
  if ! "$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod WebBuild.BuildSimulator -logFile "$LOG"; then
    echo "Build failed. Errors:"
    grep -E "error CS[0-9]+|No valid Unity Editor license" "$LOG" | sort -u | head -20
    exit 1
  fi
  # Unity rewrites these on every build; they are not real changes
  git checkout -- Assets/Unity.VisualScripting.Generated 2>/dev/null || true
  touch SimPreview/index.html
fi

# Say what the published build is made from
COMMIT=$(git rev-parse --short HEAD)
if [ -n "$(git status --porcelain -- Assets)" ]; then
  echo "Note: Assets/ has uncommitted changes; they are included in this build."
  COMMIT="$COMMIT + uncommitted changes"
fi

STAGE=$(mktemp -d)
trap 'rm -rf "$STAGE"' EXIT
cp -R SimPreview/. "$STAGE/"
touch "$STAGE/.nojekyll"   # serve the Unity files as they are
git -C "$STAGE" init -q -b "$BRANCH"
git -C "$STAGE" add -A
git -C "$STAGE" -c user.name="$(git config user.name)" -c user.email="$(git config user.email)" \
  commit -q -m "Simulator build from $COMMIT"
echo "Uploading the build (~40 MB)..."
git -C "$STAGE" push -q --force "$(git remote get-url origin)" "$BRANCH:$BRANCH"

# Turn Pages on the first time
if ! gh api "repos/$REPO/pages" >/dev/null 2>&1; then
  if gh api -X POST "repos/$REPO/pages" -f "source[branch]=$BRANCH" -f "source[path]=/" >/dev/null 2>&1; then
    echo "GitHub Pages turned on for the $BRANCH branch."
  else
    echo "The build is on the $BRANCH branch, but GitHub Pages is not on yet and gh (signed in as"
    echo "$(gh api user -q .login)) may not turn it on. Once, as the repo owner: open"
    echo "https://github.com/$REPO/settings/pages, Source: Deploy from a branch, Branch: $BRANCH / (root), Save."
    exit 1
  fi
fi

echo "Published. GitHub takes a minute or two to update: $URL"

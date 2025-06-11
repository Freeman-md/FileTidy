#!/bin/bash
set -e

VERSION=$1

if [ -z "$VERSION" ]; then
  echo "❌ Usage: ./tag-release.sh v1.0.0"
  exit 1
fi

git tag -f "$VERSION"
git push origin -f "$VERSION"
echo "✅ Tagged and pushed: $VERSION"

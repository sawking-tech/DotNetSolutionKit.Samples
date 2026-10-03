#!/usr/bin/env bash
#
# Builds an image for every service, or for the services named:
#
#   deploy/build-images.sh                      every service
#   deploy/build-images.sh Orders Billing       only these (the part of the folder name after the product)
#   REGISTRY=registry.example.com TAG=1.4.0 PUSH=1 deploy/build-images.sh
#
# An image is named <REGISTRY>/<service in lower case, dots as underscores>:<TAG>, the name the
# service's deploy files use. GIT_SHA is the last commit that touched what the image is built from -
# Common, the build props and the service's own folder - so an unchanged service gets the same image
# on every build instead of a new one per commit. Without the build attestation (--provenance=false),
# which carries a timestamp, the digest stays the same too.
set -euo pipefail

cd "$(dirname "$0")/.."

REGISTRY="${REGISTRY:-local}"
TAG="${TAG:-latest}"
PUSH="${PUSH:-0}"
PREFIX="ST.DotNetSolutionKit.Samples."

for project in src/services/*/*.API/*.API.csproj; do
    folder=$(basename "$(dirname "$(dirname "$project")")")
    service="${folder#"$PREFIX"}"

    if [ $# -gt 0 ] && ! printf '%s\n' "$@" | grep -qx "$service"; then
        continue
    fi

    name=$(echo "$service" | tr '[:upper:]' '[:lower:]' | tr '.' '_')
    image="$REGISTRY/$name:$TAG"
    sha=$(git log -1 --format=%h -- Directory.Build.props version.json src/Directory.Packages.props src/common "src/services/$folder" 2>/dev/null || true)

    echo "==> $image (commit ${sha:-local})"
    docker build --provenance=false --build-arg SERVICE="$folder" --build-arg GIT_SHA="${sha:-local}" -t "$image" .

    if [ "$PUSH" = "1" ]; then
        docker push "$image"
    fi
done

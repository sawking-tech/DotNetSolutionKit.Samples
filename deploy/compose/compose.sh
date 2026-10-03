#!/usr/bin/env bash
#
# docker compose over the infrastructure and every service of the solution:
#
#   deploy/compose/compose.sh up -d --wait     start, and wait until every service is ready
#   deploy/compose/compose.sh logs -f
#   deploy/compose/compose.sh down
#
# Each service brings its own src/services/<Service>/deploy/compose.yml, so a service generated later
# joins without editing a shared file. Paths in those files are relative to the solution root.
set -euo pipefail

cd "$(dirname "$0")/../.."

files=(-f deploy/compose/infrastructure.yml)
for service in src/services/*/deploy/compose.yml; do
    [ -e "$service" ] && files+=(-f "$service")
done

env_file=()
[ -f deploy/compose/.env ] && env_file=(--env-file deploy/compose/.env)

exec docker compose --project-directory . "${env_file[@]}" "${files[@]}" "$@"

#!/usr/bin/env bash
set -e

# ======== Colors ========
GREEN="\033[0;32m"
YELLOW="\033[0;33m"
BLUE="\033[0;34m"
RESET="\033[0m"

# ======== Folders ========
SERVICES_DIR="$(dirname "$0")"
ALL_SOLUTION=$(find "$SERVICES_DIR" -name "*.All.sln" | head -n 1)

if [ -z "$ALL_SOLUTION" ]; then
    echo -e "${YELLOW}All.sln file not found. Create it manually.${RESET}"
    exit 1
fi

echo -e "${BLUE}Using solution:${RESET} $ALL_SOLUTION"
echo -e "${BLUE}Checking service projects...${RESET}"

# Get list of already added projects (normalize slashes)
existing=$(dotnet sln "$ALL_SOLUTION" list | sed 's|\\|/|g')

# Iterate through all service folders
for service_dir in "$SERVICES_DIR"/*; do
    [ -d "$service_dir" ] || continue

    # Calculate short service name (after last dot)
    full_folder=$(basename "$service_dir")              # ST.DotNetSolutionKit.Samples.Orders
    folder_name="${full_folder##*.}"                   # Orders

    # Find all csproj files inside service folder (max 2 levels)
    for proj in $(find "$service_dir" -maxdepth 2 -name "*.csproj"); do
        proj_rel=$(realpath --relative-to="$(dirname "$ALL_SOLUTION")" "$proj" | sed 's|\\|/|g')
        proj_name=$(basename "$proj")

        if ! echo "$existing" | grep -qF "$proj_rel"; then
            dotnet sln "$ALL_SOLUTION" add "$proj" --solution-folder "$folder_name" >/dev/null
            echo -e "${GREEN}Added:${RESET} $proj_name → $folder_name"
        else
            echo -e "${YELLOW}Already connected:${RESET} $proj_name"
        fi
    done
done

echo -e "${BLUE}All projects checked.${RESET}"

# Keeps a window opened by a double click until it is read. Without a terminal, in CI or another
# script, there is nobody to press a key, and read would fail the script.
if [ -t 0 ]; then
    read -n1 -r -p "Press any key to exit..."
    echo
fi
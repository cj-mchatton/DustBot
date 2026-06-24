#!/bin/zsh

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
UNITY_APP="/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app"

if [[ ! -d "$UNITY_APP" ]]; then
  osascript -e 'display alert "Unity 6000.5.0f1 is not installed" message "Install this Unity editor version through Unity Hub, then try again."'
  exit 1
fi

open -na "$UNITY_APP" --args -projectPath "$PROJECT_PATH"

#!/bin/zsh

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
GAME_APP="$PROJECT_PATH/Build/ScreenshotPlayer/DustBot.app"
EDITOR_LAUNCHER="$PROJECT_PATH/Open DustBot.command"

if [[ -d "$GAME_APP" ]]; then
  open "$GAME_APP"
  exit 0
fi

osascript -e 'display dialog "A standalone DustBot build has not been created yet. Open the Unity project instead?" buttons {"Cancel", "Open Unity"} default button "Open Unity"'
if [[ $? -eq 0 ]]; then
  open "$EDITOR_LAUNCHER"
fi

#!/bin/bash
dotnet build "./Source/CaravanMyWay/CaravanMyWay.csproj"
osascript -e 'tell application "RimWorld" to activate'
# RimWorld Mod Development Setup for Mac

## Mod Directory Locations

The correct location for mods on Mac is:
~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods

## Create Symlink for Development

Create a symlink from your development folder to RimWorld's mod directory:

```bash
ln -s "/Users/elijahfry/code/CaravanMyWay" "/Users/elijahfry/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/CaravanMyWay"
```

## Verify Setup

Check that everything is correctly linked:

```bash
ls -la "/Users/elijahfry/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods"
```

## Testing

1. Launch RimWorld
2. Check the Mods menu - your mod should appear in the list
3. Enable the mod and restart RimWorld

## Debugging Tips

If the mod doesn't appear:

```bash
# Verify the symlink is correct
readlink "/Users/elijahfry/Library/Application Support/RimWorld/Mods/CaravanMyWay"

# Check mod structure
ls -R "/Users/elijahfry/code/CaravanMyWay"

# Ensure About.xml is properly formatted and encoded
file "/Users/elijahfry/code/CaravanMyWay/About/About.xml"
```

## Important Notes

- Do NOT create the symlink in the Steam mods directory
- Always use the user's Mods directory: `~/Library/Application Support/RimWorld/Mods`
- Restart RimWorld after making changes to mod files
- Keep your development directory outside of the RimWorld installation folder

## Development Workflow Tips

- Keep Player.log open while testing
- Use tail -f in Terminal to watch the log
- After making changes to XML files, you'll need to restart RimWorld

## Install Development Tools

```bash
brew install dotnet-sdk
brew install mono-mdk
```

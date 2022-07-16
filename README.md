# UnityMergeTool
A better tool for merging Unity asset files

This tool attempts to solve the biggest issue when working in unity with other users -- merge conflicts with scene and prefab files. While unity does provide a yaml merge tool, it is fairly limited and generally only merges non-conflicting files, otherwise it fails and falls back on a text based merge tool.

UnityMergeTool merges everything that does not conflict, and takes remote (theirs) whenever there is a conflict. It then writes out the conflicting properties into a conflict report with values from both sides of the conflict. The output is always a unity file that is loadable in the editor to reapply only the conflicting changes if required. This workflow is much easier for developers and content creators alike and saves considerable time and rework when assets conflict.

the tool has an understanding of the game object hierarchy and whenever possible includes the scene path to components in the conflict report.

it also understands prefab instance overrides and can do a recursive merge of those properties as well.

---
### Installation:

Download the latest release build for your platform and unzip it. 

---
### Usage:

    UnityMergeTool merge "BASE" "REMOTE" "LOCAL" "MERGED"

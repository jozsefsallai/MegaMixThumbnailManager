@echo off

call msbuild "MegaMixThumbnailManager.sln" -t:Restore -p:Configuration=Release
call msbuild "MegaMixThumbnailManager.sln" -t:Rebuild -p:Configuration=Release -p:Platform=x64
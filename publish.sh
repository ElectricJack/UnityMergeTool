dotnet publish -c Release -r osx.11.0-x64
cp ./merge.sh UnityMergeTool/bin/Release/netcoreapp3.1/osx.11.0-x64/merge.sh
cd UnityMergeTool/bin/Release/netcoreapp3.1
mv osx.11.0-x64 UnityMergeToolRelease
zip -r UnityMergeToolRelease.zip UnityMergeToolRelease
rm -rf UnityMergeToolRelease

rm ~/Desktop/UnityMergeToolRelease.zip
rm -rf ~/Desktop/UnityMergeToolRelease
cp ./UnityMergeToolRelease.zip ~/Desktop
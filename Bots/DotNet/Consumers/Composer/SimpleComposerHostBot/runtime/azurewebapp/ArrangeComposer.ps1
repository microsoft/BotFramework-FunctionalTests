// This script arranges the composer files into the runtime project
// so we can do a webdeploy of the project from azure

Remove-Item "ComposerDialogs" -Recurse

New-Item -Path "ComposerDialogs/dialogs" -ItemType Directory
copy-item "../../dialogs/*" "ComposerDialogs/dialogs" -force -recurse -verbose

New-Item -Path "ComposerDialogs/knowledge-base" -ItemType Directory
copy-item "../../knowledge-base/*" "ComposerDialogs/knowledge-base" -force -recurse -verbose

New-Item -Path "ComposerDialogs/language-generation" -ItemType Directory
copy-item "../../language-generation/*" "ComposerDialogs/language-generation" -force -recurse -verbose

New-Item -Path "ComposerDialogs/language-understanding" -ItemType Directory
copy-item "../../language-understanding/*" "ComposerDialogs/language-understanding" -force -recurse -verbose

New-Item -Path "ComposerDialogs/schemas" -ItemType Directory
copy-item "../../schemas/**/*.schema" "ComposerDialogs/schemas" -force -recurse -verbose

New-Item -Path "ComposerDialogs" -name "settings" -ItemType "directory"
copy-item "../../settings/*" "ComposerDialogs/settings" -force -recurse -verbose

copy-item "../../*.dialog" "ComposerDialogs/" -force -recurse -verbose 
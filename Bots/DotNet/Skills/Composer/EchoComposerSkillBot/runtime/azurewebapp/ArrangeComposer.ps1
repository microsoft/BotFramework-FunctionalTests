# This script arranges the composer files into the runtime project
# so we can do a webdeploy of the project from azure

# Helper to copy folders
function Copy-Folder ($folderName)
{
    $sourcePath = "../../" +  $folderName
    $targetPath = "ComposerDialogs/" + $folderName
    if ( Test-Path -Path $sourcePath -PathType Container )
    {
        New-Item -Path $targetPath -ItemType Directory
        copy-item ($sourcePath + "/*") $targetPath -force -recurse 
    }
}

# Helper to delete a folder (if it exists)
function Delete-Folder ($folderName)
{
    if ( Test-Path -Path $folderName -PathType Container )
    {
        # Ensure bin folder is clean
        Remove-Item $folderName -Recurse
    }
}


# Cleanup
Delete-Folder "bin"
Delete-Folder "ComposerDialogs"
Delete-Folder "wwwroot/manifests"

# Create folder for composer files
New-Item -Path "ComposerDialogs" -ItemType Directory

# Copy composer folders
Copy-Folder "dialogs"
Copy-Folder "generated"
Copy-Folder "knowledge-base"
Copy-Folder "language-generation"
Copy-Folder "language-understanding"
Copy-Folder "recognizers"
Copy-Folder "settings"

# Copy schemas
New-Item -Path "ComposerDialogs/schemas" -ItemType Directory
copy-item "../../schemas/*.schema" "ComposerDialogs/schemas" -force -recurse 

# Copy root dialog
copy-item "../../*.dialog" "ComposerDialogs/" -force -recurse 

# Copy manifests (if any)
$sourcePath = "../../manifests"
$targetPath = "wwwroot/manifests"
if ( Test-Path -Path $sourcePath -PathType Container )
{
    New-Item -Path $targetPath -ItemType Directory
    copy-item ($sourcePath + "/*") $targetPath -force -recurse 
}
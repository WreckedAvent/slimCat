del .\build -Recurse -Force
mkdir .\build
mkdir .\build\client
copy-item .\slimCat\bin\Release\* .\build\client\ -Recurse -Force -Container
copy-item .\Bootstrapper\bin\Release\Bootstrapper.exe .\build -recurse -Force
rename-item .\build\client\slimCat.exe client.exe
rename-item .\build\client\slimCat.pdb client.pdb
rename-item .\build\client\slimCat.exe.config client.exe.config
move-item .\build\client\lib\ .\build\
rename-item .\build\Bootstrapper.exe slimCat.exe
copy-item .\Shortcuts\* .\build\ -Force
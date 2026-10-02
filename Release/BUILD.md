# Release build instructions

## Debug build

`dotnet build`

## Release build

`dotnet publish ImageConverterPro.csproj -c Release -r win-x64 --self-contained false -o Release\ImageConverterPro\ApplicationFiles`

## Inno Setup installer

1. Install Inno Setup 6.
2. Run the release publish command above.
3. Open `Installer\ImageConverterPro.iss` in Inno Setup Compiler.
4. Build the script to produce the installer in `Release\Installer`.

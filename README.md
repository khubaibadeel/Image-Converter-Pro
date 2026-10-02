# Image Converter Pro

Image Converter Pro is a Windows desktop application for converting, optimizing, and editing images. It provides batch conversion, side-by-side previews, and controls for preparing images without changing the original files.

## Features

- Convert one image or a batch of images; add files or folders by browsing or drag and drop.
- Export to **PNG, JPG, WEBP, BMP,** or **TIFF**.
- Preview the source and converted image side by side, with estimated output size and dimensions.
- Set output quality, resize dimensions, aspect ratio, background color, and metadata handling.
- Choose optimization presets for Website, Email, Instagram, or Facebook.
- Edit images with crop presets, rotation, flipping, brightness, contrast, saturation, and sharpness controls.
- Review conversion and editing history, and undo or redo edits.
- Customize the light or dark theme, output defaults, preview, and performance settings.

## Getting started

Download and run a Windows installer from [GitHub Releases](https://github.com/khubaibadeel/Image-Converter-Pro/releases) if one is available. You can also build the application from source as described below.

## Build from source

### Requirements

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) to run a framework-dependent build
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) if you want to package an installer

### Build and run

From the repository root:

```powershell
dotnet restore ImageConverterPro.csproj
dotnet build ImageConverterPro.csproj -c Release
dotnet run --project ImageConverterPro.csproj
```

### Publish and create an installer

Publish the Windows x64 application:

```powershell
dotnet publish ImageConverterPro.csproj -c Release -r win-x64 --self-contained false -o Release\ImageConverterPro\ApplicationFiles
```

Then open `Installer\ImageConverterPro.iss` in Inno Setup Compiler and build it. The installer script writes its output to `Release\Installer`.

## Using the app

1. Add images to the conversion queue using **Add files**, **Add folder**, or drag and drop.
2. Select an image to inspect it and compare the original with the converted preview.
3. Choose an output format and adjust quality, resize, optimization, and metadata settings as needed.
4. Select an output folder and choose **Convert All**.
5. To edit an image instead, open the **Edit** tab, choose **Open image**, apply edits, and use **Save As** to save the result.

Conversion and editing save new output files; the source images are left untouched.

## Technology

Built with C#, WPF, and .NET 8. Image processing uses [ImageSharp](https://github.com/SixLabors/ImageSharp), and conversion and editing history is stored in a local SQLite database.

## License

No license file is currently included in this repository. Contact the repository owner for licensing and reuse permissions.

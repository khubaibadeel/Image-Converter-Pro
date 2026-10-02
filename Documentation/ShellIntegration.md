# Future Windows Explorer integration

Image Converter Pro is structured so a future shell-integration package can invoke the existing conversion services without duplicating image-processing code.

The recommended implementation is a separate, signed helper executable registered by the installer. It should accept a selected image path and output format, create the application's dependency container, then call `IImageProcessingService.ConvertImageAsync`. A native in-process shell extension is intentionally not included because it has stricter registration, COM, stability, and signing requirements.

Future installer work should add context-menu registration for commands such as **Convert to JPG** and remove that registration during uninstall.

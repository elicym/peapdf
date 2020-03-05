# PeaPDF
PeaPDF is a library to render PDFs. It can be used to display PDFs in your application, or to convert a PDF to an image.
The vast majority of PDFs can be accurately rendered. It is currently in beta, and we rely on your feedback to make PeaPDF even better.
# License
The license is Apache License, Version 2.0, allowing PeaPDF to be used for free, including commercially.
# Installation
Install NuGet package SeaPeaYou.PeaPDF.
# Usage
Include the namespace:
```csharp
using SeaPeaYou.PeaPdf;
```
Load the pdf:
```csharp
PDF pdf = new PDF(pdfBytes, password); //password is optional
```
Render:
```csharp
SKImage img = pdf.Render(pageNumber, 2); //the second parameter is the scale
```
Once you have a SKImage, you can utilize the power of SkiaSharp to work with it, [see the docs](https://docs.microsoft.com/en-us/dotnet/api/skiasharp).
For example, to save as a .jpeg or .png, use:
```csharp
using (var imgFile = File.Create(imgFilePath))
    img.Encode().SaveTo(imgFile);
```
# Dependencies
PeaPDF's framework is .NET Standard 2.0, allowing it to be used with .NET Framework & .NET Core.
It's only dependency is [SkiaSharp](https://www.nuget.org/packages/SkiaSharp/).
Nearly all code was written by myself; any that was not, is preceded by a comment why there is no licensing concern.
The following parsers were written from scratch using the formats' specifications, with regard to PDFs: JPEG, PNG, ICC, OTF, CFF, T6, LZW.
# Contact
Use [GitHub](https://github.com/elicym/peapdf/issues) to post any issues.
Alternatively, email me directly at elliott@seapeayou.net.

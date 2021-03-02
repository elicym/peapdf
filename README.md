# PeaPdf
PeaPdf is a .NET library to work with PDFs. It can be used to display PDFs in your application, convert a PDF page to an image, create new PDFs, modify existing PDFs, and fill in form fields.

We rely on your feedback to improve PeaPdf, please provide feedback on [GitHub](https://github.com/elicym/peapdf/issues), or email me at elliott@seapeayou.com.

## License
The license is Apache License, Version 2.0, allowing PeaPdf to be used for free, including commercially.

All 3rd-party components used have a likewise permissive license, see `LICENSE-3RD-PARTY` for details.

## Installation
Install NuGet package SeaPeaYou.PeaPdf.

## Usage
See [the documentation](https://github.com/elicym/peapdf/wiki).

## Dependencies
PeaPdf's framework is .NET Standard 2.0, allowing it to be used with .NET Framework & .NET Core.

It has a native library that is used for certain features (JPEG/JP2 decoding, CMYK conversion), you may or may not need it depending on what you are using PeaPdf for. The NuGet package includes a native library for Windows, for other platforms you can build it from the source.


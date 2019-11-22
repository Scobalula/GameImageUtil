# CoDImageUtil
[![Releases](https://img.shields.io/github/downloads/Scobalula/CoDImageUtil/total.svg)](https://github.com/Scobalula/CoDImageUtil/releases) [![License](https://img.shields.io/github/license/Scobalula/CoDImageUtil.svg)](https://github.com/Scobalula/CoDImageUtil/blob/master/LICENSE) [![Discord](https://img.shields.io/badge/chat-Discord-blue.svg)](https://discord.gg/fGVpV39)

CoDImageUtil is a tool to work with and process various images across all Call of Duty games for use within the Call of Duty Mod Tools. As with most games, the developers pack images, etc. and this will help with unpacking them. This tool is released especially for RaGe because of how much I love him.

## Requirements

* Windows 10 x64 or above (Windows 7/8/8.1 should work, but are untested)
* Microsoft Visual Studio 2017 Runtime ([x86](https://aka.ms/vs/16/release/vc_redist.x86.exe) and [x64](https://aka.ms/vs/16/release/vc_redist.x64.exe)) and .NET Framework 4.7.2

## Links:
* Discord Server: [https://discord.gg/fGVpV39](https://discord.gg/fGVpV39)
* Github Repo: [https://github.com/Scobalula/CoDImageUtil](https://github.com/Scobalula/CoDImageUtil)
* Latest Release: [https://github.com/Scobalula/CoDImageUtil/releases](https://github.com/Scobalula/CoDImageUtil/releases)

## Using CoDImageUtil

To use CoDImageUtil, download the latest version from the [Releases](https://github.com/Scobalula/CoDImageUtil/releases) page and run the exe. From here it is pretty easy, just select a mode and output format, and drag and drop images.

CoDImageUtil also accepts images from the material text files Greyhound produces, in this case the mode it overrides the mode and uses the semantic to determine the mode. This currently only works on Call of Duty: Modern Warfare 2019, but will support others soon.

## Modes

CoDImageUtil comes with multiple "modes" for processing different images:

* **Automatic**: this mode attempts to resolve the mode based off the input image name. Currently this is only tested on Call of Duty: Modern Warfare 2019 and Call of Duty: Infinite Warfare, but will support others soon.
* **Direct Convert**: this mode will simply perform a conversion on the given images, useful for converting DDS files (as the tool supports all DirectXTex formats), etc.
* **Patch Yellow Normal Map (XY)**: this will compute the Z (B channel) of a BC5 normal map with only XY components, also known as "Yellow Normal Maps" due their color. Note this is also used for Call of Duty: World War 2, even though they are grey, they are XY normal maps.
* **Patch Grey Normal Map (Older CoDs)**: this converts normal maps from older games that are grey with alpha to XYZ normal maps.
* **Split Normal/Gloss/Occlusion (CoD IW/MW 2019)**: this splits the Normal/Gloss/Occlusion of NOG/NG/NO images and computes the Z channels value.
* **Split RGB/A (Spec/Gloss, etc.)**: splits RGB/A channels, useful for splitting Specular/Gloss from older games and some newer games.
* **Split All Channels**: splits all channels into their own images, useful for splitting images like Call of Duty: World War 2's green images that have an image packed into each channel.
* **Split Specular Color (CoD IW/MW 2019)**: this attempts to split the Color/Specular Color using the metallic mask stored in the alpha channel of the image.
* **Split Specular Color**: this is the same as the above, only instead of using the alpha channel of the given image, it uses an image with the same name as the given image + _mask and uses the red channel of that image as the mask. For example: `colormap_and_specimage.png` the tool would expect `colormap_and_specimage_mask.png` in the same folder. Useful for Call of Duty: World War 2's images where the mask in the green image, just rename it and drag on the color map.

## License/Disclaimer

CoDImageUtil is licensed under the General Public License 3.0, you are free to use it under the terms of the GPL, including editing it, etc. to extend it. CoDImageUtil is distributed in the hope it will be useful to, but it comes WITHOUT  ANY WARRANTY, without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE, see the [LICENSE](https://github.com/Scobalula/CoDImageUtil/blob/master/LICENSE) file for more information.

To build CoDImageUtil you can simply load the project and compile, libraries are precompiled and including in the source, it is recommended to use VS 2017 with .NET Framework 4.7.2 as it is what is used for the release builds.

## Reporting Bugs

Bugs can be reported through the Github issues or through the Discord server.

## Credits

* **raptroes**: the sexy icon you all know you love
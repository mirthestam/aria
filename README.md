# Aria

A music library client that makes exploring and discovering your collection easy, with special support for classical music. It works with servers such as MPD or Lyrion, among others.

## Building

> This project is under heavy development and is not yet ready for use. It may not compile and could require unavailable
> dependencies.

1. Glone this repo recursive with `git clone --recurse-submodules`
2. Navigate to the cloned directory
2. Run the `./build.sh` script

# Dependencies
- [.NET 10](https://dotnet.microsoft.com/en-us/)

## Design Philosophy

This software aims to follow the [GNOME Human Interface Guidelines](https://developer.gnome.org/hig/).

## Contributing

Please try to follow the [GNOME Code of Conduct](https://conduct.gnome.org).

## Thanks

This project was initially inspired by [Plattenalbum](https://github.com/SoongNoonien/plattenalbum), with the initial goal of adding classical music support. To align with my
personal preference for C# and to explore a new implementation approach, the code was restructured from scratch while
preserving and building upon the original UI design. Many thanks to Martin Wagner for providing the inspiration and
foundation for this project.

The following projects were used as inspiration:

 - [Cantata](https://github.com/CDrummond/cantata)
 - [Euphonica](https://github.com/htkhiem/euphonica)
 - [LMS-Material](https://github.com/CDrummond/lms-material)
 - [Plattenalbum](https://github.com/SoongNoonien/plattenalbum)
 - [Stylophone](https://github.com/Difegue/Stylophone)

This project relies upon these great open source projects:

 - [MpcNET](https://github.com/glucaci/MpcNET)
 - [Gir.Core](https://gircore.github.io/)
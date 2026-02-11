# Aria

A remote control app for MPD and MPD-based players like Volumio that lets you browse and explore their music libraries, with dedicated support for classical music.

![screenshot](screenshots/medium.png)

## Building

> This project is under heavy development and is not yet ready for use. It may not compile and could require unavailable
> dependencies.

1. Glone this repo recursive with `git clone --recurse-submodules`
2. Navigate to the cloned directory

I recommend to install this app using Flatpak.

To Install using Flatpak:
1. Run `prebuild.sh`
2. Run `bundle.sh` to create a flatpak bundle
3. Run `flatpak install ./flatpak/aria.flatpak` to install the app.

To Run without Flatpak:
1. Run `prebuild.sh` (this does require Flatpak to be installed to be able to run pre-build tasks)
2. Run `build.sh` to build the app to `./dist`
3. Run `./dist/nl.mirthestam.aria` to run the app.

# Dependencies
- [.NET 10](https://dotnet.microsoft.com/en-us/)

## Design Philosophy

This software aims to follow the [GNOME Human Interface Guidelines](https://developer.gnome.org/hig/).

## Contributing
Anyone who wants to help is very welcome. If you want to get in contact feel free to chat with us via matrix ([#aria-player:matrix.org](https://matrix.to/#/#aria-player:matrix.org?via=matrix.org))
Please try to follow the [GNOME Code of Conduct](https://conduct.gnome.org).

## Thanks

These projects inspired Aria:

 - [Cantata](https://github.com/CDrummond/cantata) (Craig Drummond)
 - [Euphonica](https://github.com/htkhiem/euphonica) (Huỳnh Thiện Khiêm)
 - [LMS-Material](https://github.com/CDrummond/lms-material)  (Craig Drummond)
 - [Plattenalbum](https://github.com/SoongNoonien/plattenalbum) (Martin Wagner)
 - [Stylophone](https://github.com/Difegue/Stylophone) (Difegue)

This project uses the following open source libraries:

 - [MpcNET](https://github.com/glucaci/MpcNET) (Gabriel Lucaci)
 - [Gir.Core](https://gircore.github.io/) (Marcel Tiede)

Images:
 - App icon: [Vynil icons created by Freepik - Flaticon](https://www.flaticon.com/free-icons/vynil).

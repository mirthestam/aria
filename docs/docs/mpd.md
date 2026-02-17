# MPD

[Music Player Daemon](https://www.musicpd.org/) (MPD) is a flexible, powerful, server-side application for playing music. Through plugins and libraries it can play a variety of sound files while being controlled by its network protocol.

MPD runs in the background and manages your music library. You control it using clients like Aria or the `mpc` command.

## Setting up
These are instructions to set up a simple but capable MPD instance that is compatible with Aria.

MPD looks complex, but this minimal setup only takes a few steps, after which you can mostly forget about it and enjoy Aria.

MPD has a lot of advanced functions that are out of scope for this documentation. For that, please read the [MPD User's Manual](https://mpd.readthedocs.io/en/latest/user.html).

I recommend to install MPD using your distribution's package manager.

```bash
# Arch Linux
sudo pacman -S mpd mpc

# Debian / Ubuntu
sudo apt install mpd mpc
```

We are going to setup MPD to run automatically as a daemon for your current user. We'll be configuring MPD to work with the `Music` folder in your home folder. 

First, we will create the required folders to store MPD's configuration.
These folders follow the Linux XDG standard:

 - `~/.config/mpd` → configuration
 - `~/.local/share/mpd` → database, playlists, and state files

```bash
mkdir -p ~/.config/mpd
mkdir -p ~/.local/share/mpd/playlists
```

Next, we will save the MPD configuration file. Most distributions provide an default example configuration file. For the sake of simplicity, we'll be using our minimal config here.

Using this configuration, MPD will automatically detect the audio device to use. If you need to configure a specific output, you'll need to add the relevant portion as documented in the example file mentioned above. 

Save this file to `~/.config/mpd/mpd.conf`

```conf
music_directory     "~/Music"
playlist_directory  "~/.local/share/mpd/playlists"
db_file             "~/.local/share/mpd/database"
sticker_file        "~/.local/share/mpd/sticker.sql"

auto_update         "yes"
```

Now we ask systemd to run MPD, and automatically start if after boot.

```
systemctl --user daemon-reload
systemctl --user enable --now mpd
```

If your system running MPD is headless, run this command. It allows MPD to keep running even when you are not logged into a graphical session. Without this, MPD stops after logout.

```bash
loginctl enable-linger $USER
```


Test your server by running the `status` command using `mpc`.  

```bash
mpc status
```

If should show something remotely similar to:
```
volume: n/a   repeat: off   random: off   single: off   consume: off
```

Now, trigger the initial indexing of your music library using the `mpc` tool.
The first scan may take several minutes if you have a large library.
```bash
mpc update
```

## Connecting to MPD
Aria is able to automatically detect MPD instances on your network. For this to work,
the machine running MPD as well as the machine running Aria need to have an Zero-configuration networking implementation active.

Automatic discovery requires mDNS (Avahi) both on the machine running MPD as well as the machine you intend to use Aria on.

After starting MPD, it will automatically show your new MPD server as a suggested connection.

If not, you can use the `Add Player` button to manually provide information how to connect to your MPD instance.  For the example configuration above, you'll only need to verify the IP address and provide a name for your connection.

## Troubleshooting

### MPD won't start
Take a look at the log using `journalctl --user -u mpd -e`

# CarbonIntensityWallpaper
Creates a simple PNG based on 24 hours GB grid carbon intensity. Live data from here: https://carbon-intensity.github.io/api-definitions/#carbon-intensity-api-v2-0-0

Only works on Win32 platform as it uses DllImport and specific Windows messages to change the wallpaper. Could be easily adapted for others with preprocessors controlling compile.

Couple of hard-coded values currently:

- image size is 1920x1080, doesn't read screen size
- output file is c:\temp\intensity.png

Note this console app just creates and sets the wallpaper once duing execution. Use Windows Task Scheduler to run every 30mins.

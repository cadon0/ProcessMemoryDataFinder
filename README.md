# GifToTheBeatDataProvider

For use with [gif-to-the-beat](https://github.com/cadon0/gif-to-the-beat)

[Download releases here](https://github.com/cadon0/ProcessMemoryDataFinder/releases)

Reads the memory of the game [osu!](https://osu.ppy.sh/) as it runs. This will not work with [osu!lazer](https://github.com/ppy/osu)

The following information will be published (as JSON) to a websocket on `127.0.0.1:7270/GifToTheBeatOsuDataFeed`:

- status
- mapTime
- isoTime
- bpmMultiplier
- relativeOsuFilePath

The first argument to the program can be used to change port number

## License

This software is licensed under GNU GPLv3. You can find the full text of the license [here](https://github.com/Piotrekol/ProcessMemoryDataFinder/blob/master/LICENSE).

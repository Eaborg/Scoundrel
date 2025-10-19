# Intro
This is my first proper project - creating an implementation of the "Scoundrel" card game using the Monogame framework in C#, with some additional QOL features.  
As it's my first project, it's architechture, readability and implementation are likely questionable. If you have any suggestions to fix it or any of the code, please don't hesitate to let me know.  
This github repository was made a ways into the development. Thus, the initial commit is a bit beefy, and this repo doesn't have all of the development history.

For more info on Scoundrel, visit http://stfj.net/art/2011/Scoundrel.pdf  
The current art asset pack used is the "(Pixel) Poker cards" asset pack by IvoryRed, which can be found at https://ivoryred.itch.io/pixel-poker-cards

# Project info
## Features/systems
- [x] Gameplay
- [x] Tweening system
- [x] State machine
- [x] UI Layout engine
- [x] Replay file saving/loading
- [ ] Replay viewer
- [ ] Save file loading
- [ ] Leaderboard
- [ ] UI

## Basic monogame info
The MonoGame C# framework handles common needs in games, such as graphics, time, etc.  
It is not the same as something like a complete game engine, as it leaves more up to the dev.

- The Game1 class stores all the information handled by the main game loop.
- Initialize() is called first, followed by LoadContent(). These set up the program.
- Update() followed by Draw() are then called in a loop until the program ends.
- Update() is responsible for updating the game state
- Draw() is responsible for drawing the screen each frame.

## Basic file info
- Game1.cs handles the main update loop and glue logic.
- Game1 Utilities.cs stores the helper functions specific to to the Game1 class.
- Game Objects.cs stores the objects (classes, structs, enums, etc) which are used by the gameplay.
- General Utilities.cs stores helper functions potentially usable outside of this project.
- UI.cs handles the UI layout engine
- Replays.cs handles the logic for the replay feature
- Program.cs simply holds the main() function, which creates an instance of the Game1 class and runs it.
- All of the code is stored in the "Code" folder. It cleans up the file layout, but it does cause the namespaces to behave a bit wierdly.

That's all the information you need to understand the basic layout of the project.  
The rest should be ascertainable from the other comments scattered throughout the code, as well as the code itself. Though the commenting may be excessive at times.

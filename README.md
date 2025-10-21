# Intro
This is my first proper project - creating an implementation of the "Scoundrel" card game using the Monogame framework in C#, with some additional QOL features.  
While I tried my best to code smartly, as it's my first project it's architechture, readability and implementation are likely slightly questionable. If you have any suggestions to fix or improve any of these, please don't hesitate to let me know.  
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
- [ ] Full UI
- [ ] Tutorial

## Known issues
These are some issues with the code I know of, however they are all currently too much of a pain to change and/or too minor to affect the final program much

- The deck doesn't dissapear, even when there's no cards left
- At the end of a game, fleeing from a room can cause the same cards to be put away and drawn at the same time, causing the same cards to exist in two different places at once (visually, not in memory)
- The initialization of things is all over the place
- The way the cards are stored and passed around in memory is clunky. Decoupling the data of the cards, the logical position of the cards on the board, and the visual position of the cards onscreen would result in cleaner code and less possible bugs. Taking a more data-oriented approach may also be beneficial.
- The visuals of the program bug out while the window is being resized
- Parts of the program may have performance inefficiencies (though the game runs smoothly, so that doesn't seem to be a big issue yet)
- It can sometimes be hard to predict which file something will be in in the code

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

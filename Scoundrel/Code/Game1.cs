

/// BASIC DOCUMENTATION:
/// 
/// This program is made Using the MonoGame C# framework for handling common needs in games, such as graphics, time, etc.
/// It is not the same as something like a complete game engine, as it leaves more up to the dev.
/// 
/// The Game1 class stores all the information handled by the main game loop.
/// Initialize() is called first, followed by LoadContent(). These set up the program.
/// Update() followed by Draw() are then called in a loop until the program ends.
/// Update() is responsible for updating the game state
/// Draw() is responsible for drawing the screen each frame.
/// This is all part of the MonoGame framework
/// 
/// This file (Game1.cs) handles the main update loop and glue logic.
/// Game1 Utilities.cs stores the helper functions specific to to the Game1 class.
/// Game Objects.cs stores the objects (classes, structs & enums) which are used by the gameplay.
/// General Utilities.cs stores helper functions potentially usable outside of this project.
/// UI.cs handles the UI layout engine
/// Replays.cs handles the logic for the replay feature
/// Program.cs simply holds the main() function which creates an instance of the game and runs it.
/// 
/// That's all the information you need to understand the basic layout of the project.
/// The rest can be ascertained from the other comments scattered throughout the code, and the code itself.
/// 
/// For more info on the card game used (Scoundrel) visit http://stfj.net/art/2011/Scoundrel.pdf


/// Extra notes/grievances:
/// 
/// The initialization for variables/attributes is kinda all over the place.
/// 
/// It may be in:
/// - it's declaration
/// - Game1's constructor
/// - Initialize()
/// - LoadContent()
/// 
/// I honestly have no idea where I'm supposed to put alot of the initialization.
/// The C# static analyzer wants me to have values for everything once it leaves the constructor,
/// but MonoGame, by default, puts some of it's initialization (such as _spriteBatch)
/// in Initialize() or LoadContent(). Meanwhile it's sometimes just more convenient
/// to just put the value of a attribute right next to where it's defined rather than
/// the main constructor or Initialize() if it's meant to be a constant set at compile time.
/// Though unlike Zig or Rust, there's no way to do any calculation at compile time,
/// so I end up having to put a constants actual initialization somewhere else anyway
/// (such as for cardOrigin) since you can't evaluate expressions unless it's inside
/// the constructor or a method. It's even more annoying when initializing objects with
/// graphics, since graphics are supposed to be loaded in LoadContent(), but LoadContent()
/// is called after Initialize(), so I end up having to initialize some of the variables
/// in yet another place. That is, after loading the graphics inside LoadContent().
///
/// Oh well, if it works it works.
/// 

/* note: the scaling for the cards, while doesn't have to be applied manually
 *       every time the position or size changes anymore, still has to be applied
 *       on a per-sprite basis in the constructor. It may be better to create a new set of draw
 *       commands which automatically scale the coordinates, thus achieving an almost
 *       camera-zoom like effect globally. Even better, a system based approach where objects
 *       are represented by handles rather than an object themself would allow me to easily
 *       change the way things are rendered system-wide. The separating of animation from
 *       managed by each sprite to managed by an external system may also be beneficial.
 */

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Markup;
using static System.Math;

// this allows me to use the helper functions in the utilities file as regular functions
using static Scoundrel.Code.Utilities;
using Scoundrel.Code;

#nullable enable // this compiler flag allows variables storing classes to be able to be nullable

namespace Scoundrel
{
    public partial class Game1 : Game
    {
        // auto generated. Used to handle graphics output
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // sets how big the cards should be relative to their texture
        private const float cardSF = 3f;
        // relevant data for deciding the layout of the window
        private Point windowSize = new(900, 900);
        private Vector2 gameSize;
        // this is the point which the cards are positioned relative to onscreen.
        // it is calculated based on the window size, making sure that the game
        // stays in the middle of the screen
        internal Vector2 cardOrigin;

        // this is the randomization which is used by the whole project.
        // using the same randomization will ensure that predictability is minimized
        private Random globalRandom = new();

        // texture atlases / graphics
        internal Texture2D cardAtlas;
        private Texture2D deckAtlas;
        private Texture2D UITexture;
        private const float disappearSpeed = 0.2f;
        internal const float moveSpeed = 6f;

        // fonts
        private SpriteFont Proto_30;
        private SpriteFont Proto_20;

        // relevant variables for the game state
        GameState currentGameState = new();

        // relevant variables for handling the gameplay
        private Sprite deckSprite;
        internal List<Card> deck;
        internal CardSprite?[] room = new CardSprite?[4];
        private CardSprite? weaponCard = null;
        private Vector2 weaponPosition;
        private CardSprite? durabilityCard;
        private bool playerIsUsingWeapon;
        private int health;
        private bool usedHealthPotionPreviously;
        private bool playerIsAbleToRun;
        private List<CardSprite> garbage; // this is where cards are stored when their removal is animated
        private int score;

        // input tracking
        private KeyboardState lastKeyState;
        private KeyboardState keyState = Keyboard.GetState();
        private MouseState lastMouseState;
        private MouseState mouseState = Mouse.GetState();
        // array containing the number keys for logic purposes
        private readonly Keys[] numKeys = [Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9];

        // UI
        private UINode mainMenuUI;
        private UINode pauseUI;
        private UINode hudUI;
        private UINode[] UIs;
        private UIText healthText;
        private Button[] mainMenuButtons;

        // lotsa static analysis warnings here. None of them stop the game from working though
        public Game1()
        {
            // MonoGame generated code.
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            // setting the window size
            _graphics.PreferredBackBufferWidth = windowSize.X;
            _graphics.PreferredBackBufferHeight = windowSize.Y;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnWindowResize;
        }

        void OnWindowResize(object? sender, EventArgs e)
        {
            windowSize = new Point(Window.ClientBounds.Width, Window.ClientBounds.Height);
            cardOrigin = windowSize.ToVector2() / (2 * cardSF) - gameSize / 2;

            foreach (CardSprite? sprite in room) if (sprite!=null) sprite.origin = cardOrigin;
            foreach (CardSprite sprite in garbage) sprite.origin = cardOrigin;
            if (weaponCard != null) weaponCard.origin = cardOrigin;
            if (durabilityCard != null) durabilityCard.origin = cardOrigin;
            deckSprite.origin = cardOrigin;

            foreach (UINode UI in UIs)
            {
                UI.body.Size = windowSize;
                UI.layout();
            }
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here


            // calculate where everything should be positioned.
            gameSize = new Vector2(
                (48*5 + 3*10 + 20),
                (64*2 + 1*10)
            );
            cardOrigin = windowSize.ToVector2() / (2 * cardSF) - gameSize / 2;
            weaponPosition = new Vector2(
                48 + 30,
                64 + 10
            );

            // set up the variables for the game state
            currentGameState = GameState.Menu_Main;

            // set up the variables used in the game
            initializeNewGame();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            // These are the spritesheets and textures used by the game
            cardAtlas = Content.Load<Texture2D>("cardAtlas");
            deckAtlas = Content.Load<Texture2D>("deckAtlas");
            UITexture = Content.Load<Texture2D>("UI9Patch");
            Proto_30 = Content.Load<SpriteFont>("0xProto_30");
            Proto_20 = Content.Load<SpriteFont>("0xProto_20");

            // Initializing the deck you see onscreen
            deckSprite =
            new Sprite(
                cardOrigin,
                new Vector2(0,-9),
                deckAtlas
            ) {
                scaleFactor = cardSF,
                size = new Vector2(48, 73),
                textureSource = new Rectangle(48,71,49,73)
            };

            // Initializing the room
            DrawCardsFromDeck();

            // create templates for UI
            UI9Patch template9Patch = new(UITexture)
            {
                widthFit = FitType.Fit,
                heightFit = FitType.Fit,
                xAlignment = Alignment.Centred,
                innerMargin = 6 * (int)cardSF,
                childGap = 2 * (int)cardSF,
                textureMargin = 4,
                textureScale = (int)cardSF,
            };
            UINode templateWindowNode = new()
            {
                body = new Rectangle(Point.Zero, windowSize),
                xAlignment = Alignment.Centred,
                yAlignment = Alignment.Centred,
            };
            UIText templateProto_20 = new(Proto_20, "") { color = new Color(49, 49, 54) };
            UIText templateProto_30 = new(Proto_30, "") { color = new Color(49, 49, 54) };

            // main menu UI
            Button PlayButton =
            new(
                new UI9Patch(template9Patch) {
                    widthFit = FitType.Fixed,
                    body = new Rectangle(0, 0, 150, 0)
                }
                .Add( new UIText(templateProto_20).setText("Play") )
            ) {
                action = ()=>{
                    currentGameState = GameState.Gameplay_Active;
                    initializeNewGame();
                }
            };
            mainMenuUI = new UINode(templateWindowNode)
            .Add(
                new UI9Patch(template9Patch)
                .Add(
                    new UIText(templateProto_30).setText("Main menu"),
                    PlayButton.UIelement
                )
            );
            mainMenuUI.layout();
            mainMenuButtons = [PlayButton];

            // pause menu UI
            pauseUI = 
            new UINode(templateWindowNode) {
                color = new Color(Color.Black,0.3f),
            }.Add(
                new UI9Patch(template9Patch)
                .Add(
                    new UIText(Proto_30, "Game paused") { color = new Color(49, 49, 54) },
                    new UIText(Proto_20, "Press p to unpause") { color = new Color(49, 49, 54) }
                )
            );
            pauseUI.layout();

            // heads up display UI
            healthText = new UIText(templateProto_30).setText("Health: 20");
            hudUI = new UINode {
                body = new Rectangle(Point.Zero, windowSize),
                xAlignment = Alignment.Centred,
                innerMargin = 10*(int)cardSF,
            }.Add(
                new UI9Patch(template9Patch)
                .Add(healthText)
            );
            hudUI.layout();

            UIs = [mainMenuUI, pauseUI, hudUI];

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Close the program if the player inputs a close command
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            lastKeyState = keyState;
            lastMouseState = mouseState;
            keyState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            bool leftClickWasReleased = lastMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released;

            // The update-loop side of the state machine
            switch (currentGameState)
            {
                case GameState.Menu_Main:
                    foreach (Button button in mainMenuButtons)
                    {
                        if (button.UIelement.body.Contains(mouseState.Position))
                        {
                            if (mouseState.LeftButton == ButtonState.Pressed)
                                button.UIelement.color = Color.LightGray;

                            if (leftClickWasReleased)
                                button.action();
                        }
                        else button.UIelement.color = Color.White;
                    }
                break;


                case GameState.Gameplay_Active:
                    UpdateGameplay(gameTime);
                break;


                case GameState.Gameplay_Paused:
                    if (Keys.P.isFallingEdge(lastKeyState, keyState))
                        currentGameState = GameState.Gameplay_Active;
                break;


                case GameState.Gameplay_Ended:
                    // calculate deltatime
                    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    // update all tweens
                    foreach (CardSprite? card in room) card?.UpdateTweens(deltaTime);
                    foreach (CardSprite card in garbage) card.UpdateTweens(deltaTime);
                    weaponCard?.UpdateTweens(deltaTime);
                    durabilityCard?.UpdateTweens(deltaTime);
                    // collect the garbage
                    collectGarbage(garbage);
                break;
            }   
            
            base.Update(gameTime);
        }
        internal enum GameState
        {
            Menu_Main,
            Menu_LoadGame,
            Menu_LoadReplay,
            Menu_Leaderboard,
            Gameplay_Active,
            Gameplay_Paused,
            Gameplay_Ended,
            Replay,
        }
        protected void UpdateGameplay(GameTime gameTime)
        {
            // calculate relevant constants
            bool leftClickWasReleased = lastMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released;
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // if the pause button was pressed, pause the game
            if (Keys.P.isFallingEdge(lastKeyState, keyState))
                currentGameState = GameState.Gameplay_Paused;

            int selectedCardIndex = -1;
            // first figure out if any of the number keys have been pressed.
            // this is equivalent to clicking a card
            for(int i = 1; i<=4; i++)
            {
                if (room[i-1]!=null) if (numKeys[i].isFallingEdge(lastKeyState, keyState))
                {
                    selectedCardIndex = i-1; break;
                }
            }

            // the left shift key is equivalent to selecting/deselecting the weapon. This is represented here
            if (Keys.LeftShift.isFallingEdge(lastKeyState, keyState))
            {
                if (weaponCard != null)
                {
                    playerIsUsingWeapon = !playerIsUsingWeapon;
                    animateWeaponSize(1.1f, 0.1f);
                }
            }

            // then handle any other clicks that may have happened
            if (leftClickWasReleased)
            {
                // handle clicks on the room cards first
                if (selectedCardIndex == -1) // check if a card has already been selected before checking for clicks
                for (int i = 0; i < 4; i++) // loop through the cards in the current room
                if (room[i] != null) // check if there is a card at the position
                if (room[i]!.asRectangle.Contains(mouseState.Position)) // check if it has been clicked
                {
                    selectedCardIndex = i;
                    break;
                }

                // handle clicks on the weapon card
                if(weaponCard != null) if (weaponCard.asRectangle.Contains(mouseState.Position))
                {
                    playerIsUsingWeapon = !playerIsUsingWeapon;
                    animateWeaponSize(1.1f, 0.1f);
                }

                // handle clicks on the deck
                if (deckSprite.asRectangle.Contains(mouseState.Position) && playerIsAbleToRun)
                {
                    // put cards back into deck
                    int cardCount = 0;
                    for(int i = 0; i<room.Length; i++)
                    {
                        if (room[i] != null)
                        {
                            // put the card's data back into the deck
                            int index = globalRandom.Next(0, cardCount);
                            deck.Insert(index, room[i]!.card);

                            // animate the card going back into the deck
                            CardSprite clone = room[i]!.Clone();
                            room[i] = null;
                            clone.tweenX = new ExponentialOutTween(0, moveSpeed);
                            garbage.Add(clone);
                        }
                    }

                    DrawCardsFromDeck();
                    playerIsAbleToRun = false;
                    usedHealthPotionPreviously = false;
                }
            }

            // logic for what happens if you click a card
            if (selectedCardIndex != -1)
            {
                // temporary variable to clean up the code a bit
                CardSprite selectedCard = room[selectedCardIndex]!;

                // handle the card based on the card's suit
                switch (selectedCard.card.suit)
                {

                    case Suit.Hearts: // health potion

                        // if at the end the player has 20 health, the score is calculated from this
                        score = health + selectedCard.card.value;
                        // apply health regeneration
                        if (!usedHealthPotionPreviously) health = Min(20, score);

                        // animate the card disappearing
                        animateDisappearing(selectedCard, disappearSpeed);
                        // move the card to the garbage
                        garbage.Add(selectedCard.Clone());
                        room[selectedCardIndex] = null;

                        usedHealthPotionPreviously = true;
                        playerIsAbleToRun = false;
                        
                    break;

                    case Suit.Diamonds: // weapon
                        if (weaponCard != null)
                        {
                            // animate the current weapon disappearing
                            animateDisappearing(weaponCard, disappearSpeed);
                            garbage.Add(weaponCard.Clone());
                        }
                        if (durabilityCard != null)
                        {
                            // animate the durability card disappearing
                            animateDisappearing(durabilityCard, disappearSpeed);
                            garbage.Add(durabilityCard.Clone());
                            durabilityCard = null;
                        }

                        // animate the weapon card moving into the hand
                        selectedCard.tweenX = new ExponentialOutTween(weaponPosition.X, moveSpeed);
                        selectedCard.tweenY = new ExponentialOutTween(weaponPosition.Y, moveSpeed);
                        weaponCard = selectedCard.Clone();
                        room[selectedCardIndex] = null;
                        // set the weapon to not used to make the size accurate
                        playerIsUsingWeapon = false;

                        score = health;
                        playerIsAbleToRun = false;

                    break;

                    case Suit.Spades or Suit.Clubs: // enemy

                        // figure out if the player has enough durability
                        bool validDurability = true;
                        if (durabilityCard != null) validDurability = selectedCard.card.value < durabilityCard.card.value;

                        // if the player is using a weapon
                        if (weaponCard != null && playerIsUsingWeapon && validDurability)
                        {
                            // implement scoundrel's weapon rules
                            health -= Max(0, selectedCard.card.value - weaponCard!.card.value);

                            // if there's a durability card, animate it disappearing
                            if (durabilityCard != null)
                            {
                                var clone = durabilityCard.Clone();
                                animateDisappearing(clone, disappearSpeed);
                                garbage.Add(clone);
                            }

                            durabilityCard = selectedCard.Clone();
                            durabilityCard.tweenX = new ExponentialOutTween(weaponPosition.X + 40, moveSpeed);
                            durabilityCard.tweenY = new ExponentialOutTween(weaponPosition.Y + 8, moveSpeed);
                        }
                        // if the player isn't using a weapon#
                        else if (!playerIsUsingWeapon) health -= selectedCard.card.value;
                        
                        // if nothing happened, there's no need to do any thing else
                        else break; // perhaps there's a more readable way to do this

                        {// these curly brackets are just to scope the temporary "clone" variable
                            // animate the enemy card disappearing
                            var clone = selectedCard.Clone();
                            room[selectedCardIndex] = null;
                            animateDisappearing(clone, disappearSpeed);
                            garbage.Add(clone);
                        }
                        playerIsAbleToRun = false;
                        score = health;

                    break;
                }

                // death handling. this will have to be implemented once game states are added
                if(health <=0)
                {
                    // flatten the deck into the negative sum of all the enemie's values
                    score = deck.Aggregate(0, 
                        (int count, Card card) =>
                            /*if*/(card.suit == Suit.Clubs || card.suit == Suit.Spades) ?
                                count - card.value
                            /*else*/:
                                count
                    );

                    currentGameState = GameState.Gameplay_Ended;
                }


                // room progression. check if the next room should be drawn
                if(room.CountNonNulls()<=1 && deck.Count()>0)
                {
                    // draw a card for every empty space, unless there's no more cards in the deck
                    DrawCardsFromDeck();
                    usedHealthPotionPreviously = false;

                    // the player should only be able to run
                    // if there's any cards left in the deck in the first place,
                    // otherwise running is pointless since it would just rearrange the cards in your hand
                    if (deck.Count() > 0) playerIsAbleToRun = true;
                }

                // if there's no cards left in both the room and deck,
                // then the game has ended. Here's the code which handles this
                if(room.CountNonNulls()==0 && deck.Count() == 0)
                {
                    currentGameState = GameState.Gameplay_Ended;
                }

            }

            // update all tweens
            foreach (CardSprite? card in room)   card?.UpdateTweens(deltaTime);
            foreach (CardSprite card in garbage) card.UpdateTweens(deltaTime);
            weaponCard?.UpdateTweens(deltaTime);
            durabilityCard?.UpdateTweens(deltaTime);

            // garbage removal
            collectGarbage(garbage);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);

            // TODO: Add your drawing code here

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (//the current gameState is gameplay)
                currentGameState == GameState.Gameplay_Active
                //|| currentGameState == GameState.Gameplay_Paused
                || currentGameState == GameState.Gameplay_Ended
            ) {
                // draw the cards
                // TODO: make the deck disappear if the game is completed
                foreach (CardSprite card in garbage) card.Draw(_spriteBatch);// draw the cards in the garbage 
                deckSprite.Draw(_spriteBatch);// draw the deck
                foreach (CardSprite? card in room) card?.Draw(_spriteBatch);// draw the room cards
                weaponCard?.Draw(_spriteBatch);// draw the weapon card in hand
                durabilityCard?.Draw(_spriteBatch);// draw the durability card
            }

            if (currentGameState == GameState.Gameplay_Active)
            {
                // draw the UI
                healthText.setText($"Health: {health}");
                hudUI.layout();
                hudUI.Draw(_spriteBatch, GraphicsDevice);
            }
            else if (currentGameState == GameState.Gameplay_Paused)
            {
                // draw the UI
                pauseUI.Draw(_spriteBatch, GraphicsDevice);
            }
            else if (currentGameState == GameState.Gameplay_Ended)
            {
                // draw the UI
                _spriteBatch.DrawString(
                    Proto_30,
                    $"Score: {score}",
                    new Vector2(100, 100),
                    Color.White
                );
            }


            else if (currentGameState == GameState.Menu_Main)
            {
                mainMenuUI.Draw(_spriteBatch,GraphicsDevice);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
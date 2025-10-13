using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Scoundrel.Code
{
    // enum for representing the suit of a card
    internal enum Suit
    {
        Hearts,
        Diamonds,
        Spades,
        Clubs,
    }
    // this is the structure which stores the actual data of a card
    // it stores it's suit, value, position in the texture atlas, etc
    internal struct Card
    {
        public readonly int value;
        public readonly Suit suit;
        public readonly Rectangle textureSource;

        /// <summary>
        /// Helper function for finding the position in the texture atlas from a card's value and suit
        /// </summary>
        /// <param name="value"></param>
        /// <param name="suit"></param>
        /// <returns></returns>
        static Rectangle GetCardTextureSource(int value, Suit suit)
        {
            // the value of the ace is 14 in scoundrel, so it needs
            // a custom position (at the very left of the atlas)
            if (value == 14) return new Rectangle(0, (int)suit * 64, 48, 64);
            // the case for the rest of the cards
            else return new Rectangle((value - 1) * 48, (int)suit * 64, 48, 64);
        }

        /// <summary>
        /// The constructor for a card. Takes a value from 1-14 (with 14 being the ace) and a suit.
        /// The texture's source in the atlas is calculated automatically
        /// </summary>
        /// <param name="value"></param>
        /// <param name="suit"></param>
        public Card(int value, Suit suit)
        {
            this.value = value;
            this.suit = suit;
            textureSource = GetCardTextureSource(value, suit);
        }
    }

    /// <summary>
    /// base class for objects being rendered to the screen
    /// </summary>
    internal class Sprite
    {
        public Vector2 origin; // the coordinates the object is drawn relatively to
        public Vector2 location; // the location of the object relative to the origin
        public Vector2 size;
        public float scaleFactor = 1;
        public Texture2D texture;
        public Rectangle? textureSource;
        // monoGame renders textures with position and scale using rectangles.
        // this attribute simply converts the origin, location and size vectors
        // into 1 rectangle for this purpose.
        public Rectangle asRectangle
        {
            get => new Rectangle(
                (int)((origin.X+location.X)*scaleFactor),
                (int)((origin.Y+location.Y)*scaleFactor),
                (int)(size.X*scaleFactor),
                (int)(size.Y*scaleFactor)
            );
        }
        /// <summary>
        /// main constructor for a sprite. It only takes the neccesary values for it to be drawable.
        /// The other fields can be set manually using {field = value} syntax
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="location"></param>
        /// <param name="texture"></param>
        public Sprite(Vector2 origin, Vector2 location, Texture2D texture)
        {
            this.origin = origin;
            this.location = location;
            this.texture = texture;
            size = this.texture.Bounds.ToVector2();
            textureSource = this.texture.Bounds;
        }
        /// <summary>
        /// overridable function for drawing the sprite to the screen
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, asRectangle, textureSource, Color.White);
        }

    }

    /// <summary>
    /// The class which implements the functionality for sprite tweening.
    /// </summary>
    // The funcitonality for tweening was separated into it's own class
    // in case I used sprites for the UI, so that I could have untweenable sprites.
    // this ended up being useless since I made an entirely seperate class for the UI.
    internal class TweenableSprite: Sprite
    {
        // currently active tweens
        public Tween<float>? tweenX = null;
        public Tween<float>? tweenY = null;
        public Tween<float>? tweenSX = null;
        public Tween<float>? tweenSY = null;

        // note: I could add a setter which changes the targetvalue if it has a tween and the location otherwise
        // attributes for getting the target value if tweens are active, else the current value
        public float effectiveX { get => tweenX?.targetValue ?? location.X; }
        public float effectiveY { get => tweenY?.targetValue ?? location.Y; }
        public float effectiveSX { get => tweenSX?.targetValue ?? size.X; }
        public float effectiveSY { get => tweenSY?.targetValue ?? size.Y; }

        // attribute for returning all of the tweens
        public Tween<float>?[] tweens
        {   get => [
            tweenX,
            tweenY,
            tweenSX,
            tweenSY,
            ];
        }


        // the constructor simply passes all the values to the base sprite constructor
        public TweenableSprite(Vector2 origin, Vector2 location, Texture2D texture) : base(origin, location, texture) { }
        // update funciton for tweens
        public void UpdateTweens(float deltaTime)
        {
            if (tweenX != null)
            {
                bool endTween = tweenX.Update(ref location.X, deltaTime);
                if (endTween) tweenX = null;
            }
            if (tweenY != null)
            {
                bool endTween = tweenY.Update(ref location.Y, deltaTime);
                if (endTween) tweenY = null;
            }
            if (tweenSX != null)
            {
                bool endTween = tweenSX.Update(ref size.X, deltaTime);
                if (endTween) tweenSX = null;
            }
            if (tweenSY != null)
            {
                bool endTween = tweenSY.Update(ref size.Y, deltaTime);
                if (endTween) tweenSY = null;
            }
        }
    }
    // this is the class for representing a card on screen.
    // it stores a card, as well as the card's visual position and size
    internal class CardSprite : TweenableSprite
    {
        // the attributes
        public readonly Card card;
        public bool isFacedown; // currently unused

        // the constructor
        public CardSprite(Card card, Vector2 origin, Vector2 location, Texture2D texture, bool isFacedown = false) : base(origin, location, texture)
        {
            this.card = card;
            this.isFacedown = isFacedown;
        }

        // this function is just a wrapper to simplify the call for drawing a card to the screen
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (isFacedown) spriteBatch.Draw(texture, asRectangle, new Rectangle(0, 4*64, 48, 64), Color.White);
            else            spriteBatch.Draw(texture, asRectangle, card.textureSource, Color.White);
        }

        public CardSprite Clone()
        {
            return new CardSprite(card, origin, location, texture, isFacedown)
            {
                size = size,
                scaleFactor = scaleFactor,
                tweenX = tweenX,
                tweenY = tweenY,
                tweenSX = tweenSX,
                tweenSY = tweenSY,
            };
        }
    }

    /// <summary>
    /// Framework for implementing a tween between 2 values.
    /// The tween takes at least a starting value, an ending value, 
    /// and has an Update() function for incrementing the tween.
    /// </summary>
    /// <typeparam name="Number"></typeparam>
    internal abstract class Tween<Number>
    {
        public Number targetValue;

        /// <summary>
        /// The Update() method must take a reference to the value being updated,
        /// as well as the deltaTime between calculations.
        /// If the tween has ended, it must return true, otherwise false
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public abstract bool Update(ref Number currentValue, float deltaTime);
    }
    internal class LinearTween : Tween<float>
    {
        public float speed;
        private float length;
        public LinearTween(float startingValue, float targetValue, float speed)
        {
            this.targetValue = targetValue;
            this.speed = speed;
            length = targetValue - startingValue;
        }
        public override bool Update(ref float currentValue, float deltaTime)
        {
            currentValue += length * deltaTime / speed;
            bool completed = Math.Sign(targetValue-currentValue)!=Math.Sign(length);
            if (completed) currentValue = targetValue;
            return completed;
        }
    }
    internal class ExponentialOutTween : Tween<float>
    {
        public float speed;

        public ExponentialOutTween(float targetValue, float speed)
        {
            this.targetValue = targetValue;
            this.speed = speed;
        }
        public override bool Update(ref float currentValue, float deltaTime)
        {
            currentValue += (targetValue - currentValue) * Math.Min(speed * deltaTime,1);
            bool completed = Math.Abs(targetValue-currentValue)<0.1;
            if (completed) currentValue = targetValue;
            return completed;
        }
    }

}
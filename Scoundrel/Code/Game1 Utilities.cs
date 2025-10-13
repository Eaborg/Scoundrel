
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Markup;
using static System.Math;
using static Scoundrel.Code.Utilities;
using Scoundrel.Code;

namespace Scoundrel
{
    partial class Game1
    {
        protected void DrawCardsFromDeck()
        {
            for (int i = 0; i < room.Length; i++)
            if (room[i] == null)
            {
                if (deck.Count() == 0) break;

                room[i] = // create the card's sprite
                new CardSprite(
                    deck.Pop(),
                    cardOrigin,
                    new Vector2(0, -9),
                    cardAtlas
                ) {
                    // quite a few magic numbers here, but I don't want to overcomplicate it
                    size = new Vector2(48, 64),
                    scaleFactor = cardSF,
                    tweenX = new ExponentialOutTween( (48+10)*(i+1) + 10, moveSpeed),
                    tweenY = new ExponentialOutTween(0, moveSpeed)
                };
            }
        }
        protected void animateWeaponSize(float factor, float speed) 
        {
            if (playerIsUsingWeapon)
            {
                animateLinearScaling(weaponCard, new Vector2(48 * factor, 64 * factor), speed);
            }
            else
            {
                animateLinearScaling(weaponCard, new Vector2(48, 64), speed);
            }
        }
        protected void initializeNewGame()
        {
            // set up the variables used in the game
            playerIsUsingWeapon = false;
            health = 20;
            score = health;
            usedHealthPotionPreviously = false;
            garbage = new List<CardSprite>();
            playerIsAbleToRun = true;

            // initializing the deck. Scoundrel omits some of the cards, so it has to handle each suit individually
            deck = new List<Card>();
            for (int i = 2; i <= 14; i++) { deck.Add(new Card(i, Suit.Spades)); }
            for (int i = 2; i <= 14; i++) { deck.Add(new Card(i, Suit.Clubs)); }
            for (int i = 2; i <= 10; i++) { deck.Add(new Card(i, Suit.Hearts)); }
            for (int i = 2; i <= 10; i++) { deck.Add(new Card(i, Suit.Diamonds)); } /**/
            ShuffleList(ref deck, ref globalRandom);
        }
    }
}
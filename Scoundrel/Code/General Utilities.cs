using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
//using System.Numerics;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Scoundrel.Code
{
    public static class Utilities
    {
        public static void ShuffleList<T>(ref List<T> list, ref Random random)
        {
            List<T> nextList = new List<T>();
            while (list.Any())
            {
                int index = random.Next(0, list.Count);
                nextList.Add(list[index]);
                list.RemoveAt(index);
            }
            list = nextList;
        }

        public static int CountNonNulls<T>(this T?[] arr)
        {
            int count = 0;
            for(int i = 0; i<arr.Length; i++)
                if (arr[i] != null) count++;
            return count;
        }

        public static Vector2 ToVector2(this Rectangle rect)
        {
            return new Vector2(rect.Width, rect.Height);
        }

        public static Point ToPoint(this Vector2 vector)
        {
            return new Point((int)vector.X, (int)vector.Y);
        }

        public static T Pop<T>(this List<T> list)
        {
            T topItem = list[list.Count()-1];
            list.RemoveAt(list.Count()-1);
            return topItem;
        }

        public static bool isFallingEdge(this Keys key, KeyboardState previousState, KeyboardState currentState)
        {
            return previousState.IsKeyDown(key) && currentState.IsKeyUp(key);
        }

        internal static void animateDisappearing(TweenableSprite sprite, float timeToDisappear)
        {
            // this code is a bit redundant with the animateLinearScaling function, but there's no real reason to change it
            sprite.tweenX = new LinearTween(sprite.location.X, sprite.effectiveX + sprite.effectiveSX/2, timeToDisappear);
            sprite.tweenY = new LinearTween(sprite.location.Y, sprite.effectiveY + sprite.effectiveSY/2, timeToDisappear);
            sprite.tweenSX = new LinearTween(sprite.size.X, 0, timeToDisappear);
            sprite.tweenSY = new LinearTween(sprite.size.Y, 0, timeToDisappear);
        }
        internal static void animateLinearScaling(TweenableSprite sprite,Vector2 targetScale, float timeToScale)
        {// note: could be better if it didn't override existing tweens.
         //       It would probably just be best to add a proper origin that sprites scale around
            sprite.tweenX = new LinearTween(sprite.location.X, sprite.effectiveX - (targetScale.X - sprite.effectiveSX)/2, timeToScale);
            sprite.tweenY = new LinearTween(sprite.location.Y, sprite.effectiveY - (targetScale.Y - sprite.effectiveSY)/2, timeToScale);
            sprite.tweenSX = new LinearTween(sprite.size.X, targetScale.X, timeToScale);
            sprite.tweenSY = new LinearTween(sprite.size.Y, targetScale.Y, timeToScale);
        }

        internal static void collectGarbage(List<CardSprite> garbage)
        {
            // garbage removal
            for (int i = 0; i < garbage.Count(); i++)
            {
                // check if the card has any active tweens
                bool hasActiveTweens = false;
                foreach (var tween in garbage[i].tweens)
                    if (tween != null)
                        hasActiveTweens = true;

                // remove it if it has no active tweens
                if (!hasActiveTweens)
                {
                    garbage.RemoveAt(i);
                    i--;
                }
            }
        }
        internal static byte[] ToByteArray(this GameAction[] actions, int padding)
        {
            List<int> nibbleList = [];
            List<byte> byteList = [.. Enumerable.Repeat((byte)0, padding)];

            // calculate the nibbles which need to be written
            foreach (GameAction action in actions)
            {
                switch (action)
                {
                    // the "toggle weapon" action is represented by an empty nibble
                    case GameAction.ToggleWeapon toggleWeapon:
                        nibbleList.Add(0);
                    break;

                    // the "handle card" action is represented by a 1, followed by 2 bits for the room index
                    case GameAction.HandleCard handleCard:
                        nibbleList.Add(0b_0000_0100 | handleCard.roomIndex);
                    break;
                    
                    // the "flee room" action is represented by a 2 followed by 2 bits for the first room index
                    // followed by another nibble containing 2 more room indexes. The 4th index is implied
                    case GameAction.FleeRoom fleeRoom:
                        nibbleList.Add(0b_0000_1000 | fleeRoom.roomIndexes[0]);
                        nibbleList.Add((fleeRoom.roomIndexes[1]<<2) & fleeRoom.roomIndexes[2]);
                    break;

                    default: throw new ArgumentOutOfRangeException(nameof(action));
                }
            }

            // if the amount of nibbles is odd, it needs to be padded with a
            // null action for it to cleanly fit into the last byte
            if (nibbleList.Count() % 2 != 0) nibbleList.Add(0b_1100);
            // add the list of nibbles to the list of bytes
            for (int i = 0; i < nibbleList.Count(); i += 2)
            {
                byteList.Add((byte)((nibbleList[i] << 4) | nibbleList[i + 1]));
            }

            return [.. byteList];
        }
        internal static GameAction[] ToActionArray(this byte[] bytes, int padding)
        {
            ActionStream actionStream = new ActionStream(bytes, padding);
            List<GameAction> resultList = [];
            while (actionStream.hasNibblesLeft)
            {
                GameAction? action = actionStream.NextAction();
                if (action != null) resultList.Add(action);
            }
            return resultList.ToArray();
        }

        // helper function for directly serializing a double to a little endian byte array.
        public static byte[] ToByteArray_LE(this double value)
        {
            // convert the double into a byte array
            byte[] bytes = BitConverter.GetBytes(value);

            // if the system is big-endian, the bytes must be reversed
            // to be stored in little-endian format.
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return bytes;
        }
        // helper function for deserializing a little endian byte array into a double.
        // might possibly fail if the representation the system which serialized the double
        // uses is different.
        public static double ToDouble_LE(this byte[] bytes)
        {
            // clone the array to avoid side-effects
            byte[] buffer = [.. bytes];

            // the byte array is stored as little-endian, so if the system
            // is big-endian, it must be converted to big-endian
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            return BitConverter.ToDouble(buffer, 0);
        }
    }
}
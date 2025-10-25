using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace Scoundrel.Code
{
    internal class GameAction
    {
        internal class ToggleWeapon : GameAction
        {
            public override string ToString()
            => $"ToggleWeapon";
        }
        internal class HandleCard(int roomIndex) : GameAction
        {
            public readonly int roomIndex = roomIndex;
            public override string ToString()
            => $"HandleCard({roomIndex})";
        }
        internal class FleeRoom(params int[] roomIndexes) : GameAction
        {
            public readonly int[] roomIndexes = roomIndexes;
            public override string ToString()
            => $"FleeRoom({String.Join(", ", roomIndexes)})";
        }
    }
    internal class ActionStream(byte[] bytes, int bytePointer)
    {
        private byte[] bytes = bytes;
        private int bytePointer = bytePointer;
        private bool isRightmostNibble = false; // false is the left nibble, true is the right nibble
        public bool hasNibblesLeft
        {
            get => (bytePointer<bytes.Length);
        }

        // used to consume and return the next nibble in the stream
        public byte nextNibble()
        {
            // shift the indexed byte to the correct half, then bit mask it to return only the relevant nibble
            byte result = (byte)(bytes[bytePointer] >> (isRightmostNibble ? 0:4) & 0b_1111);
            // if the half-byte pointer was on the right half of a byte,
            // then the byte pointer needs to be incremented
            if (isRightmostNibble) bytePointer++;
            // increment the half-byte pointer
            isRightmostNibble = !isRightmostNibble;

            return result;
        }
        // used only for the nextAction function.
        // represents the different types of actions a player can take
        private enum ActionType
        {
            ToggleWeapon,
            HandleCard,
            FleeRoom,
            NullAction
        }
        // used to consume and return the next action in the stream
        public GameAction? NextAction()
        {
            // get the nibble representing the action
            byte actionNibble = nextNibble();
            // get the opcode encoded in the nibble representing the type of action
            ActionType opcode = (ActionType)(actionNibble >> 2);
            // get the operand encoded in the nibble representing the argument of the action
            int operand = actionNibble & 0b_0011;

            // return the corresponding gameAction
            switch (opcode)
            {
                case ActionType.ToggleWeapon:
                    return new GameAction.ToggleWeapon();
                case ActionType.HandleCard:
                    return new GameAction.HandleCard(operand);
                // note: this would technically fail if there wasn't 4 cards in the room
                case ActionType.FleeRoom: {
                    // get the next nibble (represents the other arguments)
                    byte argsNibble = nextNibble();

                    // create the array used to represent the the cards are put away
                    int[] arguments = [
                        operand, // the operand of the original nibble is the first index
                        argsNibble>>2, // the left half of the arguments nibble is the second index
                        argsNibble&0b_0011, // the right half of the arguments nibble is the third index
                        -1 // placeholder value
                    ];
                    // calculate the last index as the index which hasn't been already used
                    arguments[3] = 6 - arguments[0..3].Sum();

                    return new GameAction.FleeRoom(arguments);
                }
                case ActionType.NullAction:
                    return null;

                // if the opcode is anything else then it's invalid
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
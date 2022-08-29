using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Chip8.src
{
    internal class Keyboard
    {
        private const byte NUM_KEYS = 16;
        private readonly bool[] keyPressed;
        private byte lastKeyPressed;

        public Dictionary<Key, byte> keyMapping = new Dictionary<Key, byte>
        {
            [Key.D1] = 1,
            [Key.D2] = 2,
            [Key.D3] = 3,
            [Key.D4] = 0xC,
            [Key.Q] = 4,
            [Key.W] = 5,
            [Key.E] = 6,
            [Key.R] = 0xD,
            [Key.A] = 7,
            [Key.S] = 8,
            [Key.D] = 9,
            [Key.F] = 0xE,
            [Key.Z] = 0xA,
            [Key.X] = 0,
            [Key.C] = 0xB,
            [Key.V] = 0xF,
        };

        public Keyboard(MainWindow window)
        {
            this.keyPressed = new bool[NUM_KEYS];
            window.KeyDown += HandleKeyDown;
            window.KeyUp += HandleKeyUp;
        }

        public void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (keyMapping.ContainsKey(e.Key))
            {
                keyPressed[keyMapping[e.Key]] = true;
                lastKeyPressed = keyMapping[e.Key];
            }
        }

        public void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (keyMapping.ContainsKey(e.Key))
            {
                keyPressed[keyMapping[e.Key]] = false;   
            }
        }

        public bool HasKeyBeenPressed()
        {
            return keyPressed.Any(key => key);
        }

        public byte GetLastKeyPressed()
        {
            return lastKeyPressed;
        }

        public bool IsKeyPressed(byte key)
        {
            return keyPressed[key];
        }
    }
}



using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Data.Common;

namespace Chip8.src
{
    internal class Display
    {
        public const int DISPLAY_WIDTH = 64;
        public const int DISPLAY_HEIGHT = 32;
        public const int DISPLAY_REFRESH_RATE = 60;

        public bool[,] display = new bool[DISPLAY_HEIGHT, DISPLAY_WIDTH];
        public int scale = 8;
        public WriteableBitmap writeableImg;
        public int imgWidth;
        public int imgHeight;

        public Display()
        {
            this.imgWidth = DISPLAY_WIDTH * scale;
            this.imgHeight = DISPLAY_HEIGHT * scale;
            this.writeableImg = new WriteableBitmap(imgWidth, imgHeight, 96, 96, PixelFormats.Bgr32, null);
        }

        public bool drawSprite(byte x, byte y, byte[] sprite)
        {
            bool hasBitFlipped = false;

            for (int i = 0; i < sprite.Length; i++)
            {
                string bits = Convert.ToString(sprite[i], 2).PadLeft(8, '0');
                for (int j = 0; j < bits.Length; j++)
                {
                    int row = (i + y) % DISPLAY_HEIGHT;
                    int col = (j + x) % DISPLAY_WIDTH;
                    bool bit = bits[j] == '1';
                    bool initialVal = display[row, col];
                    display[row, col] ^= bit;
                    if (initialVal && !display[row, col])
                        hasBitFlipped = true;
                }
            }

            return hasBitFlipped;
        }

        public void render()
        {
            int[,] currentDisplay = new int[DISPLAY_HEIGHT, DISPLAY_WIDTH];
            for (int i = 0; i < DISPLAY_HEIGHT; i++) for (int j = 0; j < DISPLAY_WIDTH; j++) currentDisplay[i, j] = getColour(display[i, j]);

            this.writeableImg.Lock();
            for (int row = 0; row < imgHeight; row++)
            {
                for (int column = 0; column < imgWidth; column++)
                {
                    IntPtr backbuffer = writeableImg.BackBuffer;
                    backbuffer += row * writeableImg.BackBufferStride;
                    backbuffer += column * 4;
                    System.Runtime.InteropServices.Marshal.WriteInt32(backbuffer, currentDisplay[row / scale, column / scale]);
                }
            }

            writeableImg.AddDirtyRect(new Int32Rect(0, 0, imgWidth, imgHeight));
            writeableImg.Unlock();
        }

        private int getColour(bool colour)
        {
            byte hex = (byte)(colour ? 255 : 0);
            return int.Parse(Color.FromRgb(hex, hex, hex).ToString().Trim('#'), System.Globalization.NumberStyles.HexNumber);
        }

        public void clear()
        {
            display = new bool[DISPLAY_HEIGHT, DISPLAY_WIDTH];
        }
    }
}

using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Chip8.src
{
    internal class Chip8
    {
        public const int MEMORY_LOCATIONS = 4096;
        public const int NUM_REGISTERS = 16;
        public const int STACK_SIZE = 16;
        public const int DISPLAY_WIDTH = 64;
        public const int DISPLAY_HEIGHT = 32;
        public const int REFRESH_RATE = 60;
        public const int FONT_HEIGHT = 5;
        public const int PROGRAM_MEMORY_POS = 512;

        public ushort pc;
        public ushort iReg;
        public byte delayTimerReg;
        public byte soundTimerReg;
        public byte sp;
        
        public byte[] stack = new byte[STACK_SIZE];
        public byte[] registers = new byte[NUM_REGISTERS];
        public byte[] memory = new byte[MEMORY_LOCATIONS];

        public static readonly byte[,] fonts = new byte[,] {
            { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, // 0
            { 0x20, 0x60, 0x20, 0x20, 0x70 }, // 1
            { 0xF0, 0x10, 0xF0, 0x80, 0xF0 }, // 2
            { 0xF0, 0x10, 0xF0, 0x10, 0xF0 }, // 3
            { 0x90, 0x90, 0xF0, 0x10, 0x10 }, // 4
            { 0xF0, 0x80, 0xF0, 0x10, 0xF0 }, // 5
            { 0xF0, 0x80, 0xF0, 0x90, 0xF0 }, // 6
            { 0xF0, 0x10, 0x20, 0x40, 0x40 }, // 7
            { 0xF0, 0x90, 0xF0, 0x90, 0xF0 }, // 8
            { 0xF0, 0x90, 0xF0, 0x10, 0xF0 }, // 9
            { 0xF0, 0x90, 0xF0, 0x90, 0x90 }, // A
            { 0xE0, 0x90, 0xE0, 0x90, 0xE0 }, // B
            { 0xF0, 0x80, 0x80, 0x80, 0xF0 }, // C
            { 0xE0, 0x90, 0x90, 0x90, 0xE0 }, // D
            { 0xF0, 0x80, 0xF0, 0x80, 0xF0 }, // E
            { 0xF0, 0x80, 0xF0, 0x80, 0x80 }, // F
        };

        public Display display;
        public Image image;

        public Chip8(Image image)
        {
            initialiseFonts();
            loadROM();
            this.image = image;
            this.pc = PROGRAM_MEMORY_POS;
            this.display = new Display();
        }

        public void loop()
        {
            //for (int i = 0; i < 256; i++)
            ushort instruction = getInstruction(memory[pc], memory[pc + 1]);
            executeInstruction(instruction);
        }

        public void initialiseFonts()
        {
            for (int i = 0; i < fonts.GetLength(0); i++)
            {
                for (int j = 0; j < FONT_HEIGHT; j++)
                {
                    memory[i * FONT_HEIGHT + j] = fonts[i, j];
                }
            }
        }

        public void loadROM()
        {
            byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\IBM Logo.ch8");
            Debug.WriteLine($"File byte count: {fileBytes.Length}");
            for (int i = 0; i < fileBytes.Length / 2; i++)
            {
                ushort instruction = getInstruction(fileBytes[i * 2], fileBytes[(i * 2) + 1]);
                Debug.WriteLine($"{i}: 0x{instruction:X4}");
            }

            for (int i = 0; i < fileBytes.Length; i++) memory[PROGRAM_MEMORY_POS + i] = fileBytes[i];
        }

        public void jump(ushort instruction)
        {
            ushort address = (ushort)(instruction & 0x0FFF);
            pc = address;
        }

        public void setRegister(ushort instruction)
        {
            int registerId = (instruction & 0x0F00) >> 8;
            byte value = (byte)(instruction & 0x00FF);
            registers[registerId] = value;
        }

        public void addToRegister(ushort instruction)
        {
            int registerId = (instruction & 0x0F00) >> 8;
            byte value = (byte)(instruction & 0x00FF);
            registers[registerId] += value;
        }

        public void setIRegister(ushort instruction)
        {
            ushort value = (ushort)(instruction & 0x0FFF);
            iReg = value;
        }

        public void drawSprite(ushort instruction)
        {
            int registerId1 = (instruction & 0x0F00) >> 8;
            int registerId2 = (instruction & 0x00F0) >> 4;
            int height = instruction & 0x000F;
            byte[] sprite = new byte[height];
            for (int i = 0; i < height; i++)
            {
                sprite[i] = memory[iReg + i];
            }
            bool hasBitFlipped = display.drawSprite(registers[registerId1], registers[registerId2], sprite);
            registers[registers.Length - 1] = (byte)(hasBitFlipped ? 1 : 0);
        }
        
        public void printDisplay()
        {
            for (int i = 0; i < this.display.display.GetLength(0); i++)
            {
                for (int j = 0; j < this.display.display.GetLength(1); j++)
                {
                    Debug.Write(this.display.display[i, j] ? 1 : 0);
                }
                Debug.WriteLine("");
            }
        }

        public void executeInstruction(ushort instruction)
        {
            byte firstHexDigit = (byte)(instruction >> 12);

            switch (firstHexDigit)
            {
                case 0x0:
                    if (instruction == 0x00E0)
                    {
                        display.clear();
                        incrementPc();
                        break;
                    }
                    break;
                case 0x1:
                    jump(instruction);
                    break;
                case 0x6:
                    setRegister(instruction);
                    incrementPc();
                    break;
                case 0x7:
                    addToRegister(instruction);
                    incrementPc();
                    break;
                case 0xA:
                    setIRegister(instruction);
                    incrementPc();
                    break;
                case 0xD:
                    drawSprite(instruction);
                    incrementPc();
                    break;
                default:
                    Debug.WriteLine($"Instruction ${instruction:X4} not implemented.");
                    break;
            }
        }

        private void incrementPc()
        {
            this.pc += 2;
        }

        public ushort getInstruction(byte b1, byte b2)
        {
            return (ushort)((b1 << 8) | b2);
        }

        public void render()
        {
                this.display.render();         
        }
    }
}

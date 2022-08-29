using System.IO;
using System.Diagnostics;
using System;
using System.Timers;
using System.Net;

namespace Chip8.src
{
    internal class Chip8
    {
        private const int MEMORY_LOCATIONS = 4096;
        private const int NUM_REGISTERS = 16;
        private const int STACK_SIZE = 16;
        private const int FONT_HEIGHT = 5;
        private const int PROGRAM_MEMORY_POS = 512;
        private const int TIMER_TICK_RATE = 60;

        private ushort pc;
        private ushort iReg;
        private byte delayTimerReg;
        private byte soundTimerReg;
        private Timer timer;
        private byte sp;

        private ushort[] stack = new ushort[STACK_SIZE];
        private byte[] registers = new byte[NUM_REGISTERS];
        private byte[] memory = new byte[MEMORY_LOCATIONS];

        private static readonly byte[,] fonts = new byte[,] {
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

        private Display display;
        private Random rng;
        private Keyboard keyboard;

        public Chip8(Display display, Keyboard keyboard)
        {
            LoadFonts();
            LoadROM();

            this.display = display;
            this.keyboard = keyboard;
            pc = PROGRAM_MEMORY_POS;
            delayTimerReg = 0;
            soundTimerReg = 0;
            rng = new Random();
            StartTimers();
        }


        private void StartTimers()
        {
            timer = new Timer(1000.0 / TIMER_TICK_RATE);
            timer.Elapsed += new ElapsedEventHandler((object? source, ElapsedEventArgs e) =>
            {
                if (delayTimerReg > 0) delayTimerReg--;
                if (soundTimerReg > 0)
                {
                    Console.Beep();
                    soundTimerReg--;
                }
            });
            timer.Start();
        }

        private void LoadFonts()
        {
            for (int i = 0; i < fonts.GetLength(0); i++)
            {
                for (int j = 0; j < FONT_HEIGHT; j++)
                {
                    memory[i * FONT_HEIGHT + j] = fonts[i, j];
                }
            }
        }

        private void LoadROM()
        {
            //byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\IBM Logo.ch8");
            //byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\test_opcode.ch8");
            //byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\bc_test.ch8");
            //byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\Pong (alt).ch8");
            //byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\Tetris.ch8");
            byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\Space Invaders.ch8");
            //byte[] fileBytes = File.ReadAllBytes("C:\\Users\\Chris\\OneDrive\\Code\\Chip8\\Chip8\\roms\\chip8-test-suite.ch8");

            for (int i = 0; i < fileBytes.Length; i++) memory[PROGRAM_MEMORY_POS + i] = fileBytes[i];
        }

        // 8NNN
        private void HandleRegisterArithmeticOps(byte regId1, byte regId2, byte hex4)
        {
            switch (hex4)
            {
                case 0:
                    registers[regId1] = registers[regId2];
                    break;
                case 1:
                    registers[regId1] |= registers[regId2];
                    break;
                case 2:
                    registers[regId1] &= registers[regId2];
                    break;
                case 3:
                    registers[regId1] ^= registers[regId2];
                    break;
                case 4:
                    registers[regId1] += registers[regId2];
                    registers[0xf] = (byte)(registers[regId1] < registers[regId2] ? 1 : 0);
                    break;
                case 5:
                    byte isR1LargerThanR2 = (byte)(registers[regId1] > registers[regId2] ? 1 : 0);
                    registers[regId1] -= registers[regId2];
                    registers[0xf] = isR1LargerThanR2;
                    break;
                case 6:
                    byte lsb = (byte)(registers[regId1] & 0x1);
                    registers[regId1] >>= 1;
                    registers[0xf] = lsb;
                    break;
                case 7:
                    byte isR1SmallerThanR2 = (byte)(registers[regId1] < registers[regId2] ? 1 : 0);
                    registers[regId1] = (byte)(registers[regId2] - registers[regId1]);
                    registers[0xf] = isR1SmallerThanR2;
                    break;
                case 0xE:
                    byte msb = (byte)(registers[regId1] >> 7);
                    registers[regId1] <<= 1;
                    registers[0xf] = msb;
                    break;
                default:
                    throw new InvalidOperationException($"Instruction ${0x8000 | (regId1 << 8) | (regId2 << 4) | hex4:X4} not implemented.");
            }
        }

        // DXYN
        private void DrawSprite(byte regId1, byte regId2, byte height)
        {
            byte[] sprite = new byte[height];
            for (int i = 0; i < height; i++)
                sprite[i] = memory[iReg + i];
            bool hasBitFlipped = display.drawSprite(registers[regId1], registers[regId2], sprite);
            registers[0xF] = (byte)(hasBitFlipped ? 1 : 0);
        }

        // FXNN
        private void HandleInstructionF(byte regId, byte last2Bytes)
        {
            switch (last2Bytes)
            {
                case 0x7:
                    registers[regId] = delayTimerReg;
                    break;
                case 0xA:
                    if (this.keyboard.HasKeyBeenPressed())
                        registers[regId] = this.keyboard.GetLastKeyPressed();
                    else
                        pc -= 2;
                    break;
                case 0x15:
                    delayTimerReg = registers[regId];
                    break;
                case 0x18:
                    soundTimerReg = registers[regId];
                    break;
                case 0x1E:
                    iReg += registers[regId];
                    break;
                case 0x29:
                    iReg = (ushort)(registers[regId] * FONT_HEIGHT);
                    break;
                case 0x33:
                    memory[iReg] = (byte)(registers[regId] / 100);
                    memory[iReg + 1] = (byte)(registers[regId] / 10 % 10);
                    memory[iReg + 2] = (byte)(registers[regId] % 10);
                    break;
                case 0x55:
                    for (int i = 0; i <= regId; i++)
                        memory[iReg + i] = registers[i];
                    break;
                case 0x65:
                    for (int i = 0; i <= regId; i++)
                        registers[i] = memory[iReg + i];
                    break;
                default:
                    throw new InvalidOperationException($"Instruction ${0xF000 | (regId << 8) | last2Bytes:X4} not implemented.");
            }
        }

        private void ExecuteInstruction(ushort instruction)
        {
            byte hex1 = (byte)(instruction >> 12);
            byte hex2 = (byte)((instruction & 0x0F00) >> 8);
            byte hex3 = (byte)((instruction & 0x00F0) >> 4);
            byte hex4 = (byte)(instruction & 0x000F);

            byte last2Bytes = (byte)(instruction & 0x00FF);
            ushort last3Bytes = (ushort)(instruction & 0x0FFF);

            switch (hex1)
            {
                case 0x0:
                    if (instruction == 0x00E0)
                        display.clear();
                    else if (instruction == 0x00EE)
                        pc = stack[sp--];
                    break;
                case 0x1: // 1NNN
                    pc = last3Bytes;
                    break;
                case 0x2: // 2NNN
                    stack[++sp] = pc;
                    pc = last3Bytes;
                    break;
                case 0x3: // 3XNN
                    if (registers[hex2] == last2Bytes) IncrementPc();
                    break;
                case 0x4: // 4XNN
                    if (registers[hex2] != last2Bytes) IncrementPc();
                    break;
                case 0x5: // 5XY0
                    if (registers[hex2] == registers[hex3]) IncrementPc();
                    break;
                case 0x6: // 6XNN
                    registers[hex2] = last2Bytes;
                    break;
                case 0x7: // 7XNN
                    registers[hex2] += last2Bytes;
                    break;
                case 0x8: // 8NNN
                    HandleRegisterArithmeticOps(hex2, hex3, hex4);
                    break;
                case 0x9: // 9XY0
                    if (registers[hex2] != registers[hex3]) IncrementPc();
                    break;
                case 0xA: // ANNN
                    iReg = last3Bytes;
                    break;
                case 0xB: // BNNN
                    pc = (ushort)(last3Bytes + registers[hex2]);
                    break;
                case 0xC: // CXNN
                    registers[hex2] = (byte)(rng.Next(0, 256) & last2Bytes);
                    break;
                case 0xD: // DXYN
                    DrawSprite(hex2, hex3, hex4);
                    break;
                case 0xE: // EX9E or EXA1
                    bool hasKeyBeenPressed = this.keyboard.IsKeyPressed(registers[hex2]);
                    if ((last2Bytes == 0x9E && hasKeyBeenPressed) || (last2Bytes == 0xA1 && !hasKeyBeenPressed))
                        IncrementPc();
                    break;
                case 0xF: // FXNN
                    HandleInstructionF(hex2, last2Bytes);
                    break;
                default:
                    throw new InvalidOperationException($"Instruction ${instruction:X4} not implemented.");
            }
        }
        private void IncrementPc()
        {
            this.pc += 2;
        }

        private ushort GetInstruction(byte b1, byte b2)
        {
            return (ushort)((b1 << 8) | b2);
        }

        public void Loop()
        {
            ushort instruction = GetInstruction(memory[pc], memory[pc + 1]);
            IncrementPc();
            ExecuteInstruction(instruction);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16_emu
{
    class dcpu
    {
        ushort[] RAM = new ushort[0x10000];//should be 16 bits of 16bit words
        ushort A, B, C = new ushort();
        ushort X, Y, Z = new ushort();
        ushort I, J = new ushort();
        ushort PC, EX, IA = new ushort();
        ushort SP = 0xffff;//start of the reverse stack

        static ushort[] encode(string instruction)
        {
            //here I have to parse strings. oh deary
            //remove unnecessary whitespace
            string temp = "";
            foreach (char c in instruction)
                if (temp.Length == 0)
                    temp += c;
                else if (temp[temp.Length - 1] == ' ' && char.IsWhiteSpace(c))
                    continue;
                else temp += c;
            instruction = temp;
            instruction = instruction.ToLower();
            //get the mnemonic
            int spos = instruction.IndexOf(' ');
            string mnemonic = spos > 0 ? instruction.Substring(0, spos) : instruction;

            byte opcode;//becomes the lowest five bits of the output
            switch (mnemonic)
            {
                case "set": opcode = 0x01; break;
                case "add": opcode = 0x02; break;
                case "sub": opcode = 0x03; break;
                case "mul": opcode = 0x04; break;
                case "mli": opcode = 0x05; break;
                case "div": opcode = 0x06; break;
                case "dvi": opcode = 0x07; break;
                case "mod": opcode = 0x08; break;
                case "mdi": opcode = 0x09; break;
                case "and": opcode = 0x0a; break;
                case "bor": opcode = 0x0b; break;
                case "xor": opcode = 0x0c; break;
                case "shr": opcode = 0x0d; break;
                case "asr": opcode = 0x0e; break;
                case "shl": opcode = 0x0f; break;
                case "ifb": opcode = 0x10; break;
                case "ifc": opcode = 0x11; break;
                case "ife": opcode = 0x12; break;
                case "ifn": opcode = 0x13; break;
                case "ifg": opcode = 0x14; break;
                case "ifa": opcode = 0x15; break;
                case "ifl": opcode = 0x16; break;
                case "ifu": opcode = 0x17; break;
                case "adx": opcode = 0x1a; break;
                case "sbx": opcode = 0x1b; break;
                case "sti": opcode = 0x1e; break;
                case "std": opcode = 0x1f; break;
                case "jsr"://Special instructions have the lowest five bits unset
                case "int":
                case "iag":
                case "ias":
                case "rfi":
                case "iaq":
                case "hwn":
                case "hwq":
                case "hwi":
                    opcode = 0; break;
                default: throw new Exception("Invalid mnemonic");
            }
            byte a, b;
            ushort? aliteral=null, bliteral = null;
            //extract the arguments
            if (opcode != 0)
            {
                int commapos = instruction.IndexOf(',');
                string bstr = instruction.Substring(spos + 1, commapos - spos);
                //now safe to remove all spaces
                int space;
                while ((space = bstr.IndexOf(' ')) > 0)
                    bstr.Remove(space, 1);

                b = 0;
                if (bstr[0] == '[')
                {
                    //is a pointer
                    //check if it's a [register+next word]
                    b = 0x8;
                    if (bstr.Contains('+'))
                    {
                        if (char.IsNumber(bstr[1]))
                            // yoda instruction: [5+a]
                            // convert to normal style
                            bstr = '[' + bstr.Substring(bstr.IndexOf('+') + 1, bstr.IndexOf(']') - bstr.IndexOf('+')) + '+' + bstr.Substring(1, bstr.IndexOf('+') - 1);
                        b = 0x10;
                        bliteral = ushort.Parse(bstr.Substring(bstr.IndexOf('+') + 1, bstr.IndexOf(']') - bstr.IndexOf('+')));
                        bstr = bstr.Substring(1, bstr.IndexOf('+') - 1);//this and the next line leave only the register name/memory address
                    }
                    else bstr = bstr.Substring(1, bstr.Length - 2);
                }
                switch (bstr)
                {
                    case "a": b += 0; break;
                    case "b": b += 1; break;
                    case "c": b += 2; break;
                    case "x": b += 3; break;
                    case "y": b += 4; break;
                    case "z": b += 5; break;
                    case "i": b += 6; break;
                    case "j": b += 7; break;

                    case "push": b = 0x18; break;
                    case "sp":
                        if (b == 0x8)
                            b = 0x19;//[SP] see also: peek
                        if (b == 0x10)
                            b = 0x1a;//[SP+literal]
                        if (b == 0)
                            b = 0x1b;//SP
                        break;
                    case "peek": b = 0x19; break;
                    /***** not gonna implement "PICK n" ****/
                    case "pc":
                        if (b != 0)
                            throw new Exception("Pointers to PC not allowed");
                        b = 0x1c;
                        break;
                    case "ex":
                        if (b != 0)
                            throw new Exception("Pointers to EX not allowed");
                        b = 0x1d;
                        break;
                    default:
                        //this must be a literal
                        if (b == 0x8)
                        {
                            // [literal]
                            b = 0x1e;
                            bliteral = ushort.Parse(bstr);
                        }
                        else
                        {
                            b = 0x1f;
                            bliteral = ushort.Parse(bstr);
                        }
                        break;
                }
                //B should now be done.
                // we have b, and possibly bliteral.
                // Time for A
                string astr = instruction.Substring(commapos + 1).Trim();
                //remove all spaces
                while ((space = astr.IndexOf(' ')) > 0)
                    astr.Remove(space, 1);
                a = 0;
                if (astr[0] == '[')
                {
                    //is a pointer
                    //check if it's a [register+next word]
                    a = 0x8;
                    if (astr.Contains('+'))
                    {
                        if (char.IsNumber(astr[1]))
                            // yoda instruction: [5+a]
                            // convert to normal style
                            astr = '[' + astr.Substring(astr.IndexOf('+') + 1, astr.IndexOf(']') - astr.IndexOf('+')) + '+' + astr.Substring(1, astr.IndexOf('+') - 1);
                        a = 0x10;
                        aliteral = ushort.Parse(astr.Substring(astr.IndexOf('+') + 1, astr.IndexOf(']') - astr.IndexOf('+')));
                        astr = astr.Substring(1, astr.IndexOf('+') - 1);//this and the next line leave only the register name/memory address
                    }
                    else astr = astr.Substring(1, astr.Length - 2);
                }
                switch (astr)
                {
                    case "a": a += 0; break;
                    case "b": a += 1; break;
                    case "c": a += 2; break;
                    case "x": a += 3; break;
                    case "y": a += 4; break;
                    case "z": a += 5; break;
                    case "i": a += 6; break;
                    case "j": a += 7; break;

                    case "pop": a = 0x18; break;
                    case "sp":
                        if (a == 0x8)
                            a = 0x19;//[SP] see also: peek
                        if (a == 0x10)
                            a = 0x1a;//[SP+literal]
                        if (a == 0)
                            a = 0x1b;//SP
                        break;
                    case "peek": a = 0x19; break;
                    /***** not gonna implement "PICK n" ****/
                    case "pc":
                        if (a != 0)
                            throw new Exception("Pointers to PC not allowed");
                        a = 0x1c;
                        break;
                    case "ex":
                        if (a != 0)
                            throw new Exception("Pointers to EX not allowed");
                        a = 0x1d;
                        break;
                    default:
                        //this must be a literal
                        if (a == 0x8)
                        {
                            // [literal]
                            a = 0x1e;
                            aliteral = ushort.Parse(astr);
                        }
                        else if (ushort.Parse(astr) < 32)
                            a = (byte)(byte.Parse(astr) + 0x20);
                        else
                        {
                            a = 0x1f;
                            aliteral = ushort.Parse(astr);
                        }
                        break;
                }
                //A should be done.
                // now we have a,b, and [a|b] literals maybe

            }
            else
            {
                //TODO: handle special instructions
            }
            
            //construct the instruction code
            byte words=1;
            if (aliteral.HasValue)
                words++;
            if (bliteral.HasValue)
                words++;

            ushort[] output = new ushort[words];
            output[0] = opcode;
            output[0] |= (ushort)(b << 5);
            output[0] |= (ushort)(a << 10);
            if (aliteral.HasValue)
                output[1] = aliteral.Value;
            if (bliteral.HasValue)
                output[2] = bliteral.Value;

            return output;
        }
    }
}
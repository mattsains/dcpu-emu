using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU16_emu
{
    static class compile
    {
        /// <summary>
        /// This one resolves labels
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static ushort[] opcodeify(string[] instructions)
        {
            List<string> labels = new List<string>();
            string[] originstructions=new string[instructions.Length];

            for (int i = 0; i < instructions.Length; i++)
            {
                if (instructions[i].Substring(0, (instructions[i].IndexOf(';') > 0) ? instructions[i].IndexOf(';') : instructions[i].Length).Trim() == "")
                    continue;//a blank
                instructions[i] = instructions[i].Substring(0, instructions[i].Contains(';') ? instructions[i].IndexOf(';') : instructions[i].Length).Trim();
                originstructions[i]=instructions[i];
                if (instructions[i]!="" && instructions[i].Trim()[0] == ':')
                    labels.Add(instructions[i].Trim().Substring(1));
            }
            for (int i = 0; i < instructions.Length; i++)
                foreach (string label in labels)
                    while (instructions[i].Contains(label))
                        instructions[i]=instructions[i].Replace(label, "label");
            //instructions now contains the instructions with all labels renamed "label" and no comments
            Dictionary<string, ushort> labeladdr = new Dictionary<string, ushort>();

            ushort address = 0;
            ushort last = 0;
            for (int i=0; i<originstructions.Length;i++)
                if (originstructions[i]!=null)
                    if (originstructions[i].Trim()[0] == ':')
                        labeladdr.Add(originstructions[i].Trim().Substring(1), (ushort)(address));
                    else
                        address += last=(ushort)length(instructions[i]);
            // labeladdr should now hold the addresses of labels
            for (int i = 0; i < originstructions.Length; i++)
            if (originstructions[i]!=null)
                {
                    if (originstructions[i].Trim()[0] == ':')
                    {
                        originstructions[i]=originstructions[i].Replace(':', ';');//turn label definitions into comments
                        continue;
                    }
                    bool haslabel = false;
                    foreach (KeyValuePair<string, ushort> label in labeladdr)
                        while (originstructions[i].Contains(label.Key))
                        {
                            originstructions[i] = originstructions[i].Replace(label.Key, label.Value.ToString());
                            haslabel = true;
                        }
                    if (haslabel) originstructions[i] += 'l';
                }
            List<ushort> code = new List<ushort>();
            foreach (string instruction in originstructions)
                if (instruction != null)
                    if (instruction.Trim()[0] != ';')
                        if (instruction.EndsWith("l"))
                            code.AddRange(encode(instruction.Remove(instruction.Length - 1), false));
                        else
                            code.AddRange(encode(instruction));
            return code.ToArray();
        }
        /// <summary>
        /// Returns how many words long an instruction will be
        /// </summary>
        /// <param name="instruction">Expects every label to be called "label"</param>
        /// <returns></returns>
        public static int length(string instruction)
        {
            //an instruction will always have a space after the mnemonic
            if (instruction.Trim()[0]==':')
                return 0;
            else
            {
                bool haslabel = instruction.Contains("label");
                instruction = instruction.Replace("label", "128");
                return encode(instruction,!haslabel).Length;
            }
        }
        /// <summary>
        /// Returns instruction words for the mnemonic. Does not resolve labels.
        /// </summary>
        /// <param name="instruction">Mnemonic</param>
        /// <returns>an array of words</returns>
        public static ushort[] encode(string instruction,bool allowLiteralOptimization=true)
        {
            if (instruction.StartsWith(";")) return null;//a comment
            //here I have to parse strings. oh deary
            //remove unnecessary whitespace
            string temp = "";
            foreach (char c in instruction)
                if (temp.Length == 0)
                    temp += c;
                else if (temp[temp.Length - 1] == ' ' && char.IsWhiteSpace(c))
                    continue;
                else temp += c;
            if (temp.Length == 0) return null;//empty
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
            byte a = 0, b = 0;
            ushort? aliteral = null, bliteral = null;
            //extract the arguments
            if (opcode != 0)
            {
                int commapos = instruction.IndexOf(',');
                string bstr = instruction.Substring(spos + 1, commapos - spos - 1);
                //now safe to remove all spaces
                int space;
                while ((space = bstr.IndexOf(' ')) > 0)
                    bstr = bstr.Remove(space, 1);

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
                    astr = astr.Remove(space, 1);
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
                        else if (ushort.Parse(astr) < 32 && allowLiteralOptimization)
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
                //Handle special, single-argument instructions
                string astr = instruction.Substring(spos + 1).Trim();
                switch (mnemonic)
                {
                    case "jsr": b = 0x01; break;
                    case "int": b = 0x08; break;
                    case "iag": b = 0x09; break;
                    case "ias": b = 0x0a; break;
                    case "rfi": b = 0x0b; break;
                    case "iaq": b = 0x0c; break;
                    case "hwn": b = 0x10; break;
                    case "hwq": b = 0x11; break;
                }
                //figure out the argument
                int space;
                while ((space = astr.IndexOf(' ')) > 0)
                    astr = astr.Remove(space, 1);
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
                        aliteral = ushort.Parse(astr.Substring(astr.IndexOf('+') + 1, astr.IndexOf(']') - astr.IndexOf('+') - 1));
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
                        else if (ushort.Parse(astr) < 32 && allowLiteralOptimization)
                            a = (byte)(byte.Parse(astr) + 0x20);
                        else
                        {
                            a = 0x1f;
                            aliteral = ushort.Parse(astr);
                        }
                        break;
                }
            }

            //construct the instruction code
            byte words = 1;
            if (aliteral.HasValue)
                words++;
            if (bliteral.HasValue)
                words++;

            ushort[] output = new ushort[words];
            output[0] = opcode;
            output[0] |= (ushort)(b << 4);
            output[0] |= (ushort)(a << 10);
            if (bliteral.HasValue)
                output[--words] = bliteral.Value;
            if (aliteral.HasValue)
                output[--words] = aliteral.Value;

            return output;
        }
    }
}

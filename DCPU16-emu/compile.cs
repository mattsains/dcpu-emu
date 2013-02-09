using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace DCPU16_emu
{
    static class compile
    {
        public static ushort[] opcodeify(string instructions)
        {
            return opcodeify(instructions.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
        }
        public static ushort[] opcodeify(string[] instructions)
        {
            return opcodeify(new List<string>(instructions));
        }
        /// <summary>
        /// This one resolves labels
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public static ushort[] opcodeify(List<string> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                //remove comments
                if (instructions[i].Contains(';'))
                    instructions[i] = instructions[i].Remove(instructions[i].IndexOf(';'));
                instructions[i] = instructions[i].Trim().ToLower();
                //remove blank lines
                if (instructions[i] == "")
                {
                    instructions.RemoveAt(i);
                    i--;//I think?
                    continue;
                }
            }
            //we now have code with no comments, and no blank lines
            //get a list of labels
            Dictionary<string, ushort> labels = new Dictionary<string, ushort>();
            for (int i = 0; i < instructions.Count; i++)
                if (instructions[i][0] == ':')
                    labels.Add(instructions[i].Substring(1),0);
            //we now have a list of all the defined labels. time to make an array of them stripped
            List<string> labelstripped = new List<string>(instructions.ToArray());

            for (int i = 0; i < labelstripped.Count; i++)
            {
                if (labelstripped[i][0] == ':')
                {
                    //this is a label definition
                    labelstripped[i] = ";";
                    continue;
                }
                //this is an instruction that might use a label
                string opcode = labelstripped[i].Substring(0, labelstripped[i].IndexOf(' '));
                string[] args = labelstripped[i].Substring(labelstripped[i].IndexOf(' ') + 1).Split(',');
                for (int j = 0; j < args.Length; j++)
                {
                    //split the argument up into its components
                    List<string> parts = new List<string>();
                    string separators = "[]+";
                    parts.Add("");
                    //remove all spaces from arguments
                    string temp = "";
                    foreach (char c in args[j])
                        if (c != ' ')
                            temp += c;
                    args[j] = temp;
                    for (int k = 0; k < args[j].Length; k++)
                        if (separators.Contains(args[j][k]))
                        {
                            parts.Add(args[j][k].ToString());
                            parts.Add("");
                        }
                        else
                            parts[parts.Count - 1] += args[j][k];
                    //parts now has the argument in sections
                    args[j] = "";
                    foreach (string part in parts)
                        if (labels.ContainsKey(part))
                            args[j] += "label";//replace every label name with "label"
                        else
                            args[j] += part;
                }
                //reconstruct the instruction
                labelstripped[i] = opcode + " "+args[0];
                for (int j = 1; j < args.Length; j++)
                    labelstripped[i] += "," + args[j];
            }
            //labelstripped has now got ";" instead of label defs, (to keep the indices aligned) and all labels are called "label"
            int address=0;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i][0] == ':')
                    //dealing with a label
                    labels[instructions[i].Substring(1)] = (ushort)address;
                else address += length(labelstripped[i]);
            }
            //dictionary is now filled.
            //time to resolve labels.
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i][0] != ':')
                {
                    string opcode = instructions[i].Substring(0, instructions[i].IndexOf(' '));
                    string[] args = instructions[i].Substring(instructions[i].IndexOf(' ') + 1).Split(',');
                    bool haslabel=false;
                    for (int j = 0; j < args.Length; j++)
                    {
                        //split the argument up into its components
                        List<string> parts = new List<string>();
                        string separators = " []+";
                        parts.Add("");
                        for (int k = 0; k < args[j].Length; k++)
                            if (separators.Contains(args[j][k]))
                            {
                                parts.Add(args[j][k].ToString());
                                parts.Add("");
                            }
                            else
                                parts[parts.Count - 1] += args[j][k];
                        //parts now has the argument in sections
                        args[j] = "";
                        foreach (string part in parts)
                            if (labels.ContainsKey(part))
                            {
                                haslabel=true;
                                args[j] += labels[part];//replace every label name with "label"
                            }
                            else
                                args[j] += part;
                    }
                    //reconstruct the instruction
                    instructions[i] = opcode + " " + args[0];
                    for (int j = 1; j < args.Length; j++)
                        instructions[i] += "," + args[j];
                    if (haslabel)
                        instructions[i]+='l';//I postfix instructions containing labels with 'l' for later
                }
            }
            //labels are resolved. Now get rid of them
            for (int i = 0; i < instructions.Count; i++)
                if (instructions[i][0] == ':')
                {
                    instructions.RemoveAt(i);
                    i--;
                    continue;
                }
            //compile time
            List<ushort> output = new List<ushort>();
            foreach (string instruction in instructions)
                if (instruction[instruction.Length - 1] == 'l')
                    output.AddRange(encode(instruction.Substring(0, instruction.Length - 1), false));
                else output.AddRange(encode(instruction));
            return output.ToArray();
        }
        /// <summary>
        /// Returns how many words long an instruction will be
        /// </summary>
        /// <param name="instruction">Expects every label to be called "label"</param>
        /// <returns></returns>
        public static int length(string instruction)
        {
            //an instruction will always have a space after the mnemonic
            bool haslabel = instruction.Contains("label");
            instruction = instruction.Replace("label", "128");
            return encode(instruction,!haslabel).Length;
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
                            bstr = '[' + bstr.Substring(bstr.IndexOf('+') + 1, bstr.IndexOf(']') - bstr.IndexOf('+') - 1) + '+' + bstr.Substring(1, bstr.IndexOf('+') - 1) + ']';
                        b = 0x10;
                        string blitstr = bstr.Substring(bstr.IndexOf('+') + 1, bstr.IndexOf(']') - bstr.IndexOf('+') - 1);
                        if (blitstr.StartsWith("0x"))
                            bliteral = ushort.Parse(blitstr.Substring(2), NumberStyles.HexNumber);
                        else if (blitstr.StartsWith("0b"))
                            bliteral = Convert.ToUInt16(blitstr, 2);
                        else bliteral = ushort.Parse(blitstr);
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
                        if (bstr.StartsWith("0x"))
                            bstr = ushort.Parse(bstr.Substring(2),NumberStyles.HexNumber).ToString();
                        else if (bstr.StartsWith("0b"))
                            bstr=Convert.ToUInt16(bstr.Substring(2),2).ToString();

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
                            astr = '[' + astr.Substring(astr.IndexOf('+') + 1, astr.IndexOf(']') - astr.IndexOf('+')-1) + '+' + astr.Substring(1, astr.IndexOf('+') - 1)+']';
                        a = 0x10;
                        string alitstr = astr.Substring(astr.IndexOf('+') + 1, astr.IndexOf(']') - astr.IndexOf('+') - 1);
                        if (alitstr.StartsWith("0x"))
                            aliteral = ushort.Parse(alitstr.Substring(2), NumberStyles.HexNumber);
                        else if (alitstr.StartsWith("0b"))
                            aliteral=Convert.ToUInt16(alitstr,2);
                        else aliteral = ushort.Parse(alitstr);

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
                        if (astr.StartsWith("0x"))
                            astr = ushort.Parse(astr.Substring(2), NumberStyles.HexNumber).ToString();
                        else if (astr.StartsWith("0b"))
                            astr = Convert.ToUInt16(astr.Substring(2), 2).ToString();

                        if (a == 0x8)
                        {
                            // [literal]
                            a = 0x1e;
                            aliteral = ushort.Parse(astr);
                        }
                        else if (int.Parse(astr) < 32 && int.Parse(astr)>0 && allowLiteralOptimization)//TODO: allow negative literals. Signinggggg
                            a = (byte)(byte.Parse(astr) + 0x20);
                        else
                        {
                            a = 0x1f;
                            if (int.Parse(astr) < 0)
                                aliteral = (ushort)(65536 + int.Parse(astr));
                            else
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

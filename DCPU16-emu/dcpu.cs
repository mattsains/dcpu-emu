using System;
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

        public void load(string instructions)
        {
            this.load(instructions.Split('\n'));
        }
        public void load(string[] instructions)
        {
            PC = 0;//start loading code into RAM
            foreach (string instruction in instructions)
                foreach (ushort code in this.encode(instruction))
                    RAM[PC++] = code;
            PC = 0;//reset back to the start. Machine is now ready for execution.
        }
        public void tick()
        {
            //perform one instruction
            ushort instr = RAM[PC++];

            byte opcode = (byte)(instr & 0xF);
            ushort b = (ushort)((instr & 0x3F0) >> 4);
            ushort a = (ushort)((instr & 0xFC00) >> 10);

            if (opcode != 0)
            {
                //simple instruction
                if (a<0x08)
                {
                    //register reference
                    switch (a)
                    {
                        case 0: a=A;break;
                        case 0x1:a=B;break;
                        case 0x2:a=C;break;
                        case 0x3:a=X;break;
                        case 0x4:a=Y;break;
                        case 0x5:a=Z;break;
                        case 0x6:a=I;break;
                        case 0x7:a=J;break;
                    }
                } else if(a<0x10)
                {
                    switch (a)
                    {
                        case 0x8:a=RAM[A];break;
                        case 0x9:a=RAM[B];break;
                        case 0xA:a=RAM[C];break;
                        case 0xB:a=RAM[X];break;
                        case 0xC:a=RAM[Y];break;
                        case 0xD:a=RAM[Z];break;
                        case 0xE:a=RAM[I];break;
                        case 0xF:a=RAM[J];break;
                    }
                } else if (a<0x18)
                {
                    ushort offset=RAM[PC++];
                    switch (a)
                    {
                        case 0x10:a=RAM[A+offset];break;
                        case 0x11:a=RAM[B+offset];break;
                        case 0x12:a=RAM[C+offset];break;
                        case 0x13:a=RAM[X+offset];break;
                        case 0x14:a=RAM[Y+offset];break;
                        case 0x15:a=RAM[Z+offset];break;
                        case 0x16:a=RAM[I+offset];break;
                        case 0x17:a=RAM[J+offset];break;
                    }
                } else if (a>0x1f && a<0x40)
                {
                   a-=32;
                }else
                {
                    switch (a)
                    {
                        case 0x18:a=RAM[SP++];break;
                        case 0x19:a=RAM[SP];break;
                        case 0x1A:
                            ushort offset=RAM[PC++];
                            a=RAM[SP+offset];
                            break;
                        case 0x1B:a=SP;break;
                        case 0x1C:a=PC;break;
                        case 0x1D:a=EX;break;
                        case 0x1E:
                            ushort addr=RAM[PC++];
                            a=RAM[addr];
                            break;
                        case 0x1F:a=RAM[PC++];break;
                    }
                }
                //got A.
                string bregister="";//registers have to be a special case :(
                if (b<0x08)
                {
                    //register reference
                    
                    switch (b)
                    {
                        case 0x0:bregister="A";break;
                        case 0x1:bregister="B";break;
                        case 0x2:bregister="C";break;
                        case 0x3:bregister="X";break;
                        case 0x4:bregister="Y";break;
                        case 0x5:bregister="Z";break;
                        case 0x6:bregister="I";break;
                        case 0x7:bregister="J";break;
                    }
                    b = 0;
                } else if(b<0x10)
                {
                    switch (b)
                    {
                        case 0x8:b=A;break;
                        case 0x9:b=B;break;
                        case 0xA:b=C;break;
                        case 0xB:b=X;break;
                        case 0xC:b=Y;break;
                        case 0xD:b=Z;break;
                        case 0xE:b=I;break;
                        case 0xF:b=J;break;
                    }
                } else if (b<0x18)
                {
                    ushort offset=RAM[PC++];
                    switch (b)
                    {
                        case 0x10:b=(ushort)(A+offset);break;
                        case 0x11:b=(ushort)(B+offset);break;
                        case 0x12:b=(ushort)(C+offset);break;
                        case 0x13:b=(ushort)(X+offset);break;
                        case 0x14:b=(ushort)(Y+offset);break;
                        case 0x15:b=(ushort)(Z+offset);break;
                        case 0x16:b=(ushort)(I+offset);break;
                        case 0x17:b=(ushort)(J+offset);break;
                    }
                }else
                {
                    switch (b)
                    {
                        case 0x18:b=SP++;break;
                        case 0x19:b=SP;break;
                        case 0x1A:
                            ushort offset=RAM[PC++];
                            b=(ushort)(SP+offset);
                            break;
                        case 0x1B:bregister="SP";break;
                        case 0x1C:bregister="PC";break;
                        case 0x1D:bregister="EX";break;
                        case 0x1E:
                            ushort addr=RAM[PC++];
                            b=addr;
                            break;
                    }
                }
                switch (opcode)
                {
                    case 0x01://SET
                        switch (bregister)
                        {
                            case "A": A = a; break;
                            case "B": B = a; break;
                            case "C": C = a; break;
                            case "X": X = a; break;
                            case "Y": Y = a; break;
                            case "Z": Z = a; break;
                            case "I": I = a; break;
                            case "J": J = a; break;
                            case "SP": SP = a; break;
                            case "PC": PC = a; break;
                            case "EX": EX = a; break;
                            case "": RAM[b] = a; break;
                        } break;
                    case 0x02: //ADD
                        int result = 0;
                        switch (bregister)
                        {
                            case "A": result = a + A; break;
                            case "B": result = a + B; break;
                            case "C": result = a + C; break;
                            case "X": result = a + X; break;
                            case "Y": result = a + Y; break;
                            case "Z": result = a + Z; break;
                            case "I": result = a + I; break;
                            case "J": result = a + J; break;
                            case "SP": result = a + SP; break;
                            case "PC": result = a + PC; break;
                            case "EX": result = a + EX; break;
                            case "": result = a + RAM[b]; break;
                        }
                        EX = (ushort)((result > 65535) ? 0x1 : 0x0);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x03: //SUB
                        result = 0;
                        switch (bregister)
                        {
                            case "A": result = A - a; break;
                            case "B": result = B - a; break;
                            case "C": result = C - a; break;
                            case "X": result = X - a; break;
                            case "Y": result = Y - a; break;
                            case "Z": result = Z - a; break;
                            case "I": result = I - a; break;
                            case "J": result = J - a; break;
                            case "SP": result = SP - a; break;
                            case "PC": result = PC - a; break;
                            case "EX": result = EX - a; break;
                            case "": result = RAM[b] - a; break;
                        }
                        EX = (ushort)((result < 0) ? 0x1 : 0x0);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x04: //MUL
                        result = 0;
                        ushort btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A; result = A * a; break;
                            case "B": btemp = B; result = B * a; break;
                            case "C": btemp = C; result = C * a; break;
                            case "X": btemp = X; result = X * a; break;
                            case "Y": btemp = Y; result = Y * a; break;
                            case "Z": btemp = Z; result = Z * a; break;
                            case "I": btemp = I; result = I * a; break;
                            case "J": btemp = J; result = J * a; break;
                            case "SP": btemp = SP; result = SP * a; break;
                            case "PC": btemp = PC; result = PC * a; break;
                            case "EX": btemp = EX; result = EX * a; break;
                            case "": btemp = RAM[b]; result = RAM[b] * a; break;
                        }
                        EX = (ushort)(((btemp * a) >> 16) & 0xffff);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x05: //MLI
                        //TODO: signed multiplication
                    case 0x06: //DIV
                        result = 0;
                        btemp=0;
                        if (a != 0)
                        {
                            switch (bregister)
                            {
                                case "A": btemp = A; result = A / a; break;
                                case "B": btemp = B; result = B / a; break;
                                case "C": btemp = C; result = C / a; break;
                                case "X": btemp = X; result = X / a; break;
                                case "Y": btemp = Y; result = Y / a; break;
                                case "Z": btemp = Z; result = Z / a; break;
                                case "I": btemp = I; result = I / a; break;
                                case "J": btemp = J; result = J / a; break;
                                case "SP": btemp = SP; result = SP / a; break;
                                case "PC": btemp = PC; result = PC / a; break;
                                case "EX": btemp = EX; result = EX / a; break;
                                case "": btemp = RAM[b]; result = RAM[b] / a; break;
                            }
                            EX = (ushort)(((btemp << 16) / a) & 0xffff);
                        }
                        else
                        {
                            result = 0;
                            EX = 0;
                        }
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x07: //DVI
                        //TODO: signed division
                    case 0x08: //MOD
                        switch (bregister)
                        {
                            case "A": A %= a; break;
                            case "B": B %= a; break;
                            case "C": C %= a; break;
                            case "X": X %= a; break;
                            case "Y": Y %= a; break;
                            case "Z": Z %= a; break;
                            case "I": I %= a; break;
                            case "J": J %= a; break;
                            case "SP": SP %= a; break;
                            case "PC": PC %= a; break;
                            case "EX": EX %= a; break;
                            case "": RAM[b] %= a; break;
                        }break;
                    case 0x09: //MDI
                        //TODO: signed modulo
                    case 0x0a: //AND
                        switch (bregister)
                        {
                            case "A": A &= a; break;
                            case "B": B &= a; break;
                            case "C": C &= a; break;
                            case "X": X &= a; break;
                            case "Y": Y &= a; break;
                            case "Z": Z &= a; break;
                            case "I": I &= a; break;
                            case "J": J &= a; break;
                            case "SP": SP &= a; break;
                            case "PC": PC &= a; break;
                            case "EX": EX &= a; break;
                            case "": RAM[b] &= a; break;
                        }break;
                    case 0x0b: //BOR
                        switch (bregister)
                        {
                            case "A": A |= a; break;
                            case "B": B |= a; break;
                            case "C": C |= a; break;
                            case "X": X |= a; break;
                            case "Y": Y |= a; break;
                            case "Z": Z |= a; break;
                            case "I": I |= a; break;
                            case "J": J |= a; break;
                            case "SP": SP |= a; break;
                            case "PC": PC |= a; break;
                            case "EX": EX |= a; break;
                            case "": RAM[b] |= a; break;
                        }break;
                    case 0x0c: //XOR
                        switch (bregister)
                        {
                            case "A": A ^= a; break;
                            case "B": B ^= a; break;
                            case "C": C ^= a; break;
                            case "X": X ^= a; break;
                            case "Y": Y ^= a; break;
                            case "Z": Z ^= a; break;
                            case "I": I ^= a; break;
                            case "J": J ^= a; break;
                            case "SP": SP ^= a; break;
                            case "PC": PC ^= a; break;
                            case "EX": EX ^= a; break;
                            case "": RAM[b] ^= a; break;
                        }break;
                    case 0x0d: //SHR
                        // might be the wrong way round... endianness is hard to think about. Probably.
                        result = 0;
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A; result = A >> a; break;
                            case "B": btemp = B; result = B >> a; break;
                            case "C": btemp = C; result = C >> a; break;
                            case "X": btemp = X; result = X >> a; break;
                            case "Y": btemp = Y; result = Y >> a; break;
                            case "Z": btemp = Z; result = Z >> a; break;
                            case "I": btemp = I; result = I >> a; break;
                            case "J": btemp = J; result = J >> a; break;
                            case "SP": btemp = SP; result = SP >> a; break;
                            case "PC": btemp = PC; result = PC >> a; break;
                            case "EX": btemp = EX; result = EX >> a; break;
                            case "": btemp = RAM[b]; result = RAM[b] >> a; break;
                        }
                        EX = (ushort)(((b<<16)>>a)&0xffff);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x0e: //ASR
                        //TODO: signed SHR
                    case 0x0f: //SHL
                        result = 0;
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A; result = A << a; break;
                            case "B": btemp = B; result = B << a; break;
                            case "C": btemp = C; result = C << a; break;
                            case "X": btemp = X; result = X << a; break;
                            case "Y": btemp = Y; result = Y << a; break;
                            case "Z": btemp = Z; result = Z << a; break;
                            case "I": btemp = I; result = I << a; break;
                            case "J": btemp = J; result = J << a; break;
                            case "SP": btemp = SP; result = SP << a; break;
                            case "PC": btemp = PC; result = PC << a; break;
                            case "EX": btemp = EX; result = EX << a; break;
                            case "": btemp = RAM[b]; result = RAM[b] << a; break;
                        }
                        EX = (ushort)(((b << a) >> 16) & 0xffff);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x10: //IFB
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A;  break;
                            case "B": btemp = B;  break;
                            case "C": btemp = C;  break;
                            case "X": btemp = X;  break;
                            case "Y": btemp = Y;  break;
                            case "Z": btemp = Z;  break;
                            case "I": btemp = I;  break;
                            case "J": btemp = J;  break;
                            case "SP": btemp = SP; break;
                            case "PC": btemp = PC; break;
                            case "EX": btemp = EX; break;
                            case "": btemp = RAM[b]; break;
                        }
                        if ((btemp & a) == 0) PC++;
                        break;
                    case 0x11: //IFC
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A;  break;
                            case "B": btemp = B;  break;
                            case "C": btemp = C;  break;
                            case "X": btemp = X;  break;
                            case "Y": btemp = Y;  break;
                            case "Z": btemp = Z;  break;
                            case "I": btemp = I;  break;
                            case "J": btemp = J;  break;
                            case "SP": btemp = SP; break;
                            case "PC": btemp = PC; break;
                            case "EX": btemp = EX; break;
                            case "": btemp = RAM[b]; break;
                        }
                        if ((btemp & a) != 0) PC++;
                        break;
                    case 0x12: //IFE
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A;  break;
                            case "B": btemp = B;  break;
                            case "C": btemp = C;  break;
                            case "X": btemp = X;  break;
                            case "Y": btemp = Y;  break;
                            case "Z": btemp = Z;  break;
                            case "I": btemp = I;  break;
                            case "J": btemp = J;  break;
                            case "SP": btemp = SP; break;
                            case "PC": btemp = PC; break;
                            case "EX": btemp = EX; break;
                            case "": btemp = RAM[b]; break;
                        }
                        if (btemp != a) PC++;
                        break;
                    case 0x13: //IFN
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A;  break;
                            case "B": btemp = B;  break;
                            case "C": btemp = C;  break;
                            case "X": btemp = X;  break;
                            case "Y": btemp = Y;  break;
                            case "Z": btemp = Z;  break;
                            case "I": btemp = I;  break;
                            case "J": btemp = J;  break;
                            case "SP": btemp = SP; break;
                            case "PC": btemp = PC; break;
                            case "EX": btemp = EX; break;
                            case "": btemp = RAM[b]; break;
                        }
                        if (btemp==a) PC++;
                        break;
                    case 0x14: //IFG
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A;  break;
                            case "B": btemp = B;  break;
                            case "C": btemp = C;  break;
                            case "X": btemp = X;  break;
                            case "Y": btemp = Y;  break;
                            case "Z": btemp = Z;  break;
                            case "I": btemp = I;  break;
                            case "J": btemp = J;  break;
                            case "SP": btemp = SP; break;
                            case "PC": btemp = PC; break;
                            case "EX": btemp = EX; break;
                            case "": btemp = RAM[b]; break;
                        }
                        if (b<=a) PC++;
                        break;
                    case 0x15: //IFA
                        //TODO: signed IFG
                    case 0x16: //IFL
                        btemp=0;
                        switch (bregister)
                        {
                            case "A": btemp = A;  break;
                            case "B": btemp = B;  break;
                            case "C": btemp = C;  break;
                            case "X": btemp = X;  break;
                            case "Y": btemp = Y;  break;
                            case "Z": btemp = Z;  break;
                            case "I": btemp = I;  break;
                            case "J": btemp = J;  break;
                            case "SP": btemp = SP; break;
                            case "PC": btemp = PC; break;
                            case "EX": btemp = EX; break;
                            case "": btemp = RAM[b]; break;
                        }
                        if (b>=a) PC++;
                        break;
                    case 0x17: //IFU
                        //TODO: signed IFL
                    case 0x1a: //ADX
                        result = 0;
                        switch (bregister)
                        {
                            case "A": result = a + A + EX; break;
                            case "B": result = a + B + EX; break;
                            case "C": result = a + C + EX; break;
                            case "X": result = a + X + EX; break;
                            case "Y": result = a + Y + EX; break;
                            case "Z": result = a + Z + EX; break;
                            case "I": result = a + I + EX; break;
                            case "J": result = a + J + EX; break;
                            case "SP": result = a + SP + EX; break;
                            case "PC": result = a + PC + EX; break;
                            case "EX": result = a + EX + EX; break;
                            case "": result = a + RAM[b]+EX; break;
                        }
                        EX = (ushort)((result > 65535) ? 1 : 0);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x1b: //SBX
                        result = 0;
                        switch (bregister)
                        {
                            case "A": result = A - a+EX; break;
                            case "B": result = B - a + EX; break;
                            case "C": result = C - a + EX; break;
                            case "X": result = X - a + EX; break;
                            case "Y": result = Y - a + EX; break;
                            case "Z": result = Z - a + EX; break;
                            case "I": result = I - a + EX; break;
                            case "J": result = J - a + EX; break;
                            case "SP": result = SP - a + EX; break;
                            case "PC": result = PC - a + EX; break;
                            case "EX": result = EX - a + EX; break;
                            case "": result = RAM[b] - a + EX; break;
                        }
                        EX = (ushort)((result < 0) ? 1 : 0);
                        switch (bregister)
                        {
                            case "A": A = (ushort)result; break;
                            case "B": B = (ushort)result; break;
                            case "C": C = (ushort)result; break;
                            case "X": X = (ushort)result; break;
                            case "Y": Y = (ushort)result; break;
                            case "Z": Z = (ushort)result; break;
                            case "I": I = (ushort)result; break;
                            case "J": J = (ushort)result; break;
                            case "SP": SP = (ushort)result; break;
                            case "PC": PC = (ushort)result; break;
                            case "EX": EX = (ushort)result; break;
                            case "": RAM[b] = (ushort)result; break;
                        } break;
                    case 0x1e: //STI
                        switch (bregister)
                        {
                            case "A": A = a; break;
                            case "B": B = a; break;
                            case "C": C = a; break;
                            case "X": X = a; break;
                            case "Y": Y = a; break;
                            case "Z": Z = a; break;
                            case "I": I = a; break;
                            case "J": J = a; break;
                            case "SP": SP = a; break;
                            case "PC": PC = a; break;
                            case "EX": EX = a; break;
                            case "": RAM[b] = a; break;
                        } 
                        I++;
                        J++;
                        break;
                    case 0x1f: //STD
                        switch (bregister)
                        {
                            case "A": A = a; break;
                            case "B": B = a; break;
                            case "C": C = a; break;
                            case "X": X = a; break;
                            case "Y": Y = a; break;
                            case "Z": Z = a; break;
                            case "I": I = a; break;
                            case "J": J = a; break;
                            case "SP": SP = a; break;
                            case "PC": PC = a; break;
                            case "EX": EX = a; break;
                            case "": RAM[b] = a; break;
                        } 
                        I--;
                        J--;
                        break;
                }
            }
            else 
            { 
                //special opcodes
                switch (b)
                {
                    case 0x01:
                        RAM[SP--] = PC;
                        PC = a;
                        break;
                    //TODO: add hardware features
                }
            }
        }

        public ushort[] encode(string instruction)
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
            byte a=0, b=0;
            ushort? aliteral=null, bliteral = null;
            //extract the arguments
            if (opcode != 0)
            {
                int commapos = instruction.IndexOf(',');
                string bstr = instruction.Substring(spos + 1, commapos - spos-1);
                //now safe to remove all spaces
                int space;
                while ((space = bstr.IndexOf(' ')) > 0)
                    bstr=bstr.Remove(space, 1);

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
                    astr=astr.Remove(space, 1);
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
                    astr=astr.Remove(space, 1);
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
                        aliteral = ushort.Parse(astr.Substring(astr.IndexOf('+') + 1, astr.IndexOf(']') - astr.IndexOf('+')-1));
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
            }
            
            //construct the instruction code
            byte words=1;
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
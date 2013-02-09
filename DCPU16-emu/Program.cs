using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace DCPU16_emu
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //testing code
            string p = @"set pc,Start

:speed

:ticker
:randish

:delay
  set a,0
:delayloop
  ifg a,[speed]
  set pc,pop
  add a,1
  set pc,delayloop

:Start
jsr init
:MainLoop
  add [ticker],1
  jsr updatePlayer
  ife [dead],0
  jsr animate
  ife [dead],0
  jsr moveGhosts

  jsr delay
  jsr probeInput
  
  ife [pillsEaten],[pillsInMap]
  jsr completeLevel
set pc,mainloop

:completeLevel
  set b,0
:completeLevel_Loop
  jsr delay
  add b,1
  ifn b,10
  set pc,completeLevel_Loop
  jsr wipe
  jsr startNewLevel
  set pc,pop
  
:setDeathFrames
  set a,deathframes
  set [a],sprites
  set [a+1],death1
  set [a+2],death2
  set [a+3],death3
  set [a+4],death4
  set [a+5],death5
  set [a+6],death6
  set [a+7],death7

  set pc,pop

:deathFrames
  dat 0,1,2,3,4,5,6,7

:dead 
:deathAge

:init
  jsr setDeathFrames

  ; copy sprites to vram
  set a,sprites
  set b,0x8600
  set c,304
  jsr copy

  SET [0x9040],1  ; turn graphics mode on
  jsr startNewLevel
  set pc,pop

:animate
   add [currentFrame],1
   ifg [currentFrame],5
   set [currentFrame],0

   set a,[currentFrame]
   set b,[Direction]
   sub b,1
   mul b,6
   add a,b
   set [0x9051],[munchFrames+a]

   ;ifg [fear],0
   ;set pc,fraidyghosts

   ;set [0x9053],0x0368
   ;set [0x9055],0x0370
   ;set [0x9057],0x0378
   ;set [0x9059],0x0380

   set PC,pop
:fraidyghosts
   sub [fear],1
   set a,[blueghost]
   set b,[fear]
   and b,2
   ifg [fear],14  
   set b,0
   ife b,2
   set a,[whiteghost]
   set [0x9053],a
   set [0x9055],a
   set [0x9057],a
   set [0x9059],a
   set PC,pop


:fraidyghost
   sub [i+6],1
   set a,[blueghost]
   set b,[i+6]
   and b,2
   ifg [i+6],14  
   set b,0
   ife b,2
   set a,[whiteghost]
   set b,[i+4]
   set [b+1],a
   ife [i+6],0
   set [b+1],[i+5]
   set PC,pop

:fear

:currentFrame
:munchFrames

:updatePlayer
  ife [dead],1
  set pc,deathAction 
  add [playerX],[playerDx]
  add [playerY],[playerDy]
  ifb [playerX],0x8000
  add [playerX],81
  ifg [playerX],81
  sub [playerX],81
  
  set x,[playerX]
  set y,[playerY]
  mod x,3
  mod y,3
  bor x,y
  ife x,0
  jsr cellAlignedCheck
  set a,[playerX]
  add a,62
  set b,[playerY];
  add b,62;
  shl a,8
  bor b,a
  set [0x9050],b
  set pc,pop

:deathAction
  add [deathAge],1
  set a,[deathAge]
  sub a,10
  ifn O,0
  set pc,pop

  ;hide ghosts  
  set [0x9052],0
  set [0x9054],0
  set [0x9056],0
  set [0x9058],0

  shr a,1
  ifg a,7 
  set pc, reallyDead
  set [0x9051],0x0300
  set a,[deathFrames+a]
  set b,0x8600
  set c,16
  jsr copy
  set pc,pop

:reallyDead
  set [0x9050],0x0000
  ifg [deathage], 40
  jsr resetlevel
  set pc,pop

:startNewLevel
  set [pillsEaten],0
  jsr resetLevel
  
  set a,map
  set b,mapend
  set c,0x8000
  jsr copyimage
  
  
  set pc,pop
  
:resetlevel
  set [playerX],39
  set [playerY],69
  set [playerDX],0xffff
  set [playerDy],0

  set [direction],1
  set a,ghost1
  set [a],39
  set [a+1],38
  set [a+2],0
  set [a+3],0xffff
  set [a+6],0
  set [a+7],0

  set a,ghost2
  set [a],36
  set [a+1],42
  set [a+2],1
  set [a+3],0
  set [a+6],0
  set [a+7],0

  set a,ghost3
  set [a],45
  set [a+1],42
  set [a+2],0xffff
  set [a+3],0
  set [a+6],0
  set [a+7],0

  set a,ghost4
  set [a],39
  set [a+1],42
  set [a+2],0
  set [a+3],1
  set [a+6],0
  set [a+7],0
  
  set [dead],0
  set [deathAge],0
  set [fear],0
  set a,sprites
  set b,0x8600
  set c,16
  jsr copy
  set pc,pop


:cellAlignedCheck
  set push,x
  set push,y
  set push,z
  set push,i
  set push,j
  set [PreviousDirection],[Direction]
  ifn [lastDirectionKey],0
  set [Direction],[lastDirectionKey]
:goInDirection  
  set [playerDx],0
  set [playerDy],0

  ife [Direction],1
  set [playerDx],-1;

  ife [Direction],2
  set [playerDx],1;
  
  ife [Direction],3
  set [playerDy],-1;

  ife [Direction],4
  set [playerDy],1;

  set x,[playerX]
  set y,[playerY]
  div x,3
  div y,3  ; X,y is the cell where we are
  set a,y
  mul a,42
  add a,x
  add a,0x8000 ; a pointing to the current cell
  ife [a],0xa25d
  jsr eatPowerPill
  ife [a],0xa008
  add [pillsEaten],1
  set [a],0  ;eat the pill 
  set i,x
  set j,y
  add i,[playerDx]
  add j,[playerDy]  ; i,J is the cell where we are headed.
  set a,j
  mul a,42
  add a,i
  add a,0x8000 ; a pointing to next cell
  ife [a],0xa25d ; is it a powerpill
  set pc,powerpill
  ife [a],0xa008 ; is it a pill
  set pc,pill
  ife [a],0x0000 ; is it space
  set pc,canmove
  ifn  [direction],[previousdirection]
  set  pc,cantTurn
  set [playerDx],0
  set [playerDy],0
  set pc,cellAlignCheck_exit
:cantTurn
  set [direction],[previousdirection]
  set pc,goInDirection
:powerpill

:pill        
  
:canmove
  
:cellAlignCheck_exit
   set j,pop
   set i,pop
   set z,pop
   set y,pop
   set x,pop
   set pc,pop

:eatPowerPill
  add [pillsEaten],1
  set push,a
  set push,i

  set i,ghost1
  jsr flipGhost
  set i,ghost2
  jsr flipGhost
  set i,ghost3
  jsr flipGhost
  set i,ghost4
  jsr flipGhost

  set i,pop
  set a,pop
  set pc,pop

:flipGhost
  set [i+6],[powerPillStrength]
  set a,0
  sub a,[i+2]
  set [i+2],a
  set a,0
  sub a,[i+3]
  set [i+3],a   
   set pc,pop

:powerPillStrength
  dat 128
:playerX
  dat 39
:playerY
  dat 69

:playerDx
  dat 0xffff
:playerDy
  dat 0

:moveGhosts
  set a,ghost1
  jsr moveGhost
  set a,ghost2
  jsr moveGhost
  set a,ghost3
  jsr moveGhost
  set a,ghost4
  jsr moveGhost

  set pc,pop

:moveGhost
;a = ghost pointer
  set push,i
  set i,a
  set a,[i+4]
  ife [i+6],0
  set [a+1],[i+5]
  
  ifn [i+6],0
  jsr fraidyghost
  jsr touchCheck
  set a,[i+6]
  ifb a,1
  set pc,moveGhost_exit

  add [i],[i+2]
  add [i+1],[i+3]
 
  ifb [i],0x8000
  add [i],81
  ifg [i],81
  sub [i],81
 
  set x,[i]
  set y,[i+1]
  mod x,3
  mod y,3
  bor x,y
  ife x,0
  jsr cellAlignedGhostCheck

  set a,[i]
  add a,63
  set b,[i+1]
  add b,63
  shl a,8
  bor b,a
  set a,[i+4]
  set [a],b
:moveGhost_exit
  set i,pop
  set pc,pop

:touchCheck
  set a,[i]
  sub a,[playerX]
  ifn o,0
  jsr nega
  set b,a

  set a,[i+1]
  sub a,[playerY]
  ifn o,0
  jsr nega
  
  add a,b
  ifg a,4
  set pc,touchCheck_exit
  ife [i+6],0
  set pc,die ;no fear -> die
  
  set [i+6],0
  set [i],34
  set [i+1],42
  set [i+2],1
  set [i+3],0  
  set [i+7],10
  set pc,touchCheck_exit   
:die
  set [dead],1  

:touchCheck_exit
  set pc,pop

:nega
  xor a,0xffff
  add a,1
  set pc,pop

:cellAlignedGhostCheck
;i=ghost
  set x,[i]
  set y,[i+1]
  div x,3
  div y,3
  set a,y
  mul a,42
  add a,x
  add a,0x8000 ; a pointing to the current cell
  ifb [randish],1
  jsr ghostTryTurning
  set b,[i+3]
  mul b,42
  add b,[i+2]
  add b,a  ;  b pointed to where ghost is heading
  ife [b],0xb01c ; is it the door
  set pc,ghostCanMove
  ife [b],0xa25d ; is is a powerpill
  set pc,ghostCanMove
  ife [b],0xa008 ; is it a pill
  set pc,ghostCanMove
  ife [b],0x0000 ; is it space  
  set pc,ghostCanMove
  ;way blocked, look for another direction
  add [randish],[ticker]
  set b,[i+2]
  set [i+2],[i+3]
  set [i+3],b

  mul b,42
  add b,[i+2]
  add b,a  ;  b pointed to where ghost is heading
  ife [b],0xa25d ; is is a powerpill
  set pc,ghostCanMove
  ife [b],0xa008 ; is it a pill
  set pc,ghostCanMove
  ife [b],0x0000 ; is it space  
  set pc,ghostCanMove
  set a,0
  sub a,[i+2]
  set [i+2],a
  set a,0
  sub a,[i+3]
  set [i+3],a
:ghostCanMove
  set pc,pop

:thisthat dat 0

:ghostTryTurning
;a = current cell
;i = ghost ptr
  set b,push
  set y,push
  set x,push

  xor [thisthat],1
  set y,[i+2]
  set x,[i+3]
  ifb [thisthat],1
  set pc,ghostTryTurning_checkway 
  set b,0
  sub b,x
  set x,b
  set b,0
  sub b,y
  set y,b
:ghostTryTurning_checkway
  set b,y
  mul b,42
  add b,x
  add b,a
  ife [b],0xb01c
  set pc,ghostTryTurning_success 
  ife [b],0xa25d 
  set pc,ghostTryTurning_success 
  ife [b],0xa008 
  set pc,ghostTryTurning_success 
  ife [b],0x0000
  set pc,ghostTryTurning_success 
  set pc,ghostTryTurning_fail

:ghostTryTurning_success
  set [i+2],x
  set [i+3],y
:ghostTryTurning_fail

  set x,pop
  set y,pop
  set b,pop

  set pc,pop

:ghostdata


:ghost1
:ghost2 
:ghost3 
:ghost4 

:blueghost
:whiteghost

:probeInput
  set a,0
:probeInput_loop;
  set b,[0x9000+a]
  set [0x9000+a],0
  ifn b,0
  add [randish],[ticker]
  ifg b,4
  set b,0  
  ifn b,0 ; note: catches 1,2,3,4 not 0,1,2,3,4  
  set [lastDirectionKey],b
  add a,1
  ifg a,15 
  set pc,pop
  set pc,probeInput_loop

:lastDirectionKey
  dat 0
:previousdirection
  dat 1
:direction
  dat 1

:EndLoop

:WriteCharAt ; X, Y, Z, C, B
  SET PUSH, A
  SET PUSH, B
  SET PUSH, C
  SET A, Y
  SHL A, 5
  ADD A, X
  SHL C, 4
  BOR B, C
  SHL B, 8
  BOR B, Z
  SET [0x8000+A], B
  SET C, POP
  SET B, POP
  SET A, POP
  SET PC, POP

:scroll
    set b, 0x0
    set a,0x81e0
    :scroll_loop
        set [0x8000+b], [0x8020+b]
        set [0x8001+b], [0x8021+b]
        set [0x8002+b], [0x8022+b]
        set [0x8003+b], [0x8023+b]
        set [0x8003+b], [0x8023+b]
        set [0x8004+b], [0x8024+b]
        set [0x8005+b], [0x8025+b]
        set [0x8006+b], [0x8026+b]
        set [0x8007+b], [0x8027+b]
        add b, 8
        ifg b, 0x01df
            set pc, scroll_clear_ll
        set pc, scroll_loop

        :scroll_clear_ll

        set [a], 0
        add a, 1
        ifg a, 0x81ff
            set pc, scroll_end
        set pc, scroll_clear_ll

    :scroll_end
        set [0x1335], 0x81e0
        set pc, pop   

:fillpattern
       set a,0
       set b,0
:oloop
       set c,0
:lineloop
       set z,a
       shl z,8
       bor z,0x55
       set y,a
       shl y,8
       and y,0x0f00
       bor y,a
       shl y,4
       and y,0xff00
       bor y,0x55
       set [0x8000+b],z
       set [0x802b+b],z
       set [0x8001+b],y
       set [0x802a+b],y
       add a,1
       add b,2
       add c,1
       ifg c,15
       set pc,lineout
       set pc,lineloop
:lineout
       ifg a,255
       set pc,fillout
       add b,52
       set pc,oloop
:fillout
       set pc, pop

:wipe
  set z,0;
:wipeLoop
  jsr scroll_g
  ADD Z, 1
  IFN Z,42
  SET PC, wipeLoop
  SET PC,POP

:scroll_g
    set b, 0x0
    set a,0x8516
    :scroll_g_loop
        set [0x8000+b], [0x802a+b]
        set [0x8001+b], [0x802b+b]
        set [0x8002+b], [0x802c+b]
        set [0x8003+b], [0x802d+b]
        set [0x8004+b], [0x802e+b]
        set [0x8005+b], [0x802f+b]
        set [0x8006+b], [0x8030+b]
        set [0x8007+b], [0x8031+b]
        add b, 8
        ifg b, 0x0515
            set pc, scroll_g_clear_ll
        set pc, scroll_g_loop

        :scroll_g_clear_ll

        set [a], 0
        add a, 1
        ifg a, 0x8540
            set pc, scroll_g_end
        set pc, scroll_g_clear_ll

    :scroll_g_end
        set [0x1335], 0x81e0
        set pc, pop   
  
:copyimage
        set c,0x8000
    :copyimage_loop
        set [c],[a]
        add a,1
        add c,1
        ifg a,b
        set pc, copyimage_end
        set pc, copyimage_loop
    :copyimage_end
        set pc, pop

:copy
     ; a = source
     ; b = dest
     ; c = length
     add c,a
:copy_loop
     set [b],[a]
     add a,1
     add b,1
     ifg c,a
     set pc,copy_loop
     set pc,pop

:pillsEaten
:pillsInMap 
:map
:mapend


:death0
:sprites


;left 1-3

;up 1-3
:death1

:death2

:death3

;down 1-3


:death4

:death5

:death6

:death7

:spritesend
";
            string[] program = p.Split(new char[] { '\n', '\r' },StringSplitOptions.RemoveEmptyEntries);


            foreach (ushort opcode in compile.opcodeify(program))
                Debug.Print("{0:x}",opcode);
            //</testing code>
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

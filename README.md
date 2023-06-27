# Animator-As-Assembly
A Unity editor script to compile custom assembly into animator layers

### VCC Install Page
https://www.matthewherber.com/Animator-As-Assembly/

## Instructions
### Symbols:
- `#`: comment

### GPU Commands:
- [ ] `DRAWCHAR`: Draws a character to the screen (syntax: `DRAWCHAR CHAR`)
- [ ] `WRITECHAR`: Writes a character to the screen, shifting the 5 pixel rows to the right 4 pixels (syntax: `WRITECHAR CHAR`)
- [ ] `DRAWSTRING`: Writes a string to the screen (syntax: `DRAWSTRING STRING`)
- [ ] `DRAWREGISTER`: Writes a single digit register to the screen (syntax: `DRAWREGISTER REGISTER`)
- [ ] `DRAWCOMPLETEREGISTER`: Writes a register to the screen (syntax: `DRAWCOMPLETEREGISTER REGISTER`)
- [ ] `SHIFTSCREENRIGHT`: Shifts the screen right by the specified amount (syntax: `SHIFTSCREENRIGHT AMOUNT`)
- [ ] `SHIFTLINERIGHT`: Shifts the line right by the specified amount (syntax: `SHIFTLINERIGHT AMOUNT`)
- [ ] `SHIFTSCREENDOWN`: Shifts the screen down by the specified amount (syntax: `SHIFTSCREENDOWN AMOUNT`)
- [x] `CLEARSCREEN`: Clears the screen.
- [ ] `DRAWCHARCODE`: Draws a character to the screen based on a register, determining the character based on the ASCII standard {DRAWCHARCODE INT_CODE}
- [x] `PIXEL` - Draws a pixel to the screen {PIXEL X Y}

### Opcodes:
#### Flow Control
- [x] `JMP`: Jumps to the specified label
- [x] `LBL`: Creates a label to jump to
- [x] `JIN`: Jumpes to a label if the register is negative (based on 2s complement)
- [ ] `JEN`: Jumps to a label if the register is equal to the specified number (syntax: `JEN REGISTER NUMBER LABEL`)
- [ ] `JNEN`: Jumps to a label if the register is not equal to the specified number (syntax: `JNEN REGISTER NUMBER LABEL`)
- [x] `JEQ`: Compares the contents of the first register to the second register, and if they are equal it jumps to the specified label (syntax: `JEQ REGISTER REGISTER LABEL`)
- [x] `JIG`: Compares the contents of the first register to the second register, and if the first register is greater than the second register it jumps to the specified label (syntax: `JIG REGISTER REGISTER LABEL`)
- [x] `JIGE`: Jumps if greater than or equal
#### Math
- [x] `INC`: Increments the register by 1
- [x] `DEC`: Decrements the register by 1
- [x] `ADD`: Adds the contents of the first register to the second register and stores the result in the third register
- [x] `SUB`: Subtracts the contents of the first register from the second register and stores the result in the third register
- [x] `MUL`: Multiplies the contents of the first register by the contents of the second register and stores the result in the third register
- [x] `DIV`: Divides the contents of the first register by the contents of the second register and stores the result in the third register (rounds up). The remainder is stored in DAC2 if needed.

#### Utility
- [ ] `NOP`: Does nothing for 1 cycle
- [x] `MOV`: Copies the contents of the first register into the second register
- [x] `LD`: Loads the specified number into the register (syntax: `LD REGISTER NUMBER`)
- [ ] `SWAP`: Swaps the contents of the first register with the contents of the second register
- [x] `SHL`: Shifts the contents of the register left
- [x] `SHR`: Shifts the contents of the register right
- [x] `DELAY`: Waits for the specified number of seconds
- [ ] `RANDOM`: generates a random number between a min and max {RANDOM MIN MAX REGISTER_NUMBER}
- [x] `FLIP`: Does a bitwise flip of the register
- [x] `PROFILING`: Allows you to profile sections of your code
  - [x] `START`: Starts profiling, with the given name
  - [x] `STOP`: Stops profiling, with the given name

#### Subroutines
- [x] `JSR`: Jumps to a subroutine (syntax: `JSR SUBROUTINE_NAME`). Stores the return address in the stack
- [x] `RTS`: Returns from a subroutine (syntax: `RTS`). Jumps to the address stored in the stack
- [x] `SBR`: Designates a subroutine (syntax: `SBR SUBROUTINE_NAME`)

#### Stack
- [x] `PUSH`: Pushes a int onto the stack, moving the stack up
- [x] `POP`: Pops a int from the stack

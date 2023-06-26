# Animator-As-Assembly
A Unity editor script to compile custom assembly into animator layers

### VCC Install Page
https://www.matthewherber.com/Animator-As-Assembly/

## Instructions
### Symbols:
- `#`: comment
- `;`: subroutine

### GPU Commands:
- [ ] `DRAWCHAR`: Draws a character to the screen (syntax: `DRAWCHAR CHAR`)
- [ ] `WRITECHAR`: Writes a character to the screen, shifting the 5 pixel rows to the right 4 pixels (syntax: `WRITECHAR CHAR`)
- [ ] `DRAWSTRING`: Writes a string to the screen (syntax: `DRAWSTRING STRING`)
- [ ] `DRAWREGISTER`: Writes a single digit register to the screen (syntax: `DRAWREGISTER REGISTER`)
- [ ] `DRAWCOMPLETEREGISTER`: Writes a register to the screen (syntax: `DRAWCOMPLETEREGISTER REGISTER`)
- [ ] `SHIFTSCREENRIGHT`: Shifts the screen right by the specified amount (syntax: `SHIFTSCREENRIGHT AMOUNT`)
- [ ] `SHIFTLINERIGHT`: Shifts the line right by the specified amount (syntax: `SHIFTLINERIGHT AMOUNT`)
- [ ] `SHIFTSCREENDOWN`: Shifts the screen down by the specified amount (syntax: `SHIFTSCREENDOWN AMOUNT`)
- [ ] `CLEARSCREEN`: Clears the entire VRAM, essentially clearing the screen. ***Does not initiate a redraw***
- [ ] `DRAWCHARCODE`: Draws a character to the screen based on a register, determining the character based on the ASCII standard {DRAWCHARCODE INT_CODE}
- [ ] `PIXEL` - Draws a pixel to the screen {PIXEL X Y}

### Opcodes:
- [x] `INC`: Increments the register by 1
- [x] `DEC`: Decrements the register by 1
- [x] `JMP`: Jumps to the specified label
- [x] `LBL`: Creates a label to jump to
- [ ] `JEN`: Jumps to a label if the register is equal to the specified number (syntax: `JEN REGISTER NUMBER LABEL`)
- [ ] `JNEN`: Jumps to a label if the register is not equal to the specified number (syntax: `JNEN REGISTER NUMBER LABEL`)
- [ ] `NOP`: Does nothing for 1 cycle
- [x] `MOV`: Copies the contents of the first register into the second register
- [x] `LD`: Loads the specified number into the register (syntax: `LD REGISTER NUMBER`)
- [x] `ADD`: Adds the contents of the first register to the second register and stores the result in the third register
- [x] `SUB`: Subtracts the contents of the first register from the second register and stores the result in the third register
- [ ] `JEQ`: Compares the contents of the first register to the second register, and if they are equal it jumps to the specified label (syntax: `JEQ REGISTER REGISTER LABEL`)
- [x] `JIG`: Compares the contents of the first register to the second register, and if the first register is greater than the second register it jumps to the specified label (syntax: `JIG REGISTER REGISTER LABEL`)
- [x] `MUL`: Multiplies the contents of the first register by the contents of the second register and stores the result in the third register
  - Significantly faster than `MUL`
- [x] `DIV`: Divides the contents of the first register by the contents of the second register and stores the result in the third register (rounds up). The remainder is stored in DAC2 if needed. Note that if you use the remainder, the result will be 1 larger than it should be.
- [ ] `SWAP`: Swaps the contents of the first register with the contents of the second register
- [x] `JSR`: Jumps to a subroutine (syntax: `JSR SUBROUTINE_NAME`). Stores the return address in PC
- [x] `RTS`: Returns from a subroutine (syntax: `RTS`). Jumps to the address stored in PC
- [x] `PUSH`: Pushes a int onto the stack, moving the stack up
- [x] `POP`: Pops a int from the stack
- [x] `SHL`: Shifts the contents of the register left once
- [x] `SHR`: Shifts the contents of the register right once
- [ ] `DELAY`: Waits for the specified number of frames
- [ ] `RAND8`: Generates a random number between 0 and 255 (syntax: `RAND8 REGISTER`)
- [ ] `RANDOM`: generates a random number between a min and max {RANDOM MIN MAX REGISTER_NUMBER}

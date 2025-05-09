.global _main
.align 4
_main:
	stp	x29, x30, [sp, #-16]!
	mov	x29, sp
	adr	x0, L.str0
	bl	_printf
	ldp	x29, x30, [sp], #16
	ret
L.str0:	.asciz "Hello, world!\n"

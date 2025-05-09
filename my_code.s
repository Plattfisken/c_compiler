.global _main
.align 4
_my_func:
	stp	x29, x30, [sp, #-16]
	mov	x29, sp
	adr	x0, L.str0
	bl	_printf
	ldp	x29, x30, [sp], #16
	ret
_main:
	stp	x29, x30, [sp, #-16]
	mov	x29, sp
	mov	x0, #1
	adr	x1, L.str1
	mov	x2, #14
	bl	_write
	bl	_my_func
	ldp	x29, x30, [sp], #16
	ret
L.str0:	.asciz "Hi!\n"
L.str1:	.asciz "Hello, World!\n"
